# ☁️ Infraestructura de AWS (Terraform)

[English](README.md) · **Español**

La API de FinanceTracker desplegada **tambien en AWS**, con la misma imagen de
contenedor que usa Azure. Pensada para **levantarse y destruirse con un solo
comando**: se paga solo mientras se usa, y deja preparada la migracion de Azure
a AWS.

## Que crea

| Pieza | En AWS | Equivale en Azure |
|---|---|---|
| API | ECS Express Mode (Fargate + balanceador + HTTPS) | App Service |
| Base de datos | RDS SQL Server Express 2022 (`db.t3.micro`) | Azure SQL |
| Secretos | Almacen de parametros (SSM), `SecureString` | Key Vault |
| Logs | CloudWatch Logs (7 dias) | Application Insights |
| Red | VPC por defecto + 2 grupos de seguridad | (gestionada por App Service) |

Ficheros:

- `versions.tf`: version de Terraform, proveedor `aws` y estado remoto en S3.
- `network.tf`: grupos de seguridad. La base de datos solo acepta el puerto 1433 desde la API.
- `database.tf`: la instancia de RDS.
- `secrets.tf`: cadena de conexion y clave JWT en SSM.
- `ecs.tf`: cluster, roles IAM, grupo de logs y el servicio Express Mode.
- `outputs.tf`: `api_url` y `database_host`.

## Una sola vez (fuera de Terraform)

Estas piezas son de la cuenta y **no se borran con `destroy`**, a proposito:

1. **Bucket del estado** `financetracker-tfstate-188656931134` (versionado). Tiene que existir antes que el propio estado.
2. **Usuario IAM** `terraform-financetracker` con su perfil de CLI `financetracker`.
3. **Presupuesto** de 10 € con aviso por correo, creado en Billing. Si lo gestionara este codigo, cada `destroy` lo borraria, y el aviso tiene que estar siempre activo.

## Uso

```powershell
cd terraform\aws
$env:AWS_PROFILE        = "financetracker"
$env:TF_VAR_db_password = "..."   # solo letras y numeros
$env:TF_VAR_jwt_key     = "..."   # la misma que Jwt--Key en el Key Vault de Azure

terraform init      # solo la primera vez
terraform plan      # deberia decir 18 to add
terraform apply
terraform destroy   # al terminar, para no pagar
```

Al acabar el `apply`, comprobar `<api_url>/health/ready` (incluye la base de
datos) y el login de la cuenta demo en `<api_url>/swagger`.

### Tiempos medidos

| Operacion | Tiempo |
|---|---|
| `terraform apply` completo | ~20 min |
| `terraform destroy` completo | ~20 min (RDS ~4 min; el resto, vaciar el balanceador) |

**Consecuencia para la migracion:** 20 minutos es mucho mas que los 5 de caida
maxima permitidos. AWS se levanta **antes** de la ventana de cambio, con Azure
sirviendo; la caida queda solo para la ultima copia de datos y el cambio de DNS.

## Secretos

- La contrasena de RDS y la clave JWT son variables **efimeras** y los
  argumentos que las reciben son **write-only** (`password_wo`, `value_wo`):
  llegan a AWS pero **no quedan en el estado** ni en el plan. Nunca van en
  `terraform.tfvars`.
- El servicio de ECS lee los dos parametros de SSM y los inyecta como
  `ConnectionStrings__DefaultConnection` y `Jwt__Key`. El rol de ejecucion solo
  puede leer esos dos parametros.
- Para rotar un secreto: nuevo valor en la variable de entorno y subir en 1
  `db_password_version` o `jwt_key_version`.
- La clave JWT es la misma que en Azure para que, al migrar, los usuarios con
  sesion abierta no tengan que volver a entrar.

## Contrato con Azure

Las dos nubes ejecutan la misma imagen con los mismos valores y exponen las
mismas salidas, para que los scripts y el runbook de migracion no dependan de
la nube:

| Entrada | Salida |
|---|---|
| `container_image`, clave JWT, contrasena de la base de datos | `api_url`, `database_host` |

## Decisiones

- **VPC por defecto:** tiene subredes publicas en 3 zonas, que es lo que piden el balanceador y RDS. Una VPC propia con subredes privadas obligaria a pagar un NAT Gateway.
- **ECS Express Mode** en vez de App Runner (cerrado a clientes nuevos desde abril de 2026). Crea y gestiona el balanceador, el certificado y la URL.
- **Destruible a proposito:** sin proteccion contra borrado y sin instantanea final. Los datos reales siguen en Azure hasta la migracion.
- **Siempre 1 tarea** (`min = max = 1`): sin escalado, ni en coste.
- **`wait_for_steady_state`:** si la API no arranca, el `apply` falla en la terminal y no hay que buscar el error en la consola.
- **`depends_on` en el servicio** hacia los permisos IAM: si `destroy` borrara antes los permisos, el servicio se quedaria atascado en `DRAINING` sin poder borrar el balanceador.
- La direccion de RDS (`database_host`) es **estable** aunque se destruya y se recree la instancia con el mismo nombre.

## Coste aproximado (encendido)

Unos **10 céntimos por hora** entre todo (balanceador, IP publicas, Fargate 0,5 vCPU / 1 GB y RDS). Lo caro es dejarlo encendido sin usarlo: por eso `destroy` al terminar.

## Pendiente

- Validar esta carpeta en el CI y revisar los avisos de Trivy sobre la configuracion.
- Mover el Terraform de Azure a `terraform/azure/` para que las dos nubes queden simetricas.
