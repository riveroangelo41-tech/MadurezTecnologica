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

        public GestorConversacion()
        {
            // Inicializa los objetos necesarios para la gestión de conversaciones
            _clienteIA = new ClienteIA();
            _constructorPrompt = new ConstructorPrompt();
            _gestorDiagnostico = new GestorDiagnostico();
            _repoMensaje = new RepositorioMensaje();
            _repoConversacion = new RepositorioConversacion();
            _repoEmpresa = new RepositorioEmpresa();
            _repoDiagnostico = new RepositorioDiagnostico();
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
        public async Task<Diagnostico> RegenerarDiagnosticoFinal(int conversacionId) // Este método se llama cuando el usuario hace clic en "Refinar diagnóstico" para generar un nuevo diagnóstico final considerando toda la conversación previa
        {
            //Validaciones previas 

            if (!_repoConversacion.Existe(conversacionId))
            {
                throw new ArgumentException( 
                    $"La conversación con ID {conversacionId} no existe."); // Validar que la conversación existe antes de intentar cargar su historial o generar un diagnóstico final. Si no existe, se lanza una excepción con un mensaje claro.
            }

            var historial = CargarHistorial(conversacionId); // Cargar el historial de mensajes de la conversación para construir el contexto que se enviará a Claude. Este historial incluirá tanto el análisis inicial como todas las interacciones posteriores del usuario.

            if (historial.Count < 2) // Validar que hay suficientes mensajes en el historial para justificar un refinamiento del diagnóstico. Si solo hay el análisis inicial (1 mensaje), no tiene sentido generar un nuevo diagnóstico final, ya que no se ha aportado información adicional ni se ha tenido una conversación significativa.
            {
                throw new InvalidOperationException(
                    "Se requiere al menos un intercambio conversacional previo para refinar el diagnóstico. " +
                    "Esta conversación solo tiene el análisis inicial.");
            }

            // construir el contexto para Claude 

            var mensajesIA = ConstruirMensajesParaIA(historial); // Convertir el historial de mensajes al formato que espera la API de Claude que es lista de objetos con Role y Content.

            // Asegurar que la conversación empiece con "user"
            if (mensajesIA.Count > 0 && mensajesIA[0].Role == "assistant")  // Si el primer mensaje es del assistant, insertamos un mensaje "user" al inicio para cumplir con el formato que exige la API de Claude, que requiere que la conversación comience con un mensaje del usuario.
            {
                // Insertar un mensaje "user" al inicio del contexto para cumplir con el formato requerido por la API de Claude, que exige que la conversación comience con un mensaje del usuario. Este mensaje explica que se compartió el informe inicial y se recibió un análisis de madurez tecnológica, estableciendo así el contexto para toda la conversación posterior.
                mensajesIA.Insert(0, new MensajeIA
                {
                    Role = "user",
                    Content = "Te compartí el informe de mi empresa y me entregaste un análisis de madurez tecnológica."
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

            // Cargar historial
            var historial = CargarHistorial(conversacionId);
            var mensajesIA = ConstruirMensajesParaIA(historial);

            if (mensajesIA.Count > 0 && mensajesIA[0].Role == "assistant")
            {
                mensajesIA.Insert(0, new MensajeIA
                {
                    Role = "user",
                    Content = "Te compartí el informe de mi empresa y me entregaste el siguiente análisis..."
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

    }
}