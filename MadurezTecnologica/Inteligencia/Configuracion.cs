using Microsoft.Extensions.Configuration; // importar la librería para manejar la configuración

namespace MadurezTecnologica.Inteligencia
{
    public static class Configuracion //clase para acceder a los parámetros de configuración de la IA, como la API Key, modelo, etc.
    {
        private static IConfiguration? _config; //almacena la configuracion cargada desde el archivo appconfi.json

        private static IConfiguration ObtenerConfig() //metodo para cargar la configuracion
        {
            if (_config != null) return _config;

            var builder = new ConfigurationBuilder();
            string rutaArchivo = System.IO.Path.Combine(AppContext.BaseDirectory, "appconfi.json");

            if (System.IO.File.Exists(rutaArchivo))
            {
                // Modo normal (desarrollo o publish con archivo al lado): leer del archivo.
                // Permite además sobreescribir la config sin recompilar.
                builder.AddJsonFile(rutaArchivo, optional: false);
                _config = builder.Build();
            }
            else
            {
                // Modo single-file (.exe solo): leer la config del RECURSO EMBEBIDO.
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string? nombreRecurso = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("appconfi.json", StringComparison.OrdinalIgnoreCase));

                if (nombreRecurso == null)
                    throw new Exception("No se encontró la configuración (appconfi.json) ni como archivo ni embebida en el ejecutable.");

                using var stream = asm.GetManifestResourceStream(nombreRecurso)!;
                using var ms = new System.IO.MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                builder.AddJsonStream(ms);
                _config = builder.Build();   // se construye mientras el stream sigue vivo
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

        // === AUTENTICACIÓN (RF-33) ===
        // Credenciales almacenadas en appconfi.json. La contraseña se guarda como
        // hash SHA-256 (nunca en texto plano).

        public static string UsuarioAutenticacion
        {
            get => ObtenerConfig()["Autenticacion:Usuario"] ?? "";
        }

        public static string PasswordHashAutenticacion
        {
            get => ObtenerConfig()["Autenticacion:PasswordHash"] ?? "";
        }
    }
}





