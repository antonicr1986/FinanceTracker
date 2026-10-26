---
name: pair
description: Pair programming con la IA sobre una historia ya refinada. La IA escribe código en pasos pequeños guiados por tests (un test → código mínimo) y se para a esperar mi visto bueno. Nunca hace commit.
disable-model-invocation: true
---

# Pair

Tú eres el driver: escribes el código. Yo soy el navigator: reviso, decido y te digo sí o no. Trabajamos una historia por sesión.

## 1. Revisión

- Lee la historia que te doy (idealmente el bloque generado por `/story`: criterios, plan, "Fuera de alcance").
- Contrasta el plan con el código actual. Si algo ya no encaja, avísame antes de empezar.
- Si no hay plan, propón uno de 5 puntos como máximo y espera mi aprobación.

## 2. Lista de tests

- A partir de los criterios de aceptación, propón **la lista de tests: solo los nombres, no los escribas todavía.**
- Indica **dónde** se prueba cada uno (endpoint, servicio, componente…). Prefiere el nivel más alto que sea práctico.
- Marca los criterios que no se pueden probar bien (por ejemplo, los visuales) como **verificación manual**.
- Espera mi aprobación. Esta lista es la especificación: la apruebo yo, no tú.

## 3. Ciclo

Un paso = un test de la lista:

1. Escribe el test.
2. Ejecútalo y **muestra que falla** (rojo).
3. Escribe el **código mínimo** que lo haga pasar. Nada que el test no exija.
4. Ejecuta los tests y muestra que pasan (verde).

Por defecto haces **1 paso** y te paras. Si te digo "haz N", haces N ciclos completos (test → código → verde, uno tras otro), nunca "N tests y luego el código".

Cuando te pares, envía un micro mensaje:

```
✅ Pasa "<nombre del test>". <qué he hecho en una línea> (<archivo:línea>)
Decidido: <decisión de criterio propio, si la hay>
¿Cambio algo o sigo con "<siguiente test>"?
```

En una tanda de N, muestra primero los tests y después el código: los tests son lo que más necesito revisar.

### Para inmediatamente (incluso a mitad de tanda) si

- Necesitas tomar una decisión de diseño que no está en el plan.
- Se te ocurre añadir algo que no he pedido, aunque parezca razonable.
- Hacer pasar un test requeriría cambiar, saltar o borrar un test.
- Llevas dos intentos sin llegar a verde.
- El paso supera ~50 líneas o toca más de 3 archivos: propón dividirlo.

### Reglas de los tests

- El valor esperado sale del criterio de aceptación, **nunca se calcula con la misma lógica que el código**.
- Prueba el comportamiento a través de la interfaz pública, no detalles internos.

## 4. Comprobación

Cuando la lista esté en verde:

- Verifiquemos juntos los criterios marcados como **verificación manual**.
- Sugiéreme ejecutar `/code-review` (revisa con un contexto limpio; no te revises a ti mismo).
- Si algo se repite o se ve desordenado, **sugiere** el refactor; yo decido. Refactoriza solo con los tests en verde, y nunca mezclado con funcionalidad nueva.
- Si hemos tomado una decisión que aplica a todo el proyecto, propón una línea para `CLAUDE.md`.

## 5. Commit

**Nunca hagas commit.** Cuando haya un buen punto (tests en verde, una unidad lógica), avísame y sugiere el mensaje, separando funcionalidad y refactor en commits distintos. El commit lo hago yo.
