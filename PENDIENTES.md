# Pendientes

Mejoras identificadas que quedan para más adelante. Al terminar una, se marca
con `[x]` y se anota la fecha o el commit.

## DAST (OWASP ZAP)

Contexto: el primer informe de ZAP (baseline contra la raíz de la API) salió con
0 High y 0 Medium, pero solo probó `/`, `/robots.txt` y `/sitemap.xml`, que
devuelven 404. No vio ningún endpoint real.

- [ ] **Cabeceras de seguridad en `Program.cs`** (alertas 10035 y 10049 de ZAP).
  Middleware propio justo después de `app.UseExceptionHandler()`:
  `Strict-Transport-Security`, `X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` y
  `Cache-Control: no-store` (excepto en `/swagger`).
  - `app.UseHsts()` no sirve: el TLS lo termina Azure y dentro del contenedor la
    petición llega por HTTP, así que no añadiría la cabecera.
  - Sin `Content-Security-Policy` de momento: rompería Swagger UI (scripts inline).
  - Comprobar en local `dotnet build` y `dotnet format --verify-no-changes`.
- [ ] **Cambiar el job `dast` a `zaproxy/action-api-scan`** usando
  `/swagger/v1/swagger.json` (`format: openapi`), para que ZAP pruebe todos los
  endpoints.
- [ ] **Pasar `fail_action` a `true`** cuando el informe salga limpio. Los falsos
  positivos se excluyen en `.zap/rules.tsv` (`rules_file_name` en el job).

## Pipeline (`ci.yml`)

- [ ] **OIDC en el job `deploy`**: sustituir `AZURE_WEBAPP_PUBLISH_PROFILE` por
  `azure/login` con una identidad propia (rol *Website Contributor* limitado a la
  Web App, federated credential restringida al environment `production`).
  Después: borrar el secret, poner `webdeploy_publish_basic_authentication_enabled = false`
  en `main.tf` y `terraform apply`.
- [ ] **Required reviewers** en el environment `production` (Settings → Environments).
- [ ] **SAST con CodeQL** (Settings → Code security → CodeQL, Default setup).
- [ ] **Cadena de suministro**:
  - [ ] Publicar exactamente la imagen escaneada (o desplegar por digest `@sha256:`).
  - [ ] Generar SBOM (`trivy image --format cyclonedx`).
  - [ ] Firmar la imagen / procedencia (`actions/attest-build-provenance` o cosign).
  - [ ] Verificar el checksum de gitleaks y fijar la versión de Trivy.
  - [ ] Fijar las actions por SHA y añadir `.github/dependabot.yml` (`github-actions`, `nuget`, `docker`).

## Repositorio

- [ ] Branch protection en `master` (PR obligatoria, checks en verde, sin force push).
- [ ] Dependabot alerts + security updates, y secret scanning con push protection.
- [ ] `SECURITY.md` con cómo reportar vulnerabilidades.

## Azure / runtime

- [ ] **Rotar la contraseña del administrador SQL**: nueva contraseña en
  `TF_VAR_sql_admin_password`, subir `sql_admin_password_version` a `1`,
  `terraform apply` y **actualizar la cadena de conexión** de la Web App.
- [ ] **Key Vault** para `Jwt__Key` y la cadena de conexión, con Key Vault
  references y la managed identity de la Web App (ya creada).
- [ ] **SQL sin contraseña**: autenticación Microsoft Entra con la managed identity.
- [ ] Monitorización: Application Insights y alertas (health check, picos de 401/403/500).
- [ ] Revisar las exclusiones de `terraform/.trivyignore` si se sube de plan (AZU-0022, AZU-0027).

## Hecho

- [x] Escaneo de seguridad de Terraform (Trivy), `permissions` globales, `deploy`
  depende de todos los controles, job `dast`, `concurrency` y `timeout-minutes`.
- [x] Managed identity y HTTP/2 en la Web App (`terraform apply`).
