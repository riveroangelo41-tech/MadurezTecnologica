using MadurezTecnologica.Inteligencia;
using MadurezTecnologica.Logica;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    // Resultado del análisis con metadatos útiles
    public class ResultadoAnalisis
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
        public string TextoAnalisis { get; set; } = "";
        public ModoOperacion ModoUsado { get; set; }
        public int CaracteresProcesados { get; set; }
        public DateTime FechaAnalisis { get; set; }
    }

    public class OrquestadorAnalisis
    {
        // Componentes que el orquestador coordina
        private readonly GestorInforme _gestorInforme;
        private readonly ConstructorPrompt _constructorPrompt;
        private readonly ClienteIA _clienteIA;
        private readonly DetectorConexion _detectorConexion;

        public OrquestadorAnalisis()
        {
            _gestorInforme = new GestorInforme();
            _constructorPrompt = new ConstructorPrompt();
            _clienteIA = new ClienteIA();
            _detectorConexion = new DetectorConexion();
        }

        // Método principal: orquesta el análisis completo de un informe
        public async Task<ResultadoAnalisis> AnalizarInformePdf(string rutaPdf, Empresa empresa)
        {
            var resultado = new ResultadoAnalisis
            {
                FechaAnalisis = DateTime.Now
            };

            try
            {
                // PASO 1: Validar que el PDF se puede leer
                if (!_gestorInforme.EsPdfValido(rutaPdf))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El archivo PDF no es válido o no se puede leer";
                    return resultado;
                }

                // PASO 2: Detectar modo de operación
                var modo = await _detectorConexion.DetectarModo();
                resultado.ModoUsado = modo;

                // PASO 3: Bifurcar según modo
                switch (modo)
                {
                    case ModoOperacion.Online:
                        return await AnalizarEnLineaConClaude(rutaPdf, empresa, resultado);

                    case ModoOperacion.OfflineSinRed:
                        resultado.Exitoso = false;
                        resultado.Mensaje = "No hay conexión a internet. El modo offline estará disponible próximamente.";
                        return resultado;

                    case ModoOperacion.OfflineForzado:
                        resultado.Exitoso = false;
                        resultado.Mensaje = "Modo offline activado manualmente. El motor offline estará disponible próximamente.";
                        return resultado;

                    default:
                        resultado.Exitoso = false;
                        resultado.Mensaje = "Modo de operación no reconocido";
                        return resultado;
                }
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error durante el análisis: {ex.Message}";
                return resultado;
            }
        }

        // Método privado que ejecuta el análisis en modo online (con Claude)
        private async Task<ResultadoAnalisis> AnalizarEnLineaConClaude(string rutaPdf, Empresa empresa, ResultadoAnalisis resultado)
        {
            // Extraer texto del PDF
            string textoInforme = _gestorInforme.ExtraerTexto(rutaPdf);
            resultado.CaracteresProcesados = textoInforme.Length;

            if (string.IsNullOrWhiteSpace(textoInforme))
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "El PDF está vacío o no contiene texto extraíble";
                return resultado;
            }

            // Construir los prompts
            string promptSistema = _constructorPrompt.PromptSistema();
            string promptAnalisis = _constructorPrompt.PromptAnalisisInforme(
                textoInforme,
                empresa.Nombre,
                empresa.Sector
            );

            // Enviar a Claude y recibir el análisis
            string respuestaClaude = await _clienteIA.EnviarMensaje(promptAnalisis, promptSistema);

            resultado.Exitoso = true;
            resultado.Mensaje = "Análisis completado exitosamente";
            resultado.TextoAnalisis = respuestaClaude;
            return resultado;
        }
    }
}