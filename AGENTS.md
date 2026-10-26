# AGENTS.md

Las instrucciones de este proyecto para agentes de IA están en [`CLAUDE.md`](CLAUDE.md).
Léelo antes de empezar y síguelo como si estuviera escrito aquí; es la única fuente de verdad.
No dupliques ni modifiques instrucciones en este archivo: cualquier cambio va en `CLAUDE.md`.

## Flujo de trabajo con skills

Trabajamos con cuatro skills, en este orden:

```
/backlog  →  /story  →  /pair  →  /code-review
```

| Skill | Cuándo usarla | Archivo |
| --- | --- | --- |
| `/backlog` | Hay una idea o funcionalidad nueva y hay que dividirla en historias de usuario. | [`.claude/skills/backlog/SKILL.md`](.claude/skills/backlog/SKILL.md) |
| `/story` | Se va a construir una historia: añadir 2–3 criterios de aceptación y un mini plan. | [`.claude/skills/story/SKILL.md`](.claude/skills/story/SKILL.md) |
| `/pair` | La historia está refinada y aprobada: implementarla con TDD, un test cada vez. | [`.claude/skills/pair/SKILL.md`](.claude/skills/pair/SKILL.md) |
| `/code-review` | Los tests de la historia están en verde: revisar los cambios antes del commit. | [`.claude/skills/code-review/SKILL.md`](.claude/skills/code-review/SKILL.md) |
| `/integrar-kit` | Fuera del flujo: el kit se acaba de instalar en un proyecto que ya existía y hay que fusionar su configuración. | [`.claude/skills/integrar-kit/SKILL.md`](.claude/skills/integrar-kit/SKILL.md) |

Solo se usan cuando te las pido; no las lances por tu cuenta.
Si tu herramienta no ejecuta comandos con `/`, abre el `SKILL.md` correspondiente y sigue sus pasos a mano.

## Reglas que aplican siempre

- No inventes requisitos: si falta información, pregunta.
- Lo más simple que cumpla los criterios.
- Para y espera mi aprobación al final de cada fase.
- **Nunca hagas commit ni push.** Sugiere el mensaje (en inglés) y el commit lo hago yo.
