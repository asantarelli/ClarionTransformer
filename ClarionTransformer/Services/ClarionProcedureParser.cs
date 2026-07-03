using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ClarionTransformer.Services
{
    public class ProcedureBlock
    {
        public string Name      { get; set; }
        public int    StartLine { get; set; }  // 1-based, inclusive
        public int    EndLine   { get; set; }   // 1-based, inclusive
        public string Text      { get; set; }
    }

    /// <summary>
    /// Ubica el bloque PROCEDURE activo (donde esta el caret) en un source Clarion.
    /// Los procedimientos y metodos de clase se declaran sin indentacion, con el
    /// patron "Etiqueta PROCEDURE" en la columna 1 — a diferencia de los parametros
    /// PROCEDURE dentro de una CLASS, que van indentados. El bloque se extiende
    /// desde esa linea hasta la linea anterior a la proxima declaracion del mismo
    /// tipo, o hasta el final del archivo.
    /// </summary>
    public static class ClarionProcedureParser
    {
        private static readonly Regex ProcedureStart =
            new Regex(@"^(\S+)\s+PROCEDURE\b", RegexOptions.IgnoreCase);

        public static ProcedureBlock FindProcedureAtLine(string source, int caretLine)
        {
            if (string.IsNullOrEmpty(source)) return null;

            var lines = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            var starts = new List<int>();
            for (int i = 0; i < lines.Length; i++)
                if (ProcedureStart.IsMatch(lines[i]))
                    starts.Add(i);

            if (starts.Count == 0) return null;

            int caretIdx = caretLine - 1;
            int chosen = -1;
            foreach (var s in starts)
            {
                if (s <= caretIdx) chosen = s;
                else break;
            }
            if (chosen < 0) return null; // el caret esta antes del primer PROCEDURE del archivo

            int endIdx = lines.Length - 1;
            foreach (var s in starts)
            {
                if (s > chosen) { endIdx = s - 1; break; }
            }

            // Recortar lineas en blanco finales
            while (endIdx > chosen && string.IsNullOrWhiteSpace(lines[endIdx]))
                endIdx--;

            var sb = new StringBuilder();
            for (int j = chosen; j <= endIdx; j++)
            {
                sb.Append(lines[j]);
                if (j < endIdx) sb.Append("\r\n");
            }

            return new ProcedureBlock
            {
                Name      = ProcedureStart.Match(lines[chosen]).Groups[1].Value,
                StartLine = chosen + 1,
                EndLine   = endIdx + 1,
                Text      = sb.ToString()
            };
        }
    }
}
