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

        // Advertencia de inconsistencia de sector detectada por la IA (o keywords en
        // offline). Cuando RequiereConfirmacionSector=true, el análisis NO se ejecutó
        // ni se persistió; la vista debe mostrar el mensaje y preguntar al usuario si
        // quiere continuar. Si acepta, se vuelve a llamar con ignorarValidacionSector=true.
        public bool RequiereConfirmacionSector { get; set; }
        public string SectorRegistrado { get; set; } = "";
        public string SectorDetectado { get; set; } = "";

        // Advertencia de inconsistencia de RIF: el RIF registrado no aparece en el PDF.
        public bool RequiereConfirmacionRif { get; set; }
        public string RifRegistrado { get; set; } = "";

        // Advertencia de inconsistencia en cantidad de empleados: el número del PDF
        // difiere del registrado en más del margen permitido (20%).
        public bool RequiereConfirmacionEmpleados { get; set; }
        public int EmpleadosRegistrados { get; set; }
        public int EmpleadosDetectados { get; set; }
    }

    public class OrquestadorAnalisis
    {
        private readonly GestorInforme _gestorInforme;
        private readonly GestorDiagnostico _gestorDiagnostico;
        private readonly DetectorConexion _detectorConexion;
        private readonly MotorOffline _motorOffline;
        private readonly RepositorioEmpresa _repoEmpresa;
        private readonly RepositorioConversacion _repoConversacion;
        private readonly RepositorioMensaje _repoMensaje;
        private readonly RepositorioDiagnostico _repoDiagnostico;

        public OrquestadorAnalisis()
        {
            _gestorInforme = new GestorInforme();
            _gestorDiagnostico = new GestorDiagnostico();
            _detectorConexion = new DetectorConexion();
            _motorOffline = new MotorOffline();
            _repoEmpresa = new RepositorioEmpresa();
            _repoConversacion = new RepositorioConversacion();
            _repoMensaje = new RepositorioMensaje();
            _repoDiagnostico = new RepositorioDiagnostico();
        }

        public async Task<ResultadoAnalisis> AnalizarInformePdf(
            string rutaPdf,
            Empresa empresa,
            CancellationToken ct = default,
            Action<string>? progreso = null,
            bool ignorarValidacionSector = false,
            bool ignorarValidacionRif = false,
            bool ignorarValidacionEmpleados = false)
        {
            var resultado = new ResultadoAnalisis
            {
                FechaAnalisis = DateTime.Now
            };

            // Enlazar la cancelación del usuario con la del monitor de conexión: si la red
            // se cae a mitad del análisis, este token se cancela y aborta la petición a la IA.
            using var ctsEnlazado = CancellationTokenSource.CreateLinkedTokenSource(
                ct, Inteligencia.DetectorConexion.TokenConexion);
            ct = ctsEnlazado.Token;

            try
            {
                progreso?.Invoke("Validando archivo PDF...");
                ct.ThrowIfCancellationRequested();

                // PASO 1: Validar PDF
                if (!_gestorInforme.EsPdfValido(rutaPdf))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El archivo PDF no es válido o no se puede leer";
                    return resultado;
                }

                // PASO 2: Detectar modo
                progreso?.Invoke("Detectando modo de operación...");
                var modo = await _detectorConexion.DetectarModo();
                resultado.ModoUsado = modo;
                ct.ThrowIfCancellationRequested();

                // PASO 3: Extraer texto del PDF (común a ambos modos)
                progreso?.Invoke("Extrayendo texto del PDF...");
                string textoInforme = _gestorInforme.ExtraerTexto(rutaPdf);
                resultado.CaracteresProcesados = textoInforme.Length;
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(textoInforme))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El PDF está vacío o no contiene texto extraíble";
                    return resultado;
                }

                // PASO 4: Decidir flujo según modo
                if (modo != ModoOperacion.Online)
                {
                    progreso?.Invoke("Analizando con motor offline...");
                    return EjecutarAnalisisOffline(textoInforme, rutaPdf, empresa, resultado, modo,
                        ignorarValidacionSector, ignorarValidacionRif);
                }

                // === FLUJO ONLINE ===

                // PASO 5: Validar coherencia con IA
                progreso?.Invoke("Validando coherencia del informe con la empresa...");
                var validacion = await _gestorDiagnostico.ValidarCoherenciaPDF(textoInforme, empresa, ct);
                resultado.MetodoValidacion = validacion.MetodoUsado;
                ct.ThrowIfCancellationRequested();

                if (!validacion.EsCoherente)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"El informe no corresponde a la empresa registrada ({empresa.Nombre}). {validacion.Mensaje}";
                    return resultado;
                }

                // PASO 5.5: Validar que el SECTOR del informe corresponda al sector
                // registrado. Es una capa adicional sobre la validación de nombre
                // (RF derivado de corrección del tutor). Se salta si el usuario ya
                // confirmó en un diálogo previo que quiere continuar a pesar de la advertencia.
                if (!ignorarValidacionSector && !string.IsNullOrWhiteSpace(empresa.Sector))
                {
                    progreso?.Invoke("Verificando coherencia del sector...");
                    var (sectorOk, sectorDetectado) = await _gestorDiagnostico.ValidarSectorPDF(textoInforme, empresa, ct);
                    ct.ThrowIfCancellationRequested();

                    if (!sectorOk)
                    {
                        resultado.Exitoso = false;
                        resultado.RequiereConfirmacionSector = true;
                        resultado.SectorRegistrado = empresa.Sector;
                        resultado.SectorDetectado = sectorDetectado;
                        resultado.Mensaje = string.IsNullOrWhiteSpace(sectorDetectado)
                            ? $"El contenido del informe no coincide con el sector registrado ({empresa.Sector})."
                            : $"El informe parece corresponder al sector \"{sectorDetectado}\", pero la empresa está registrada como \"{empresa.Sector}\".";
                        return resultado;
                    }
                }

                // PASO 5.6: Validar que el RIF registrado aparezca en el PDF. Es una
                // validación estricta (offline+online) porque el RIF es un dato único
                // e inequívoco. Se salta si el usuario ya confirmó continuar.
                if (!ignorarValidacionRif && !string.IsNullOrWhiteSpace(empresa.Rif))
                {
                    progreso?.Invoke("Verificando el RIF...");
                    if (!TextoContieneRif(textoInforme, empresa.Rif))
                    {
                        resultado.Exitoso = false;
                        resultado.RequiereConfirmacionRif = true;
                        resultado.RifRegistrado = empresa.Rif;
                        resultado.Mensaje = $"El RIF de la empresa ({empresa.Rif}) no aparece en el informe.";
                        return resultado;
                    }
                }

                // PASO 5.7: Validar la cantidad de empleados. Solo online (offline no
                // hay forma confiable de detectarlo). Margen de tolerancia del 20%.
                if (!ignorarValidacionEmpleados && empresa.CantidadEmpleados > 0)
                {
                    progreso?.Invoke("Verificando cantidad de empleados...");
                    int empleadosDetectados = await _gestorDiagnostico.DetectarEmpleadosPDF(textoInforme, ct);
                    ct.ThrowIfCancellationRequested();

                    if (empleadosDetectados > 0 && !EmpleadosDentroDelMargen(empresa.CantidadEmpleados, empleadosDetectados, 0.20))
                    {
                        resultado.Exitoso = false;
                        resultado.RequiereConfirmacionEmpleados = true;
                        resultado.EmpleadosRegistrados = empresa.CantidadEmpleados;
                        resultado.EmpleadosDetectados = empleadosDetectados;
                        resultado.Mensaje = $"El informe menciona aproximadamente {empleadosDetectados} empleados, pero la empresa está registrada con {empresa.CantidadEmpleados}.";
                        return resultado;
                    }
                }

                // PASO 6: Realizar el diagnóstico con Claude
                progreso?.Invoke("Generando diagnóstico con la IA (esto puede tomar 30-60 segundos)...");
                var (diagnostico, textoCrudo) = await _gestorDiagnostico.RealizarDiagnostico(textoInforme, empresa, ct);
                ct.ThrowIfCancellationRequested();

                // PASO 7: Persistir todo en la BD
                progreso?.Invoke("Guardando resultados...");
                PersistirAnalisis(empresa, rutaPdf, textoInforme, textoCrudo, diagnostico, resultado);

                resultado.Exitoso = true;
                resultado.Mensaje = $"Análisis completado y guardado en BD. Validación: {validacion.Mensaje}";
                resultado.TextoAnalisis = textoCrudo;
                resultado.Diagnostico = diagnostico;

                // Destilación progresiva de conocimiento: dispara un ciclo en background
                // ahora que hay un dictamen IA nuevo en el corpus. Es no bloqueante y silencioso.
                DestilacionAutomatica.DispararEnBackground();

                return resultado;
            }
            catch (OperationCanceledException)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Análisis cancelado por el usuario.";
                throw;
            }
            catch (Inteligencia.VpnRequeridaException)
            {
                // Bloqueo regional (VPN apagada): propagar para que la UI muestre el
                // mensaje específico de "enciende la VPN".
                throw;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error durante el análisis: {ex.Message}";
                return resultado;
            }
        }

        // ===================================================
        // FLUJO OFFLINE
        // ===================================================
        private ResultadoAnalisis EjecutarAnalisisOffline(
            string textoInforme, string rutaPdf, Empresa empresa,
            ResultadoAnalisis resultado, ModoOperacion modo,
            bool ignorarValidacionSector,
            bool ignorarValidacionRif)
        {
            // Validación local de coherencia (sin IA): buscar referencias a la empresa
            var validacionOffline = ValidarCoherenciaOffline(textoInforme, empresa);
            resultado.MetodoValidacion = validacionOffline.MetodoUsado;

            if (!validacionOffline.EsCoherente)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"El informe no parece corresponder a la empresa registrada ({empresa.Nombre}). " +
                                    $"{validacionOffline.Mensaje}";
                return resultado;
            }

            // Validación de sector por keywords (equivalente offline de la validación
            // de sector online con IA). Se salta si el usuario ya confirmó continuar.
            if (!ignorarValidacionSector && !string.IsNullOrWhiteSpace(empresa.Sector))
            {
                var (sectorOk, sectorDetectado) = ValidarSectorOffline(textoInforme, empresa.Sector);
                if (!sectorOk)
                {
                    resultado.Exitoso = false;
                    resultado.RequiereConfirmacionSector = true;
                    resultado.SectorRegistrado = empresa.Sector;
                    resultado.SectorDetectado = sectorDetectado ?? "";
                    resultado.Mensaje = string.IsNullOrWhiteSpace(sectorDetectado)
                        ? $"El contenido del informe no coincide con el sector registrado ({empresa.Sector})."
                        : $"El informe parece corresponder al sector \"{sectorDetectado}\", pero la empresa está registrada como \"{empresa.Sector}\".";
                    return resultado;
                }
            }

            // Validación estricta del RIF también en offline (búsqueda literal + variaciones).
            // Empleados NO se valida en offline por falta de forma confiable de detectarlo.
            if (!ignorarValidacionRif && !string.IsNullOrWhiteSpace(empresa.Rif))
            {
                if (!TextoContieneRif(textoInforme, empresa.Rif))
                {
                    resultado.Exitoso = false;
                    resultado.RequiereConfirmacionRif = true;
                    resultado.RifRegistrado = empresa.Rif;
                    resultado.Mensaje = $"El RIF de la empresa ({empresa.Rif}) no aparece en el informe.";
                    return resultado;
                }
            }

            // Ejecutar motor offline
            var diagnostico = _motorOffline.AnalizarTexto(textoInforme, empresa);

            // Construir un "texto crudo" sintético para guardar como mensaje
            string textoCrudo = ConstruirTextoCrudoOffline(diagnostico, modo);

            // Persistir igual que el flujo online (incluyendo el texto real del informe)
            PersistirAnalisis(empresa, rutaPdf, textoInforme, textoCrudo, diagnostico, resultado);

            resultado.Exitoso = true;
            resultado.Mensaje = modo == ModoOperacion.OfflineSinRed
                ? $"Análisis completado en modo OFFLINE (sin conexión a internet). " +
                  $"Validación: {validacionOffline.Mensaje}"
                : $"Análisis completado en modo OFFLINE forzado. " +
                  $"Validación: {validacionOffline.Mensaje}";
            resultado.TextoAnalisis = textoCrudo;
            resultado.Diagnostico = diagnostico;
            return resultado;
        }

        // Resultado de validación offline (la online usa una clase del GestorDiagnostico)
        private record ValidacionOffline(bool EsCoherente, string Mensaje, string MetodoUsado);

        private ValidacionOffline ValidarCoherenciaOffline(string texto, Empresa empresa)
        {
            string textoLower = texto.ToLowerInvariant();
            string nombreLower = empresa.Nombre?.ToLowerInvariant() ?? "";
            string rifLower = empresa.Rif?.ToLowerInvariant() ?? "";

            string nombreCore = nombreLower
                .Replace(",", " ")
                .Replace(".", " ")
                .Replace(" c a ", " ")
                .Replace(" s a ", " ")
                .Replace(" c.a.", " ")
                .Replace(" s.a.", " ")
                .Trim();
            string primeraPalabra = nombreCore.Split(' ')[0];

            bool rifEncontrado = !string.IsNullOrWhiteSpace(rifLower) && textoLower.Contains(rifLower);
            bool nombreEncontrado = !string.IsNullOrWhiteSpace(primeraPalabra) &&
                                    primeraPalabra.Length >= 3 &&
                                    textoLower.Contains(primeraPalabra);
            bool sectorEncontrado = ValidarSector(textoLower, empresa.Sector);

            // RIF + nombre + sector = match perfecto
            if (rifEncontrado && nombreEncontrado && sectorEncontrado)
                return new ValidacionOffline(true,
                    "Coincidencia RIF + nombre + palabras del sector",
                    "offline (RIF + nombre + sector)");

            if (rifEncontrado && nombreEncontrado)
                return new ValidacionOffline(true,
                    "Se encontró el RIF y el nombre de la empresa en el texto",
                    "offline (RIF + nombre)");

            if (rifEncontrado)
                return new ValidacionOffline(true,
                    "Se encontró el RIF de la empresa en el texto",
                    "offline (RIF)");

            if (nombreEncontrado && sectorEncontrado)
                return new ValidacionOffline(true,
                    "Se encontró el nombre y palabras del sector en el texto",
                    "offline (nombre + sector)");

            if (nombreEncontrado)
                return new ValidacionOffline(true,
                    "Se encontró el nombre de la empresa en el texto",
                    "offline (nombre)");

            return new ValidacionOffline(false,
                "No se encontró ni el RIF ni el nombre de la empresa en el texto del PDF.",
                "offline");
        }

        // Verifica si al menos UNA palabra significativa del sector aparece en el texto
        // del informe (filtra stopwords y palabras muy cortas)
        private bool ValidarSector(string textoLower, string? sector)
        {
            if (string.IsNullOrWhiteSpace(sector)) return false;

            var stopwords = new HashSet<string>
            {
                "de", "del", "la", "el", "los", "las", "y", "o", "para", "con", "en",
                "un", "una", "unos", "unas", "por", "que", "se", "su", "sus", "al", "a",
                "como"
            };

            var palabras = sector.ToLowerInvariant()
                .Replace(",", " ").Replace(".", " ").Replace(";", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var palabra in palabras)
            {
                if (palabra.Length < 4) continue;
                if (stopwords.Contains(palabra)) continue;

                if (textoLower.Contains(palabra))
                    return true;
            }

            return false;
        }

        private string ConstruirTextoCrudoOffline(Diagnostico diag, ModoOperacion modo)
        {
            string etiqueta = modo == ModoOperacion.OfflineSinRed
                ? "[ANÁLISIS OFFLINE - Sin conexión a internet]"
                : "[ANÁLISIS OFFLINE - Modo offline forzado]";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(etiqueta);
            sb.AppendLine();
            sb.AppendLine($"Nivel CMMI detectado: {diag.NivelMadurez}");
            sb.AppendLine();
            sb.AppendLine("RESUMEN:");
            sb.AppendLine(diag.ResumenEmpresa);
            sb.AppendLine();
            sb.AppendLine("FORTALEZAS:");
            sb.AppendLine(diag.Fortalezas);
            sb.AppendLine();
            sb.AppendLine("DEBILIDADES:");
            sb.AppendLine(diag.Debilidades);
            sb.AppendLine();
            sb.AppendLine("RIESGOS:");
            sb.AppendLine(diag.Riesgos);
            sb.AppendLine();
            sb.AppendLine("RECOMENDACIONES:");
            sb.AppendLine(diag.Recomendaciones);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("Este análisis fue generado por el motor offline basado en detección de patrones. " +
                          "Para una evaluación más profunda y matizada se recomienda repetir el análisis cuando " +
                          "haya conexión a internet disponible.");
            return sb.ToString();
        }

        // Método privado que persiste todo el análisis en cascada en la BD
        private void PersistirAnalisis(Empresa empresa, string rutaPdf, string textoInforme, string textoCrudo, Diagnostico diagnostico, ResultadoAnalisis resultado)
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

            // 2.5. Guardar el TEXTO REAL DEL INFORME como mensaje de contexto (Orden 0).
            // No se muestra en el chat (VistaChat lo filtra), pero SÍ se envía a la IA
            // cuando el usuario conversa online, para que Claude tenga el informe real y
            // pueda dar un análisis genuino (evita que rechace un análisis offline genérico).
            if (!string.IsNullOrWhiteSpace(textoInforme))
            {
                _repoMensaje.Guardar(new Mensaje
                {
                    ConversacionId = conversacionId,
                    Remitente = "INFORME",
                    Contenido = textoInforme,
                    Timestamp = DateTime.Now,
                    Orden = 0
                });
            }

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

        // ===================================================
        // VALIDACIÓN DE SECTOR OFFLINE (por palabras clave)
        // ===================================================
        // Diccionario: sector registrado → palabras clave características.
        // Se usa cuando NO hay conexión (no podemos preguntar a la IA).
        // La estrategia: contar cuántas keywords aparecen del sector REGISTRADO vs.
        // cuántas aparecen de cada OTRO sector. Si otro sector tiene sustancialmente
        // más matches, se marca como inconsistencia.
        private static readonly Dictionary<string, string[]> KEYWORDS_SECTOR =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Desarrollo de software a la medida"] = new[] { "software a la medida", "desarrollo a medida", "fábrica de software", "outsourcing de desarrollo" },
                ["Desarrollo web / aplicaciones web"]  = new[] { "aplicación web", "sitio web", "html", "css", "javascript", "react", "angular", "vue", "frontend", "backend web" },
                ["Desarrollo móvil (iOS / Android)"]   = new[] { "app móvil", "aplicación móvil", "android", "ios", "swift", "kotlin", "react native", "flutter" },
                ["Fintech / Software financiero"]       = new[] { "fintech", "financiero", "bancario", "pagos", "transacciones", "billetera", "wallet" },
                ["E-commerce / Comercio electrónico"]  = new[] { "e-commerce", "ecommerce", "comercio electrónico", "tienda online", "carrito de compras", "checkout" },
                ["EdTech / Software educativo"]         = new[] { "edtech", "educativo", "educación", "estudiante", "aprendizaje", "e-learning", "curso online", "lms" },
                ["HealthTech / Software para salud"]   = new[] { "healthtech", "salud", "médico", "paciente", "clínico", "hospital", "historia clínica", "telemedicina" },
                ["Videojuegos"]                         = new[] { "videojuego", "videojuegos", "gameplay", "unity", "unreal engine", "gaming", "juego móvil" },
                ["Software empresarial (ERP, CRM)"]    = new[] { "erp", "crm", "gestión empresarial", "workflow", "sap", "oracle ebs", "planificación de recursos" },
                ["Servicios en la nube / SaaS"]        = new[] { "saas", "software as a service", "servicios en la nube", "aws", "azure", "gcp", "iaas", "paas" },
                ["Ciberseguridad"]                      = new[] { "ciberseguridad", "seguridad informática", "pentesting", "vulnerabilidad", "cifrado", "iso 27001", "soc" },
                ["Inteligencia artificial / Machine learning"] = new[] { "inteligencia artificial", "machine learning", "deep learning", "modelo predictivo", "redes neuronales", "nlp" },
                ["Data / Analytics / Big Data"]        = new[] { "big data", "analytics", "análisis de datos", "data warehouse", "business intelligence", "power bi", "tableau", "etl" },
                ["DevOps / Infraestructura"]           = new[] { "devops", "kubernetes", "docker", "ci/cd", "pipeline de despliegue", "terraform", "ansible", "infraestructura como código" },
                ["Software embebido / IoT"]            = new[] { "iot", "internet of things", "embebido", "firmware", "microcontrolador", "sensores", "arduino", "raspberry" },
                ["Consultoría / Servicios TI"]         = new[] { "consultoría", "asesoría en ti", "servicios profesionales de ti" }
            };

        // Valida por keywords si el sector del texto encaja con el sector registrado.
        // Devuelve (esCoherente, sectorDetectado).
        private (bool esCoherente, string sectorDetectado) ValidarSectorOffline(string texto, string sectorRegistrado)
        {
            string textoLower = (texto ?? "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(sectorRegistrado)) return (true, "");

            // "Otro" o sector fuera del diccionario: no podemos validar → aceptamos.
            if (!KEYWORDS_SECTOR.ContainsKey(sectorRegistrado)) return (true, "");

            // Contar matches por sector
            var matches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var (sector, kws) in KEYWORDS_SECTOR)
            {
                int n = 0;
                foreach (var kw in kws)
                {
                    if (textoLower.Contains(kw.ToLowerInvariant())) n++;
                }
                matches[sector] = n;
            }

            int matchesRegistrado = matches[sectorRegistrado];

            // Buscar el sector con más matches
            string sectorGanador = sectorRegistrado;
            int matchesGanador = matchesRegistrado;
            foreach (var (sector, n) in matches)
            {
                if (n > matchesGanador)
                {
                    sectorGanador = sector;
                    matchesGanador = n;
                }
            }

            // Criterio de inconsistencia: el sector registrado tiene 0 matches Y otro
            // sector tiene al menos 2 matches. Ser tolerante para no rechazar informes
            // válidos (una empresa Fintech puede mencionar "erp" una vez sin ser ERP).
            if (matchesRegistrado == 0 && matchesGanador >= 2)
                return (false, sectorGanador);

            return (true, "");
        }

        // ===================================================
        // VALIDACIÓN DE RIF (búsqueda estricta en el texto)
        // ===================================================
        // Busca el RIF registrado en el texto del PDF admitiendo variaciones comunes:
        //  - Como fue escrito: "J-12345678-9"
        //  - Sin guiones: "J123456789"
        //  - Sin prefijo de letra: "12345678-9"
        //  - Solo los dígitos: "123456789"
        // Es más robusto que buscar solo la forma literal, sin ser tan tolerante como
        // para dar falsos positivos.
        private static bool TextoContieneRif(string texto, string rif)
        {
            if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrWhiteSpace(rif))
                return false;

            string textoLower = texto.ToLowerInvariant();
            string rifLower = rif.ToLowerInvariant().Trim();

            // 1. Forma literal
            if (textoLower.Contains(rifLower)) return true;

            // 2. Sin guiones ni espacios ("J123456789")
            string rifSinSeparadores = System.Text.RegularExpressions.Regex.Replace(rifLower, @"[\s\-]", "");
            string textoSinSeparadores = System.Text.RegularExpressions.Regex.Replace(textoLower, @"[\s\-]", "");
            if (textoSinSeparadores.Contains(rifSinSeparadores)) return true;

            // 3. Solo la parte numérica ("123456789") — 8 o 9 dígitos consecutivos
            var soloDigitos = System.Text.RegularExpressions.Regex.Match(rifLower, @"\d{7,10}");
            if (soloDigitos.Success)
            {
                string num = soloDigitos.Value;
                if (textoSinSeparadores.Contains(num)) return true;
                if (textoLower.Contains(num)) return true;
            }

            return false;
        }

        // ===================================================
        // VALIDACIÓN DE EMPLEADOS (margen porcentual)
        // ===================================================
        // Devuelve true si el número detectado está DENTRO del margen permitido
        // respecto al registrado. Ejemplo: registrado=50, margen=0.20 → rango [40, 60].
        // Si el número detectado es 0 o negativo, se considera "no detectable" → true
        // (no bloquea). El caller decide si preguntar o no según empleadosDetectados > 0.
        private static bool EmpleadosDentroDelMargen(int registrados, int detectados, double margen)
        {
            if (registrados <= 0 || detectados <= 0) return true;
            double diff = Math.Abs(detectados - registrados) / (double)registrados;
            return diff <= margen;
        }
    }
}