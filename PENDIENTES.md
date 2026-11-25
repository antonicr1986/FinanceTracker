# Pendientes

Mejoras identificadas que quedan para más adelante. Al terminar una, se marca
con `[x]` y se anota la fecha o el commit.

## DAST (OWASP ZAP)

Contexto: el primer informe de ZAP (baseline contra la raíz de la API) salió con
0 High y 0 Medium, pero solo probó `/`, `/robots.txt` y `/sitemap.xml`, que
devuelven 404. No vio ningún endpoint real.

- [x] **Cabeceras de seguridad** (`Middleware/SecurityHeaders.cs`): HSTS, `nosniff`,
  `X-Frame-Options`, `Referrer-Policy`, y en las respuestas de la API (no en Swagger)
  `Content-Security-Policy: default-src 'none'` y `Cache-Control: no-store`. Sin cabecera
  `Server`. Con tests.
- [x] **Corregido `GET /api/Users/{id}`**: no exigía sesión y devolvía el nombre y el email de
  cualquier usuario por su id (BOLA/IDOR, OWASP API1). Ahora exige JWT y solo devuelve el
  propio usuario (404 con un id ajeno). Con tests.
- [x] **Job `dast-api`**: ZAP `action-api-scan` activo y autenticado (cuenta demo) contra una
  API + SQL Server desechables levantados con `docker compose` en el runner, a partir del
  `swagger.json`. Nunca ataca producción.
- [x] **`fail_action: true`** en `dast` y `dast-api`, y `deploy` depende de `dast-api`: un hallazgo
  nuevo bloquea el despliegue. Riesgos aceptados como INFO en `.zap/rules.tsv`, con su motivo.
- [x] `Cross-Origin-Resource-Policy: same-origin` en las respuestas de la API (alerta 90004).

## Pipeline (`ci.yml`)

- [x] **OIDC en el job `deploy`**: `azure/login` con la identidad `id-financetracker-deploy`
  (rol *Website Contributor* solo sobre la Web App, federated credential restringida al
  environment `production`, en `terraform/deploy-identity.tf`). Borrado el secret
  `AZURE_WEBAPP_PUBLISH_PROFILE` y Web Deploy con contraseña apagado. Hecho (25 nov 2026).
- [~] ~~**Required reviewers** en el environment `production`~~. Descartado (25 nov 2026): con un solo
  desarrollador, quien hace push y quien aprueba es la misma persona; el CI ya bloquea cualquier
  despliegue que no pase los controles. Revisar si alguien más tiene permiso de push.
- [x] **SAST con CodeQL** (Settings → Code security → CodeQL, Default setup).
- [ ] **Cadena de suministro**:
  - [x] Desplegar por digest (`@sha256:`) exactamente la imagen escaneada y firmada (25 nov 2026).
  - [ ] Generar SBOM (`trivy image --format cyclonedx`).
  - [ ] Firmar la imagen / procedencia (`actions/attest-build-provenance` o cosign).
  - [x] Verificar el checksum de gitleaks (`GITLEAKS_SHA256` + `sha256sum --check` en `ci.yml`).
  - [ ] Fijar la versión de Trivy (ahora se instala la última del repositorio apt).
  - [ ] Fijar las actions por SHA y añadir `.github/dependabot.yml` (`github-actions`, `nuget`, `docker`).

## Kubernetes (Helm)

Contexto: la API ya se despliega en Kubernetes con el chart `charts/financetracker`
(PR #8), validado en el CI con `helm lint --strict` y `helm template`. Hoy el
despliegue en el cluster es manual (`helm install` / `helm upgrade`).

- [ ] **GitOps con Argo CD**: instalar Argo CD en el cluster local y crear una
  Application que apunte a `charts/financetracker` en `master`, con *sync*
  automatico y *self-heal*. Probar: cambiar `api.replicas` en Git, escalar a mano
  con `kubectl` (drift corregido) y `git revert` como rollback.
- [ ] **Imagen fijada por commit**: que el CI actualice `api.image.tag` en
  `values.yaml` con el `sha-...` de la imagen publicada, en vez de usar `latest`
  (necesario para que Argo CD despliegue versiones concretas). Fijar también la
  versión del chart de Traefik (`--version`) en las instrucciones.
- [ ] **Mantener el chart al dia con la app**: si la API necesita una variable
  de entorno o un secreto nuevo, anadirlo tambien al chart (el lint del CI no lo detecta).
- [x] **Escanear el chart con Trivy** (`trivy config charts/` en el CI) y endurecer el `securityContext`:
  API como `app` (UID 1654), sin root ni privilegios y con disco de solo lectura; excepción KSV-0014
  solo para SQL Server en `charts/.trivyignore.yaml` (25 nov 2026).
- [x] Añadir al chart: HPA por CPU (2-4 réplicas), `topologySpreadConstraints` entre nodos y un
  PodDisruptionBudget (`minAvailable: 1`) para la API (25 nov 2026).
- [ ] **Sesión ACR + AKS** con el mismo chart: ACR Basic, AKS con 1 nodo (`--attach-acr`), valores de
  producción (`sql.enabled=false` con Azure SQL, imagen por `sha-...`) e Ingress con IP pública.
  **Crear, practicar y borrar el mismo día** (los nodos se pagan por horas).

## Repositorio

- [x] Ruleset `proteger-main`: sin borrado ni force push en la rama principal (sin PR obligatoria: se trabaja con push directo).
- [x] Dependabot alerts + security updates, y secret scanning con push protection.
- [ ] `SECURITY.md` con cómo reportar vulnerabilidades.

## Azure / runtime

- [x] **Rotar la contraseña del administrador SQL** (octubre de 2026, versión 1). Para la próxima: nueva contraseña en
  `TF_VAR_sql_admin_password`, subir `sql_admin_password_version` en 1,
  `terraform apply` y **actualizar la cadena de conexión** de la Web App: está duplicada en la app setting `ConnectionStrings__DefaultConnection` y en la connection string `DefaultConnection` (tipo SQLAzure); hay que cambiar las dos.
- [x] **Key Vault** para `Jwt__Key` y la cadena de conexión (21 de octubre de 2026):
  `kv-financetracker` con RBAC, soft delete y purge protection (`terraform/keyvault.tf`), rol
  *Key Vault Secrets User* para la managed identity de la Web App y app settings con
  `@Microsoft.KeyVault(...)`. Los valores se cargaron con la CLI, fuera del estado.
- [x] **Quitar la cadena de conexión duplicada**: borrada la connection string `DefaultConnection`
  (tenía la contraseña en claro); la API usa solo la app setting que lee de Key Vault. Hecho (25 nov 2026).
- [x] **Mover a Key Vault el resto de secretos** de la Web App: `APPLICATIONINSIGHTS_CONNECTION_STRING`
  es una referencia a Key Vault (`appinsights-connection-string`). Las `DOCKER_REGISTRY_SERVER_*`
  estaban vacías y se han borrado: la imagen de GHCR es pública. Hecho (25 nov 2026).
- [x] **Presupuesto con alerta** en Azure Cost Management (creado en el portal; ya ha avisado por email).
- [ ] **Renombrar la base de datos** `acrfinancetracker` (el prefijo `acr` parece un Container
  Registry): `az sql db rename`, actualizar la cadena de conexión (en Key Vault), cambiar
  `sql_database_name` y `terraform state rm azurerm_mssql_database.main` para que `imports.tf` la
  vuelva a adoptar. El plan debe decir `1 to import, 0 to destroy`; si dice `destroy`, parar.
- [ ] **Misma región para la API y la base de datos** (ahora Belgium Central y Sweden Central): menos
  latencia y datos en un solo sitio. Valorar si el plan gratuito lo permite.
- [ ] **SQL sin contraseña**: autenticación Microsoft Entra con la managed identity.
- [x] Monitorización: Application Insights (OpenTelemetry) y alertas por email de 5xx y picos de 401/403 (`terraform/monitoring.tf`).
- [x] Auditoría de SQL al mismo Log Analytics (`SQLSecurityAuditEvents`) y AZU-0027 fuera de `.trivyignore` (25 nov 2026).
- [ ] Revisar las exclusiones de `terraform/.trivyignore` si se sube de plan (AZU-0022, AZU-0027).

## Otros repositorios

- [ ] **jenkins-docker**: Configuration as Code (JCasC) y `plugins.txt`, para reproducir Jenkins
  sin copiar `jenkins_home`; mover a Key Vault/variables lo que quede; cambiar
  `gitleaks-action@v2` cuando publiquen la versión con Node 24.
- [ ] **antonio-cv**: acciones fijadas por SHA, ejecución semanal (`schedule`) del job `links`,
  Lighthouse CI, verificar el checksum de gitleaks y subir a mano `VERCEL_CLI_VERSION` (Dependabot
  no la ve).
- [ ] **Equipo**: instalar PowerShell 7 (`pwsh`) y arreglar `winget` (App Installer de Microsoft).

## Hecho

- [x] Escaneo de seguridad de Terraform (Trivy), `permissions` globales, `deploy`
  depende de todos los controles, job `dast`, `concurrency` y `timeout-minutes`.
- [x] Managed identity y HTTP/2 en la Web App (`terraform apply`).
