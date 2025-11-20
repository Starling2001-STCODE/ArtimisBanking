# ArtemisBanking – Módulos y Requerimientos Funcionales

Este documento resume la arquitectura y los módulos funcionales de **ArtemisBanking**, con sus requerimientos identificados por código.  
Los desarrolladores pueden usar estos puntos como **checklist de implementación y validación**.

---

## 1. Arquitectura General

### 1.1. Proyectos / Capas

- **Core / Domain**
  - `ArtemisBanking.Core.Domain`
    - Entities/
      - User (admin, cajero, cliente, comercio)
      - Loan (+ LoanInstallment)
      - CreditCard (+ CreditCardConsumption)
      - SavingsAccount (+ SavingsAccountTransaction)
      - Beneficiary
      - Merchant (Comercio)
      - Payment / PaymentTransaction (para Hermes Pay)
    - Enums/
      - UserRole (Admin, Cashier, Client, Merchant)
      - AccountType (Principal, Secundaria)
      - AccountStatus (Activa, Cancelada)
      - LoanStatus (Activo, Completado, Moroso)
      - CreditCardStatus (Activa, Cancelada)
      - TransactionType (Débito, Crédito)
      - TransactionOrigin (Cajero, Transferencia, Préstamo, Tarjeta, etc.)
      - TransactionStatus (Aprobada, Rechazada)

- **Application (Casos de uso)**
  - `ArtemisBanking.Application`
    - Common/ (PagedResult, Result<T>, etc.)
    - Mapping/ (Perfiles AutoMapper)
    - Identity/ (IAuthService, IAccountService)
    - Users/ (IUserService, DTOs)
    - Loans/ (ILoanService, DTOs)
    - CreditCards/ (ICreditCardService, DTOs)
    - SavingsAccounts/ (ISavingsAccountService, DTOs)
    - Beneficiaries/ (IBeneficiaryService, DTOs)
    - Transactions/ (ITransactionService, DTOs)
    - Merchants/ (IMerchantService, DTOs)
    - Payments/ (IPaymentProcessorService, DTOs)
    - Notifications/ (INotificationService – emails de aprobación, cambios de límite, etc.)

- **Infrastructure**
  - `ArtemisBanking.Infrastructure.Persistence`
    - AppDbContext (EF Core)
    - Configurations/ de entidades
    - Repositories/ (UserRepository, LoanRepository, etc.)
    - DependencyInjection/PersistenceServicesRegistration.cs
  - `ArtemisBanking.Infrastructure.Identity`
    - ASP.NET Identity (roles, JWT, seeding admin/cajero/cliente/comercio)
  - `ArtemisBanking.Infrastructure.Shared`
    - Email/ (envío de correos: activación, préstamos, cambio tasa, cambio límite, notificaciones, etc.)
    - Jobs/ (Hangfire/Quartz para cuotas atrasadas, tareas diarias)
    - Payments/ (cliente para Hermes Pay si aplica)

- **Web y API**
  - `ArtemisBanking.Web` (ASP.NET Core MVC)
    - UI para Administrador, Cajero y Cliente (Dashboards, gestión, transacciones).
  - `ArtemisBanking.WebApi` (ASP.NET Core Web API)
    - Endpoints de:
      - Login/Account
      - Gestión de Usuarios
      - Gestión de Préstamos
      - Gestión de Tarjetas
      - Gestión de Cuentas de Ahorro
      - Gestión de Comercios
      - Hermes Pay (Procesador de pago)

**Dependencias:**

- Web / WebApi → Infrastructure → Application → Domain

---

## 2. Módulo de Autenticación & Account (Login / Reset / Confirmación)

### 2.1. Requerimientos

- [ ] **[AUTH-01]** Pantalla de login (Web):
  - Usuario + contraseña.
  - Si ya está logueado y entra a `/login`, redirigir al Home según rol.

- [ ] **[AUTH-02]** Validaciones de login:
  - Usuario/clave incorrectos → mensaje de error.
  - Usuario inactivo → mensaje indicando que debe activar la cuenta vía correo.

- [ ] **[AUTH-03]** Reset de contraseña:
  - Desde login, botón “Restablecer contraseña”.
  - Formulario para usuario → desactivar usuario + generar token + enviar correo con enlace (token en querystring).
  - Pantalla de nueva contraseña (password + confirmación):
    - Cambiar contraseña.
    - Volver a activar usuario.
    - Redirigir a login.

- [ ] **[AUTH-04]** Seguridad por roles:
  - Usuarios no autenticados no pueden ver módulos de Admin/Cliente/Cajero.
  - Cada rol solo accede a sus módulos.
  - Accesos indebidos → pantalla “Acceso denegado” con link al Home del rol.

- [ ] **[AUTH-05]** Seeding inicial:
  - Crear usuarios por defecto: admin, cajero, cliente (y para API: admin + comercio).

- [ ] **[AUTH-06]** Web API Login:
  - Endpoint `POST /account/login` retorna JWT.
  - Resto de endpoints del módulo Account requieren `Authorization: Bearer {token}`.

### 2.2. Arquitectura asociada

- Domain: User, UserRole, reglas de activación.
- Application: IAuthService, IAccountService (Login, ConfirmAccount, GetResetToken, ResetPassword).
- Infrastructure.Identity: Identity + JWT + seeding.
- Web/WebApi: AccountController (MVC + API).

---

## 3. Módulo de Gestión de Usuarios (Admin)

### 3.1. Requerimientos

- [ ] **[USR-01]** Listado paginado (20 por página) de usuarios (excepto comercios), ordenado desc por fecha de creación.

- [ ] **[USR-02]** Datos mostrados por usuario:
  - Usuario, cédula, nombre y apellido, email.
  - Rol (admin, cajero, cliente).
  - Estado (activo/inactivo).

- [ ] **[USR-03]** Crear usuario:
  - Formulario: Nombre, Apellido, Cédula, Correo, Usuario, Password, Confirmación, Tipo de usuario.
  - Si tipo = Cliente → campo “Monto inicial”.
  - Validar unicidad de usuario/email (global).

- [ ] **[USR-04]** Creación de cliente:
  - Asignar automáticamente cuenta de ahorro principal:
    - Balance inicial = “Monto inicial”.
    - Nº de 9 dígitos único (no repetido en cuentas ni préstamos).
    - Marcar como principal.
  - Usuario se crea inactivo.
  - Enviar correo con enlace de activación.

- [ ] **[USR-05]** Filtro por rol:
  - Select para ver solo admin / cajeros / clientes.

- [ ] **[USR-06]** Activar/Inactivar usuario:
  - Botón de cambio de estado con confirmación.
  - No se puede desactivar al admin actualmente autenticado.

- [ ] **[USR-07]** Edición de usuario:
  - Admin/cajeros: editar datos personales + usuario + contraseña (con confirmación). Rol no editable.
  - Clientes: igual, con campo “Monto adicional” → suma al saldo de la cuenta principal.

### 3.2. Arquitectura asociada

- Domain: User, SavingsAccount.
- Application: IUserService (crear, editar, activar/inactivar, asignar cuenta principal).
- Persistence: UserRepository, SavingsAccountRepository.
- Web: UsersController + vistas CRUD.
- API: Endpoints de gestión de usuarios.

---

## 4. Módulo de Gestión de Préstamos

### 4.1. Requerimientos

- [ ] **[LOAN-01]** Listado de préstamos activos (20 por página, desc por fecha).

- [ ] **[LOAN-02]** Datos por préstamo:
  - Nº préstamo, cliente, capital total, cuotas totales, cuotas pagadas,
  - monto pendiente, tasa anual, plazo (meses), indicador al día / en mora.

- [ ] **[LOAN-03]** Búsqueda y filtros:
  - Buscar por cédula.
  - Ver préstamos activos + completados (activos primero).
  - Filtro por estado (activos/completados).

- [ ] **[LOAN-04]** Asignar préstamo (flujo por pasos):
  - Paso 1: listado de clientes activos sin préstamo activo + buscador por cédula.
  - Paso 2: selección de cliente (radio) + “Siguiente”.
  - Paso 3: formulario:
    - Plazo (múltiplos de 6 meses: 6–60).
    - Monto a prestar.
    - Tasa anual (>0).

- [ ] **[LOAN-05]** Validaciones de riesgo:
  - Comparar deuda actual vs. deuda con nuevo préstamo vs. deuda promedio del sistema.
  - Si supera promedio → pantalla de advertencia (2 mensajes posibles) + confirmación.

- [ ] **[LOAN-06]** Registro del préstamo:
  - Guardar cliente, monto, plazo, tasa anual, admin que asigna, estado activo.
  - Generar tabla de amortización (sistema francés, cuota fija).
  - Primera cuota: día del mes siguiente a la fecha de creación.

- [ ] **[LOAN-07]** Job de atraso:
  - Tarea diaria que marca cuotas como atrasadas si la fecha pasó y no están pagadas.

- [ ] **[LOAN-08]** Impacto en cuentas:
  - El monto del préstamo aprobado se suma al balance de la cuenta de ahorro principal del cliente.

- [ ] **[LOAN-09]** Notificaciones:
  - Enviar correo al cliente al aprobar préstamo (monto, plazo, tasa, cuota).

- [ ] **[LOAN-10]** Ver detalles del préstamo:
  - Pantalla con tabla de amortización (fechas, valor cuota, estado, atraso).

- [ ] **[LOAN-11]** Editar tasa:
  - Form con campo de tasa anual.
  - Recalcular cuotas futuras (no las vencidas/pagadas).
  - Enviar correo al cliente con nueva tasa y nueva cuota.

### 4.2. Arquitectura asociada

- Domain: Loan, LoanInstallment (sistema francés, reglas de alto riesgo).
- Application: ILoanService (List, AssignLoan, RecalculateRisk, GenerateSchedule, UpdateRate).
- Persistence: repos Loans/LoanInstallments.
- Shared: jobs + INotificationService.
- Web: LoansController + vistas.
- API: endpoints de gestión de préstamos.

---

## 5. Módulo de Gestión de Tarjetas de Crédito

### 5.1. Requerimientos

- [ ] **[CARD-01]** Listado de tarjetas activas (20 por página, desc).

- [ ] **[CARD-02]** Datos por tarjeta:
  - Nº tarjeta, cliente, límite, fecha expiración (MM/AA), deuda actual.

- [ ] **[CARD-03]** Búsqueda y filtros:
  - Buscar por cédula → tarjetas activas + canceladas (activas primero).
  - Filtro por estado (activa/cancelada).

- [ ] **[CARD-04]** Asignar tarjeta:
  - Listado de clientes activos + deuda promedio del sistema + buscador por cédula.
  - Selección de cliente (radio) + “Siguiente”.
  - Form con límite de crédito.

- [ ] **[CARD-05]** Registro:
  - Guardar cliente, límite, fecha de expiración (+3 años desde hoy).
  - Nº de tarjeta: 16 dígitos único.
  - CVC: 3 dígitos cifrados con SHA-256.
  - Guardar admin asignador.

- [ ] **[CARD-06]** Detalle de tarjeta:
  - Listado de consumos (desc):
    - Fecha, monto, comercio o “AVANCE”, estado (APROBADO/RECHAZADO).

- [ ] **[CARD-07]** Editar límite:
  - Form con nuevo límite.
  - Validar que no sea menor a la deuda actual.
  - Si cambia, enviar correo con nuevo límite y 4 últimos dígitos de la tarjeta.

- [ ] **[CARD-08]** Cancelar tarjeta:
  - Confirmación con texto incluyendo 4 últimos dígitos.
  - Validar que deuda actual sea 0.
  - Si deuda > 0 → bloquear cancelación con mensaje.
  - Si deuda = 0 → marcar tarjeta como cancelada, bloquear consumos/pagos futuros y ocultarla del listado de productos del cliente.

### 5.2. Arquitectura asociada

- Domain: CreditCard, CreditCardConsumption (reglas de límite, cancelación, estados).
- Application: ICreditCardService (asignar, listar, cambiar límite, cancelar, consumos).
- Persistence: repos + constraints para nº tarjeta único.
- Shared: cifrado SHA-256 del CVC + notificaciones.
- Web: CreditCardsController.
- API: módulo `/api/credit-cards`.

---

## 6. Módulo de Cuentas de Ahorro

### 6.1. Requerimientos

- [ ] **[SAV-01]** Listado de cuentas de ahorro activas (principales y secundarias), 20 por página, desc.

- [ ] **[SAV-02]** Datos por cuenta:
  - Nº cuenta, nombre cliente, balance, tipo (principal/secundaria).

- [ ] **[SAV-03]** Búsqueda y filtros:
  - Buscar por cédula → todas las cuentas (activas + canceladas, activas primero).
  - Filtro por estado (activa/cancelada) y tipo (principal/secundaria).

- [ ] **[SAV-04]** Asignar cuenta secundaria:
  - Listado de clientes activos + buscador por cédula.
  - Form con balance inicial (puede ser 0).
  - Generar nº de cuenta de 9 dígitos único (no repetido en préstamos ni otras cuentas).
  - Marcar como secundaria, activa, guardar admin que asigna.

- [ ] **[SAV-05]** Detalle de cuenta:
  - Listado de transacciones de esa cuenta:
    - Fecha, monto, tipo (DÉBITO/CRÉDITO), beneficiario, origen, estado (APROBADA/RECHAZADA).

- [ ] **[SAV-06]** Cancelar cuenta secundaria:
  - Confirmación con nº de cuenta.
  - Si tiene balance > 0 → transferir automáticamente a la cuenta principal del cliente antes de cancelar.
  - Marcar cuenta como cancelada y ocultarla del listado de productos.

- [ ] **[SAV-07]** API Savings Account:
  - Endpoints protegidos con JWT para:
    - Listar cuentas.
    - Crear cuentas secundarias.
    - Ver transacciones por cuenta.

### 6.2. Arquitectura asociada

- Domain: SavingsAccount, SavingsAccountTransaction.
- Application: ISavingsAccountService (list, assignSecondary, cancel, getTransactions).
- Persistence: repos + unicidad de nº cuenta.
- Web: SavingsAccountsController.
- API: `/api/savings-accounts`.

---

## 7. Módulo de Funcionalidades Cliente

### 7.1. Requerimientos (resumen)

- [ ] **[CLI-01]** Home Cliente:
  - Menú: Home, Transacciones (Express, Tarjeta de crédito, Préstamo, Beneficiarios), Beneficiario, Avance de efectivo, Transferencia, Cerrar sesión.
  - Listado de productos financieros activos:
    - Cuentas de ahorro
    - Préstamos
    - Tarjetas de crédito
  - Mostrar secciones solo si hay datos.

- [ ] **[CLI-02]** Beneficiarios:
  - CRUD de beneficiarios de cuentas de terceros, asociados al cliente.

- [ ] **[CLI-03]** Transacciones:
  - Transferencias express, pagos a tarjeta, pagos a préstamo, transacciones usando beneficiarios.
  - Validación de saldo.
  - Creación de registros de transacción y actualización de saldos.

- [ ] **[CLI-04]** Avances de efectivo:
  - Crear transacción desde tarjeta de crédito hacia cuenta de ahorro (marcar consumo como “AVANCE”).

- [ ] **[CLI-05]** Transferencias entre cuentas:
  - Transferencias entre cuentas de ahorro (propias o de terceros).
  - Envío de correos a emisor y receptor.

### 7.2. Arquitectura asociada

- Domain: Beneficiary, *Transaction* entities.
- Application: IBeneficiaryService, ITransactionService.
- Shared: INotificationService (emails de transferencias).
- Web: ClientHomeController, TransactionsController, BeneficiariesController.

---

## 8. Módulo de Funcionalidades Cajero

### 8.1. Requerimientos (resumen)

- [ ] **[CASH-01]** Home Cajero:
  - Menú con opciones: Depósito, Retiro, Pago a tarjeta, Pago a préstamo, Transacciones a terceros, etc.

- [ ] **[CASH-02]** Depósitos:
  - Depósitos a cuentas de ahorro.
  - Ajustar balance y registrar transacción (origen “depósito cajero”).

- [ ] **[CASH-03]** Retiros:
  - Retiros desde cuenta de ahorro.
  - Validar saldo.
  - Registrar DÉBITO con origen “retiro cajero”.

- [ ] **[CASH-04]** Pagos a tarjeta y préstamo:
  - Selección de producto + monto de pago.
  - Actualizar deuda de tarjeta/préstamo.
  - Registrar transacciones correspondientes.

- [ ] **[CASH-05]** Transacciones a cuentas de terceros:
  - Flujo similar a transferencias cliente, pero desde interfaz de cajero.
  - Envío de correos similares al módulo cliente.

### 8.2. Arquitectura asociada

- Domain: reutiliza SavingsAccountTransaction, LoanInstallmentPayment, CreditCardConsumption (para pagos).
- Application: ITransactionService o ICashierService (casos de uso específicos).
- Web: CashierController + vistas.

---

## 9. Módulo de Comercios & Procesador de Pago (Hermes Pay, API)

### 9.1. Gestión de Comercios (API)

- [ ] **[MER-01]** Gestión de Comercios:
  - Endpoints bajo JWT.
  - Solo rol Administrador puede consumirlos.
  - Crear / editar / consultar comercios según detalle del diseño.

### 9.2. Procesador de Pago Hermes Pay

- [ ] **[PAY-01]** Procesador de pagos:
  - API para que comercios registren pagos con tarjeta/crédito/cuenta.
  - Validar fondos.
  - Crear consumos en tarjetas.
  - Registrar transacciones en cuentas.
  - Responder con estados APROBADO/RECHAZADO según reglas.

### 9.3. Seguridad API

- [ ] **[SEC-API-01]** Seguridad de la API:
  - Uso de JWT.
  - Validación de rol (Admin vs Merchant).
  - Accesos indebidos → 403 Forbidden.

### 9.4. Arquitectura asociada

- Domain: Merchant, Payment, PaymentStatus.
- Application: IMerchantService, IPaymentProcessorService.
- Infrastructure.Shared: cliente Hermes Pay (si aplica).
- WebApi: controladores `/api/merchants/*`, `/api/payments/*` con `[Authorize(Roles = "...")]`.

---

## 10. Notas para QA / Validación

- Cada requerimiento marcado como `[ ]` debe validarse tanto:
  - A nivel de **lógica de negocio** (casos de uso en Application).
  - A nivel de **UI/API** (comportamiento observable).
- Recomendado:
  - Crear una matriz `Requerimiento → Test Cases`.
  - Marcar estado: **Pendiente / En progreso / Completado / Bloqueado**.
