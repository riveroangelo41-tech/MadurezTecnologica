using MadurezTecnologica.Datos;
using MadurezTecnologica.Inteligencia;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    public class GestorConversacion
    {
        // Atributos para manejar la interacción con la IA y el acceso a datos
        private readonly ClienteIA _clienteIA;
        private readonly ConstructorPrompt _constructorPrompt;
        private readonly GestorDiagnostico _gestorDiagnostico;
        private readonly RepositorioMensaje _repoMensaje;
        private readonly RepositorioConversacion _repoConversacion;
        private readonly RepositorioEmpresa _repoEmpresa;
        private readonly RepositorioDiagnostico _repoDiagnostico;

        private readonly MotorChatOffline _motorChatOffline;
        private readonly MotorOffline _motorOffline;

        public GestorConversacion()
        {
            _clienteIA = new ClienteIA();
            _constructorPrompt = new ConstructorPrompt();
            _gestorDiagnostico = new GestorDiagnostico();
            _repoMensaje = new RepositorioMensaje();
            _repoConversacion = new RepositorioConversacion();
            _repoEmpresa = new RepositorioEmpresa();
            _repoDiagnostico = new RepositorioDiagnostico();
            _motorChatOffline = new MotorChatOffline();
            _motorOffline = new MotorOffline();
        }

        // Carga todos los mensajes de una conversación, ordenados cronológicamente
        public List<Mensaje> CargarHistorial(int conversacionId)
        {
            return _repoMensaje.ObtenerPorConversacion(conversacionId);
        }

        // Convierte los mensajes de la BD al formato que espera la API de Claude
        public List<MensajeIA> ConstruirMensajesParaIA(List<Mensaje> mensajes)
        {
            var lista = new List<MensajeIA>();
            foreach (var m in mensajes)
            {
                lista.Add(new MensajeIA
                {
                    Role = (m.Remitente == "IA") ? "assistant" : "user", // Asume que los mensajes de la IA tienen Remitente "IA"
                    Content = m.Contenido // El contenido se puede procesar aquí si es necesario (ej. eliminar formato, agregar contexto, etc.)
                });
            }
            return lista;
        }

        // Calcula el siguiente Orden para un nuevo mensaje en la conversación
        public int CalcularSiguienteOrden(int conversacionId)
        {
            var historial = CargarHistorial(conversacionId);
            if (historial.Count == 0) return 1; // Si no hay mensajes, el primer orden es 1

            int max = 0; // Encuentra el máximo Orden en el historial para asignar el siguiente
            foreach (var m in historial) // Recorre los mensajes para encontrar el mayor Orden
            {
                if (m.Orden > max) max = m.Orden; // Actualiza max si encuentra un Orden mayor
            }
            return max + 1;
        }

        // Construye un resumen del historial para mostrar al usuario
        public string ResumirHistorial(int conversacionId)
        {
            // Carga los mensajes de la conversación y construye un resumen con el número de mensajes y una vista previa de cada uno
            var mensajes = CargarHistorial(conversacionId);
            if (mensajes.Count == 0) return "Conversación sin mensajes.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Conversación con {mensajes.Count} mensaje(s):");

            // Agrega una línea para cada mensaje mostrando el remitente, el orden y una vista previa del contenido (hasta 80 caracteres)
            for (int i = 0; i < mensajes.Count; i++)
            {
                var m = mensajes[i];
                string preview = m.Contenido.Length > 80
                    ? m.Contenido.Substring(0, 80) + "..."
                    : m.Contenido;
                sb.AppendLine($"  [{m.Orden}] {m.Remitente}: {preview}");
            }

            return sb.ToString(); // Devuelve el resumen construido como una cadena
        }
        public async Task<string> EnviarMensajeUsuario(int conversacionId, string textoUsuario)
        {
            // === Validaciones previas ===

            // 1. Validar texto no vacío
            if (string.IsNullOrWhiteSpace(textoUsuario))
            {
                throw new ArgumentException("El mensaje del usuario no puede estar vacío.");
            }

            // 2. Validar texto no excesivamente largo (límite por mensaje individual)
            const int MAX_LONGITUD_MENSAJE = 10000;
            if (textoUsuario.Length > MAX_LONGITUD_MENSAJE)
            {
                throw new ArgumentException(
                    $"El mensaje es demasiado largo ({textoUsuario.Length} caracteres). " +
                    $"Máximo permitido: {MAX_LONGITUD_MENSAJE} caracteres.");
            }

            // 3. Validar que la conversación existe
            if (!_repoConversacion.Existe(conversacionId))
            {
                throw new ArgumentException(
                    $"La conversación con ID {conversacionId} no existe en la base de datos.");
            }

            // === Resto del flujo (igual que antes) ===

            // 4. Cargar el historial actual de la conversación
            var historial = CargarHistorial(conversacionId);


            // 2. Convertir el historial al formato de Claude
            var mensajesIA = ConstruirMensajesParaIA(historial);

            // 3. Asegurar que la conversación empiece con un mensaje "user"
            //    (la API lo exige; nuestro historial empieza con el análisis del assistant)
            if (mensajesIA.Count > 0 && mensajesIA[0].Role == "assistant")
            {
                mensajesIA.Insert(0, new MensajeIA
                {
                    Role = "user",
                    Content = "Te compartí el informe de mi empresa y me entregaste el siguiente análisis de madurez tecnológica."
                });
            }

            // 4. Agregar el nuevo mensaje del usuario al final
            mensajesIA.Add(new MensajeIA
            {
                Role = "user",
                Content = textoUsuario
            });


            // 5. Enviar todo el contexto a Claude
            // Garantizar alternancia correcta de roles antes de enviar
            mensajesIA = NormalizarAlternancia(mensajesIA);

            string promptSistema = _constructorPrompt.PromptSistema();

            string respuestaIA = await _clienteIA.EnviarConversacion(mensajesIA, promptSistema);

            // 6. Si llegamos aquí, la llamada fue exitosa. Ahora persistimos ambos mensajes.
            int ordenUsuario = CalcularSiguienteOrden(conversacionId);

            // 7. Guardar el mensaje del usuario
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "Usuario",
                Contenido = textoUsuario,
                Timestamp = DateTime.Now,
                Orden = ordenUsuario
            });

            // 8. Guardar la respuesta de la IA
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "IA",
                Contenido = respuestaIA,
                Timestamp = DateTime.Now,
                Orden = ordenUsuario + 1
            });

            // 9. Devolver la respuesta para mostrarla al usuario
            return respuestaIA;
        }
        public async Task<Diagnostico> RegenerarDiagnosticoFinal(int conversacionId)
        {
            //Validaciones previas
            if (!_repoConversacion.Existe(conversacionId))
            {
                throw new ArgumentException(
                    $"La conversación con ID {conversacionId} no existe.");
            }

            var historial = CargarHistorial(conversacionId);

            if (historial.Count < 2)
            {
                throw new InvalidOperationException(
                    "Se requiere al menos un intercambio conversacional previo para refinar el diagnóstico. " +
                    "Esta conversación solo tiene el análisis inicial.");
            }

            // === RAMA OFFLINE ===
            if (DetectorConexion.EstarForzadoOffline())
            {
                return RegenerarDiagnosticoFinalOffline(conversacionId, historial);
            }

            // construir el contexto para Claude 

            var mensajesIA = ConstruirMensajesParaIA(historial); // Convertir el historial de mensajes al formato que espera la API de Claude que es lista de objetos con Role y Content.

            // Asegurar que la conversación empiece con "user" con instrucciones claras
            if (mensajesIA.Count > 0 && mensajesIA[0].Role == "assistant")
            {
                mensajesIA.Insert(0, new MensajeIA
                {
                    Role = "user",
                    Content =
                        "A continuación te paso el informe técnico de mi empresa. " +
                        "Tu primera respuesta será el análisis de madurez tecnológica que ya realizaste sobre él. " +
                        "Después tendremos una conversación de seguimiento basada en ese análisis. " +
                        "IMPORTANTE: tienes acceso completo al análisis previo que ya hiciste — está justo en tu siguiente mensaje. " +
                        "NO me pidas que vuelva a compartir el informe ni digas que no tienes memoria; el análisis está disponible en el contexto."
                });
            }

            // Agregar la solicitud de refinamiento
            mensajesIA.Add(new MensajeIA
            {
                Role = "user",
                Content =
                    "Considerando toda nuestra conversación previa y la información adicional que te he " +
                    "proporcionado, regenera el diagnóstico de madurez tecnológica con el mismo formato " +
                    "estructurado de 7 secciones (RESUMEN DE LA EMPRESA, NIVEL DE MADUREZ, FORTALEZAS, " +
                    "DEBILIDADES, RIESGOS, RECOMENDACIONES, PREGUNTAS PARA EL USUARIO). Incorpora todos " +
                    "los detalles aportados durante la conversación. Este es el diagnóstico final."
            });

            // Garantizar alternancia de roles
            mensajesIA = NormalizarAlternancia(mensajesIA);

            //  Enviar a Claude =

            string promptSistema = _constructorPrompt.PromptSistema();
            string respuestaCruda = await _clienteIA.EnviarConversacion(mensajesIA, promptSistema);

            // Parsear 

            var diagnosticoFinal = _gestorDiagnostico.ParsearRespuesta(respuestaCruda);
            diagnosticoFinal.ConversacionId = conversacionId;
            diagnosticoFinal.EsFinal = true;

            // Desmarcar los Final anteriores: solo el más reciente debe ser FINAL,
            // los anteriores quedan como Intermedios (refinamientos previos en el historial).
            _repoDiagnostico.DesmarcarFinalesPorConversacion(conversacionId);

            int idGuardado = _repoDiagnostico.Guardar(diagnosticoFinal);
            diagnosticoFinal.Id = idGuardado;

            // Persistir también la solicitud y la respuesta en el historial 

            int ordenSolicitud = CalcularSiguienteOrden(conversacionId); // Calcular el siguiente orden para asignar a los nuevos mensajes que se van a guardar en el historial. Esto asegura que los mensajes se mantengan en el orden correcto dentro de la conversación.
            // Guardar la solicitud de diagnóstico final como un nuevo mensaje en el historial de la conversación, con el remitente "Usuario" y el contenido indicando que se trata de una solicitud de diagnóstico final que incluye toda la conversación previa. Esto permite mantener un registro completo de la interacción y el contexto que llevó a la generación del diagnóstico final.
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "Usuario",
                Contenido = "[Solicitud de diagnóstico final con toda la conversación]",
                Timestamp = DateTime.Now,
                Orden = ordenSolicitud
            });
            // Guardar la respuesta de la IA (el diagnóstico final en formato crudo) como un nuevo mensaje en el historial de la conversación, con el remitente "IA" y el contenido siendo la respuesta cruda recibida de Claude. Esto permite mantener un registro completo de la interacción y el contexto que llevó a la generación del diagnóstico final, así como la respuesta exacta que se le dio al usuario.
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "IA",
                Contenido = respuestaCruda,
                Timestamp = DateTime.Now,
                Orden = ordenSolicitud + 1
            });

            return diagnosticoFinal;
        }


        private List<MensajeIA> NormalizarAlternancia(List<MensajeIA> mensajes)
        {
            var normalizada = new List<MensajeIA>();

            foreach (var m in mensajes)
            {
                // Si el último mensaje agregado tiene el mismo rol, combinarlos
                if (normalizada.Count > 0 && normalizada[normalizada.Count - 1].Role == m.Role)
                {
                    normalizada[normalizada.Count - 1].Content += "\n\n" + m.Content;
                }
                else
                {
                    normalizada.Add(new MensajeIA { Role = m.Role, Content = m.Content });
                }
            }

            return normalizada;
        }

        public Conversacion? ObtenerConversacionActiva(int empresaId)
        {
            return _repoConversacion.ObtenerUltimaPorEmpresa(empresaId);
        }

        // Estima cuántos tokens consume aproximadamente una conversación
        public int EstimarTokens(int conversacionId)
        {
            var mensajes = CargarHistorial(conversacionId);
            int totalCaracteres = 0;

            foreach (var m in mensajes)
            {
                totalCaracteres += m.Contenido.Length; // Suma la longitud de cada mensaje para obtener el total de caracteres
            }

            // Aproximación estándar: 1 token ≈ 4 caracteres
            return totalCaracteres / 4;
        }

        // Indica si la conversación se está acercando al límite de tokens
        public bool ConversacionMuyLarga(int conversacionId, int umbralTokens = 150000)
        {
            return EstimarTokens(conversacionId) > umbralTokens;
        }

        public async IAsyncEnumerable<string> EnviarMensajeUsuarioStream(
     int conversacionId,
     string textoUsuario,
     [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // Validaciones (igual que en EnviarMensajeUsuario)
            if (string.IsNullOrWhiteSpace(textoUsuario))
                throw new ArgumentException("El mensaje del usuario no puede estar vacío.");

            if (textoUsuario.Length > 10000)
                throw new ArgumentException($"Mensaje demasiado largo ({textoUsuario.Length} caracteres).");

            if (!_repoConversacion.Existe(conversacionId))
                throw new ArgumentException($"La conversación con ID {conversacionId} no existe.");

            // === RAMA OFFLINE — responder con MotorChatOffline ===
            if (DetectorConexion.EstarForzadoOffline())
            {
                await foreach (var chunk in EnviarMensajeOfflineStream(conversacionId, textoUsuario, ct))
                    yield return chunk;
                yield break;
            }

            // Cargar historial
            var historial = CargarHistorial(conversacionId);
            var mensajesIA = ConstruirMensajesParaIA(historial);

            if (mensajesIA.Count > 0 && mensajesIA[0].Role == "assistant")
            {
                mensajesIA.Insert(0, new MensajeIA
                {
                    Role = "user",
                    Content =
                        "A continuación te paso el informe técnico de mi empresa. " +
                        "Tu primera respuesta será el análisis de madurez tecnológica que ya realizaste sobre él. " +
                        "Después tendremos una conversación de seguimiento basada en ese análisis. " +
                        "IMPORTANTE: tienes acceso completo al análisis previo que ya hiciste — está justo en tu siguiente mensaje. " +
                        "NO me pidas que vuelva a compartir el informe ni digas que no tienes memoria; el análisis está disponible en el contexto."
                });
            }

            mensajesIA.Add(new MensajeIA { Role = "user", Content = textoUsuario });
            mensajesIA = NormalizarAlternancia(mensajesIA);

            // Persistir el mensaje del usuario AHORA (antes del stream)
            int orden = CalcularSiguienteOrden(conversacionId);
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "Usuario",
                Contenido = textoUsuario,
                Timestamp = DateTime.Now,
                Orden = orden
            });

            // Construir la respuesta progresivamente mientras emitimos chunks
            var respuestaCompleta = new System.Text.StringBuilder();
            string promptSistema = _constructorPrompt.PromptSistema();

            // === DEBUG: imprimir qué se envía a Claude ===
            System.Diagnostics.Debug.WriteLine("==========================================");
            System.Diagnostics.Debug.WriteLine($"ENVIANDO A CLAUDE (conv {conversacionId}):");
            System.Diagnostics.Debug.WriteLine($"  Prompt sistema: {(promptSistema?.Length ?? 0)} caracteres");
            System.Diagnostics.Debug.WriteLine($"  Cantidad de mensajes: {mensajesIA.Count}");
            for (int i = 0; i < mensajesIA.Count; i++)
            {
                var m = mensajesIA[i];
                string preview = m.Content.Length > 120 ? m.Content.Substring(0, 120) + "..." : m.Content;
                System.Diagnostics.Debug.WriteLine($"  [{i}] role='{m.Role}' content='{preview}'");
            }
            System.Diagnostics.Debug.WriteLine("==========================================");

            await foreach (var chunk in _clienteIA.EnviarConversacionStream(mensajesIA, promptSistema, ct))
            {
                respuestaCompleta.Append(chunk);
                yield return chunk;
            }

            // Al terminar el stream, persistir el mensaje completo de la IA
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "IA",
                Contenido = respuestaCompleta.ToString(),
                Timestamp = DateTime.Now,
                Orden = orden + 1
            });
        }

        // ===================================================
        // FLUJO OFFLINE — Refinamiento de diagnóstico final
        // ===================================================
        private Diagnostico RegenerarDiagnosticoFinalOffline(int conversacionId, List<Mensaje> historial)
        {
            var conv = _repoConversacion.ObtenerPorId(conversacionId);
            var empresa = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;

            if (empresa == null)
                throw new InvalidOperationException(
                    "No se pudo obtener la empresa de la conversación para refinar offline.");

            // Construir un "texto enriquecido" combinando:
            //  - El análisis inicial (primer mensaje de IA)
            //  - Los mensajes del usuario posteriores (información que aportó)
            var sb = new System.Text.StringBuilder();
            foreach (var msg in historial)
            {
                if (msg.Remitente == "IA" && msg.Orden == 1)
                {
                    // Primer mensaje de IA = análisis original; lo incluimos completo
                    sb.AppendLine(msg.Contenido);
                    sb.AppendLine();
                }
                else if (msg.Remitente == "Usuario")
                {
                    string contenido = msg.Contenido ?? "";
                    if (contenido.StartsWith("[Solicitud") || contenido.StartsWith("[ANÁLISIS"))
                        continue; // ignorar mensajes de sistema

                    sb.AppendLine($"Información aportada por el usuario: {contenido}");
                    sb.AppendLine();
                }
            }

            string textoEnriquecido = sb.ToString();

            // Re-ejecutar el motor offline con el texto enriquecido
            var diagnosticoFinal = _motorOffline.AnalizarTexto(textoEnriquecido, empresa);
            diagnosticoFinal.ConversacionId = conversacionId;
            diagnosticoFinal.EsFinal = true;

            // Desmarcar Finales anteriores (igual que online)
            _repoDiagnostico.DesmarcarFinalesPorConversacion(conversacionId);

            int idGuardado = _repoDiagnostico.Guardar(diagnosticoFinal);
            diagnosticoFinal.Id = idGuardado;

            // Persistir mensajes en el historial
            int orden = CalcularSiguienteOrden(conversacionId);
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "Usuario",
                Contenido = "[Refinamiento offline con toda la conversación]",
                Timestamp = DateTime.Now,
                Orden = orden
            });

            string respuestaSintetica =
                $"[ANÁLISIS OFFLINE - Refinamiento]\n\n" +
                $"Nuevo Nivel CMMI detectado: {diagnosticoFinal.NivelMadurez}\n\n" +
                $"RESUMEN:\n{diagnosticoFinal.ResumenEmpresa}\n\n" +
                $"FORTALEZAS:\n{diagnosticoFinal.Fortalezas}\n\n" +
                $"DEBILIDADES:\n{diagnosticoFinal.Debilidades}\n\n" +
                $"RIESGOS:\n{diagnosticoFinal.Riesgos}\n\n" +
                $"RECOMENDACIONES:\n{diagnosticoFinal.Recomendaciones}\n\n" +
                $"Este refinamiento se generó con el motor offline considerando la información adicional " +
                $"que aportaste en la conversación. Para un análisis más profundo y matizado, repite el " +
                $"refinamiento con la IA conectada.";

            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "IA",
                Contenido = respuestaSintetica,
                Timestamp = DateTime.Now,
                Orden = orden + 1
            });

            return diagnosticoFinal;
        }

        // ===================================================
        // FLUJO OFFLINE — Chat por plantillas
        // ===================================================
        private async IAsyncEnumerable<string> EnviarMensajeOfflineStream(
            int conversacionId,
            string textoUsuario,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // 1. Persistir mensaje del usuario
            int orden = CalcularSiguienteOrden(conversacionId);
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "Usuario",
                Contenido = textoUsuario,
                Timestamp = DateTime.Now,
                Orden = orden
            });

            // 2. Obtener contexto: empresa + último diagnóstico
            var conv = _repoConversacion.ObtenerPorId(conversacionId);
            Empresa? empresa = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;
            var historialDiag = _repoDiagnostico.ObtenerHistorialPorConversacion(conv?.Id ?? 0)
                                                 .OrderByDescending(d => d.FechaGeneracion)
                                                 .ToList();
            var ultimoDiag = historialDiag.FirstOrDefault();
            var anteriorDiag = historialDiag.Skip(1).FirstOrDefault();

            // 3. Generar respuesta con el motor offline (con contexto de comparación)
            string respuesta = _motorChatOffline.GenerarRespuesta(
                textoUsuario, ultimoDiag, empresa, anteriorDiag);

            // 4. Persistir respuesta completa de una vez
            _repoMensaje.Guardar(new Mensaje
            {
                ConversacionId = conversacionId,
                Remitente = "IA",
                Contenido = respuesta,
                Timestamp = DateTime.Now,
                Orden = orden + 1
            });

            // 5. Emitir como chunks simulados (palabra por palabra para mantener UX fluida)
            foreach (var chunk in SimularStreaming(respuesta))
            {
                if (ct.IsCancellationRequested) yield break;
                await Task.Delay(25, ct);
                yield return chunk;
            }
        }

        private IEnumerable<string> SimularStreaming(string texto)
        {
            // Dividimos por palabras manteniendo los espacios para que la concatenación sea natural
            var palabras = texto.Split(' ');
            for (int i = 0; i < palabras.Length; i++)
            {
                yield return i == 0 ? palabras[i] : " " + palabras[i];
            }
        }

    }
}