<!--
Plantilla de PR. GitHub la carga automáticamente al abrir un PR.
Los comentarios como este no se ven en el PR publicado.

¿No es una historia (configuración, dependencias, mantenimiento)? Sustituye la sección
"Historia" por una o dos líneas con qué cambia y por qué, y en el checklist deja solo
la sección "Proyecto".
-->

## Historia

<!-- "Closes #N" enlaza la issue y GitHub la cierra al fusionar el PR. -->
Closes #<número de la issue>

<!-- Una o dos líneas: qué cambia, desde el punto de vista del usuario. -->
<resumen>

## Cómo probarlo

<!-- Pasos para verificarlo a mano, sobre todo los criterios marcados como "verificación manual" en /pair. -->

1. <paso>
2. <paso>

## Checklist

<!-- Marca cada casilla con [x]. Si alguna no aplica, táchala con ~~texto~~ y explica por qué. -->

### Historia (`/story`)

- [ ] Todos los criterios de aceptación están cubiertos, por un test o por verificación manual.
- [ ] No se ha colado nada de "Fuera de alcance".

### Implementación (`/pair`)

- [ ] Los tests se escribieron antes que el código y no se ha cambiado ninguno para que pase.
- [ ] Funcionalidad y refactor van en commits separados.

### Revisión (`/code-review`)

- [ ] Pasado sin hallazgos bloqueantes.

### Proyecto

- [ ] `CLAUDE.md` o un ADR (`docs/decisions/`) actualizados si se tomó una decisión que afecta a todo el proyecto.
- [ ] Sin secretos ni datos sensibles en el código, la configuración o los logs.

## Notas para quien revisa

<!-- Opcional: decisiones tomadas, dudas o zonas donde conviene mirar con más atención. -->
