# Guía de uso — ClarionTransformer

## Índice

1. [Instalación](#instalación)
2. [Configuración inicial](#configuración-inicial)
3. [Uso cotidiano](#uso-cotidiano)
4. [El archivo de protocolo](#el-archivo-de-protocolo)
5. [Perfiles](#perfiles)
6. [Backup automático](#backup-automático)
7. [Solución de problemas](#solución-de-problemas)
8. [Preguntas frecuentes](#preguntas-frecuentes)

---

## Instalación

### Paso 1 — Obtener una API Key de Anthropic

1. Crear una cuenta en [console.anthropic.com](https://console.anthropic.com)
2. Ir a **Billing** y cargar créditos (mínimo $5 USD)
3. Ir a **API Keys** → **Create Key**
4. Copiar la clave (empieza con `sk-ant-...`) — se muestra una sola vez

### Paso 2 — Instalar el addin

1. Descargar `ClarionTransformer.dll` y `ClarionTransformer.addin` desde [Releases](../../releases)
2. Crear la carpeta (si no existe):
   ```
   C:\Clarion11\accessory\addins\ClarionTransformer\
   ```
3. Copiar los dos archivos descargados a esa carpeta
4. Reiniciar el IDE de Clarion

Verificar que aparezcan en **Tools**:
- `Transformar a ABC (Claude)...`
- `Refactorizar (Claude)...`
- `ClarionTransformer - Configuracion...`

---

## Configuración inicial

Ir a **Tools → ClarionTransformer - Configuracion...**

### Perfiles fijos: "Legacy -> ABC" y "Refactorizar"

El addin trae dos perfiles predefinidos, uno por cada comando de menú:

- **Legacy -> ABC** — usado por `Tools → Transformar a ABC (Claude)...`
- **Refactorizar** — usado por `Tools → Refactorizar (Claude)...`

Elegí el perfil en el combo de arriba de la configuración para editar **su** protocolo, sus instrucciones adicionales, el backup y el checkbox de comentarios — cada comando de menú siempre usa el perfil con ese nombre exacto, no hace falta "activarlo" antes de usarlo. Si renombrás o borrás alguno de los dos, el comando correspondiente deja de funcionar hasta que vuelvas a crear un perfil con ese nombre exacto.

### API Key

Pegar la clave de Anthropic en el campo **API Key**. Se almacena en:
```
%APPDATA%\ClarionTransformer\clarion-transformer.json
```
No se envía a ningún lugar más que a la API de Anthropic.

### Modelo

El modelo predeterminado es `claude-sonnet-4-6`, que ofrece la mejor calidad para esta tarea.

| Modelo | Velocidad | Calidad | Costo |
|--------|-----------|---------|-------|
| `claude-sonnet-4-6` | Normal (~10s) | Alta | Medio |
| `claude-haiku-4-5-20251001` | Rápido (~3s) | Media | Bajo |

### Archivo de protocolo

Hacer clic en `...` para seleccionar tu archivo `.md` de protocolo, o escribir la ruta directamente.

Si no configurás ninguno, el addin busca automáticamente:
```
%APPDATA%\ClarionTransformer\Protocolo_ClarionTransformer.md
```
Si tampoco existe ese archivo, el addin muestra un aviso y no ejecuta la transformación — hace falta configurar un protocolo antes de poder usar el comando. Podés copiar alguno de los [ejemplos incluidos](protocolo/) a esa ubicación como punto de partida.

### Backup Old/New

El checkbox **Guardar codigo original y transformado (Old/New) antes de aplicar** guarda, antes de aplicar la transformación, dos archivos `.clw` en `_ClarionTransformerBackups\` dentro de la carpeta de la solución Clarion abierta:

- `NombreProcedimiento-Old-yyyyMMdd-HHmmss.clw` — el bloque exactamente como estaba antes de transformar
- `NombreProcedimiento-New-yyyyMMdd-HHmmss.clw` — el resultado que devolvió Claude

`NombreProcedimiento` es el procedimiento detectado en la posición del cursor (o `Seleccion` si no se pudo determinar). Se recomienda mantenerlo activado, especialmente en migraciones Legacy → ABC.

### Agregar comentarios explicativos

El checkbox **Agregar comentarios explicativos en bloques complejos** le pide a Claude que, además de aplicar el protocolo, agregue comentarios (`!`) donde el funcionamiento del código no sea evidente a simple vista — sin tocar el resto del comentado ni comentar líneas triviales. Útil sobre todo en **Refactorizar**, para documentar lógica que quedó sin explicar.

---

## Uso cotidiano

### Transformar una selección

1. Abrir el archivo `.clw` en el editor
2. Seleccionar el bloque de código a transformar (puede ser una línea, un `IF`, un `LOOP`, o varios procedimientos)
3. Ir a **Tools → Transformar a ABC (Claude)...** (migración Legacy→ABC) o **Tools → Refactorizar (Claude)...** (limpieza de código ABC), según lo que necesites
4. Esperar la respuesta de Claude (diálogo de progreso con botón Cancelar)
5. Revisar el resultado en el editor
6. Si está conforme, guardar con `Ctrl+S`
7. Si no está conforme, deshacer con `Ctrl+Z`

### Transformar el procedimiento activo

1. Abrir el archivo `.clw` en el editor
2. Ubicar el cursor en cualquier línea dentro del `PROCEDURE` que querés transformar (sin seleccionar texto)
3. Ir a **Tools → Transformar a ABC (Claude)...** o **Tools → Refactorizar (Claude)...**
4. El addin detecta automáticamente el bloque `PROCEDURE` completo (desde la declaración hasta el `PROCEDURE` siguiente o el fin del archivo) y lo envía a Claude

> **Consejo:** Antes de transformar por primera vez un archivo importante, guardarlo (`Ctrl+S`) para tener un punto de restauración además del backup Old/New.

---

## El archivo de protocolo

El archivo de protocolo es el corazón del addin. Es un archivo de texto (`.md`) que contiene las reglas que Claude debe seguir al transformar el código.

### ¿Qué puede incluir?

- Equivalencias de sintaxis (ej. `GET/PUT/ADD/DELETE` → métodos `Access:<Archivo>`)
- Convenciones de manejo de errores
- Reglas de naming
- Qué código eliminar (código muerto, patrones obsoletos)
- Ejemplos de antes/después
- Qué NO tocar (declaraciones de archivos, firmas de procedimientos, etc.)
- Cualquier otra convención de tu equipo

### ¿Cómo editarlo?

Desde la configuración del addin, hacer clic en **Editar** — esto abre el archivo en el editor predeterminado del sistema.

También podés editarlo con cualquier editor de texto: Notepad, VS Code, Notepad++, etc.

Los cambios se aplican en la próxima transformación, sin reiniciar el IDE.

### Ejemplos incluidos

- [protocolo/Protocolo_LegacyABC.md](protocolo/Protocolo_LegacyABC.md) — migración Legacy → ABC: `GET/PUT/ADD/DELETE`, loops `LOOP/GET/NEXT`, manejo de `ERRORCODE()`.
- [protocolo/Protocolo_Refactorizar.md](protocolo/Protocolo_Refactorizar.md) — refactorización de código ABC existente: errores, naming, código muerto, modernización de patrones.

---

## Perfiles

Cada perfil agrupa protocolo, instrucciones adicionales, backup y comentarios. Los dos que trae el addin ("Legacy -> ABC" y "Refactorizar") están atados por nombre a sus respectivos comandos de menú — no hace falta "activar" uno para usarlo, cada comando siempre usa el suyo.

### Editar un perfil existente

1. Abrir la configuración
2. Elegir "Legacy -> ABC" o "Refactorizar" en el combo de arriba
3. Ajustar su protocolo, instrucciones adicionales, backup y comentarios
4. Guardar

### Crear un perfil adicional

1. Abrir la configuración
2. Hacer clic en **Nuevo**
3. Asignar un nombre (ej: "SDGI4 completo")
4. Seleccionar el archivo de protocolo correspondiente y configurar backup/comentarios

Un perfil nuevo con otro nombre no tiene un comando de menú fijo propio — por ahora solo "Legacy -> ABC" y "Refactorizar" están conectados a Tools. Si necesitás un tercer comando de menú para un perfil propio, hay que agregarlo al código del addin (avisale a quien lo mantiene).

### ¿Para qué usar perfiles adicionales?

- Distintos proyectos con distintas convenciones dentro del mismo tipo de transformación
- Variantes de "Refactorizar" más estrictas o más permisivas según el estado del proyecto

---

## Backup automático

Cuando el perfil activo tiene el backup habilitado, cada transformación guarda el bloque original y el transformado como dos archivos `.clw` separados (`...-Old-...` / `...-New-...`, con fecha y hora) en `_ClarionTransformerBackups\` dentro de la solución Clarion abierta. Cada transformación genera un par nuevo (con su propio timestamp), así que es un historial acumulativo — no se sobrescriben backups anteriores.

Si no hay una solución abierta en el IDE (o no se puede determinar su carpeta), el addin avisa que no pudo generar el backup pero igual aplica la transformación.

---

## Solución de problemas

### El menú no aparece en Tools

- Verificar que los archivos `.dll` y `.addin` estén en la carpeta correcta de addins
- Verificar que el nombre de la carpeta coincida con el nombre del addin
- Reiniciar completamente el IDE

### Error "API key no configurada"

- Ir a la configuración y verificar que la API Key esté cargada
- Verificar que la clave empiece con `sk-ant-`

### Error de API (401 Unauthorized)

- La API Key es incorrecta o fue revocada
- Generar una nueva desde [console.anthropic.com](https://console.anthropic.com)

### Error de API (529 / Overloaded)

- La API de Anthropic está temporalmente sobrecargada
- Reintentar en unos minutos

### "No hay texto seleccionado y no se encontro un PROCEDURE activo"

- Verificar que el cursor esté dentro de un `PROCEDURE` (después de su línea de declaración)
- Los procedimientos deben declararse sin indentación (`NombreProcedimiento PROCEDURE`) para que el addin los detecte

### El resultado tiene errores de compilación en Clarion

- Revisar el archivo de protocolo — puede estar indicando cambios que no aplican al código transformado
- Agregar instrucciones más precisas en **Instrucciones adicionales** del perfil
- Usar `Ctrl+Z` para deshacer, o restaurar desde el `.bak` si ya se guardó el archivo

---

## Preguntas frecuentes

**¿Mis archivos de código se envían a Anthropic?**

Solo el bloque seleccionado (o el procedimiento activo) se envía a la API junto con el contenido del archivo de protocolo. No se envía el resto del archivo ni otros archivos del proyecto.

**¿Cuánto cuesta cada transformación?**

Depende del tamaño del bloque y del protocolo. Aproximadamente:
- Con `claude-sonnet-4-6`: ~$0.002 a $0.02 por transformación
- Con `claude-haiku-4-5-20251001`: ~$0.0002 a $0.002 por transformación

**¿Puedo compartir mi archivo de protocolo con el equipo?**

Sí, es un archivo de texto plano. Podés versionarlo en tu repositorio de código o compartirlo por cualquier medio. Cada integrante del equipo lo configura en su perfil local.

**¿Funciona con Clarion 12?**

Sí, el addin es compatible con Clarion 11 y 12.
