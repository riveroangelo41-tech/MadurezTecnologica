using System.Globalization;
using System.Text;
using MadurezTecnologica.Inteligencia;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    // DTO con el resultado de la validación de coherencia
    public class ResultadoValidacion
    {
        public bool EsCoherente { get; set; }
        public string Mensaje { get; set; } = "";
        public string MetodoUsado { get; set; } = "";
    }

    public class GestorDiagnostico
    {
        private readonly ClienteIA _clienteIA; 
        private readonly ConstructorPrompt _constructorPrompt;

        public GestorDiagnostico()
        {
            _clienteIA = new ClienteIA();
            _constructorPrompt = new ConstructorPrompt();
        }

        // Valida que el texto del PDF realmente corresponda a la empresa indicada
        public async Task<ResultadoValidacion> ValidarCoherenciaPDF(string textoInforme, Empresa empresa, CancellationToken ct = default)
        {
            // Paso 1: Normalizar ambos textos para comparación
            string nombreNormalizado = NormalizarTexto(empresa.Nombre);
            string textoNormalizado = NormalizarTexto(textoInforme);

            // Paso 2: Búsqueda literal del nombre completo
            if (textoNormalizado.Contains(nombreNormalizado))
            {
                return new ResultadoValidacion
                {
                    EsCoherente = true,
                    Mensaje = "Coincidencia textual exacta del nombre de la empresa",
                    MetodoUsado = "Texto literal"
                };
            }

            // Paso 3: Búsqueda del nombre sin sufijos legales (C.A., S.A., etc.)
            string nombreSinSufijos = QuitarSufijosLegales(nombreNormalizado);
            if (nombreSinSufijos.Length >= 4 && textoNormalizado.Contains(nombreSinSufijos))
            {
                return new ResultadoValidacion
                {
                    EsCoherente = true,
                    Mensaje = "Coincidencia textual del nombre principal (sin sufijos legales)",
                    MetodoUsado = "Texto sin sufijos"
                };
            }

            // Paso 4: Como último recurso, consultar a Claude
            try
            {
                string promptValidacion = _constructorPrompt.PromptValidacionCoherencia(
                    textoInforme,
                    empresa.Nombre,
                    empresa.Sector
                );

                string respuestaIA = await _clienteIA.EnviarMensaje(promptValidacion, null, ct);
                string respuestaNormalizada = respuestaIA.Trim().ToUpper();

                bool coherente = respuestaNormalizada.StartsWith("SI") || respuestaNormalizada.StartsWith("SÍ"); // Se asume que Claude responderá con un "Sí" o "No" claro al inicio de su respuesta para indicar la coherencia
                
                return new ResultadoValidacion
                {
                    EsCoherente = coherente,
                    Mensaje = coherente
                        ? "Validación confirmada por IA"
                        : "La IA determinó que el informe no corresponde a la empresa indicada",
                    MetodoUsado = "IA"
                };
            }
            catch (OperationCanceledException)
            {
                // Cancelación (usuario o caída de red): dejar propagar para abortar el flujo.
                throw;
            }
            catch (VpnRequeridaException)
            {
                // Bloqueo regional (VPN apagada): dejar propagar para mostrar el mensaje
                // correcto, en vez de reportarlo como "el informe no corresponde".
                throw;
            }
            catch (Exception ex)
            {
                // Si falla la validación con IA, ser conservador y rechazar
                return new ResultadoValidacion
                {
                    EsCoherente = false,
                    Mensaje = $"No se pudo validar la coherencia: {ex.Message}",
                    MetodoUsado = "Error"
                };
            }
        }

        // Verifica con la IA que el TEMA del informe corresponde al sector registrado
        // de la empresa. Distinto de ValidarCoherenciaPDF, que valida nombre. Este
        // valida encaje temático (evita analizar un PDF de "Software empresarial" bajo
        // una empresa registrada como "Videojuegos").
        // Devuelve: (esCoherente, sectorDetectado). Si esCoherente=true, sectorDetectado es "".
        public async Task<(bool esCoherente, string sectorDetectado)> ValidarSectorPDF(
            string textoInforme, Empresa empresa, CancellationToken ct = default)
        {
            try
            {
                string prompt = _constructorPrompt.PromptValidacionSector(textoInforme, empresa.Sector ?? "");
                string respuesta = await _clienteIA.EnviarMensaje(prompt, null, ct);
                string norm = respuesta.Trim().ToUpper();

                if (norm.StartsWith("SI") || norm.StartsWith("SÍ"))
                    return (true, "");

                // NO — intentar extraer el sector detectado (línea "Sector detectado: X")
                string sectorDetectado = "";
                var lineas = respuesta.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var linea in lineas)
                {
                    string l = linea.Trim();
                    int idx = l.IndexOf("Sector detectado:", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        sectorDetectado = l.Substring(idx + "Sector detectado:".Length).Trim();
                        break;
                    }
                }
                return (false, sectorDetectado);
            }
            catch (OperationCanceledException) { throw; }
            catch (VpnRequeridaException) { throw; }
            catch
            {
                // Si falla la validación, ser tolerante — no bloqueamos el análisis
                // por errores infra (la validación es una capa adicional, no crítica).
                return (true, "");
            }
        }

        // Pregunta a la IA cuántos empleados menciona el informe. Devuelve el número
        // detectado, o -1 si no se puede determinar / la llamada falla.
        // Se usa para comparar con el registrado en la empresa y detectar inconsistencias
        // (RF derivado de corrección del tutor: cantidad de empleados debe encajar).
        public async Task<int> DetectarEmpleadosPDF(string textoInforme, CancellationToken ct = default)
        {
            try
            {
                string prompt = _constructorPrompt.PromptDetectarEmpleados(textoInforme);
                string respuesta = await _clienteIA.EnviarMensaje(prompt, null, ct);
                string norm = respuesta.Trim();

                // Extraer el primer número entero (con signo opcional) que aparezca
                var match = System.Text.RegularExpressions.Regex.Match(norm, @"-?\d+");
                if (!match.Success) return -1;
                if (!int.TryParse(match.Value, out int n)) return -1;
                return n;
            }
            catch (OperationCanceledException) { throw; }
            catch (VpnRequeridaException) { throw; }
            catch
            {
                // Si falla la detección, ser tolerante — no bloqueamos el análisis
                return -1;
            }
        }

        // Realiza el diagnóstico completo utilizando Claude, devuelve el diagnóstico estructurado y el texto crudo de la respuesta
        public async Task<(Diagnostico diagnostico, string textoCrudo)> RealizarDiagnostico(string textoInforme, Empresa empresa, CancellationToken ct = default)
        {
            string promptSistema = _constructorPrompt.PromptSistema();
            string promptAnalisis = _constructorPrompt.PromptAnalisisInforme(
                textoInforme,
                empresa.Nombre,
                empresa.Sector
            );

            string textoCrudo = await _clienteIA.EnviarMensaje(promptAnalisis, promptSistema, ct);
            Diagnostico diagnostico = ParsearRespuesta(textoCrudo); // El método ParsearRespuesta se encarga de extraer el nivel de madurez, fortalezas, debilidades, riesgos y recomendaciones del texto crudo devuelto por Claude

            return (diagnostico, textoCrudo); // Devuelve tanto el diagnóstico estructurado como el texto crudo para que pueda ser almacenado o revisado posteriormente
        }

        // Normaliza un texto: minúsculas, sin acentos, sin espacios extras
        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";

            string resultado = texto.ToLower().Trim();

            // Quitar acentos (descomponer caracteres y eliminar diacríticos)
            var sb = new StringBuilder();
            foreach (char c in resultado.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        // Quita sufijos legales del nombre de empresa (C.A., S.A., etc.)
        private string QuitarSufijosLegales(string textoNormalizado)
        {
            string[] sufijos = {
                ", c.a.", ", c.a", " c.a.", " c.a",
                ", s.a.", ", s.a", " s.a.", " s.a",
                ", s.r.l.", ", s.r.l", " s.r.l.", " s.r.l",
                ", s.l.", ", s.l", " s.l.", " s.l",
                ", corp.", " corp", " inc.", " inc",
                " ltd.", " ltd", " llc"
            };

            string resultado = textoNormalizado.Trim();
            foreach (var sufijo in sufijos)
            {
                if (resultado.EndsWith(sufijo))
                {
                    resultado = resultado.Substring(0, resultado.Length - sufijo.Length).Trim();
                    break;
                }
            }

            return resultado;
        }

        // Parsea la respuesta de texto de Claude a un objeto Diagnostico estructurado
        public Diagnostico ParsearRespuesta(string textoClaude)
        {
            var diagnostico = new Diagnostico
            {
                FechaGeneracion = DateTime.Now,
                EsFinal = false,
                ResumenEmpresa = LimpiarMarkdown(ExtraerSeccion(textoClaude, "RESUMEN DE LA EMPRESA", "NIVEL DE MADUREZ")),
                NivelMadurez = ExtraerNivel(textoClaude),
                Fortalezas = LimpiarMarkdown(ExtraerSeccion(textoClaude, "FORTALEZAS", "DEBILIDADES")),
                Debilidades = LimpiarMarkdown(ExtraerSeccion(textoClaude, "DEBILIDADES", "RIESGOS")),
                Riesgos = LimpiarMarkdown(ExtraerSeccion(textoClaude, "RIESGOS", "RECOMENDACIONES")),
                Recomendaciones = LimpiarMarkdown(ExtraerSeccion(textoClaude, "RECOMENDACIONES", "PREGUNTAS"))
            };

            return diagnostico;
        }

        // Extrae el nivel de madurez (número entre 1 y 5) del texto
        private int ExtraerNivel(string texto)
        {

            int idx = texto.IndexOf("NIVEL DE MADUREZ", StringComparison.OrdinalIgnoreCase); // Buscar la frase que precede al número del nivel de madurez, si no lo encuentra devuelve -1
            if (idx == -1) return 0; // Si no se encuentra la frase, se asume que no se pudo extraer el nivel de madurez

            // Se toman los proximos 100 caracteres despues de encontrar el nivel de madurez
            int finBusqueda = Math.Min(idx + 100, texto.Length); // Limitar la búsqueda para evitar leer todo el texto innecesariamente
            string fragmento = texto.Substring(idx, finBusqueda - idx); // El fragmento donde se espera encontrar el número del nivel de madurez

            foreach (char c in fragmento) //Recorre caracter por caracter buscando el primer dígito entre 1 y 5, que es el nivel de madurez
            {
                if (c >= '1' && c <= '5')
                {
                    return c - '0'; //si no lo encuentra devuelve 0



                }



            }

            return 0;
        }

        // Extrae el contenido de una sección entre dos marcadores
        private string ExtraerSeccion(string texto, string marcadorInicio, string marcadorFin)
        {
            int idxInicio = texto.IndexOf(marcadorInicio, StringComparison.OrdinalIgnoreCase); // Buscar el marcador de inicio de la sección, si no lo encuentra devuelve -1
            if (idxInicio == -1) return ""; // Si no se encuentra el marcador de inicio lo devuelve vacioi

            // El contenido real empieza DESPUÉS del marcador
            int inicioContenido = idxInicio + marcadorInicio.Length;

            // Buscar el marcador final desde donde termina el inicial
            int idxFin = texto.IndexOf(marcadorFin, inicioContenido, StringComparison.OrdinalIgnoreCase); // Buscar el marcador de fin de la sección, si no lo encuentra devuelve -1
            if (idxFin == -1) idxFin = texto.Length; // Si no se encuentra el marcador de fin, se asume que la sección llega hasta el final del texto

            string contenido = texto.Substring(inicioContenido, idxFin - inicioContenido); // Extraer el contenido entre los dos marcadores

            // Limpiar caracteres iniciales como ":", saltos de línea, espacios
            contenido = contenido.TrimStart(':', '\n', '\r', ' ', '-', '\t');

            return contenido.Trim();
        }

        // Limpia caracteres de formato Markdown que Claude pudiera haber dejado
        private string LimpiarMarkdown(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;

            // Quitar negritas: **texto** → texto
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\*\*(.+?)\*\*", "$1");

            // Quitar cursivas: *texto* → texto
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"(?<!\*)\*(?!\*)([^*]+?)\*(?!\*)", "$1");

            // Quitar headers: ### Título → Título
            texto = System.Text.RegularExpressions.Regex.Replace(
                texto, @"^#{1,6}\s+", "",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            // Quitar separadores horizontales: ---
            texto = System.Text.RegularExpressions.Regex.Replace(
                texto, @"^---+$", "",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            // Quitar líneas que parecen tablas (empiezan y terminan con |)
            texto = System.Text.RegularExpressions.Regex.Replace(
                texto, @"^\|.*\|.*$", "",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            // Eliminar líneas en blanco múltiples (3+ saltos → 2 saltos)
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\n{3,}", "\n\n");

            return texto.Trim();
        }


    }
}