# Changelog

## [1.0.1] - 2026-09-19

### Atribución y configuración propia

- `LICENSE` incluye el aviso de copyright y licencia MIT de Clarion Assistant (ClarionLive), del que deriva `Services/EditorService.cs`. El archivo lleva además una cabecera con el origen, y el README lo menciona.
- El manifiesto `.addin` declara `author="asantarelli"` (antes decía `ClarionAssistant`).
- Configuración y protocolo predeterminado en `%APPDATA%\ClarionTransformer\` en lugar de `%APPDATA%\ClarionAssistant\`. La primera vez que se abre, el addin mueve `clarion-transformer.json` y `Protocolo_ClarionTransformer.md` de la carpeta anterior, así que no se pierden perfiles, API key ni protocolo.

## [1.0.0] - 2026-07-03

### Versión inicial

- Dos comandos de menú fijos, cada uno atado por nombre a su perfil: `Transformar a ABC (Claude)...` (perfil "Legacy -> ABC") y `Refactorizar (Claude)...` (perfil "Refactorizar").
- Transforma el texto seleccionado en el editor, o el `PROCEDURE` activo si no hay selección (detección por posición del cursor). Aplicación por posición exacta (línea/columna), no por búsqueda de texto — evita fallos cuando el mismo código aparece más de una vez en el archivo.
- Soporte de archivo de protocolo `.md` configurable por perfil.
- Opción **"Agregar comentarios explicativos"** por perfil: le pide a Claude que comente los bloques cuyo funcionamiento no sea obvio.
- Backup opcional por perfil: guarda el código original y el transformado como dos archivos `.clw` (`...-Old-...` / `...-New-...`) en `_ClarionTransformerBackups\` dentro de la solución Clarion abierta. Historial acumulativo, no sobrescribe backups anteriores.
- Fallback automático a `Protocolo_ClarionTransformer.md` en `%APPDATA%\ClarionAssistant\`; si no existe, avisa que hace falta configurar un protocolo.
- Diálogo de progreso con cancelación durante la llamada a la API.
- Manejo de errores de API con mensajes descriptivos.
- TLS 1.2 forzado para compatibilidad con .NET Framework en Clarion IDE.
- Protocolos de ejemplo incluidos: `Protocolo_LegacyABC.md` y `Protocolo_Refactorizar.md`.
