---
name: integrar-kit
description: Integra el ai-project-kit en un proyecto que ya existía. Lee .claude/integrar-kit/INTEGRACION.md (lo genera instalar.ps1 en modo proyecto existente), aplica lo que tiene una respuesta obvia, te pregunta solo los conflictos reales en un único plan y te deja un comando para commit, push y PR. Con "paso a paso" va archivo por archivo. Nunca hace commit.
disable-model-invocation: true
---

# Integrar el kit en un proyecto existente

Eres quien integra el kit en un proyecto que ya tenía su forma de trabajar. El proyecto manda: el kit se adapta a él, no al revés. Aplicas lo que tiene una respuesta razonable obvia y me preguntas solo lo que de verdad tengo que decidir yo.

**Modo:** si al invocarte escribo "paso a paso", sigue el [modo paso a paso](#modo-paso-a-paso). Si no, el modo rápido (por defecto).

## Entrada

1. **`.claude/integrar-kit/INTEGRACION.md`**: los archivos que fusionar (estado y qué vigilar), los pipelines del proyecto, la versión del kit y lo que **ya hizo el instalador** (rama, archivos copiados, `.gitignore`, etiquetas): no lo rehagas ni lo investigues. Si no existe, para y dime que ejecute primero el instalador (`instalar.ps1 <ruta>`).
2. **`.claude/integrar-kit/kit/`**: la versión del kit de cada archivo de la tabla.
3. **El proyecto**: su `CLAUDE.md`, el README, los pipelines, cómo se compila y se prueba, y los últimos commits (`git log --oneline -20`) para conocer el idioma y el estilo de los mensajes.

## Paso 1: analizar y medir

Lee todo lo anterior. Antes de proponer nada, mide lo que puede cambiar una decisión:

- **Cobertura, igual que el CI.** Busca en el pipeline del proyecto el comando de tests y el script o paso que calcula la cobertura, y reprodúcelo tal cual (mismos parámetros, mismo script, mismos archivos incluidos). No la midas a tu manera: el CI puede contar archivos que tú no (por ejemplo, el punto de entrada sin tests). Si no tiene medición en el CI, usa la del kit (`dotnet test --collect:"XPlat Code Coverage"`, todas las líneas).
- **Formato con el `.editorconfig` del kit.** Cópialo temporalmente al proyecto, ejecuta `dotnet format --verify-no-changes` (o el formateador del proyecto), anota cuántos archivos y avisos por regla saldrían, y **quítalo**. Si alguna regla choca con una convención clara del proyecto o del lenguaje, prepara el ajuste.

## Paso 2: un único plan

Preséntamelo en este formato y para hasta que responda:

```markdown
## El proyecto
<Tres líneas: stack, cómo se compila y se prueba, qué CI tiene.>

## Lo que aplicaré
| Archivo | Qué haré |
| --- | --- |
| `CLAUDE.md` | <una línea> |

## Lo que necesito que decidas
1. <Conflicto>: (a) <opción recomendada> · (b) <alternativa>
2. ...

Responde en un solo mensaje, por ejemplo: "ok, 1a, 2b".
```

**Van a "Lo que aplicaré"** (sin preguntar), siguiendo las reglas de cada archivo de abajo: fusionar `CLAUDE.md` y `AGENTS.md`, unir permisos, sustituir skills de una versión anterior del kit sin cambios propios, Dependabot, la sección del README, el `.editorconfig` si no cambia ningún archivo, y la cobertura mínima con la regla de abajo.

**Van a "Lo que necesito que decidas"** solo los conflictos reales, cada uno con una opción recomendada:

- **Commits de la IA:** si `CLAUDE.md` o `settings.json` del proyecto dejan a la IA hacer commit o push y el kit lo prohíbe. Recomienda la regla del kit.
- **`.editorconfig` que cambiaría archivos:** cuántos y por qué reglas. Recomienda adoptarlo ajustando las reglas que chocan con las convenciones del proyecto, para no tocar su código; si aun así cambia algo mecánico (un salto de línea final, por ejemplo), en un commit aparte.
- **Cobertura mínima:** solo si el proyecto ya exigía un mínimo distinto. Si no exigía ninguno, aplica la regla sin preguntar.
- **Skills con cambios propios del proyecto:** enséñame la diferencia.
- Cualquier otra contradicción entre el proyecto y el kit que no resuelvan las reglas de abajo.

Si no hay ningún conflicto, dilo y pide solo un "ok".

## Paso 3: aplicar y verificar

Aplica todo lo aprobado y después:

- Compila y ejecuta los tests con los comandos del proyecto; comprueba el formato y la cobertura **igual que el CI**, con el mínimo nuevo.
- `git status` y `git diff --stat`: que no haya nada fuera del plan.
- Si algo falla, explícame qué y propón cómo arreglarlo antes de cerrar.

## Paso 4: cerrar

1. Escribe en `.claude/kit-version` la versión que indica `INTEGRACION.md`.
2. Escribe la descripción del PR en `.git/INTEGRACION-PR.md` (dentro de `.git/` nunca se sube): qué se ha integrado, las decisiones tomadas, cómo probarlo y el resultado de la verificación, siguiendo la plantilla de PR del proyecto como PR que no es una historia.
3. Borra la carpeta temporal `.claude/integrar-kit/`. **No borres la skill** (`.claude/skills/integrar-kit/`): se queda para futuras actualizaciones del kit.
4. Dame el resumen y **un único bloque de comandos** para copiar, en el idioma y el estilo de los commits del proyecto. Un solo commit con todo, salvo que hayamos decidido un commit aparte de formato; en ese caso, el de formato primero:

```bash
git add -A
git commit -m "chore: integrate ai-project-kit <versión>"
git push -u origin <rama>
gh pr create --base <rama principal> --title "chore: integrate ai-project-kit <versión>" --body-file .git/INTEGRACION-PR.md
```

Si `gh` no está instalado o no tiene sesión (`gh auth status`), sustituye la última línea por el enlace para abrir el PR en la web y dime que pegue como descripción el contenido de `.git/INTEGRACION-PR.md`.

## Reglas de cada archivo

- **`CLAUDE.md`:** mantén todas las reglas y secciones del proyecto. Añade solo las del kit que falten (flujo de skills, reglas de CI con los valores reales del proyecto, actions fijadas por SHA...), adaptadas a lo que ya dice. No inventes contenido para secciones que no puedes rellenar con datos reales.
- **`AGENTS.md`:** debe seguir remitiendo a `CLAUDE.md` como única fuente. Añade las skills que falten a su tabla. **Mantén las reglas críticas que el kit repite ahí** (nunca commit ni push, no inventar requisitos): otras herramientas de IA (Copilot, Aider…) no aplican `settings.json` y no siempre siguen el enlace a `CLAUDE.md`, así que para ellas es la única protección.
- **`.claude/settings.json`:** une los permisos de los dos. Si chocan, gana el más restrictivo. Nunca quites un `deny` del proyecto. No añadas permisos de herramientas que el proyecto no usa (npm sin front, por ejemplo) y sí los de las que usa y el kit no conoce (por ejemplo, los tests de Android con Gradle).
- **Skills:** si la del proyecto es una versión anterior del kit sin cambios propios, sustitúyela.
- **`.editorconfig`:** según lo medido en el paso 1.
- **CI:** si el proyecto ya tiene pipelines, **no los sustituyas ni añadas otro que haga lo mismo**: añade a los existentes solo los controles del kit que falten (secretos con gitleaks y SHA-256, formato, cobertura mínima). Si no tiene CI, copia el del kit y ajusta sus variables. Las actions nuevas, fijadas por SHA con comentario de versión.
- **Cobertura mínima:** la medida en el paso 1 **menos 5 puntos, redondeada hacia abajo a un múltiplo de 5** (56 % → 50 %), para que el CI no falle de golpe con el primer cambio sin tests. Pon el mismo valor en el pipeline y en `CLAUDE.md`.
- **`.github/dependabot.yml`:** añade los ecosistemas que falten. Respeta la frecuencia, los grupos y las exclusiones del proyecto.
- **`README.md`:** no lo sustituyas. Añade una sección corta "Desarrollo con IA" con el flujo de skills y un enlace a `CLAUDE.md` y `AGENTS.md`.

## Modo paso a paso

Igual que el modo rápido, pero en el paso 2 el plan solo lista los archivos y lo que propones para cada uno, y después vas **archivo por archivo**: me enseñas qué cambia (no el archivo entero), esperas mi visto bueno ("ok", "salta" o un cambio) y lo aplicas antes de pasar al siguiente. Los pasos 3 y 4 no cambian.

## Reglas

- **Nunca hagas commit ni push.** Te limitas a dejarme los comandos.
- **No borres nada del proyecto** (reglas, permisos, pasos de CI, exclusiones de Dependabot) sin que te lo pida.
- **No toques el código de la aplicación**, salvo cambios de formato aprobados.
- Si algo del informe no cuadra con lo que ves en el proyecto, dímelo en lugar de suponer.
