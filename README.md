# ArtemisBanking

Plataforma bancaria educativa construida con **ASP.NET Core**, **Clean/Onion Architecture** y **SQLite**.  
Incluye módulos de:

- Autenticación y gestión de usuarios
# ArtemisBanking

Proyecto backend y frontend minimal para un sistema bancario (API + Web MVC) organizado en capas: `Core.Domain`, `Application`, `Infrastructure.*`, `WebApi` y `Web`.

## Requisitos

- .NET SDK 9.0 (net9.0) — descarga desde https://dotnet.microsoft.com/en-us/download
- Git
- (Opcional) Postman para importar la colección de ejemplos

## Preparar el entorno (Windows - PowerShell)

1. Abrir PowerShell en la raíz del repo:

```powershell
cd 'C:\Users\chich\source\ArtemisBanking\ArtemisBanking'
```

2. Restaurar paquetes y compilar:

```powershell
dotnet restore
dotnet build --configuration Debug
```

3. (Opcional) Instalar la herramienta `dotnet-ef` si vas a ejecutar migraciones:

```powershell
dotnet tool install --global dotnet-ef
```

## Base de datos

Este proyecto usa SQLite por defecto en la carpeta del proyecto `WebApi` (`src/ArtemisBanking.WebApi`). El archivo de base de datos por defecto es:

```
src/ArtemisBanking.WebApi/artemisbanking.db
```

Si necesitas aplicar migraciones manualmente:

```powershell
dotnet ef database update --project src\ArtemisBanking.Infrastructure.Persistence\ArtemisBanking.Infrastructure.Persistence.csproj
```

Nota: `WebApi` ejecuta `db.Database.EnsureCreated()` en arranque y también ejecuta el `IdentitySeeder` para crear usuarios/roles por defecto.

## Ejecutar la API y la Web

- Ejecutar la API (WebApi):

```powershell
dotnet run --project src\ArtemisBanking.WebApi\ArtemisBanking.WebApi.csproj
```

- Ejecutar la Web (MVC):

```powershell
dotnet run --project src\ArtemisBanking.Web\ArtemisBanking.Web.csproj
```

Los endpoints de la API estarán disponibles en la URL que `dotnet run` muestre (por ejemplo `https://localhost:5001`).

## Colección Postman

He añadido un placeholder para la colección Postman en:

```
postman/ArtimisBanking.postman_collection.json
```

Importa esa colección en Postman para ejemplos y pruebas. (La colección actualmente es un placeholder vacío; puedes ampliarla con requests para `CreditCards`, `Loans`, `Users`, etc.)

## Flujo de trabajo recomendado

- Crea una rama para la funcionalidad: `git checkout -b feature/mi-cambio`
- Implementa y añade tests (no hay proyecto de tests por defecto en este repo).
- Ejecuta `dotnet build` y prueba localmente.
- Haz `git add` / `git commit` y sube la rama a GitHub.
- Abre un Pull Request en GitHub para revisión.

## Licencia

Añade un archivo `LICENSE` si quieres publicar con una licencia específica (por ejemplo MIT).

---

Si quieres, puedo:
- Añadir una colección Postman con endpoints de ejemplo (crear requests para `CreditCards`, `Users`, `Auth`).
- Añadir un `LICENSE` (MIT) y hacer commit + push.
- Crear workflows de `GitHub Actions` para build/CI.
