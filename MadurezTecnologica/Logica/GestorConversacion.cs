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
        private readonly RepositorioMensaje _repoMensaje;
        private readonly RepositorioConversacion _repoConversacion;
        private readonly RepositorioEmpresa _repoEmpresa;
        private readonly RepositorioDiagnostico _repoDiagnostico;

        public GestorConversacion()
        {
            // Inicializa los objetos necesarios para la gestión de conversaciones
            _clienteIA = new ClienteIA();
            _constructorPrompt = new ConstructorPrompt();
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
            // Validación básica
            if (string.IsNullOrWhiteSpace(textoUsuario))
            {
                throw new ArgumentException("El mensaje del usuario no puede estar vacío.");
            }

            // 1. Cargar el historial actual de la conversación
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

    }
}