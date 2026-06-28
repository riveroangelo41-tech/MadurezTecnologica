using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    /// <summary>
    /// Motor de chat offline. Responde mensajes del usuario sin necesidad de IA,
    /// detectando intenciones por keywords y respondiendo con plantillas contextualizadas
    /// con el último diagnóstico de la empresa (si existe).
    /// </summary>
    public class MotorChatOffline
    {
        // Familias de keywords para clasificar la intención del usuario
        private readonly string[] _intencionSaludo = new[]
        {
            "hola", "buenos días", "buen día", "buenas", "saludos", "hey", "qué tal", "que tal"
        };

        private readonly string[] _intencionNivel = new[]
        {
            "nivel", "cmmi", "cuanto", "cuánto", "cual es mi nivel", "cuál es mi nivel",
            "qué nivel", "que nivel", "puntaje", "score", "calificación", "calificacion"
        };

        private readonly string[] _intencionFortalezas = new[]
        {
            "fortaleza", "fortalezas", "fuerte", "fuertes", "bueno", "buenas prácticas",
            "buenas practicas", "qué hago bien", "que hago bien", "qué tenemos bien"
        };

        private readonly string[] _intencionDebilidades = new[]
        {
            "debilidad", "debilidades", "débil", "debil", "problema", "problemas", "falla",
            "fallas", "qué falla", "que falla", "qué falta", "que falta"
        };

        private readonly string[] _intencionRiesgos = new[]
        {
            "riesgo", "riesgos", "peligro", "amenaza", "amenazas", "vulnerabilidad",
            "vulnerabilidades", "exposición", "exposicion"
        };

        private readonly string[] _intencionRecomendaciones = new[]
        {
            "recomendación", "recomendacion", "recomendaciones", "qué hago", "que hago",
            "qué debo", "que debo", "sugerencia", "sugerencias", "consejo", "consejos",
            "mejorar", "próximo paso", "proximo paso", "siguiente paso", "cómo subir", "como subir"
        };

        private readonly string[] _intencionResumen = new[]
        {
            "resumen", "explica", "explícame", "explicame", "cuéntame", "cuentame",
            "dime", "describe", "qué dice", "que dice"
        };

        private readonly string[] _intencionAyuda = new[]
        {
            "ayuda", "ayúdame", "ayudame", "qué puedo preguntar", "que puedo preguntar",
            "que sabes hacer", "qué sabes hacer", "menú", "menu", "opciones"
        };

        private readonly string[] _intencionComparar = new[]
        {
            "comparar", "compara", "diferencia", "diferencias", "qué cambió", "que cambio",
            "qué cambió", "evolución", "evolucion", "progreso", "vs anterior", "vs el anterior",
            "comparación", "comparacion", "antes y ahora"
        };

        private readonly string[] _intencionExportar = new[]
        {
            "exportar", "descargar", "guardar", "pdf", "documento", "imprimir",
            "compartir", "copiar"
        };

        private readonly string[] _intencionDetalle = new[]
        {
            "más detalle", "mas detalle", "más información", "mas informacion",
            "explica más", "explica mas", "profundiza", "extiende", "amplía", "amplia",
            "más completo", "mas completo", "explícalo mejor", "explicalo mejor"
        };

        public string GenerarRespuesta(string mensajeUsuario, Diagnostico? ultimoDiagnostico, Empresa? empresa)
        {
            return GenerarRespuesta(mensajeUsuario, ultimoDiagnostico, empresa, null);
        }

        public string GenerarRespuesta(string mensajeUsuario, Diagnostico? ultimoDiagnostico,
            Empresa? empresa, Diagnostico? diagnosticoAnterior)
        {
            if (string.IsNullOrWhiteSpace(mensajeUsuario))
                return "No detecté ningún texto en tu mensaje. ¿Puedes repetirlo?";

            string mensajeLower = mensajeUsuario.ToLowerInvariant().Trim();

            // 1. Detectar intención (orden importa: las más específicas primero)
            if (Coincide(mensajeLower, _intencionSaludo))
                return GenerarSaludo(empresa);

            if (Coincide(mensajeLower, _intencionAyuda))
                return GenerarAyuda();

            if (Coincide(mensajeLower, _intencionComparar))
                return GenerarComparacion(ultimoDiagnostico, diagnosticoAnterior);

            if (Coincide(mensajeLower, _intencionExportar))
                return GenerarRespuestaExportar();

            if (Coincide(mensajeLower, _intencionDetalle))
                return GenerarRespuestaDetalle(ultimoDiagnostico);

            if (Coincide(mensajeLower, _intencionNivel))
                return GenerarRespuestaNivel(ultimoDiagnostico);

            if (Coincide(mensajeLower, _intencionFortalezas))
                return GenerarSeccion("Fortalezas", ultimoDiagnostico?.Fortalezas, "✅");

            if (Coincide(mensajeLower, _intencionDebilidades))
                return GenerarSeccion("Debilidades", ultimoDiagnostico?.Debilidades, "⚠️");

            if (Coincide(mensajeLower, _intencionRiesgos))
                return GenerarSeccion("Riesgos", ultimoDiagnostico?.Riesgos, "🔴");

            if (Coincide(mensajeLower, _intencionRecomendaciones))
                return GenerarSeccion("Recomendaciones", ultimoDiagnostico?.Recomendaciones, "💡");

            if (Coincide(mensajeLower, _intencionResumen))
                return GenerarSeccion("Resumen", ultimoDiagnostico?.ResumenEmpresa, "📄");

            // 2. No detecté intención clara → respuesta por defecto
            return GenerarRespuestaPorDefecto(ultimoDiagnostico);
        }

        private string GenerarComparacion(Diagnostico? actual, Diagnostico? anterior)
        {
            if (actual == null)
                return "No hay diagnóstico actual para comparar. Carga un PDF para empezar.";

            if (anterior == null)
                return "Solo tienes un diagnóstico hasta ahora, no hay con qué comparar.\n\n" +
                       "Cuando refines tu diagnóstico (botón \"+ Generar evaluación\"), tendrás " +
                       "una versión anterior con la cual comparar.";

            int diferenciaNivel = actual.NivelMadurez - anterior.NivelMadurez;
            string tendencia = diferenciaNivel switch
            {
                > 0 => $"📈 Subiste {diferenciaNivel} nivel(es) — de {anterior.NivelMadurez} a {actual.NivelMadurez}",
                < 0 => $"📉 Bajaste {Math.Abs(diferenciaNivel)} nivel(es) — de {anterior.NivelMadurez} a {actual.NivelMadurez}",
                _ => $"📊 Te mantienes en el mismo nivel: {actual.NivelMadurez}"
            };

            return $"COMPARACIÓN CON EL DIAGNÓSTICO ANTERIOR\n\n" +
                   $"{tendencia}\n\n" +
                   $"📅 Anterior: {anterior.FechaGeneracion:dd/MM/yyyy} (Nivel {anterior.NivelMadurez})\n" +
                   $"📅 Actual:   {actual.FechaGeneracion:dd/MM/yyyy} (Nivel {actual.NivelMadurez})\n\n" +
                   $"Para ver detalles específicos de qué cambió en cada sección, ve a 'Historial' " +
                   $"y abre cada diagnóstico para revisarlos lado a lado.\n\n" +
                   $"Para análisis cualitativos detallados de la evolución, activa el modo conectado y " +
                   $"pídele a la IA un análisis comparativo.";
        }

        private string GenerarRespuestaExportar()
        {
            return "Para exportar tu diagnóstico, ve a la sección \"Historial\" en el menú lateral.\n\n" +
                   "Ahí encontrarás el botón \"📥 Exportar todo\" que genera un archivo .txt con todos " +
                   "los diagnósticos de la empresa activa, listo para compartir o imprimir.\n\n" +
                   "Esta función está disponible tanto en modo conectado como offline.";
        }

        private string GenerarRespuestaDetalle(Diagnostico? diag)
        {
            if (diag == null)
                return "Aún no hay diagnóstico para profundizar. Carga un PDF primero.";

            return "Estoy en modo offline y mis respuestas son resúmenes basados en plantillas. " +
                   "Para análisis más profundos y matizados sobre tu situación específica:\n\n" +
                   "• Activa el modo conectado (indicador del header)\n" +
                   "• Pregúntale directamente a la IA sobre lo que quieras explorar\n" +
                   "• La IA tiene acceso a toda la conversación previa y puede dar respuestas " +
                   "personalizadas y profundas\n\n" +
                   "Mientras tanto, puedo darte info estructurada del diagnóstico actual " +
                   "(escribe \"resumen\", \"fortalezas\", \"debilidades\", \"riesgos\" o \"recomendaciones\").";
        }

        private bool Coincide(string mensajeLower, string[] keywords)
        {
            foreach (var kw in keywords)
            {
                if (mensajeLower.Contains(kw))
                    return true;
            }
            return false;
        }

        private string GenerarSaludo(Empresa? empresa)
        {
            string nombreEmpresa = empresa?.Nombre ?? "tu empresa";
            return $"¡Hola! Estoy funcionando en modo offline.\n\n" +
                   $"Puedo responder preguntas básicas sobre el último diagnóstico de {nombreEmpresa} " +
                   $"usando plantillas locales (sin consultar a la IA).\n\n" +
                   $"Pregúntame sobre: nivel CMMI, fortalezas, debilidades, riesgos, recomendaciones, " +
                   $"o escribe \"ayuda\" para ver las opciones.";
        }

        private string GenerarAyuda()
        {
            return "Estoy en modo offline. Puedo responder a estas preguntas:\n\n" +
                   "📊 SOBRE EL DIAGNÓSTICO ACTUAL\n" +
                   "• \"¿Cuál es mi nivel CMMI?\" — te digo el nivel detectado\n" +
                   "• \"¿Cuáles son mis fortalezas?\" — lista las fortalezas\n" +
                   "• \"¿Y mis debilidades?\" — lista las debilidades\n" +
                   "• \"¿Qué riesgos hay?\" — lista los riesgos\n" +
                   "• \"¿Qué recomiendas?\" — te doy las recomendaciones\n" +
                   "• \"Hazme un resumen\" — repaso general\n\n" +
                   "📈 SOBRE LA EVOLUCIÓN\n" +
                   "• \"Compárame con el anterior\" — diferencia entre diagnósticos\n" +
                   "• \"¿Qué cambió?\" — comparación rápida\n\n" +
                   "📁 OTRAS ACCIONES\n" +
                   "• \"Exportar\" — dónde encontrar la opción de exportar\n" +
                   "• \"Más detalle\" — cómo profundizar usando la IA\n\n" +
                   "Para conversaciones más profundas y análisis a medida, desactiva el modo offline " +
                   "desde el indicador del header para usar la IA.";
        }

        private string GenerarRespuestaNivel(Diagnostico? diag)
        {
            if (diag == null)
                return "Aún no hay un diagnóstico generado para esta empresa. " +
                       "Carga un PDF en la sección 'Cargar Informe' para generar el primer análisis.";

            string tipo = diag.EsFinal ? "final" : "intermedio";
            return $"Según el último diagnóstico {tipo} (modo offline), el nivel CMMI detectado es:\n\n" +
                   $"📊 Nivel {diag.NivelMadurez}\n\n" +
                   $"Este valor proviene de la detección de patrones en el informe. Si tienes conexión, " +
                   $"la IA puede dar un análisis más matizado.";
        }

        private string GenerarSeccion(string titulo, string? contenido, string emoji)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return $"No hay información sobre {titulo.ToLower()} en el diagnóstico actual.\n\n" +
                       "Carga un PDF en 'Cargar Informe' o genera un nuevo diagnóstico para tener datos.";

            return $"{emoji} {titulo.ToUpper()}\n\n{contenido}\n\n" +
                   "Esta información proviene del motor offline. Para un análisis más detallado, " +
                   "vuelve al modo conectado.";
        }

        private string GenerarRespuestaPorDefecto(Diagnostico? diag)
        {
            if (diag == null)
                return "Estoy en modo offline y aún no hay diagnóstico cargado para esta empresa. " +
                       "Sube un informe en PDF en la sección 'Cargar Informe' para empezar.\n\n" +
                       "Mientras tanto, puedes escribirme \"ayuda\" para ver qué puedo responder.";

            return "Estoy en modo offline y no logré detectar tu intención con claridad.\n\n" +
                   "Te sugiero preguntarme sobre nivel CMMI, fortalezas, debilidades, riesgos o " +
                   "recomendaciones del último diagnóstico. Escribe \"ayuda\" para ver todas las " +
                   "opciones disponibles.\n\n" +
                   "Para conversaciones más libres y análisis personalizado, activa el modo conectado " +
                   "desde el indicador del header.";
        }
    }
}
