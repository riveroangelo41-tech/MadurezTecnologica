using MadurezTecnologica.Inteligencia;

using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MadurezTecnologica.Inteligencia
{
    public class MensajeIA
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class PeticionIA
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("system")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? System { get; set; }

        [JsonPropertyName("messages")]
        public List<MensajeIA> Messages { get; set; } = new List<MensajeIA>();
    }

    public class BloqueContenido
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    public class RespuestaIA
    {
        [JsonPropertyName("content")]
        public List<BloqueContenido> Content { get; set; } = new List<BloqueContenido>();
    }

    public class ClienteIA
    {
        private readonly string _apiKey;
        private readonly string _modelo;
        private readonly int _maxTokens;

        public ClienteIA()
        {
            _apiKey = Configuracion.ApiKey;
            _modelo = Configuracion.Modelo;
            _maxTokens = Configuracion.MaxTokens;
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> EnviarMensaje(string mensajeUsuario, string? promptSistema = null)
        {
            var peticion = new PeticionIA
            {
                Model = _modelo,
                MaxTokens = _maxTokens,
                System = promptSistema,
                Messages = new List<MensajeIA>
                {
                    new MensajeIA { Role = "user", Content = mensajeUsuario }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(peticion);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error de la API ({response.StatusCode}): {errorBody}");
            }

            var respuesta = await response.Content.ReadFromJsonAsync<RespuestaIA>();

            if (respuesta == null || respuesta.Content.Count == 0)
            {
                throw new Exception("La respuesta de la API vino vacía o mal formada");
            }

            return respuesta.Content[0].Text;
        }

        public async Task<string> EnviarConversacion(List<MensajeIA> mensajes, string? promptSistema = null)
        {
            var peticion = new PeticionIA
            {
                Model = _modelo,
                MaxTokens = _maxTokens,
                System = promptSistema,
                Messages = mensajes
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(peticion);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error de la API ({response.StatusCode}): {errorBody}");
            }

            var respuesta = await response.Content.ReadFromJsonAsync<RespuestaIA>();

            if (respuesta == null || respuesta.Content.Count == 0)
            {
                throw new Exception("La respuesta de la API vino vacía o mal formada");
            }

            return respuesta.Content[0].Text;
        }

        
        // STREAMING: devuelve la respuesta chunk por chunk
        
        public async IAsyncEnumerable<string> EnviarConversacionStream(
            List<MensajeIA> mensajes,
            string? promptSistema = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(120);
            http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var bodyObj = new
            {
                model = _modelo,
                max_tokens = _maxTokens,
                system = promptSistema,
                stream = true,
                messages = mensajes.Select(m => new { role = m.Role, content = m.Content }).ToArray()
            };

            string bodyJson = JsonSerializer.Serialize(bodyObj);
            var content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json");

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = content
            };

            using var response = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? linea;
            while ((linea = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                if (!linea.StartsWith("data: ")) continue;
                string json = linea.Substring(6).Trim();
                if (json == "[DONE]") break;

                string? deltaTexto = ExtraerDeltaTexto(json);
                if (!string.IsNullOrEmpty(deltaTexto))
                {
                    yield return deltaTexto;
                }
            }
        }

        private string? ExtraerDeltaTexto(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var tipo) &&
                    tipo.GetString() == "content_block_delta" &&
                    root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("type", out var deltaTipo) &&
                    deltaTipo.GetString() == "text_delta" &&
                    delta.TryGetProperty("text", out var texto))
                {
                    return texto.GetString();
                }
            }
            catch { /* ignorar errores de parseo */ }

            return null;
        }
    }
}