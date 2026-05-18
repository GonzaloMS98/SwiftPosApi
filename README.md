# SwiftPosApi

Backend oficial de SwiftPOS. Este proyecto es independiente de `SwiftPosUi` y contiene la API del MVP.

## Stack

- .NET 8
- ASP.NET Core Web API
- PostgreSQL con Entity Framework Core
- JWT para autenticacion

## Estado actual

Etapa implementada:

- Etapa 0: workspace separado y limpieza de backend dentro de UI.
- Etapa 1: solucion .NET, proyectos base, Swagger/OpenAPI, endpoint health y proyecto de pruebas.
- Etapa 2 parcial: EF Core/PostgreSQL, entidades base multi-tenant, seed demo y migracion inicial.
- Etapa 3 inicial: auth JWT, contexto autenticado y endpoints protegidos de tenant/store/users.
- Etapa 4 parcial: endpoints iniciales de catalogo para categorias y productos.
- Etapa 5 parcial: ordenes POS, items, checkout, ventas y pagos manuales.

Pendiente inmediato:

- Levantar PostgreSQL local y aplicar migracion inicial.
- Aplicar migracion transaccional POS.
- Probar login JWT, catalogo y checkout POS contra la base migrada.

Credenciales seed de desarrollo:

- email: `admin@swiftpos.local`
- password: `SwiftposDemo123!`

## Estructura

- `src/SwiftPos.Api`: API HTTP.
- `src/SwiftPos.Application`: casos de uso y contratos de aplicacion.
- `src/SwiftPos.Domain`: entidades, value objects y reglas de dominio.
- `src/SwiftPos.Infrastructure`: persistencia, integraciones y servicios externos.
- `tests/SwiftPos.Tests`: pruebas automatizadas.

## Herramientas locales

Restaurar herramientas:

```bash
dotnet tool restore
```

Levantar PostgreSQL local:

```bash
docker compose -f docker-compose.postgres.yml up -d
```

Si Docker responde `permission denied` al acceder a `/var/run/docker.sock`, corregir permisos de Docker para el usuario local antes de continuar. En este entorno se observo que el usuario `advanta` no pertenece al grupo que controla el socket de Docker.

Validar que PostgreSQL responde:

```bash
pg_isready -h localhost -p 5432
```

Generar migracion:

```bash
dotnet tool run dotnet-ef migrations add NombreMigracion --project src/SwiftPos.Infrastructure --startup-project src/SwiftPos.Api --output-dir Persistence/Migrations
```

Aplicar migraciones:

```bash
dotnet tool run dotnet-ef database update --project src/SwiftPos.Infrastructure --startup-project src/SwiftPos.Api
```

Generar SQL idempotente de migraciones:

```bash
dotnet tool run dotnet-ef migrations script --project src/SwiftPos.Infrastructure --startup-project src/SwiftPos.Api --idempotent --output /tmp/swiftpos-initial.sql
```

Connection string local usada por `appsettings.Development.json`:

```text
Host=localhost;Port=5432;Database=swiftpos;Username=swiftpos;Password=swiftpos_dev
```

## Comandos

Restaurar:

```bash
dotnet restore
```

Compilar:

```bash
dotnet build
```

En entornos restringidos, usar directorios temporales para CLI/NuGet y build serial:

```bash
DOTNET_CLI_HOME=/tmp/dotnet-home \
NUGET_PACKAGES=/tmp/nuget-packages \
MSBUILDUSEREXTENSIONSPATH=/tmp/msbuild \
dotnet build SwiftPosApi.sln -m:1
```

Ejecutar API:

```bash
dotnet run --project src/SwiftPos.Api
```

Smoke test de auth y catalogo con la API corriendo:

```bash
scripts/smoke-auth-catalog.sh
```

Health check:

```http
GET /health
```

Swagger:

```http
GET /swagger
GET /swagger/v1/swagger.json
```

## Render

El proyecto incluye `Dockerfile` para desplegar `SwiftPosApi` como Web Service en Render.

Configuracion recomendada en Render:

- Runtime: Docker.
- Root directory: raiz de este repositorio `SwiftPosApi`.
- Health check path: `/health`.
- Variables de entorno:
  - `DATABASE_URL`: connection string PostgreSQL en formato URI, por ejemplo Neon.
  - `Jwt__Issuer`: issuer del token.
  - `Jwt__Audience`: audience del token.
  - `Jwt__SigningKey`: secreto fuerte de produccion.
  - `Jwt__ExpirationMinutes`: minutos de expiracion.

La API usa `PORT` automaticamente cuando Render lo define. Para la base de datos acepta `DATABASE_URL` en formato `postgresql://...` o `ConnectionStrings__DefaultConnection` en formato Npgsql.
# SwiftPosApi
