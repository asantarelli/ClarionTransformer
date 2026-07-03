================================================================================
PROTOCOLO CLARION - MIGRACION LEGACY -> ABC
================================================================================
Version: 1.0
Objetivo: convertir código Clarion "legacy" (acceso directo a archivos con
GET/PUT/ADD/DELETE, ERRORCODE()) al patrón ABC (Application Builder Classes)
usando los objetos Access:<Archivo> generados por el App Generator.

Aplica sobre selecciones parciales o procedimientos completos. Si el fragmento
no incluye la declaración local de variables, agregar solo las estrictamente
necesarias para que el bloque sea válido (ver sección 2).

================================================================================
1. PRINCIPIOS GENERALES
================================================================================

* No cambiar la lógica de negocio: la transformación es 1:1, no una reescritura
  funcional. Cada operación legacy tiene un equivalente ABC directo.
* Conservar comentarios existentes. Si una línea legacy se reemplaza por varias
  líneas ABC, mantener el comentario original junto a la primera.
* Conservar nombres de archivos, claves (KEY) y campos tal cual están escritos.
* No agregar manejo de errores adicional al que ya existía en el código legacy;
  simplemente traducirlo al equivalente ABC (ver sección 4).
* Usar los objetos Access:<Archivo> ya generados por el diccionario (no crear
  clases nuevas ni redeclarar los objetos ABC).

================================================================================
2. VARIABLE DE RESULTADO DE OPERACION
================================================================================

Toda operación ABC que se agregue debe capturar su resultado en una variable
LONG local llamada lResOp# (si el bloque no la tiene declarada, agregarla al
principio del bloque de datos con un comentario breve):

  lResOp#     LONG        !Resultado de operacion ABC

Si el código legacy ya usa alguna convención de variable de resultado propia,
respetarla en lugar de introducir lResOp#.

================================================================================
3. EQUIVALENCIAS DE OPERACIONES
================================================================================

GET(archivo, clave) -> Fetch
--------------------------------------------------------------------------------
  Legacy:
    GET(Clientes, Cli:CodigoKey)
    IF ERRORCODE()
      ! no encontrado
    END

  ABC:
    lResOp# = Access:Clientes.Fetch(Cli:CodigoKey)
    IF lResOp# <> Level:Benign
      ! no encontrado
    END

PUT(archivo) -> Update
--------------------------------------------------------------------------------
  Legacy:
    PUT(Clientes)

  ABC:
    lResOp# = Access:Clientes.Update()

ADD(archivo) -> Insert
--------------------------------------------------------------------------------
  Legacy:
    ADD(Clientes)

  ABC:
    lResOp# = Access:Clientes.Insert()

DELETE(archivo) -> Delete
--------------------------------------------------------------------------------
  Legacy:
    DELETE(Clientes)

  ABC:
    lResOp# = Access:Clientes.Delete()

================================================================================
4. MANEJO DE ERRORES
================================================================================

ERRORCODE() -> lResOp# <> Level:Benign
--------------------------------------------------------------------------------
  Legacy:
    GET(Clientes, Cli:CodigoKey)
    IF ERRORCODE()
      MESSAGE('No se encontro el cliente')
      RETURN
    END

  ABC:
    lResOp# = Access:Clientes.Fetch(Cli:CodigoKey)
    IF lResOp# <> Level:Benign
      MESSAGE('No se encontro el cliente')
      RETURN
    END

ERROR() -> ERROR() (se mantiene igual)
--------------------------------------------------------------------------------
* La función ERROR() (mensaje textual del último error) se mantiene igual, ya
  que sigue siendo válida sobre los objetos ABC.

Códigos de error especificos (ERRORCODE() = NN)
--------------------------------------------------------------------------------
* Comparar contra las constantes ABC en vez del número mágico. Ejemplos:
  - ERRORCODE() = 40  ->  lResOp# = Error:NoFind (registro no encontrado)
  - ERRORCODE() = 41  ->  lResOp# = Error:NoDup (duplicado en índice único)
  Si el código original usaba un número que no tiene equivalente claro,
  mantenerlo sin traducir y dejar un comentario "! TODO: revisar codigo error".

================================================================================
5. LOOPS: LOOP / GET / NEXT -> ESTRUCTURA ABC
================================================================================

Legacy:
  CLEAR(CLI:Record)
  CLI:Cod_Cliente = Cod_Cliente
  SET(CLI:CLI02,CLI:CLI02)
  LOOP
    NEXT(Clientes)
    IF ERRORCODE() THEN BREAK.
    IF CLI:Cod_Cliente <> Cod_Cliente THEN BREAK.
    ! procesar Cli:Record
  END

ABC:
  Clear(CLI:Record)
  CLI:Cod_Cliente = Cod_Cliente
  SET(CLI:CLI02,CLI:CLI02)
  Clientes{PROP:Where} = 'Cod_Cliente = ' & Cod_Cliente
  LOOP UNTIL  Access:Clientes.Next()
    ! procesar Cli:Record
  END

* Si el rango se arma manualmente con SET(archivo, clave, valor)..NEXT, preferir
  Access:<Archivo>.SetRange(campo, valor) + SET(clave) antes del LOOP, tal como
  en el ejemplo. Si el rango es más complejo (multi-campo), usar
  Access:<Archivo>.SetRange(campo1, valor1, campo2, valor2, ...) según aplique.

================================================================================
6. HOLD / RELEASE / OPEN / CLOSE
================================================================================

  HOLD(archivo)     -> Access:<Archivo>.TryHold()  (o .Hold() si se requiere que falle con SYSTEM abort)
  RELEASE(archivo)  -> Access:<Archivo>.Release()
  OPEN(archivo)     -> Access:<Archivo>.Open()  (normalmente ya manejado por el framework; no tocar si el open esta en un embed generado)
  CLOSE(archivo)    -> Access:<Archivo>.Close()

================================================================================
7. QUE NO TRANSFORMAR
================================================================================

* Declaraciones de archivos, claves y estructuras del diccionario (FILE, KEY,
  RECORD) — no son código de procedimiento.
* Llamadas a Access:<Archivo> que ya estén en formato ABC — dejarlas intactas.
* Código no relacionado con acceso a datos (UI, cálculos, formato de strings).

================================================================================
8. EJEMPLO COMPLETO
================================================================================

Antes:
  BuscarCliente PROCEDURE(LONG pCodigo)
    CODE
    Cli:Codigo = pCodigo
    GET(Clientes, Cli:CodigoKey)
    IF ERRORCODE()
      MESSAGE('Cliente no encontrado')
      RETURN
    END
    Cli:Activo = 1
    PUT(Clientes)

Despues:
  BuscarCliente PROCEDURE(LONG pCodigo)
    lResOp#     LONG
    CODE
    Cli:Codigo = pCodigo
    lResOp# = Access:Clientes.Fetch(Cli:CodigoKey)
    IF lResOp# <> Level:Benign
      MESSAGE('Cliente no encontrado')
      RETURN
    END
    Cli:Activo = 1
    lResOp# = Access:Clientes.Update()
