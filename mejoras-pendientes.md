# Oportunidades de mejora

Hallazgos de una revisión completa del backend (agosto 2026). **Ninguno está aplicado**: este documento describe el problema, la evidencia en el código y el arreglo propuesto, para decidir qué se ataca y en qué orden.

Complementa a [arquitectura.md](arquitectura.md) y [roles-y-auth.md](roles-y-auth.md).

## Resumen

| # | Hallazgo | Severidad | Esfuerzo |
|---|---|---|---|
| 1 | Escalada de privilegios en `POST /api/restaurant_user_roles` | 🔴 Crítica | Bajo |
| 2 | Método fantasma `oneisys` rompe el módulo Category | 🔴 Bloqueante | Trivial |
| 3 | El tablero está cerrado a cocina y caja | 🟠 Alta | Bajo |
| 4 | `QueryException` filtra SQL en producción | 🟠 Alta | Trivial |
| 5 | Seguimiento de orden de invitado sin verificar dueño | 🟡 Media | Medio |
| 6 | Sin tests del dominio | 🟡 Media | Medio |
| 7 | Dos representaciones del dinero (carrito vs orden) | 🟡 Media | Medio |
| 8 | Estados y campos muertos (`ON_HOLD`, `PARTIALLY_PAID`, `tax`, `tip`) | 🟢 Baja | — |
| 9 | Restos del proyecto móvil en el repo de la API | 🟢 Baja | Trivial |

---

## 1. 🔴 Escalada de privilegios en `POST /api/restaurant_user_roles`

**Cualquier usuario autenticado puede convertirse en `admin` de cualquier restaurante.** Es explotable hoy, en producción, con un solo request.

### Evidencia

`routes/restaurantUserRole.php:7` — el recurso completo está protegido únicamente por autenticación:

```php
Route::middleware(['auth:sanctum'])->group(function () {
    Route::apiResource('restaurant_user_roles', RestaurantUserRoleController::class);
});
```

No hay `role:admin`. No hay Policy. `CreateRestaurantUserRoleRequest::authorize()` devuelve `true` sin condiciones, y el servicio (`CreateRestaurantUserRoleService::execute`) hace un `create()` directo del array validado — nadie comprueba **quién** está asignando el rol ni **sobre qué restaurante** tiene autoridad.

### Explotación

Cualquiera que se registre en la app (el flujo de cliente QR crea usuarios) obtiene un token Sanctum. Con ese token:

```http
POST /api/restaurant_user_roles
Authorization: Bearer <token de cualquier cliente>

{ "user_id": "<su propio id>", "restaurant_id": "<cualquier restaurante>", "role": "admin" }
```

A partir de ahí es `admin` de ese restaurante: pasa todos los `role:` de `routes/mobile.php`, entra al canal privado `restaurant.{id}.orders` (`routes/channels.php:10` autoriza a cualquier rol activo), ve el tablero completo, aplica descuentos arbitrarios, marca órdenes como pagadas y cierra cuentas. También puede asignarse roles en **todos** los restaurantes de la plataforma, o quitarle el rol al admin legítimo vía `DELETE`.

El impacto no es solo de datos: `POST orders/{id}/discount` y `POST orders/{id}/pay` son dinero real.

### Agravantes

- **Todos los verbos están igual de expuestos**, no solo `store`. `PUT/PATCH` permite reescribir un rol existente y `DELETE` permite revocar el del admin real. `index` lista el mapa de roles de toda la plataforma.
- **El rol no se valida contra el enum.** `CreateRestaurantUserRoleRequest:18` y `UpdateRestaurantUserRoleRequest:20` usan `'role' => 'required|string|max:50'`, mientras que `App\RestaurantUserRole\Domain\Enums\Role` define el conjunto válido (`admin`, `waiter`, `kitchen`, `cashier`, `customer`). Se aceptan valores basura que quedan persistidos y luego no casan con ningún `role:`, dejando usuarios en un limbo silencioso. Además la columna es `varchar(32)` y la regla permite `max:50` → un rol de 33–50 caracteres revienta con `QueryException` (ver hallazgo 4).

### Corrección a la documentación

`docs/roles-y-auth.md` lista como deuda técnica que *"no hay unicidad → puede duplicar (user, restaurant, role)"*. **Eso es incorrecto**: la migración `2025_08_05_022446_create_restaurant_user_roles_table.php:27` sí declara `unique(['user_id','restaurant_id','role'])`.

El problema real es distinto: al no haber validación previa, un intento de duplicado escapa como `QueryException` → **500 con el mensaje de Postgres crudo** en vez de un 422 limpio. Hay que corregir esa línea del documento.

### Arreglo propuesto

> 📌 **Este arreglo se resuelve dentro del diseño del portal**, no como parche aislado: asignar roles a personas de un restaurante es una función del portal web. Ver [plataforma-multitenant.md](plataforma-multitenant.md) (decisión D1). Lo de abajo es el detalle técnico que ese diseño debe cubrir.

1. **Exigir autoridad sobre el restaurante.** Aplicar `role:admin` al grupo, de modo que `EnsureRestaurantRole` resuelva el `restaurant_id` del body y verifique rol activo (los superuser siguen pasando):

   ```php
   Route::middleware(['auth:sanctum', 'role:admin'])->group(function () {
       Route::apiResource('restaurant_user_roles', RestaurantUserRoleController::class);
   });
   ```

   ⚠️ Ojo con `show`/`update`/`destroy`: la ruta lleva el **id del registro de rol**, no un `restaurant_id`, así que `EnsureRestaurantRole::resolveRestaurantId()` no lo encuentra y lanzaría `InvalidArgumentException` (422). Para esos verbos hace falta resolver el restaurante *desde el registro* — una Policy (`RestaurantUserRolePolicy`) encaja mejor que el middleware.

2. **Validar el rol contra el enum** en ambos FormRequests:

   ```php
   'role' => ['required', Rule::in(Role::values())],
   ```

3. **Prohibir la auto-asignación de `admin`**: un admin no debería poder crear el primer admin de un restaurante donde no lo es. El alta del primer admin debe ser un flujo aparte (seeder, comando de artisan, o ligada a la creación del restaurante).

4. **Validar el duplicado antes del insert** (`Rule::unique` sobre la tripleta) para devolver 422 en vez de 500.

5. **Dejar traza**: quién asignó qué rol a quién y cuándo. Hoy no hay auditoría de cambios de privilegios.

### Verificación

Escribir un test de integración que confirme: un usuario con rol `customer` recibe **403** al intentar `POST /api/restaurant_user_roles` asignándose `admin`; un `admin` del restaurante A recibe **403** al asignar roles en el restaurante B.

---

## 2. 🔴 Método fantasma `oneisys` rompe el módulo Category

`app/Category/Domain/Contracts/CategoryRepositoryPort.php:16` declara:

```php
public function oneisys(string $parentId): array;
```

`CategoryRepository` (que implementa el puerto) **no lo implementa**, y no existe ninguna otra referencia en el proyecto. Es un fatal error de PHP al cargar la clase: tumba el módulo Category completo en cuanto el contenedor resuelve el binding.

Está en el working tree **sin commitear**. Parece un tecleo accidental. **Arreglo:** borrar la línea.

---

## 3. 🟠 El tablero está cerrado a cocina y caja

El ciclo de vida está repartido en tres grupos de rutas en `routes/mobile.php`, uno por rol:

| Grupo | Middleware | Contiene |
|---|---|---|
| Mesero | `role:waiter,admin` (línea 68) | **`GET staff/orders`** (línea 71) + confirm / kitchen / served / cancel / items |
| Cocina | `role:kitchen,admin` (línea 88) | `ready` + `items/{id}/status` |
| Caja | `role:cashier,admin` (línea 98) | discount / pay / close / refund |

El tablero quedó **dentro del grupo del mesero**. Un usuario cuyo único rol es `kitchen` o `cashier` recibe **403** al llamar `GET /api/mobile/v1/staff/orders`.

### Por qué importa

Cocina puede llamar `POST orders/{order_id}/ready`, pero **no tiene forma de averiguar ese `{order_id}`**. Los otros endpoints de lectura no le sirven:

- `GET mobile/v1/orders` filtra por `customer_id = Auth::id()` — son las órdenes donde el cocinero figura como *cliente*, no las del restaurante.
- `GET mobile/v1/orders/{order_id}` exige tener ya el ID.

En la práctica, **las consolas de cocina y caja no se pueden construir**. Caja puede cobrar una orden pero no puede listar cuáles están `SERVED` esperando pago.

### El matiz que lo hace difícil de diagnosticar

El canal de WebSocket **sí** los autoriza: `routes/channels.php:10` solo exige *cualquier* rol activo en el restaurante. Cocina y caja reciben los eventos en vivo sin problema.

Pero un WebSocket entrega **deltas, no estado inicial**. Una tablet de cocina recién abierta —o que reconectó tras perder wifi— muestra pantalla vacía hasta que alguien mueva una orden. El síntoma se reporta como *"a veces se ven las órdenes, a veces no"* en vez de como un 403 limpio, y se persigue en el realtime cuando el problema está en la autorización del endpoint de hidratación.

### Arreglo propuesto

Sacar `staff/orders` del grupo del mesero a su propio grupo con los cuatro roles:

```php
Route::prefix('mobile/v1')->middleware(['auth:sanctum', 'role:waiter,kitchen,cashier,admin'])->group(function () {
    Route::get('staff/orders', [MobileOrderController::class, 'board']);
});
```

**Decisión de diseño pendiente:** si cada rol debe recibir un tablero **filtrado por defecto** (cocina → `CONFIRMED/IN_KITCHEN/READY`; caja → `SERVED/PARTIALLY_PAID/PAID`). El endpoint ya acepta `status[]` (validado contra el enum en `ListRestaurantOrdersRequest`), así que el cliente puede filtrar — pero un default por rol en el servidor evita que una consola vea estados que no le competen. `ListRestaurantOrdersService` recibe los `statuses` como parámetro, así que aplicarlo es barato.

---

## 4. 🟠 `QueryException` filtra SQL en producción

`bootstrap/app.php:120` devuelve el mensaje crudo de la base de datos sin mirar `APP_DEBUG`:

```php
$exceptions->render(function (QueryException $e, Request $request) {
    return response()->json(['errors' => [[
        'status' => 500,
        'message' => $e->getMessage(),   // ← SQL + nombres de tabla/columna + valores
        ...
```

El catch-all de `Throwable` (línea 143) **sí** respeta `config('app.debug')`; este handler, declarado antes, se lo salta. Un violación de constraint expone nombres de tablas, columnas, índices y a veces valores de los parámetros.

Se conecta con el hallazgo 1: el duplicado en `restaurant_user_roles` llega por esta vía.

**Arreglo:** aplicar el mismo criterio que el catch-all (`$debug ? $e->getMessage() : 'Error interno del servidor'`), y prevenir en los FormRequests los constraints que sí son esperables (unicidad, longitud) para que salgan como 422.

---

## 5. 🟡 Seguimiento de orden de invitado sin verificar dueño

`MobileOrderController::ownsOrder()` (`app/Mobile/Controllers/MobileOrderController.php:171`) devuelve `true` cuando la orden **no tiene `customer_id`** — es decir, para toda orden creada por un invitado:

```php
if (!$userId || !$order->customer_id) {
    return true;
}
```

La `guest_session` que creó la orden nunca se compara. Cualquiera que conozca (o adivine) un `order_id` puede leer la orden completa: ítems, notas, totales, mesa.

**Los ULID son ordenables por tiempo**, así que a partir de un ID propio se pueden inferir vecinos: no es fuerza bruta ciega. La exposición es de datos de clientes del mismo local en la misma franja horaria.

**Arreglo propuesto:** persistir la `guest_session_id` en la orden al hacer checkout (hoy solo vive en el carrito, que se marca `converted`) y compararla en `ownsOrder()`. Alternativa: un token de seguimiento opaco por orden, devuelto en el checkout y exigido en el `show`.

---

## 6. 🟡 Sin tests del dominio

`tests/` contiene únicamente los `ExampleTest` de fábrica de Laravel.

El módulo `Order` es exactamente el tipo de código que sí conviene testear: una máquina de estados de 11 estados con transiciones declarativas, lock optimista por `version`, cálculo de totales en centavos e idempotencia por clave. Y es **barato de testear**: los servicios dependen de puertos (interfaces), así que se prueban con dobles, sin base de datos.

Candidatos de mayor valor por línea escrita:

- `OrderStateMachine` / `OrderItemStateMachine` — tabla de transiciones válidas e inválidas. Test puro, sin infraestructura.
- `OrderTotals` — que los ítems cancelados no sumen; que `total = subtotal - discount + tax + tip`.
- `Money::toCents` — redondeo en los bordes (`.005`, negativos).
- `ChangeOrderStatusService` — que un `version` desactualizado produzca `OrderConflictException` (409).
- `CreateOrderFromCartService` — que la misma `Idempotency-Key` devuelva la orden existente en vez de duplicar; que un carrito ya `converted` falle.
- Los dos casos de autorización del hallazgo 1 y el 403 del hallazgo 3.

---

## 7. 🟡 Dos representaciones del dinero

El carrito guarda **decimales** (`carts.subtotal_amount` es `decimal(10,2)`, los ítems igual) y la orden guarda **centavos enteros** (`orders.subtotal` es `bigint`). La conversión ocurre en el checkout, en `CreateOrderFromCartService::subtotalInCents()` y `copyItems()`, vía `Money::toCents()`.

Funciona, pero es la costura donde aparecerán descuadres de un centavo. El punto más frágil es el reparto de extras en `AddCartItemService` (~línea 127):

```php
$extrasUnitPrice = $quantity > 0 ? $extrasUnitPrice / $quantity : 0.0;
$extrasUnitPrice = round($extrasUnitPrice, 2);
```

Se divide entre la cantidad y se redondea a 2 decimales; luego `unit_price * quantity` reconstruye el total. Con extras que no dividen exacto (3 unidades, extra de 0.10) el `line_total` no coincide con la suma real de extras, y ese error viaja al `order_item`.

**Arreglo propuesto:** unificar en centavos enteros de punta a punta (el dominio de Order ya lo hace bien), o como mínimo calcular el `line_total` del carrito sumando los extras totales sin pasar por un precio unitario redondeado.

---

## 8. 🟢 Estados y campos muertos

- `OrderStatus::OnHold` y `OrderStatus::PartiallyPaid` están en el enum y en la máquina de estados (con transiciones definidas), pero **ningún endpoint los alcanza**. Solo aparecen como guardas defensivas en `ApplyDiscountService:40` y `CancelOrderItemService:42`.
- `orders.tax` y `orders.tip` se escriben **siempre en 0** (`CreateOrderFromCartService:81-82`, `CreateOrderService:57-58`) y no hay endpoint para modificarlos. `OrderTotals::total()` ya los contempla.

No es un bug, es alcance a medio construir. Vale decidir explícitamente: o se completan (pago parcial, propina, poner en espera) o se retiran para que el modelo no prometa lo que no hace.

---

## 9. 🟢 Restos del proyecto móvil en el repo de la API

`app.json`, `eas.json` y `.expo/` son configuración de Expo/EAS (incluyen `bundleIdentifier` y `projectId` de EAS) dentro del repositorio del backend Laravel. Pertenecen a `resflow-mobile`. Conviene retirarlos o, si se usan para algo, documentar por qué están aquí.
