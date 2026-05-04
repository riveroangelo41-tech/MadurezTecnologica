using MadurezTecnologica.Inteligencia;

namespace MadurezTecnologica.Inteligencia
{
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

        public string EstadoActual()
        {
            return $"Cliente IA listo. Modelo: {_modelo}, Max tokens: {_maxTokens}";
        }
    }
}