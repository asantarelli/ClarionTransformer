================================================================================
PROTOCOLO CLARION - REFACTORIZACION DE CODIGO ABC
================================================================================
Version: 1.0
Objetivo: mejorar código Clarion que YA usa el patrón ABC, sin cambiar su
comportamiento observable. Aplica sobre selecciones parciales o procedimientos
completos.

================================================================================
1. PRINCIPIOS GENERALES
================================================================================

* Refactorizar, no reescribir: el comportamiento funcional debe ser idéntico
  antes y después. No agregar features, no cambiar la lógica de negocio.
* Cambios permitidos: estructura, legibilidad, manejo de errores, naming,
  eliminación de código muerto, modernización de patrones ABC.
* Si una mejora es dudosa o cambia comportamiento, NO aplicarla — dejar el
  código original y, si corresponde, un comentario "! TODO: revisar".
* Conservar toda lógica de validación y mensajes de usuario exactamente igual
  (mismo texto, mismas condiciones).

================================================================================
2. MANEJO DE ERRORES
================================================================================

* Toda llamada a un método Access:<Archivo> que devuelva un resultado (Fetch,
  Update, Insert, Delete, Next, Previous) debe capturar el valor en una
  variable LONG y verificarlo antes de continuar con lógica que dependa de
  que la operación haya tenido éxito.
* Patrón estándar:
    lResOp# = Access:<Archivo>.<Metodo>()
    IF lResOp# <> Level:Benign
      ! manejo de error
    END
* Si el código ignora el resultado de una operación que puede fallar de forma
  esperable (ej. Fetch que puede no encontrar registro), agregar la
  verificación correspondiente en vez de dejarla silenciosa.
* No envolver en manejo de errores las operaciones que el patrón ABC ya
  resuelve internamente (por ejemplo, dentro de un embed generado que ya
  tiene su propio ErrorTrap).

================================================================================
3. NAMING CONVENTIONS
================================================================================

* Variables locales temporales: prefijo "l" + nombre descriptivo, sufijo "#"
  para LONG/numéricas de trabajo (ej. lResOp#, lContador#).
* Strings de trabajo: prefijo "l" + nombre descriptivo + "$" (ej. lMensaje$).
* No renombrar variables que sean parámetros de PROCEDURE, campos de archivo
  del diccionario, ni variables globales — solo variables de trabajo locales
  claramente temporales dentro del bloque a transformar.
* Si renombrar una variable local requiere tocar líneas fuera del bloque
  seleccionado (por ejemplo, la variable se declara fuera del fragmento
  enviado), NO renombrarla — dejarla como está.

================================================================================
4. ELIMINACION DE CODIGO MUERTO
================================================================================

* Eliminar:
  - Líneas comentadas que sean código viejo abandonado (no documentación).
  - Variables locales declaradas pero nunca usadas dentro del bloque.
  - Bloques IF/CASE con condiciones que nunca pueden cumplirse (constantes
    contradictorias evidentes).
* NO eliminar:
  - Comentarios explicativos, aunque parezcan redundantes.
  - Código aparentemente no usado si podría ser invocado desde fuera del
    fragmento visible (por ejemplo, una ROUTINE referenciada por nombre).

================================================================================
5. MODERNIZACION DE PATRONES
================================================================================

* Reemplazar comparaciones redundantes con booleanos:
    IF lFlag# = TRUE   ->  IF lFlag#
    IF lFlag# = FALSE  ->  IF NOT lFlag#
* Preferir CASE sobre cadenas largas de IF/ELSIF cuando se compara la misma
  variable contra múltiples valores constantes.
* Usar CLEAR(campo) en vez de asignaciones manuales a cero/vacío cuando aplica
  a toda la estructura (record, group).
* Preferir Access:<Archivo>.SetRange(...) + SET(clave) sobre construir el
  rango manualmente campo por campo con CLEAR + asignación, cuando el
  resultado es equivalente.

================================================================================
6. ESTRUCTURA Y LEGIBILIDAD
================================================================================

* Indentación: 2 espacios por nivel, consistente con el resto del bloque.
* Una instrucción por línea (no usar ";" para compactar múltiples sentencias
  salvo que el original ya lo haga y el patrón sea IF ... THEN BREAK.).
* Separar con una línea en blanco los bloques lógicos distintos dentro de un
  mismo procedimiento (ej. validación / acceso a datos / actualización UI).

================================================================================
7. QUE NO TOCAR
================================================================================

* Firma del PROCEDURE (nombre, parámetros, tipo de retorno).
* Declaraciones de archivos, claves y estructuras del diccionario.
* Código generado automáticamente dentro de secciones claramente marcadas
  como "!Generated" o embebidos del App Generator, salvo que el usuario haya
  seleccionado explícitamente esa sección.

================================================================================
8. EJEMPLO
================================================================================

Antes:
  ActualizarStock PROCEDURE(LONG pCodigo, LONG pCantidad)
    x       LONG
    CODE
    Art:Codigo = pCodigo
    Access:Articulos.Fetch(Art:CodigoKey)
    x = TRUE
    IF x = TRUE
      Art:Stock = Art:Stock + pCantidad
      Access:Articulos.Update()
    END

Despues:
  ActualizarStock PROCEDURE(LONG pCodigo, LONG pCantidad)
    lResOp#     LONG
    CODE
    Art:Codigo = pCodigo
    lResOp# = Access:Articulos.Fetch(Art:CodigoKey)
    IF lResOp# = Level:Benign
      Art:Stock = Art:Stock + pCantidad
      lResOp# = Access:Articulos.Update()
    END
