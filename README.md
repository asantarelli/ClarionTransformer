# ClarionTransformer

Addin para el IDE de **Clarion 11/12** que transforma código Clarion usando la API de Claude (Anthropic AI).

Toma el bloque de código seleccionado en el editor —o el procedimiento completo si no hay selección— y le pide a Claude que lo transforme según las reglas de un archivo de protocolo `.md` definido por el usuario. Trae dos comandos de menú fijos: **Transformar a ABC** (perfil "Legacy -> ABC") y **Refactorizar** (perfil "Refactorizar"), cada uno con su propio protocolo — sin necesidad de cambiar de perfil activo entre uno y otro.

---

## Características

- Transforma el **texto seleccionado** en el editor, o el **PROCEDURE activo** si no hay selección
- Dos comandos de menú fijos: **Transformar a ABC** y **Refactorizar**, cada uno con su perfil y protocolo propios
- Aplica el resultado directamente en el editor, sin copiar y pegar
- Utiliza un **archivo de protocolo personalizable** (`.md`) que define las reglas de transformación
- Soporte de **múltiples perfiles**: uno por tipo de transformación o por proyecto
- Opción **"Agregar comentarios explicativos"** por perfil: le pide a Claude que comente los bloques cuyo funcionamiento no sea obvio
- Backup opcional del código original y transformado (`Old`/`New`), guardado dentro de la solución activa, configurable por perfil
- Fallback automático al archivo `Protocolo_ClarionTransformer.md` en `%APPDATA%\ClarionTransformer\`
- Instrucciones adicionales por perfil (campo libre de texto)
- Compatible con Clarion 11 y Clarion 12

---

## Requisitos

- Clarion 11 o 12 instalado
- Cuenta en [Anthropic Console](https://console.anthropic.com) con créditos de API
- API Key de Anthropic (`sk-ant-...`)

---

## Instalación

1. Descargar `ClarionTransformer.dll` y `ClarionTransformer.addin` de [Releases](../../releases)
2. Copiar ambos archivos a:
   ```
   <ClarionRoot>\accessory\addins\ClarionTransformer\
   ```
3. Reiniciar el Clarion IDE
4. El menú **Tools** mostrará las opciones:
   - `Transformar a ABC (Claude)...`
   - `Refactorizar (Claude)...`
   - `ClarionTransformer - Configuracion...`

---

## Configuración

Ir a **Tools → ClarionTransformer - Configuracion...**

El addin viene con dos perfiles predefinidos, **"Legacy -> ABC"** y **"Refactorizar"**, cada uno asociado a su propio comando de menú. Elegí el perfil en el combo de arriba para editar su configuración — no hace falta "activarlo": cada comando de menú siempre usa el perfil con ese nombre exacto.

| Campo | Descripción |
|-------|-------------|
| **API Key** | Tu clave de Anthropic (global, todos los perfiles) |
| **Modelo** | Modelo de Claude a usar (recomendado: `claude-sonnet-4-6`) |
| **Archivo de protocolo** | Ruta al `.md` con las reglas de transformación (por perfil) |
| **Backup Old/New** | Si se guardan dos archivos `.clw` (código original y transformado) por perfil |
| **Agregar comentarios explicativos** | Si Claude debe comentar los bloques cuyo funcionamiento no sea obvio (por perfil) |
| **Instrucciones adicionales** | Reglas extra en texto libre (por perfil) |

> Si creás un perfil nuevo con otro nombre, no vas a tener un comando de menú fijo para él — se usa solo si lo dejás como "perfil activo" y en el futuro se agrega un comando genérico para eso.

### Archivo de protocolo

El archivo de protocolo es un `.md` de texto libre que describe las reglas que Claude debe aplicar al transformar el código. Incluye dos ejemplos completos en este repositorio:

- [protocolo/Protocolo_LegacyABC.md](protocolo/Protocolo_LegacyABC.md) — migración de acceso directo a archivos (GET/PUT/ADD/DELETE) al patrón ABC
- [protocolo/Protocolo_Refactorizar.md](protocolo/Protocolo_Refactorizar.md) — refactorización de código ABC existente (errores, naming, código muerto, modernización)

Si no configurás un archivo de protocolo, el addin busca automáticamente:
```
%APPDATA%\ClarionTransformer\Protocolo_ClarionTransformer.md
```
Si tampoco existe, el addin avisa que hace falta configurar un protocolo antes de transformar.

---

## Uso

1. Abrir un archivo `.clw` en el editor del IDE
2. **Con selección:** seleccionar el bloque de código a transformar
   **Sin selección:** ubicar el cursor dentro del `PROCEDURE` a transformar
3. Ir a **Tools → Transformar a ABC (Claude)...** o **Tools → Refactorizar (Claude)...**, según lo que necesites
4. Claude transforma el código y aplica el resultado directamente en el editor
5. Revisar el resultado y guardar con `Ctrl+S`

> **Nota:** Si el perfil activo tiene el backup habilitado, se guardan dos archivos (`NombreProcedimiento-Old-fecha-hora.clw` y `...-New-...clw`) en `_ClarionTransformerBackups\` dentro de la solución Clarion abierta, antes de aplicar la transformación. Aun así, se recomienda tener el archivo bajo control de versiones.

---

## Compilar desde fuente

Requiere Visual Studio 2019 o superior con .NET Framework 4.7.2.

```bash
msbuild ClarionTransformer\ClarionTransformer.csproj /p:Configuration=Debug /p:ClarionRoot="D:\Clarion11"
```

Antes de compilar por primera vez, copiar la plantilla del addin:
```bash
copy ClarionTransformer\ClarionTransformer.addin.template ClarionTransformer\ClarionTransformer.addin
```

---

## Licencia

MIT License — ver [LICENSE](LICENSE)

`Services/EditorService.cs` deriva de [Clarion Assistant](https://github.com/ClarionLive/ClarionAssistant) (Copyright (c) 2025-2026 ClarionLive, MIT). Su aviso de copyright y licencia se conserva en [LICENSE](LICENSE).

---

## Contribuciones

Los PRs son bienvenidos. Si tenés un archivo de protocolo interesante para compartir, podés agregarlo en la carpeta `protocolo/`.
