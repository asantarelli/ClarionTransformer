using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ICSharpCode.Core;
using ClarionTransformer.Dialogs;
using ClarionTransformer.Services;

namespace ClarionTransformer.Commands
{
    /// <summary>
    /// Base comun para los comandos de menu que transforman codigo con un perfil
    /// fijo (identificado por nombre) en vez del "perfil activo" configurable.
    /// Toma la seleccion activa del editor (o, si no hay seleccion, el PROCEDURE
    /// donde esta el caret), la envia a Claude junto con el protocolo del perfil,
    /// y aplica el resultado directo en el editor.
    /// </summary>
    public abstract class AbstractProfileTransformCommand : AbstractMenuCommand
    {
        private const string BackupSubfolder = "_ClarionTransformerBackups";

        /// <summary>Nombre exacto del perfil (TransformerProfile.ProfileName) que este comando ejecuta.</summary>
        protected abstract string ProfileName { get; }

        public override void Run()
        {
            try
            {
                var settings = TransformerProfileService.Load();
                var profile  = TransformerProfileService.GetProfileByName(ProfileName);

                if (profile == null)
                {
                    MessageBox.Show(
                        "No se encontro el perfil \"" + ProfileName + "\".\n\n" +
                        "Creálo desde Tools > ClarionTransformer - Configuracion (botón Nuevo) " +
                        "con ese nombre exacto y configúra su protocolo.",
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Prioridad: campo configurado > variable de entorno (Claude Code / Max plan)
                string apiKey = settings.AnthropicApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                    apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show(
                        "No hay API key disponible.\n\n" +
                        "Opciones:\n" +
                        "1. Ve a: Tools > ClarionTransformer - Configuracion > pestana IA\n" +
                        "   y pega tu clave de Anthropic (sk-ant-...).\n\n" +
                        "2. O asegurate de tener Claude Code instalado y configurado\n" +
                        "   (usa la variable de entorno ANTHROPIC_API_KEY automaticamente).",
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                settings.AnthropicApiKey = apiKey;

                var editorSvc = new EditorService();
                if (!editorSvc.HasActiveTextEditor())
                {
                    MessageBox.Show("No hay un editor activo. Abri un archivo .clw primero.",
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string source = editorSvc.GetActiveDocumentContent();
                if (string.IsNullOrEmpty(source))
                {
                    MessageBox.Show("No se pudo leer el editor activo.",
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string selected = editorSvc.GetSelectedTextRaw();
                bool isSelection = !string.IsNullOrWhiteSpace(selected);

                string codeBlock;
                int[] applyRange;          // {startLine, startCol, endLine, endCol} — donde aplicar el resultado
                string procedureName;      // solo para nombrar el backup; puede quedar null

                if (isSelection)
                {
                    var selRange = editorSvc.GetSelectionRange();
                    if (selRange == null)
                    {
                        MessageBox.Show(
                            "Hay texto seleccionado pero no se pudo determinar su posicion exacta en el editor.\n" +
                            "Volve a seleccionar el bloque e intenta de nuevo.",
                            "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    codeBlock   = selected;
                    applyRange  = selRange;

                    var pos = editorSvc.GetCursorPosition();
                    var enclosing = pos != null ? ClarionProcedureParser.FindProcedureAtLine(source, pos[0]) : null;
                    procedureName = enclosing?.Name;
                }
                else
                {
                    var pos = editorSvc.GetCursorPosition();
                    int caretLine = pos != null ? pos[0] : 1;
                    var procBlock = ClarionProcedureParser.FindProcedureAtLine(source, caretLine);
                    if (procBlock == null)
                    {
                        MessageBox.Show(
                            "No hay texto seleccionado y no se encontro un PROCEDURE activo en la posicion del cursor.\n\n" +
                            "Selecciona un bloque de codigo o coloca el cursor dentro de un procedimiento.",
                            "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var lines = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                    int lastLineIdx = procBlock.EndLine - 1;
                    int lastColLen  = lastLineIdx < lines.Length ? lines[lastLineIdx].Length + 1 : 1;

                    codeBlock     = procBlock.Text;
                    applyRange    = new[] { procBlock.StartLine, 1, procBlock.EndLine, lastColLen };
                    procedureName = procBlock.Name;
                }

                string resolvedProtocol = ResolveProtocolFile(profile.AiProtocolFile);
                if (resolvedProtocol == null)
                {
                    MessageBox.Show(
                        "No hay un protocolo de transformacion configurado para el perfil \"" + ProfileName + "\".\n\n" +
                        "Configura un archivo .md en: Tools > ClarionTransformer - Configuracion\n" +
                        "o crea el archivo predeterminado:\n" +
                        TransformerProfileService.DefaultProtocolPath,
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string prompt = BuildPrompt(codeBlock, profile, resolvedProtocol, isSelection, procedureName);

                // Mostrar dialogo de progreso y llamar la API en background
                string result = null;
                Exception error = null;

                using (var dlg = new ProgressDialog("Aplicando \"" + ProfileName + "\"... (puede tardar unos segundos)"))
                {
                    var ct = dlg.CancellationToken;

                    var thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            var task = AnthropicApiClient.SendMessageAsync(
                                apiKey, settings.AiModel, prompt, ct);
                            result = task.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { error = ex; }
                        finally
                        {
                            if (!ct.IsCancellationRequested)
                                dlg.Invoke(new Action(() => dlg.DialogResult = DialogResult.OK));
                        }
                    });
                    thread.IsBackground = true;
                    thread.Start();

                    dlg.ShowDialog();
                }

                if (error != null)
                {
                    string msg = error.Message;
                    if (error.InnerException != null)
                        msg += "\n\nDetalle: " + error.InnerException.Message;
                    MessageBox.Show("Error al llamar la API de Claude:\n\n" + msg,
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (result == null) return; // cancelado

                string newCode = CleanResponse(result);
                if (string.IsNullOrWhiteSpace(newCode))
                {
                    MessageBox.Show(
                        "Claude respondio pero el resultado quedo vacio despues de limpiar el formato.\n\n" +
                        "Respuesta recibida:\n" + (result.Length > 500 ? result.Substring(0, 500) + "..." : result),
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (profile.CreateBackup)
                    TryCreateBlockBackup(codeBlock, newCode, procedureName);

                var replResult = editorSvc.ReplaceRange(
                    applyRange[0], applyRange[1], applyRange[2], applyRange[3], newCode);

                if (!replResult.Success)
                {
                    MessageBox.Show("Claude respondio correctamente pero no se pudo aplicar en el editor:\n" +
                        replResult.ErrorMessage, "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show(
                    "Codigo transformado con exito (\"" + ProfileName + "\").\n\nRevisa el resultado y guarda si estas conforme (Ctrl+S).",
                    "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado: " + ex.Message,
                    "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Guarda el codigo original y el transformado como dos archivos .clw separados
        /// (...-Old-... / ...-New-...) en una subcarpeta dentro de la solucion Clarion
        /// abierta. Best-effort: si no se puede resolver la carpeta de la solucion o
        /// falla la escritura, avisa pero no interrumpe la transformacion ya aplicada.
        /// </summary>
        private static void TryCreateBlockBackup(string oldCode, string newCode, string procedureName)
        {
            try
            {
                string solutionDir = ResolveSolutionDirectory();
                if (solutionDir == null)
                {
                    MessageBox.Show(
                        "No se pudo determinar la carpeta de la solucion Clarion abierta; no se genero backup.",
                        "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string backupDir = Path.Combine(solutionDir, BackupSubfolder);
                Directory.CreateDirectory(backupDir);

                string safeName  = SanitizeFileNamePart(string.IsNullOrWhiteSpace(procedureName) ? "Seleccion" : procedureName);
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

                string oldPath = Path.Combine(backupDir, $"{safeName}-Old-{timestamp}.clw");
                string newPath = Path.Combine(backupDir, $"{safeName}-New-{timestamp}.clw");

                File.WriteAllText(oldPath, oldCode, Encoding.UTF8);
                File.WriteAllText(newPath, newCode, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo crear el backup Old/New:\n" + ex.Message,
                    "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string ResolveSolutionDirectory()
        {
            string solutionPath = EditorService.GetOpenSolutionPath();
            if (string.IsNullOrWhiteSpace(solutionPath)) return null;

            if (Directory.Exists(solutionPath)) return solutionPath;
            if (File.Exists(solutionPath)) return Path.GetDirectoryName(solutionPath);

            return null;
        }

        private static string SanitizeFileNamePart(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(Array.IndexOf(invalid, c) >= 0 || c == '.' || c == ' ' ? '_' : c);
            return sb.ToString();
        }

        private static string CleanResponse(string text)
        {
            // Quitar fences de markdown si Claude los agrego
            text = Regex.Replace(text, @"```[^\n]*\n?", "", RegexOptions.IgnoreCase).Replace("```", "");
            return text.Trim('\r', '\n');
        }

        private static string ResolveProtocolFile(string configured)
        {
            // 1. Archivo configurado en el perfil
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
                return configured;

            // 2. Predeterminado en %APPDATA%\ClarionTransformer\
            string defaultPath = TransformerProfileService.DefaultProtocolPath;
            if (File.Exists(defaultPath))
                return defaultPath;

            // 3. Sin protocolo — el llamador debe avisar al usuario
            return null;
        }

        private static string BuildPrompt(string codeBlock, TransformerProfile p, string protocolFile, bool isSelection, string procedureName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Transforma el siguiente codigo Clarion aplicando el protocolo que se indica a continuacion.");
            sb.AppendLine("Devuelve UNICAMENTE el codigo transformado, sin explicaciones ni bloques de codigo markdown.");
            sb.AppendLine(isSelection
                ? "El fragmento es una SELECCION PARCIAL de codigo (no necesariamente un procedimiento completo): conserva la indentacion relativa y no agregues encabezados PROCEDURE/CODE que no estaban en el original."
                : "El fragmento es el PROCEDIMIENTO COMPLETO" + (string.IsNullOrEmpty(procedureName) ? "" : " \"" + procedureName + "\"") + ": conserva la declaracion PROCEDURE, el bloque de datos locales y la seccion CODE.");
            if (p.Reindent)
            {
                sb.AppendLine("Revisa y corrige la indentacion Clarion de TODO el bloque devuelto, incluida la primera linea " +
                    "(" + p.IndentSpaces + " espacios por nivel de anidamiento: IF/ELSIF/ELSE/END, LOOP/END, CASE/OF/ELSE/END, CLASS/END, etc.). " +
                    "Usa como base el nivel de indentacion con el que arranca la primera linea del bloque original que te paso " +
                    "mas abajo — la primera linea de tu respuesta debe quedar indentada igual que esa, no la dejes en columna 0. " +
                    "Si la indentacion original esta mal, corregila; no te limites a copiarla tal cual.");
            }
            else
            {
                sb.AppendLine("Conserva la indentacion del bloque original tal cual esta, incluida la primera linea.");
            }

            if (p.AddComments)
            {
                sb.AppendLine("Ademas, agrega comentarios breves (con '!') que expliquen el funcionamiento de los bloques " +
                    "que no sean evidentes a simple vista (logica de negocio no trivial, condiciones poco obvias, workarounds). " +
                    "No comentes lineas triviales ni obvias, y no agregues comentarios redundantes con el nombre de variables o metodos.");
            }

            sb.AppendLine();
            sb.AppendLine("=== PROTOCOLO DE TRANSFORMACION ===");
            sb.AppendLine();
            sb.AppendLine(File.ReadAllText(protocolFile, Encoding.UTF8));
            sb.AppendLine();
            sb.AppendLine("=== FIN DEL PROTOCOLO ===");

            if (!string.IsNullOrWhiteSpace(p.AiExtraInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("INSTRUCCIONES ADICIONALES DEL PERFIL:");
                foreach (var line in p.AiExtraInstructions.Replace("\r\n", "\n").Split('\n'))
                {
                    string t = line.Trim();
                    if (!string.IsNullOrEmpty(t)) sb.AppendLine("   - " + t);
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- CODIGO A TRANSFORMAR ---");
            sb.AppendLine();
            sb.Append(codeBlock);

            return sb.ToString();
        }
    }
}
