using MadurezTecnologica.Datos;
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

        // Nuevas propiedades para persistencia
        public bool PersistidoEnBD { get; set; }
        public int? EmpresaId { get; set; }
        public int? ConversacionId { get; set; }
        public int? DiagnosticoId { get; set; }
    }

    public class OrquestadorAnalisis
    {
        private readonly GestorInforme _gestorInforme;
        private readonly GestorDiagnostico _gestorDiagnostico;
        private readonly DetectorConexion _detectorConexion;
        private readonly RepositorioEmpresa _repoEmpresa;
        private readonly RepositorioConversacion _repoConversacion;
        private readonly RepositorioMensaje _repoMensaje;
        private readonly RepositorioDiagnostico _repoDiagnostico;

        public OrquestadorAnalisis()
        {
            _gestorInforme = new GestorInforme();
            _gestorDiagnostico = new GestorDiagnostico();
            _detectorConexion = new DetectorConexion();
            _repoEmpresa = new RepositorioEmpresa();
            _repoConversacion = new RepositorioConversacion();
            _repoMensaje = new RepositorioMensaje();
            _repoDiagnostico = new RepositorioDiagnostico();
        }

        public async Task<ResultadoAnalisis> AnalizarInformePdf(string rutaPdf, Empresa empresa)
        {
            var resultado = new ResultadoAnalisis
            {
                FechaAnalisis = DateTime.Now
            };

            try
            {
                // PASO 1: Validar PDF
                if (!_gestorInforme.EsPdfValido(rutaPdf))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El archivo PDF no es válido o no se puede leer";
                    return resultado;
                }

                // PASO 2: Detectar modo
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

                // PASO 5: Validar coherencia
                var validacion = await _gestorDiagnostico.ValidarCoherenciaPDF(textoInforme, empresa);
                resultado.MetodoValidacion = validacion.MetodoUsado;

                if (!validacion.EsCoherente)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"El informe no corresponde a la empresa registrada ({empresa.Nombre}). {validacion.Mensaje}";
                    return resultado;
                }

                // PASO 6: Realizar el diagnóstico con Claude (texto crudo + estructurado)
                var (diagnostico, textoCrudo) = await _gestorDiagnostico.RealizarDiagnostico(textoInforme, empresa);

                // PASO 7: Persistir todo en la BD
                PersistirAnalisis(empresa, rutaPdf, textoCrudo, diagnostico, resultado);

                resultado.Exitoso = true;
                resultado.Mensaje = $"Análisis completado y guardado en BD. Validación: {validacion.Mensaje}";
                resultado.TextoAnalisis = textoCrudo;
                resultado.Diagnostico = diagnostico;
                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error durante el análisis: {ex.Message}";
                return resultado;
            }
        }

        // Método privado que persiste todo el análisis en cascada en la BD
        private void PersistirAnalisis(Empresa empresa, string rutaPdf, string textoCrudo, Diagnostico diagnostico, ResultadoAnalisis resultado)
        {
            // 1. Verificar si la empresa ya existe (por RIF)
            int empresaId;
            var empresaExistente = _repoEmpresa.ObtenerPorRif(empresa.Rif);

            if (empresaExistente != null)
            {
                empresaId = empresaExistente.Id;
            }
            else
            {
                empresa.FechaRegistro = DateTime.Now;
                empresaId = _repoEmpresa.Guardar(empresa);
            }
            resultado.EmpresaId = empresaId;

            // 2. Crear una nueva conversación para este análisis
            var conversacion = new Conversacion
            {
                EmpresaId = empresaId,
                FechaInicio = DateTime.Now,
                RutaInforme = rutaPdf
            };
            int conversacionId = _repoConversacion.Guardar(conversacion);
            resultado.ConversacionId = conversacionId;

            // 3. Guardar el análisis completo como primer mensaje (de la IA)
            var mensajeAnalisis = new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "IA",
                Contenido = textoCrudo,
                Timestamp = DateTime.Now,
                Orden = 1
            };
            _repoMensaje.Guardar(mensajeAnalisis);

            // 4. Guardar el diagnóstico estructurado
            diagnostico.ConversacionId = conversacionId;
            int diagnosticoId = _repoDiagnostico.Guardar(diagnostico);
            resultado.DiagnosticoId = diagnosticoId;

            resultado.PersistidoEnBD = true;
        }
    }
}