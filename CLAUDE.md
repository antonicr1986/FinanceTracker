# FinanceTracker (API)

## Contexto

API REST de finanzas personales (movimientos, categorías, presupuestos y panel) con tres clientes que la consumen.
Guía de desarrollo con el detalle (dónde tocar qué, arranque en local, trampas): [`DEVELOPING.md`](DEVELOPING.md).

Stack: .NET 8 + ASP.NET Core, EF Core sobre Azure SQL (SQL Server 2022 en Docker en local), JWT. Infraestructura en Terraform (Azure) y chart de Helm.
Decisiones caras: **todo está filtrado por usuario.** Cada consulta de los servicios lleva `.Where(x => x.UserId == userId)` con el id de `ICurrentUserService`; un endpoint que lo olvide filtra datos de otras cuentas.

## Comandos

- Compilar: `dotnet build FinanceTracker.sln`
- Tests: `dotnet test`
- Un solo test: `dotnet test --filter "FullyQualifiedName~NombreDelTest"`
- Ejecutar en local: `dotnet run --project FinanceTracker.Api` (Swagger en http://localhost:5279/swagger) o `docker compose up`
- Lint / formato: `dotnet format FinanceTracker.sln`
- Migración nueva: `dotnet ef migrations add Nombre --project FinanceTracker.Infraestructure --startup-project FinanceTracker.Api` (se aplica sola al arrancar)

## Estructura

`Api -> Application -> Infrastructure -> Domain`, una capa por proyecto:

- `FinanceTracker.Api`: controladores, arranque (`Program.cs`) y middleware. Sin lógica de negocio.
- `FinanceTracker.Application`: interfaces de servicios, DTOs y resultados. Es el contrato entre Api e Infraestructura.
- `FinanceTracker.Infraestructure`: servicios, `AppDbContext`, migraciones y seeders. La **carpeta** lleva "e" por una errata heredada; el csproj y los namespaces son `FinanceTracker.Infrastructure`. No la "arregles" a medias.
- `FinanceTracker.Domain`: entidades y enums.
- `FinanceTracker.Tests`: xUnit con EF InMemory (`Services/`, `Api/`).
- `terraform/`, `charts/financetracker/`: infraestructura de Azure y despliegue en Kubernetes.

## Convenciones

- Código en inglés; comentarios y documentación en castellano.
- El controlador solo traduce el resultado del servicio a un código HTTP; la regla de negocio vive en el servicio.
- Secretos nunca en el repo: User Secrets en local, variables de entorno (`Jwt__Key`, `ConnectionStrings__DefaultConnection`) y Azure Key Vault en Azure.
- Las credenciales de la cuenta demo son públicas a propósito y van anotadas con `// gitleaks:allow`. Nunca `--no-verify` para saltar el hook de gitleaks.

## Forma de trabajar

- Flujo: `/backlog` → `/story` → `/pair` → `/code-review`. Una historia por sesión.
- `/integrar-kit` no forma parte del flujo: solo se usa al instalar o actualizar el kit.
- TDD: un test → código mínimo → verde. Pararse y esperar mi visto bueno.
- Lo más simple que cumpla los criterios. Nada "para el futuro".
- No inventes requisitos: si falta información, pregunta.
- **Nunca hagas commit ni push.** Sugiere el mensaje y el commit lo hago yo.
- Mensajes de commit en inglés, formato Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `ci:`, `chore:`). Funcionalidad y refactor en commits separados.
- Sin atribución de IA en los commits.

## CI/CD

- Pipelines: `.github/workflows/ci.yml` (GitHub Actions, el que bloquea y despliega) y `azure-pipelines.yml` (espejo en Azure DevOps).
- El CI exige: formato según `.editorconfig`, ningún paquete NuGet vulnerable, compilar en Release, que pasen los tests, cobertura de líneas ≥ 30 %, ningún secreto en el código ni en el historial (gitleaks), Terraform y Helm válidos, imagen sin vulnerabilidades altas (Trivy) y DAST de la API (ZAP).
- Antes de proponer un commit, ejecuta `dotnet format` y `dotnet test` en local.
- Entorno: producción. Un push a `master` despliega la imagen de GHCR al App Service de Azure, sin paso manual.
- No modifiques el pipeline sin avisarme.
- Al añadir una action a un workflow, fíjala por SHA con un comentario de versión (`uses: autor/action@<sha> # v1.2.3`), nunca por etiqueta (`@v1`).

## Decisiones del proyecto

<!-- Una línea por cada decisión que aplica a todo el proyecto (las propone /pair). Si es importante, detállala en docs/decisions/ con la plantilla 0000-plantilla.md. -->

## No tocar

- Migraciones ya aplicadas en `FinanceTracker.Infraestructure/Migrations/`: los cambios del modelo van en una migración nueva.
- `terraform/terraform.tfvars`: es local y lleva secretos.
