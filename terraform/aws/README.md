# ☁️ AWS infrastructure (Terraform)

**English** · [Español](README.es.md)

The FinanceTracker API deployed **on AWS as well**, using the same container
image as Azure. Built to be **brought up and torn down with a single command**:
you only pay while it is in use, and it lays the groundwork for migrating from
Azure to AWS.

## What it creates

| Piece | On AWS | Azure equivalent |
|---|---|---|
| API | ECS Express Mode (Fargate + load balancer + HTTPS) | App Service |
| Database | RDS SQL Server Express 2022 (`db.t3.micro`) | Azure SQL |
| Secrets | Parameter Store (SSM), `SecureString` | Key Vault |
| Logs | CloudWatch Logs (7 days) | Application Insights |
| Network | Default VPC + 2 security groups | (managed by App Service) |

Files:

- `versions.tf`: Terraform version, `aws` provider and remote state in S3.
- `network.tf`: security groups. The database only accepts port 1433 from the API.
- `database.tf`: the RDS instance.
- `secrets.tf`: connection string and JWT key in SSM.
- `ecs.tf`: cluster, IAM roles, log group and the Express Mode service.
- `outputs.tf`: `api_url` and `database_host`.

## One-time setup (outside Terraform)

These pieces belong to the account and are **not removed by `destroy`**, on purpose:

1. **State bucket** `financetracker-tfstate-188656931134` (versioned). It has to exist before the state itself.
2. **IAM user** `terraform-financetracker` with its CLI profile `financetracker`.
3. **Budget** of €10 with an email alert, created in Billing. If this code managed it, every `destroy` would delete it, and the alert must always be on.

## Usage

```powershell
cd terraform\aws
$env:AWS_PROFILE        = "financetracker"
$env:TF_VAR_db_password = "..."   # letters and numbers only
$env:TF_VAR_jwt_key     = "..."   # the same as Jwt--Key in the Azure Key Vault

terraform init      # first time only
terraform plan      # should say 18 to add
terraform apply
terraform destroy   # when done, to stop paying
```

After `apply`, check `<api_url>/health/ready` (it includes the database) and
log in with the demo account at `<api_url>/swagger`.

### Measured times

| Operation | Time |
|---|---|
| Full `terraform apply` | ~20 min |
| Full `terraform destroy` | ~20 min (RDS ~4 min; the rest is draining the load balancer) |

**What this means for the migration:** 20 minutes is far more than the 5
minutes of maximum downtime allowed. AWS is brought up **before** the cutover
window, while Azure is still serving; the downtime covers only the final data
copy and the DNS switch.

## Secrets

- The RDS password and the JWT key are **ephemeral** variables, and the
  arguments that receive them are **write-only** (`password_wo`, `value_wo`):
  they reach AWS but **are not stored in the state** or the plan. They never
  go in `terraform.tfvars`.
- The ECS service reads both SSM parameters and injects them as
  `ConnectionStrings__DefaultConnection` and `Jwt__Key`. The execution role can
  only read those two parameters.
- To rotate a secret: set the new value in the environment variable and bump
  `db_password_version` or `jwt_key_version` by 1.
- The JWT key is the same as in Azure so that, after the migration, users with
  an open session do not have to log in again.

## Contract with Azure

Both clouds run the same image with the same values and expose the same
outputs, so scripts and the migration runbook do not depend on the cloud:

| Inputs | Outputs |
|---|---|
| `container_image`, JWT key, database password | `api_url`, `database_host` |

## Decisions

- **Default VPC:** it has public subnets in 3 availability zones, which is what the load balancer and RDS need. A custom VPC with private subnets would require paying for a NAT Gateway.
- **ECS Express Mode** instead of App Runner (closed to new customers since April 2026). It creates and manages the load balancer, the certificate and the URL.
- **Destroyable on purpose:** no deletion protection and no final snapshot. The real data stays in Azure until the migration.
- **Always 1 task** (`min = max = 1`): no scaling, and no scaling costs.
- **`wait_for_steady_state`:** if the API does not start, `apply` fails in the terminal instead of you having to find the error in the console.
- **`depends_on` on the service** pointing to the IAM permissions: if `destroy` removed the permissions first, the service would get stuck in `DRAINING`, unable to delete the load balancer.
- The RDS address (`database_host`) is **stable** even when the instance is destroyed and recreated with the same name.

## Approximate cost (while running)

About **10 cents per hour** in total (load balancer, public IPs, Fargate 0.5 vCPU / 1 GB and RDS). The expensive part is leaving it on without using it: hence `destroy` when done.

## To do

- Validate this folder in CI and review Trivy's configuration warnings.
- Move the Azure Terraform to `terraform/azure/` so both clouds are symmetrical.
