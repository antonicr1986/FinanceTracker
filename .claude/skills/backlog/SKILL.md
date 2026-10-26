---
name: backlog
description: Convierte una idea de software o una funcionalidad hablada en un backlog de historias de usuario cortas y verticales. Sin criterios de aceptación ni implementación; eso es cosa de /story.
disable-model-invocation: true
---

# Backlog

Tu trabajo es ayudarme a dividir lo que te cuento en historias de usuario. Yo aporto los requisitos; tú los ordenas y los troceas.

## Conversación

1. Escucha la idea o la funcionalidad. Si algo no está claro, pregunta. Una pregunta cada vez, con tu respuesta sugerida.
2. Si es un proyecto nuevo, hablemos del stack. Solo del stack, no de la arquitectura ni de la implementación.
3. Pregunta si aplica alguna de estas decisiones caras de cambiar. Solo pregunta, no las diseñes:
   - Multi-tenant o multi-organización
   - Modelo de permisos o roles
   - Modo offline o sincronización
   - Multi-idioma o multi-moneda
4. Propón el backlog. Yo lo ajusto.

## Reglas

- **No inventes requisitos.** Divide solo lo que te he contado. Si ves un hueco, pregúntame; no lo rellenes.
- **Cada historia aporta valor por sí sola** y atraviesa todas las capas (datos, backend, UI). Nunca dividas por capas.
- **Nada de historias técnicas.** El actor nunca es el desarrollador ni el sistema. "Configurar la base de datos" o "crear el modelo de usuario" no son historias; esa infraestructura se construye dentro de la primera historia que la necesite.
- **Pocas historias.** 10–15 como máximo. Prefiero añadir que recortar.
- **Una línea por historia.** Sin criterios de aceptación, sin detalles técnicos, sin estimaciones ni prioridades.
- La primera historia es la más pequeña que se pueda usar de principio a fin (walking skeleton).

## Formato de salida

```markdown
## Contexto
<una o dos líneas: qué es y para quién>
Stack: <lo que hemos acordado>
Decisiones caras: <las que aplican, o "ninguna">

## Backlog
1. Como <actor>, quiero <acción>, para <beneficio>.
2. Como <actor>, quiero <acción>, para <beneficio>. Requiere: #1
```

Añade `Requiere: #N` solo cuando una historia no tenga sentido sin otra.

Al terminar, ofrece guardar el contexto y el stack en `CLAUDE.md` (o `AGENTS.md`). Las historias van a mi gestor de tareas; no las guardes en archivos salvo que te lo pida.
