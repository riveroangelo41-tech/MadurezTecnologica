using Microsoft.Extensions.Configuration; // importar la librería para manejar la configuración

namespace MadurezTecnologica.Inteligencia
{
    public static class Configuracion //clase para acceder a los parámetros de configuración de la IA, como la API Key, modelo, etc.
    {
        private static IConfiguration? _config; //almacena la configuracion cargada desde el archivo appconfi.json

        private static IConfiguration ObtenerConfig() //metodo para cargar la configuracion
        {
            // Si ya se cargó la configuración, la devuelve. Si no, la carga desde el archivo appconfi.json
            if (_config == null)
            {
                // codigo para cargar la configuración paso a paso:contrius, buscar archivo, cargarlo y devolver el resultado
                _config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appconfi.json", optional: false)
                    .Build();
            }
            return _config;
        }

        public static string ApiKey //propiedad para obtener la API Key de Anthropic desde la configuración
        {
            get
            {
                // Obtiene la API Key desde la configuración. Si no está configurada, lanza una excepción.
                var clave = ObtenerConfig()["Anthropic:ApiKey"];
                if (string.IsNullOrWhiteSpace(clave))
                {
                    throw new Exception("La API Key no está configurada en appconfi.json");
                }
                return clave;
            }
        }

        public static string Modelo//propiedad para obtener el modelo de Anthropic desde la configuración
        {
            get
            {
                return ObtenerConfig()["Anthropic:Modelo"] ?? "claude-sonnet-4-6";
            }
        }

        public static int MaxTokens//propiedad para obtener el maximo de tokens permitidos por Anthropic desde la configuración
        {
            get
            {
                var valor = ObtenerConfig()["Anthropic:MaxTokens"];
                return int.TryParse(valor, out int n) ? n : 4000;
            }
        }
    }
}





