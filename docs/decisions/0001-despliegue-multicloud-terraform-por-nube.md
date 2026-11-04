# 0001. Despliegue multicloud: un Terraform por nube con el mismo contrato

- **Fecha:** 2026-11-04
- **Estado:** Aceptada

## Contexto

La API corre en Azure (App Service + Azure SQL). Queremos poder desplegarla
también en AWS y dejar preparada una migración de Azure a AWS con **menos de 5
minutos de caída**, manteniendo Azure como nube principal mientras tanto.

Condiciones:

- Proyecto personal: el coste tiene que ser mínimo. AWS no puede estar
  encendido todo el tiempo.
- Las dos nubes tienen que ejecutar exactamente la misma API, sin ramas ni
  builds distintos.
- Los secretos no pueden acabar ni en el repo ni en el estado de Terraform.
- No tocar la infraestructura ni el pipeline de Azure, que están en producción.

## Decisión

Un **Terraform independiente por nube** (`terraform/` para Azure,
`terraform/aws/` para AWS), cada uno con su estado remoto en su propia nube, y
un **contrato común**: la misma imagen de GHCR y la misma clave JWT como
entradas, y las mismas salidas (`api_url`, `database_host`).

AWS se trata como un entorno **bajo demanda**: se levanta y se destruye con
`terraform apply` / `terraform destroy`. Lo que tiene que sobrevivir al
`destroy` (bucket del estado, usuario IAM de Terraform y presupuesto con aviso
de gasto) se crea a mano una sola vez, fuera de Terraform.

## Alternativas consideradas

- **Un único Terraform con un interruptor `cloud = "aws" | "azure"`:** mezcla
  dos sistemas que no se parecen en nada en el mismo estado, y un error en una
  nube puede bloquear la otra. Más difícil de leer y de mantener.
- **Mover ya Azure a `terraform/azure/`:** más simétrico, pero obliga a tocar
  el pipeline que despliega a producción. Se deja para un cambio aparte.
- **AWS siempre encendido:** más vistoso, pero con coste fijo mensual
  (balanceador, IP públicas y RDS) sin uso real.
- **Presupuesto de AWS gestionado por Terraform:** cada `destroy` lo borraría,
  justo cuando el aviso de gasto tiene que seguir activo.

## Consecuencias

- **Positivas:** cada nube se levanta, se cambia o se destruye sin tocar la
  otra; los scripts y el runbook de migración usan las mismas salidas en las
  dos; el coste de AWS es casi cero cuando no se usa; los secretos son
  write-only y no quedan en ningún estado.
- **Negativas:** levantar AWS tarda unos 20 minutos, así que en una migración
  hay que hacerlo **antes** de la ventana de caída; el contrato común hay que
  mantenerlo a mano en los dos Terraform; Azure y AWS quedan de momento en
  carpetas asimétricas (`terraform/` y `terraform/aws/`).
