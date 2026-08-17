# Plataforma multi-tenant: portales y modelo de autorización

Documento de **diseño previo**, no de implementación. Recoge las decisiones que conviene tomar antes de escribir el portal web, porque son las que salen baratas ahora y caras después de tener datos en producción.

Relacionado: [mejoras-pendientes.md](mejoras-pendientes.md) · [roles-y-auth.md](roles-y-auth.md) · [arquitectura.md](arquitectura.md)

---

## 1. La visión

Tres consolas sobre la misma API:

| Consola | Quién | Para qué |
|---|---|---|
| **Portal de plataforma** | Superuser (nosotros) | Crear restaurantes, configurarlos, asignar el admin inicial, soporte |
| **Portal del restaurante** | Admin del restaurante | Configurar su local a su preferencia, gestionar personal, ver reportes |
| **App móvil** | Personal (mesero / cocina / caja) + admin con funciones limitadas | Operación del día a día |

El backend actual cubre bien la tercera. Las dos primeras no existen todavía, y es ahí donde se define el modelo de autorización de toda la plataforma.

---

## 2. Estado actual del modelo (verificado en código)

Lo que hay hoy, para no diseñar sobre supuestos:

**Identidad y nivel de plataforma**
- `users.is_superuser` — booleano, default `false` (`create_users_table.php:19`).
- `users.is_client` — booleano, default `true`. Lo usa `ResolveUserRoleService` para respaldar el rol `customer`.
- Bypass de superuser en `EnsureRestaurantRole:32` y en `routes/channels.php:11`: si es superuser, pasa todo.

**Rol por restaurante**
- Tabla `restaurant_user_roles` = `user_id` + `restaurant_id` + `role` + `is_active`, con `unique(user_id, restaurant_id, role)`.
- Enum `Role`: `admin`, `waiter`, `kitchen`, `cashier`, `customer`.
- `ResolveUserRoleService` calcula el rol efectivo con prioridad `admin > cashier > kitchen > waiter > customer`.
- La autorización es **por ruta**, vía `role:` (`EnsureRestaurantRole`), que resuelve el `restaurant_id` de la ruta o del body.

**Dueño del restaurante**
- `restaurants.owner_id` — ULID nullable con FK a `users` (`create_restaurants_table.php:22`).
- Está completamente cableado en el CRUD: modelo, entidad, repositorio, FormRequests y Resource.
- ⚠️ **Pero no otorga ninguna autoridad.** `EnsureRestaurantRole` nunca lo consulta. Un dueño sin fila en `restaurant_user_roles` no puede entrar a su propio restaurante. Hoy es metadato descriptivo, no un permiso.

**Tokens**
- `AuthRepository.php:22` y `:45` emiten todos los tokens igual:
  ```php
  $authenticatedUser->createToken('token', ['*'], now()->addHours(15))
  ```
  Todas las abilities de Sanctum, 15 horas. **Las abilities no se usan en ningún lado.**

**Lo que no existe**
- Concepto de grupo, cadena u organización (varios locales bajo un mismo dueño).
- Permisos configurables por restaurante.
- Auditoría de cambios de privilegios.

---

## 3. Decisiones

### D1 — El portal *es* el arreglo de autorización, no un trabajo posterior

**Estado: decidido.**

`POST /api/restaurant_user_roles` está hoy abierto a cualquier usuario autenticado (hallazgo 1 de [mejoras-pendientes.md](mejoras-pendientes.md)). Y "asignar roles a personas de un restaurante" es exactamente una de las funciones del portal.

No tiene sentido parchear el endpoint ahora y rediseñarlo en dos meses. **Se diseña el modelo de autorización del portal una vez, y el agujero se cierra como consecuencia.**

Esto reordena la prioridad: el hallazgo 1 deja de ser deuda pendiente y pasa a ser el primer entregable de diseño del portal.

---

### D2 — Grupos / cadenas: decidir ahora, implementar cuando toque

**Recomendación: crear el nivel de agrupación antes de que haya datos en producción.**

Hoy dos sucursales del mismo dueño son dos restaurantes sin relación. Las consecuencias:

- El dueño necesita una fila de rol por cada local y entra por separado a cada uno.
- **No hay reportes consolidados** — que es precisamente lo que compra un dueño de cadena, y el cliente que más paga.
- No hay nada compartido entre locales (catálogo, personal, configuración).

**Costo de hacerlo ahora:** una migración (`restaurant_groups` + `restaurants.group_id`) y tocar el resolvedor de roles.
**Costo de hacerlo después:** un refactor que atraviesa reportes, autorización y todo lo que hoy indexa por `restaurant_id`.

Sugerencia práctica: crear la tabla y la columna ahora, y que el portal V1 solo maneje grupos de un restaurante. La estructura queda lista sin construir la funcionalidad completa.

**Pendiente de decidir:** si el rol vive a nivel de grupo (`group_user_roles`, "admin de toda la cadena") o sigue siendo por restaurante con un rol adicional de grupo para el dueño. Lo segundo es menos disruptivo.

---

### D3 — Qué hacer con `owner_id`

**Pendiente de decidir.** Hay dos caminos y conviene no dejarlo ambiguo:

1. **`owner_id` otorga autoridad** — `EnsureRestaurantRole` lo consulta y el dueño pasa como si fuera `admin`. Simple, pero mezcla dos conceptos (propiedad comercial vs permiso operativo) y hace la autorización menos uniforme.
2. **`owner_id` es solo metadato** (situación actual de facto) y al crear el restaurante el portal **crea automáticamente** la fila `restaurant_user_roles` con rol `admin` para el dueño. La autorización sigue teniendo una sola fuente de verdad.

**Recomendación: la opción 2.** Un solo camino de autorización es más fácil de razonar y de auditar. Pero hay que implementar ese "crear el rol automáticamente", porque hoy no ocurre y por eso el campo quedó a medias.

Esto se conecta con el punto siguiente de D1: el alta del **primer admin** de un restaurante no puede exigir ser admin de ese restaurante (problema del huevo y la gallina). Ese flujo pertenece al portal de plataforma, con autoridad de superuser.

---

### D4 — Roles fijos vs permisos configurables

**Recomendación firme: NO construir un motor RBAC. Mantener los roles fijos.**

La frase "que el admin configure a su preferencia" esconde una bifurcación cara:

- **Roles fijos** (lo que hay): el rol determina las capacidades, hardcodeado en las rutas (`role:waiter,admin`). Claro, rápido, suficiente para la enorme mayoría de casos.
- **Permisos configurables:** tabla de permisos, mapeo rol→permiso por restaurante, middleware que consulta la base en vez de comparar contra un enum. Flexible y significativamente más caro — en construcción, en pruebas y en soporte.

Construir RBAC completo en este punto del proyecto es el error clásico: se siente como "arquitectura preparada para el futuro" y en la práctica consume meses para una flexibilidad que casi ningún cliente usa.

**Alternativa propuesta:** una tabla de **configuración por restaurante** (`restaurant_settings`) para las pocas perillas que los dueños sí piden:

- descuento máximo que puede aplicar un cajero,
- si un mesero puede cancelar una orden ya confirmada,
- propina sugerida / activada,
- impuesto aplicable.

Cubre la necesidad real sin volverse un sistema de permisos genérico. Si algún día un cliente grande exige permisos finos, se agrega encima — pero con demanda comprobada, no por anticipación.

---

### D5 — Alcance del token: móvil vs web

**Recomendación: usar abilities de Sanctum. Ya están instaladas y hoy se desperdician.**

El plan contempla que el admin tenga en el móvil un subconjunto de lo que puede hacer en el portal web. Hoy eso solo se puede lograr **escondiendo botones en la UI**, lo cual no es seguridad: el token del móvil puede llamar cualquier endpoint.

Como todos los tokens salen con `['*']`, la distinción no existe a nivel de servidor.

Propuesta:

- El login del móvil emite un token con un conjunto reducido de abilities.
- El login del portal web emite el conjunto completo.
- Un middleware verifica con `tokenCan()` además del rol.

Resultado: mismo usuario, mismo rol, distinta capacidad según por dónde entró — validado en el servidor. Es cambiar dónde y cómo se emite el token, no rehacer la autenticación.

**Pendiente de decidir:** la lista concreta de abilities y su granularidad (por módulo, por acción, o solo `mobile` / `web` / `platform`). Empezar grueso; afinar después es fácil, lo contrario no.

---

### D6 — `is_superuser` como booleano

**Pendiente, no urgente.** Un booleano todo-o-nada sobre los datos de todos los clientes tiene tres límites que van a aparecer:

- No permite delegar (un empleado de soporte que lee pero no escribe).
- No deja traza de quién hizo qué a nivel plataforma.
- No distingue entre operar la plataforma y operar un restaurante concreto.

No bloquea el portal V1. Anotarlo para cuando el equipo crezca.

---

## 4. Reportes: la base ya está construida

Vale reconocerlo porque cambia la estimación: la parte difícil de los reportes **ya está hecha**.

`order_status_history` guarda cada transición con autor y momento, y `orders` tiene timestamps de hito (`placed_at`, `confirmed_at`, `served_at`, `paid_at`, `closed_at`, `cancelled_at`). Con eso se calculan tiempo de preparación, tiempo de servicio y cuellos de botella por franja horaria — las métricas que un dueño realmente mira. Muchos POS no las tienen porque solo guardaron el estado final.

Dos recomendaciones para cuando se construyan:

- **No consultar `orders` en vivo** para los dashboards.
- **Tampoco montar un data warehouse.** Una tabla de agregados recalculada por la noche alcanza durante años.

Nota: los reportes consolidados por cadena dependen de D2. Es otra razón para decidir el modelo de grupos antes.

---

## 5. Secuencia sugerida

1. **Decidir D2, D3 y D5** (grupos, autoridad del dueño, alcance del token). Son decisiones de modelo; el resto se construye encima.
2. **Diseñar la autorización del portal** — cierra el hallazgo 1 de seguridad como efecto (D1).
3. **Migración de grupos**, aunque V1 solo maneje grupos de un restaurante.
4. **Portal de plataforma**: crear restaurante → crear dueño → asignarle rol admin automáticamente.
5. **Portal del restaurante**: gestión de personal + `restaurant_settings` (D4).
6. **Reportes** sobre la base ya existente.

En paralelo y sin depender de lo anterior: los hallazgos 2, 3 y 4 de [mejoras-pendientes.md](mejoras-pendientes.md) (el `oneisys`, el tablero de cocina/caja y el `QueryException`), y los tests del dominio.

---

## 6. Preguntas abiertas

- ¿El rol vive a nivel de grupo o de restaurante? (D2)
- ¿`owner_id` autoriza o solo describe? (D3)
- ¿Qué granularidad tienen las abilities del token? (D5)
- ¿Qué perillas concretas entran en `restaurant_settings`? Conviene definirlas con un cliente piloto real, no en abstracto.
- ¿Cómo se da de alta el primer restaurante y el primer superuser? (seeder, comando de artisan, o alta manual)
