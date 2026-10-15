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

- [ ] **OIDC en el job `deploy`**: sustituir `AZURE_WEBAPP_PUBLISH_PROFILE` por
  `azure/login` con una identidad propia (rol *Website Contributor* limitado a la
  Web App, federated credential restringida al environment `production`).
  Después: borrar el secret, poner `webdeploy_publish_basic_authentication_enabled = false`
  en `main.tf` y `terraform apply`.
- [ ] **Required reviewers** en el environment `production` (Settings → Environments).
- [x] **SAST con CodeQL** (Settings → Code security → CodeQL, Default setup).
- [ ] **Cadena de suministro**:
  - [ ] Publicar exactamente la imagen escaneada (o desplegar por digest `@sha256:`).
  - [ ] Generar SBOM (`trivy image --format cyclonedx`).
  - [ ] Firmar la imagen / procedencia (`actions/attest-build-provenance` o cosign).
  - [ ] Verificar el checksum de gitleaks y fijar la versión de Trivy.
  - [ ] Fijar las actions por SHA y añadir `.github/dependabot.yml` (`github-actions`, `nuget`, `docker`).

## Repositorio

- [x] Ruleset `proteger-main`: sin borrado ni force push en la rama principal (sin PR obligatoria: se trabaja con push directo).
- [x] Dependabot alerts + security updates, y secret scanning con push protection.
- [ ] `SECURITY.md` con cómo reportar vulnerabilidades.

## Azure / runtime

- [x] **Rotar la contraseña del administrador SQL** (octubre de 2026, versión 1). Para la próxima: nueva contraseña en
  `TF_VAR_sql_admin_password`, subir `sql_admin_password_version` en 1,
  `terraform apply` y **actualizar la cadena de conexión** de la Web App: está duplicada en la app setting `ConnectionStrings__DefaultConnection` y en la connection string `DefaultConnection` (tipo SQLAzure); hay que cambiar las dos.
- [ ] **Key Vault** para `Jwt__Key` y la cadena de conexión, con Key Vault
  references y la managed identity de la Web App (ya creada).
- [ ] **SQL sin contraseña**: autenticación Microsoft Entra con la managed identity.
- [ ] Monitorización: Application Insights y alertas (health check, picos de 401/403/500).
- [ ] Revisar las exclusiones de `terraform/.trivyignore` si se sube de plan (AZU-0022, AZU-0027).

## Hecho

- [x] Escaneo de seguridad de Terraform (Trivy), `permissions` globales, `deploy`
  depende de todos los controles, job `dast`, `concurrency` y `timeout-minutes`.
- [x] Managed identity y HTTP/2 en la Web App (`terraform apply`).
