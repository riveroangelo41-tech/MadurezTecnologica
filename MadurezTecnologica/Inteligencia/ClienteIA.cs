using MadurezTecnologica.Inteligencia;

using System.Net.Http; // importar la librería para hacer solicitudes HTTP
using System.Net.Http.Json; // importar la librería para manejar JSON en las solicitudes HTTP
using System.Text.Json; // importar la librería para manejar JSON en general
using System.Text.Json.Serialization; // importar la librería para manejar la serialización JSON

namespace MadurezTecnologica.Inteligencia
{
    public class MensajeIA //clase que representa el mensaje que se envía a la IA, con atributos para el rol del emisor y el contenido del mensaje
    {
        [JsonPropertyName("role")] //atributo de quien envia el mensaje
        public string Role { get; set; } = "";

        [JsonPropertyName("content")] //atributo del contenido del mensaje
        public string Content { get; set; } = "";



    }
    public class PeticionIA//clase que representa la petición que se envía a la IA, con atributos para el modelo, el número máximo de tokens y la lista de mensajes
    {
        [JsonPropertyName("model")]//atributo del modelo de IA a utilizar
        public string Model { get; set; } = "";

        [JsonPropertyName("max_tokens")]//atributo del número máximo de tokens que se pueden generar en la respuesta
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]//atributo de la lista de mensajes que se envían a la IA
        public List<MensajeIA> Messages { get; set; } = new List<MensajeIA>();





    }

    public class BloqueContenido//clase que representa un bloque de contenido en la respuesta de la IA, con atributos para el tipo de bloque y el contenido del bloque
    {
        [JsonPropertyName("type")]//atributo del tipo de bloque (por ejemplo, "paragraph", "code", etc.)
        public string Type { get; set; } = "";

        [JsonPropertyName("text")]//atributo del contenido del bloque
        public string Text { get; set; }= "";





    }

    public class RespuestaIA // clase que representa la respuesta completa que devuelve la IA
    {

        [JsonPropertyName("content")]//atributo del contenido completo de la respuesta de. la IA
        public List<BloqueContenido> Content { get; set; } = new List<BloqueContenido>();



    }
    public class ClienteIA //clase para interactuar con la IA, utilizando los parámetros de configuración definidos en la clase Configuracion
    {
        private readonly string _apiKey;//almacena la API Key de Anthropic para autenticar las solicitudes a la IA
        private readonly string _modelo;//almacena el nombre del modelo de IA a utilizar
        private readonly int _maxTokens;//almacena el número máximo de tokens que se pueden generar en una respuesta

        public ClienteIA()//constructor que inicializa los campos de configuración al crear una instancia del cliente de IA
        {
            _apiKey = Configuracion.ApiKey;
            _modelo = Configuracion.Modelo;
            _maxTokens = Configuracion.MaxTokens;
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> EnviarMensaje(string mensajeUsuario)
        {
            // Construir la petición
            var peticion = new PeticionIA
            {
                Model = _modelo,
                MaxTokens = _maxTokens,
                Messages = new List<MensajeIA>
        {
            new MensajeIA { Role = "user", Content = mensajeUsuario }
        }
            };

            // Preparar el HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(peticion);

            // Enviar y esperar respuesta
            var response = await _httpClient.SendAsync(request);

            // Verificar que la respuesta sea exitosa
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error de la API ({response.StatusCode}): {errorBody}");
            }

            // Leer y pasar la respuesta
            var respuesta = await response.Content.ReadFromJsonAsync<RespuestaIA>();

            if (respuesta == null || respuesta.Content.Count == 0)
            {
                throw new Exception("La respuesta de la API está vacía");
            }

            // Devolver el texto del primer bloque
            return respuesta.Content[0].Text;
        }
    }
}