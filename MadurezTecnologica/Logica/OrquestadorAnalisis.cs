using MadurezTecnologica.Inteligencia;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    public class ResultadoAnalisis
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
        public string TextoAnalisis { get; set; } = "";
        public Diagnostico? Diagnostico { get; set; }
        public ModoOperacion ModoUsado { get; set; }
        public int CaracteresProcesados { get; set; }
        public DateTime FechaAnalisis { get; set; }
        public string MetodoValidacion { get; set; } = "";
    }

    public class OrquestadorAnalisis
    {
        private readonly GestorInforme _gestorInforme;
        private readonly GestorDiagnostico _gestorDiagnostico;
        private readonly DetectorConexion _detectorConexion;

        public OrquestadorAnalisis()
        {
            _gestorInforme = new GestorInforme();
            _gestorDiagnostico = new GestorDiagnostico();
            _detectorConexion = new DetectorConexion();
        }
        // Método principal para analizar un informe PDF, que coordina todos los pasos del proceso de análisis
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

                // PASO 3: Si no es online, devolver mensaje informativo
                if (modo != ModoOperacion.Online)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = modo == ModoOperacion.OfflineSinRed
                        ? "No hay conexión a internet. El modo offline estará disponible próximamente."
                        : "Modo offline activado manualmente. El motor offline estará disponible próximamente.";
                    return resultado;
                }

                // PASO 4: Extraer texto del PDF
                string textoInforme = _gestorInforme.ExtraerTexto(rutaPdf);
                resultado.CaracteresProcesados = textoInforme.Length;

                if (string.IsNullOrWhiteSpace(textoInforme))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El PDF está vacío o no contiene texto extraíble";
                    return resultado;
                }

                // PASO 5: Validar coherencia entre PDF y empresa registrada
                var validacion = await _gestorDiagnostico.ValidarCoherenciaPDF(textoInforme, empresa);
                resultado.MetodoValidacion = validacion.MetodoUsado;

                if (!validacion.EsCoherente)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"El informe no corresponde a la empresa registrada ({empresa.Nombre}). {validacion.Mensaje}";
                    return resultado;
                }
                // PASO 6: Realizar el diagnóstico con Claude
                Diagnostico diagnostico = await _gestorDiagnostico.RealizarDiagnostico(textoInforme, empresa);

                resultado.Exitoso = true;
                resultado.Mensaje = $"Análisis completado. Validación: {validacion.Mensaje}";
                resultado.Diagnostico = diagnostico;
                resultado.TextoAnalisis = $"Nivel: {diagnostico.NivelMadurez} | Ver objeto Diagnostico para detalles";
                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error durante el análisis: {ex.Message}";
                return resultado;
            }
        }
    }
}