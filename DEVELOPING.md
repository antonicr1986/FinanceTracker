# Guía de desarrollo — FinanceTracker (API)

Orientación rápida para volver al proyecto sin releer el código. El `README.md`
cuenta qué hace la aplicación de cara a fuera; esto cuenta dónde tocar y qué
duele.

## La aplicación en 30 segundos

API REST en .NET 8, cuatro capas, cada una en su proyecto:

```
Api  ->  Application  ->  Infrastructure  ->  Domain
```

- **Api** — controladores, arranque, middleware. No tiene lógica de negocio.
- **Application** — interfaces (`ITransactionService`…), DTOs y resultados.
  Es el contrato: la Api y la Infraestructura solo se conocen a través de aquí.
- **Infrastructure** — implementación de esos servicios, `AppDbContext`,
  migraciones y seeder.
- **Domain** — entidades (`User`, `Category`, `Transaction`, `Budget`) y enums.

Una petición recorre siempre el mismo camino:

```
HTTP -> Controller -> IXxxService -> XxxService -> AppDbContext -> SQL
```

El controlador solo traduce: llama al servicio y convierte su resultado en un
código HTTP. La regla de negocio vive en el servicio.

**Todo está filtrado por usuario.** Los servicios sacan el id del token con
`ICurrentUserService` y añaden `.Where(x => x.UserId == userId)` a cada
consulta. Un endpoint nuevo que se olvide de eso filtra datos de otras cuentas.

## Mapa: dónde tocar qué

| Quiero… | Archivo |
|---|---|
| Añadir o cambiar un endpoint | `FinanceTracker.Api/Controllers/*.cs` |
| Cambiar una regla de negocio | `FinanceTracker.Infraestructure/Services/*.cs` |
| Cambiar la forma de lo que entra o sale | `FinanceTracker.Application/DTOs/<área>/` |
| Añadir un servicio nuevo | interfaz en `Application/Interfaces/` + clase en `Infraestructure/Services/` + `AddScoped` en `Program.cs` |
| Cambiar el modelo de datos | `FinanceTracker.Domain/Entities/` + migración |
| Tocar CORS, JWT, health checks, Swagger | `FinanceTracker.Api/Program.cs` |
| Cambiar logging o los orígenes permitidos | `FinanceTracker.Api/appsettings.json` |
| Datos de la cuenta demo | `FinanceTracker.Infraestructure/Data/DemoDataSeeder.cs` |
| Tests | `FinanceTracker.Tests/Services/` (xUnit + EF InMemory) |

Ojo: la **carpeta** se llama `FinanceTracker.Infraestructure` (con "e"), pero el
csproj, el ensamblado y los namespaces son `FinanceTracker.Infrastructure`.
Es una errata heredada; no intentes "arreglarla" a medias.

## Arrancar en local

Los secretos no están en el repo. La primera vez, en
`FinanceTracker.Api/` (el csproj ya tiene `UserSecretsId`):

```bash
dotnet user-secrets set "Jwt:Key" "<una clave larga, 32+ caracteres>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena de Azure SQL o LocalDB>"
```

Después, cada vez:

```bash
dotnet run --project FinanceTracker.Api      # http://localhost:5279/swagger
dotnet test                                   # todos los tests
```

Al arrancar, la API aplica las migraciones pendientes (`db.Database.Migrate()`)
y siembra la cuenta demo. Contra una base de datos vacía se monta sola.

Alternativa sin tocar Azure: `docker compose up` levanta la API y un SQL Server
2022 local, con `MSSQL_SA_PASSWORD` y `JWT_KEY` en un `.env` (ver `.env.example`).

### Migración nueva

```bash
dotnet ef migrations add NombreDeLaMigracion \
  --project FinanceTracker.Infraestructure \
  --startup-project FinanceTracker.Api
```

No hace falta aplicarla a mano: se aplica al siguiente arranque.

## Las trampas que más duelen

1. **Si falta un secreto, la API no arranca.** `Program.cs` lanza
   `InvalidOperationException` si no encuentra `Jwt:Key`, `Jwt:Issuer` o
   `Jwt:Audience`. Un arranque que muere al instante casi siempre es eso, no un
   fallo de código. En Azure las mismas claves van como variables de entorno con
   doble guion bajo: `Jwt__Key`, `ConnectionStrings__DefaultConnection`.

2. **El firewall de Azure SQL.** Si la IP de casa cambia, el arranque local
   falla con el error 40613 ("Database not currently available"). Se arregla
   añadiendo la IP nueva en el firewall del servidor `financetrackerapp-srv`,
   no tocando la cadena de conexión.

3. **`GET /api/Transactions` miente en su firma.** Declara
   `ActionResult<List<TransactionDto>>` pero devuelve un `PagedResult<T>`
   (`{ items, totalCount, pageNumber, pageSize, totalPages }`), y `PageSize`
   está limitado a 100 por `[Range(1, 100)]`: pedir más responde 400. Si algún
   día se corrige la firma, hay que avisar al frontend.

4. **El seeder de la demo es idempotente.** Si la cuenta
   `demo@financetracker.app` ya existe, `DemoDataSeeder` no hace nada. Si
   alguien borra sus movimientos, **no se recrean**: hay que sembrarlos a mano.
   Sus credenciales son públicas a propósito y van anotadas con
   `// gitleaks:allow`.

5. **El hook de gitleaks bloquea el commit** en cuanto huele a credencial. La
   salida es anotar la línea con `// gitleaks:allow`, nunca `--no-verify`.

## Antes de pushear

```bash
dotnet build && dotnet test
```

El CI (`.github/workflows/ci.yml`) corre gitleaks, build y tests, y solo si
pasan publica en el App Service de Azure. Un push a `master` despliega a
producción; no hay paso manual.

Sin atribución de IA en los commits.
