---
name: integrar-kit
description: Integra el ai-project-kit en un proyecto que ya existía. Lee .claude/integrar-kit/INTEGRACION.md (lo genera instalar.ps1 en modo proyecto existente), lo fusiona todo sin pedirte decisiones (cada conflicto tiene una opción por defecto que te resume al final) y te deja un comando para commit, push y PR. Con "con plan" te pregunta antes; con "paso a paso" va archivo por archivo. Nunca hace commit.
disable-model-invocation: true
---

# Integrar el kit en un proyecto existente

Eres quien integra el kit en un proyecto que ya tenía su forma de trabajar. El proyecto manda: el kit se adapta a él, no al revés. **Lo haces de principio a fin sin pedirme que decida nada:** cada conflicto tiene una opción por defecto (abajo), la aplicas y al final me cuentas qué has elegido. Nada queda en un commit, así que cualquier decisión se cambia con un mensaje ("cambia la 2").

**Modo:** si al invocarte escribo "con plan", sigue el [modo con plan](#modo-con-plan); si escribo "paso a paso", el [modo paso a paso](#modo-paso-a-paso). Si no, el modo automático (por defecto).

## Entrada

1. **`.claude/integrar-kit/INTEGRACION.md`**: los archivos que fusionar (estado y qué vigilar), los pipelines del proyecto, la versión del kit y lo que **ya hizo el instalador** (rama, archivos copiados, `.gitignore`, etiquetas): no lo rehagas ni lo investigues. Si no existe, para y dime que ejecute primero el instalador (`instalar.ps1 <ruta>`).
2. **`.claude/integrar-kit/kit/`**: la versión del kit de cada archivo de la tabla.
3. **El proyecto**: su `CLAUDE.md`, el README (y sus traducciones), guías como `DEVELOPING.md` o `CONTRIBUTING.md`, los pipelines, las instrucciones de otras herramientas de IA (`.github/copilot-instructions.md`, `.cursorrules`…), cómo se compila y se prueba, y los últimos commits (`git log --oneline -20`) para conocer el idioma y el estilo de los mensajes.

## Paso 1: analizar y medir

Lee todo lo anterior. Antes de cambiar nada, mide lo que puede cambiar una decisión:

- **Cobertura, igual que el CI.** Busca en los pipelines del proyecto el comando de tests y el paso que calcula la cobertura, y reprodúcelo tal cual (mismos parámetros, mismo script, mismos archivos incluidos). No la midas a tu manera: el CI puede contar archivos que tú no (por ejemplo, el punto de entrada sin tests). Si ningún pipeline la mide, usa la del kit (`dotnet test --collect:"XPlat Code Coverage"`, todas las líneas).
- **Formato con el `.editorconfig` del kit.** Guarda el del proyecto, copia temporalmente el del kit, ejecuta `dotnet format --verify-no-changes` (o el formateador del proyecto), anota qué archivos cambiarían y por qué regla, y **restaura el del proyecto**. Si cambia archivos, prueba la versión ajustada (ver [Decisiones por defecto](#decisiones-por-defecto)) hasta que no cambie ninguno.

Si te falta una herramienta para medir (el SDK del proyecto, Python…), mide con lo que haya y apúntalo para el resumen; no pares.

## Paso 2: aplicar

Aplica todo siguiendo las [reglas de cada archivo](#reglas-de-cada-archivo) y, donde el proyecto y el kit choquen, las [decisiones por defecto](#decisiones-por-defecto). Mientras trabajas, como mucho una línea de vez en cuando con lo que estás haciendo.

**Solo paras a preguntarme si** no puedes seguir sin un dato que solo yo tengo (por ejemplo, el informe dice algo que no cuadra con el proyecto y cambia lo que hay que hacer), o si cualquier opción rompería el CI o borraría algo del proyecto. En ese caso, una sola pregunta con una opción recomendada, y sigues con el resto mientras.

## Paso 3: verificar

- Compila y ejecuta los tests con los comandos del proyecto; comprueba el formato y la cobertura **igual que el CI**, con el mínimo nuevo.
- Valida la sintaxis de los archivos de configuración que hayas tocado (JSON, YAML).
- `git status` y `git diff --stat`: que no haya nada fuera de lo aplicado.
- Si algo falla, arréglalo si la causa es tuya; si no, explícamelo en el resumen con cómo arreglarlo.

## Paso 4: cerrar

1. Escribe en `.claude/kit-version` la versión que indica `INTEGRACION.md`.
2. Escribe la descripción del PR en `.git/INTEGRACION-PR.md` (dentro de `.git/` nunca se sube): qué se ha integrado, las decisiones tomadas, cómo probarlo y el resultado de la verificación, siguiendo la plantilla de PR del proyecto como PR que no es una historia.
3. Borra la carpeta temporal `.claude/integrar-kit/`. **No borres la skill** (`.claude/skills/integrar-kit/`): se queda para futuras actualizaciones del kit.
4. Dame el resumen en este formato, corto. Si no ha habido ningún conflicto, omite la lista de decisiones:

   ```markdown
   **Hecho.** <Una línea: qué se ha integrado y en qué rama.>

   **Verificado:** <build, tests, formato y cobertura (medida y mínimo), en una o dos líneas>. <Lo que solo se podrá ver en el CI, si hay algo.>

   **Decisiones que he tomado** (dime "cambia la N" si quieres la otra):
   1. <Conflicto>: <lo que he hecho>. Alternativa: <la otra opción>.

   <Si algo del informe no cuadraba con el proyecto, una línea por cada cosa.>
   ```

5. Después, **un único bloque de comandos** para copiar, en el idioma y el estilo de los commits del proyecto. Un solo commit con todo, salvo que haya un commit aparte de formato; en ese caso, el de formato primero:

```bash
git add -A
git commit -m "chore: integrate ai-project-kit <versión>"
git push -u origin <rama>
gh pr create --base <rama principal> --title "chore: integrate ai-project-kit <versión>" --body-file .git/INTEGRACION-PR.md
```

Si `gh` no está instalado o no tiene sesión (`gh auth status`), sustituye la última línea por el enlace para abrir el PR en la web y dime que pegue como descripción el contenido de `.git/INTEGRACION-PR.md`.

Si después te pido cambiar una decisión, aplícala, vuelve a verificar lo que afecte y actualiza `.git/INTEGRACION-PR.md`.

## Decisiones por defecto

Cuando el proyecto y el kit chocan, aplica esto sin preguntar y apúntalo en "Decisiones que he tomado":

| Conflicto | Qué haces | Alternativa que ofreces |
| --- | --- | --- |
| `CLAUDE.md`, `settings.json` u otras instrucciones dejan a la IA hacer commit o push | La regla del kit: nunca commit ni push. | Mantener lo que permitía el proyecto. |
| El `.editorconfig` del kit cambiaría archivos | Adoptarlo quitando o bajando a `suggestion` las reglas que chocan con lo que ya hace el código (BOM, orden de `using`, namespaces…), hasta que no cambie ninguno. Un comentario en el archivo explica cada ajuste. | Adoptarlo tal cual, con un commit aparte de formato. |
| El proyecto ya exigía una cobertura mínima distinta | Mantener la del proyecto. | La regla del kit (medida − 5, múltiplo de 5). |
| Una skill del proyecto tiene cambios propios respecto a una versión anterior del kit | Mantener la del proyecto. | Sustituirla por la del kit (en el resumen, la diferencia en una línea). |
| Una herramienta del CI se descarga sin comprobar su SHA-256 | Pasar a la versión y el SHA-256 que trae el kit, manteniendo los parámetros del proyecto. | Mantener la versión del proyecto y añadir su SHA-256. |
| El estilo de los commits del proyecto no es Conventional Commits | Conventional Commits en inglés a partir de ahora, salvo que el proyecto documente otra convención (entonces, la suya). | Documentar el estilo actual. |
| El proyecto tiene instrucciones para otras herramientas de IA (`copilot-instructions.md`, `.cursorrules`…) | Añadir al final dos líneas: que sigan `CLAUDE.md` y que nunca hagan commit ni push. | No tocarlas. |
| Cualquier otra contradicción | La opción que respete lo que ya hace el proyecto y no rompa su CI. | La del kit. |

## Reglas de cada archivo

- **`CLAUDE.md`:** mantén todas las reglas y secciones del proyecto. Añade solo las del kit que falten (flujo de skills, reglas de CI con los valores reales del proyecto, actions fijadas por SHA...), adaptadas a lo que ya dice. Si el proyecto no tenía, rellénalo con datos reales de su README y sus guías, y remite a ellas en lugar de copiarlas. No inventes contenido para secciones que no puedes rellenar con datos reales.
- **`AGENTS.md`:** debe seguir remitiendo a `CLAUDE.md` como única fuente. Añade las skills que falten a su tabla. **Mantén las reglas críticas que el kit repite ahí** (nunca commit ni push, no inventar requisitos): otras herramientas de IA (Copilot, Aider…) no aplican `settings.json` y no siempre siguen el enlace a `CLAUDE.md`, así que para ellas es la única protección.
- **`.claude/settings.json`:** une los permisos de los dos. Si chocan, gana el más restrictivo. Nunca quites un `deny` del proyecto. No añadas permisos de herramientas que el proyecto no usa (npm sin front, por ejemplo) y sí los de las que usa y el kit no conoce (por ejemplo, los tests de Android con Gradle, o `terraform fmt` con `terraform apply` en `deny`). Si hay archivos locales con secretos (`*.tfvars`, por ejemplo), añade su lectura a `deny`.
- **Skills:** si la del proyecto es una versión anterior del kit sin cambios propios, sustitúyela.
- **`.editorconfig`:** según lo medido en el paso 1.
- **CI:** si el proyecto ya tiene pipelines, **no los sustituyas ni añadas otro que haga lo mismo**: añade a los existentes solo los controles del kit que falten (secretos con gitleaks y SHA-256, formato, cobertura mínima), en el pipeline que bloquea el merge o el despliegue. Si no tiene CI, copia el del kit y ajusta sus variables. Las actions nuevas, fijadas por SHA con comentario de versión; las que ya tenía el proyecto no se tocan. Sigue el estilo de los comentarios del archivo.
- **Cobertura mínima:** la medida en el paso 1 **menos 5 puntos, redondeada hacia abajo a un múltiplo de 5** (56 % → 50 %, 36 % → 30 %), para que el CI no falle de golpe con el primer cambio sin tests. Pon el mismo valor en el pipeline y en `CLAUDE.md`.
- **`.github/dependabot.yml`:** añade los ecosistemas que falten. Respeta la frecuencia, los grupos y las exclusiones del proyecto.
- **`README.md`:** no lo sustituyas. Añade una sección corta "Desarrollo con IA" con el flujo de skills y un enlace a `CLAUDE.md` y `AGENTS.md`, en el idioma y el estilo del README (y en cada traducción que tenga).

## Modo con plan

Igual que el automático, pero antes del paso 2 me presentas un único plan y paras hasta que responda:

```markdown
## El proyecto
<Tres líneas: stack, cómo se compila y se prueba, qué CI tiene.>

## Lo que aplicaré
| Archivo | Qué haré |
| --- | --- |
| `CLAUDE.md` | <una línea> |

## Lo que necesito que decidas
1. <Conflicto>: (a) <opción por defecto> · (b) <alternativa>

Responde en un solo mensaje, por ejemplo: "ok, 1a, 2b".
```

## Modo paso a paso

Igual que el modo con plan, pero el plan solo lista los archivos y lo que propones para cada uno, y después vas **archivo por archivo**: me enseñas qué cambia (no el archivo entero), esperas mi visto bueno ("ok", "salta" o un cambio) y lo aplicas antes de pasar al siguiente. Los pasos 3 y 4 no cambian.

## Reglas

- **Nunca hagas commit ni push.** Te limitas a dejarme los comandos.
- **No borres nada del proyecto** (reglas, permisos, pasos de CI, exclusiones de Dependabot) sin que te lo pida.
- **No toques el código de la aplicación**, salvo cambios de formato.
- Si algo del informe no cuadra con lo que ves en el proyecto, dímelo en el resumen; solo paras si cambia lo que hay que hacer.
