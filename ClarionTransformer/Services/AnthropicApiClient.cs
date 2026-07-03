using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ClarionTransformer.Services
{
    /// <summary>
    /// Minimal client para la API de mensajes de Anthropic.
    /// No depende de ninguna librería externa — serializa/parsea JSON a mano.
    /// </summary>
    public static class AnthropicApiClient
    {
        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            // Forzar TLS 1.2+ — requerido por Anthropic. .NET Framework puede
            // defaultear a TLS 1.0 en algunas configuraciones del sistema.
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            return client;
        }

        /// <summary>
        /// Envia un prompt a Claude y devuelve el texto de la respuesta.
        /// </summary>
        public static async Task<string> SendMessageAsync(
            string apiKey,
            string model,
            string prompt,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("API key no configurada. Configura la clave en ClarionTransformer - Configuracion (pestana IA).");

            string body = BuildRequestJson(model, prompt);

            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
            {
                request.Headers.Add("x-api-key", apiKey);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using (var response = await _http.SendAsync(request, ct).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errMsg = ExtractErrorMessage(responseBody);
                        throw new HttpRequestException(
                            string.Format("Error de API ({0}): {1}", (int)response.StatusCode, errMsg));
                    }

                    return ExtractResponseText(responseBody);
                }
            }
        }

        private static string BuildRequestJson(string model, string prompt)
        {
            // Escaped prompt for JSON
            string escaped = prompt
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\t", "\\t");

            return string.Format(
                "{{\"model\":\"{0}\",\"max_tokens\":8192,\"messages\":[{{\"role\":\"user\",\"content\":\"{1}\"}}]}}",
                model, escaped);
        }

        private static string ExtractResponseText(string json)
        {
            // Parse: {"content":[{"type":"text","text":"..."}],...}
            // Find first "text" value inside "content" array
            var m = Regex.Match(json,
                @"""content""\s*:\s*\[.*?""type""\s*:\s*""text"".*?""text""\s*:\s*""((?:[^""\\]|\\.)*)""",
                RegexOptions.Singleline);

            if (!m.Success)
                throw new InvalidOperationException("No se pudo parsear la respuesta de la API.\nRespuesta: " +
                    (json.Length > 300 ? json.Substring(0, 300) + "..." : json));

            return UnescapeJson(m.Groups[1].Value);
        }

        private static string ExtractErrorMessage(string json)
        {
            var m = Regex.Match(json, @"""message""\s*:\s*""((?:[^""\\]|\\.)*)""");
            return m.Success ? UnescapeJson(m.Groups[1].Value) : json;
        }

        private static string UnescapeJson(string s)
        {
            return s
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
    }
}
