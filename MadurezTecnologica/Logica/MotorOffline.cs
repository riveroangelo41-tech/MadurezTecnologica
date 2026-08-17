using MadurezTecnologica.Datos;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    public class MotorOffline
    {
        // Paquete heurístico activo (destilado de Claude por el Destilador).
        // Se carga bajo demanda; si no hay paquete activo se comporta como antes.
        private PaqueteHeuristico? _paqueteActivo;
        private bool _paqueteCargado = false;

        private PaqueteHeuristico? PaqueteActivo
        {
            get
            {
                if (!_paqueteCargado)
                {
                    try { _paqueteActivo = new RepositorioPaqueteHeuristico().ObtenerActivo(); }
                    catch { _paqueteActivo = null; }
                    _paqueteCargado = true;
                }
                return _paqueteActivo;
            }
        }

        
        // Estas son las palabras o frases que indican prácticas asociadas a cada nivel de madurez
       

        private readonly string[] _keywordsNivel1 = new[]
        {
            "ad-hoc", "ad hoc", "no documentado", "informal", "caótico",
            "sin proceso", "sin metodología", "whatsapp", "correo electrónico",
            "no tenemos pruebas", "no tenemos tests", "sin documentación",
            "no hay estándares", "no formalizado", "sin formalizar",
            "cada uno como puede", "depende de la persona", "en la cabeza"
        };

        private readonly string[] _keywordsNivel2 = new[]
        {
            "git", "control de versiones", "github", "gitlab", "bitbucket",
            "algunas pruebas", "pruebas básicas", "cierto control",
            "control básico", "respaldo manual", "documentación básica",
            "metodología básica", "scrum básico", "kanban básico"
        };

        private readonly string[] _keywordsNivel3 = new[]
        {
            "scrum", "kanban", "metodología formal", "metodología documentada",
            "code review obligatorio", "revisiones de código obligatorias",
            "estándares documentados", "convenciones de código",
            "documentación de arquitectura", "iso 27001", "iso 9001",
            "ambientes separados", "staging", "ci/cd básico",
            "sprints", "retrospectivas formales", "definition of done"
        };

        private readonly string[] _keywordsNivel4 = new[]
        {
            "métricas", "kpi", "kpis", "velocity", "cycle time", "lead time",
            "cobertura de pruebas", "defect density", "sla", "slo", "sli",
            "monitoreo activo", "grafana", "datadog", "sonarqube",
            "story points", "planning poker", "monte carlo", "estadística",
            "iso/iec 25010", "iso 27001", "soc 2", "pci dss",
            "métrica organizacional", "indicador cuantitativo"
        };

        private readonly string[] _keywordsNivel5 = new[]
        {
            "a/b testing", "experimentación", "chaos engineering",
            "machine learning", "ml predictivo", "mejora continua",
            "auto-optimización", "automatización avanzada",
            "pruebas mutacionales", "bug bounty", "innovación estructurada",
            "patentes", "investigación y desarrollo", "i+d",
            "gremlin", "stryker", "experimentos en producción",
            "feature flags", "canary releases", "blue-green deployment"
        };

    
       
        public Diagnostico AnalizarTexto(string textoInforme, Empresa empresa)
        {
            if (string.IsNullOrWhiteSpace(textoInforme))
            {
                throw new ArgumentException("El texto del informe está vacío.");
            }

            // Normalizar texto para hacer matching insensible a mayúsculas
            string textoNormalizado = textoInforme.ToLowerInvariant();

            // Contar matches de las listas BASE (peso 1 cada uno)
            double p1 = ContarMatches(textoNormalizado, _keywordsNivel1);
            double p2 = ContarMatches(textoNormalizado, _keywordsNivel2);
            double p3 = ContarMatches(textoNormalizado, _keywordsNivel3);
            double p4 = ContarMatches(textoNormalizado, _keywordsNivel4);
            double p5 = ContarMatches(textoNormalizado, _keywordsNivel5);

            // Aporte de los indicadores DESTILADOS del paquete activo (peso = asociación).
            // Si no hay paquete activo, este bloque no altera nada.
            var paquete = PaqueteActivo;
            if (paquete != null)
            {
                foreach (var ind in paquete.Indicadores)
                {
                    if (textoNormalizado.Contains(ind.Termino)
                        && !ContextoNegado(textoNormalizado, ind.Termino))
                    {
                        switch (ind.Nivel)
                        {
                            case 1: p1 += ind.Peso; break;
                            case 2: p2 += ind.Peso; break;
                            case 3: p3 += ind.Peso; break;
                            case 4: p4 += ind.Peso; break;
                            case 5: p5 += ind.Peso; break;
                        }
                    }
                }
            }

            int nivelDeterminado = DeterminarNivel(p1, p2, p3, p4, p5);

            // Construir diagnóstico estructurado. Marcamos Origen="OFFLINE" para que
            // el Destilador NO aprenda de sus propios outputs (evita loop degradante).
            var diagnostico = new Diagnostico
            {
                NivelMadurez = nivelDeterminado,
                ResumenEmpresa = GenerarResumenEmpresa(empresa, nivelDeterminado),
                Fortalezas = GenerarFortalezas(nivelDeterminado),
                Debilidades = GenerarDebilidades(nivelDeterminado),
                Riesgos = GenerarRiesgos(nivelDeterminado),
                Recomendaciones = GenerarRecomendacionesPorGaps(textoNormalizado, nivelDeterminado),
                FechaGeneracion = DateTime.Now,
                EsFinal = false,
                Origen = "OFFLINE"
            };

            return diagnostico;
        }

        private readonly string[] _palabrasNegacion = new[]
        {
            "no ", "sin ", "ausencia de ", "no hay ", "no tienen ", "no tenemos ",
            "no usan ", "no usamos ", "no aplican ", "no se aplica ", "no existen ",
            "no existe ", "falta ", "carece ", "carecen ", "no está ", "no están ",
            "ningún ", "ninguna ", "nunca "
            };

        private int ContarMatches(string texto, string[] keywords)
        {
            int total = 0;

            foreach (var kw in keywords)
            {
                int posicion = 0;
                while ((posicion = texto.IndexOf(kw, posicion)) != -1)
                {
                    // Verificar si hay una palabra de negación en los 40 caracteres anteriores
                    int inicio = Math.Max(0, posicion - 40);
                    int longitudContexto = posicion - inicio;
                    string contextoAnterior = texto.Substring(inicio, longitudContexto);

                    bool tieneNegacion = false;
                    foreach (var neg in _palabrasNegacion)
                    {
                        if (contextoAnterior.Contains(neg))
                        {
                            tieneNegacion = true;
                            break;
                        }
                    }

                    // Solo contar si NO está negada
                    if (!tieneNegacion)
                    {
                        total++;
                    }

                    posicion += kw.Length;
                }
            }

            return total;
        }

        private int DeterminarNivel(double p1, double p2, double p3, double p4, double p5)
        {
            // Estrategia: el nivel con mayor puntaje gana
            // En caso de empate, gana el nivel MÁS BAJO (más conservador)
            double max = Math.Max(p1, Math.Max(p2, Math.Max(p3, Math.Max(p4, p5))));

            if (max <= 0) return 1;
            if (p1 == max) return 1;
            if (p2 == max) return 2;
            if (p3 == max) return 3;
            if (p4 == max) return 4;
            return 5;
        }

        // Detector de negación público a nivel de este método (mismo criterio que ContarMatches:
        // busca negadores en los 40 chars anteriores). Reutilizado por el aporte destilado.
        private bool ContextoNegado(string texto, string termino)
        {
            int pos = texto.IndexOf(termino);
            if (pos < 0) return false;
            int inicio = Math.Max(0, pos - 40);
            string contexto = texto.Substring(inicio, pos - inicio);
            foreach (var neg in _palabrasNegacion)
                if (contexto.Contains(neg)) return true;
            return false;
        }

        // ===================================================
        // GENERACIÓN DE SECCIONES DEL DIAGNÓSTICO
        // ===================================================

        private string GenerarResumenEmpresa(Empresa empresa, int nivel)
        {
            return $"Análisis offline de la empresa {empresa.Nombre}. " +
                   $"Se realizó una evaluación automática basada en detección de patrones en el informe " +
                   $"proporcionado. El sistema identificó indicios compatibles con el nivel CMMI {nivel} " +
                   $"de madurez tecnológica. Este análisis es preliminar; para una evaluación más precisa " +
                   $"se recomienda complementar con el análisis IA cuando haya conexión disponible.";
        }

        private string GenerarFortalezas(int nivel)
        {
            switch (nivel)
            {
                case 1: return "- La empresa cuenta con personal técnico activo en proyectos de desarrollo.";
                case 2:
                    return "- Uso de sistemas de control de versiones (Git).\n" +
                              "- Existen algunas prácticas básicas de gestión.\n" +
                              "- Procesos rudimentarios implementados.";
                case 3:
                    return "- Metodologías formales de desarrollo implementadas (Scrum, Kanban).\n" +
                              "- Procesos documentados a nivel organizacional.\n" +
                              "- Revisiones de código formalizadas.\n" +
                              "- Ambientes separados de desarrollo y producción.";
                case 4:
                    return "- Procesos medidos cuantitativamente con métricas formales.\n" +
                              "- Indicadores y KPIs establecidos.\n" +
                              "- Control estadístico de procesos.\n" +
                              "- Pruebas automatizadas con cobertura significativa.";
                case 5:
                    return "- Cultura de mejora continua basada en datos.\n" +
                              "- Experimentación sistemática (A/B testing).\n" +
                              "- Procesos auto-optimizados.\n" +
                              "- Innovación estructurada y formal.";
                default: return "- Información insuficiente para determinar fortalezas.";
            }
        }

        private string GenerarDebilidades(int nivel)
        {
            switch (nivel)
            {
                case 1:
                    return "- Ausencia de procesos formales de desarrollo.\n" +
                              "- Falta de documentación técnica y de procesos.\n" +
                              "- Sin sistema de control de calidad estructurado.\n" +
                              "- Alta dependencia de personas específicas.";
                case 2:
                    return "- Procesos aplicados inconsistentemente entre proyectos.\n" +
                              "- Falta de estándares organizacionales.\n" +
                              "- Documentación escasa o desactualizada.\n" +
                              "- Pruebas no sistematizadas.";
                case 3:
                    return "- Sin métricas cuantitativas formales de procesos.\n" +
                              "- Falta de KPIs consolidados a nivel organizacional.\n" +
                              "- Mejora basada en intuición más que en datos.";
                case 4:
                    return "- Ausencia de experimentación sistemática.\n" +
                              "- Mejora continua no automatizada.\n" +
                              "- Datos no se traducen automáticamente en cambios de proceso.";
                case 5:
                    return "- Optimizaciones marginales posibles en frontera con IA generativa.\n" +
                              "- Reto de mantener nivel de innovación a escala.";
                default: return "- Información insuficiente para determinar debilidades.";
            }
        }

        private string GenerarRiesgos(int nivel)
        {
            switch (nivel)
            {
                case 1:
                    return "- Alto riesgo de retrasos y defectos en entregas.\n" +
                              "- Dependencia crítica de personal clave (bus factor bajo).\n" +
                              "- Vulnerabilidad ante incidentes de seguridad.\n" +
                              "- Imposibilidad de escalar operaciones de forma controlada.";
                case 2:
                    return "- Calidad inconsistente entre proyectos.\n" +
                              "- Riesgos de seguridad por procesos no estandarizados.\n" +
                              "- Pérdida de conocimiento ante rotación de personal.";
                case 3:
                    return "- Falta de evidencia cuantitativa para decisiones estratégicas.\n" +
                              "- Riesgo de estancamiento sin métricas de mejora.";
                case 4:
                    return "- Riesgo de optimización local sin visión sistémica.\n" +
                              "- Posible burocratización si las métricas no se accionan.";
                case 5: return "- Riesgo de complacencia en alto nivel actual.";
                default: return "- Información insuficiente para determinar riesgos.";
            }
        }

        // === RECOMENDACIONES PERSONALIZADAS por gaps detectados ===
        // Identifica qué keywords del nivel SIGUIENTE no están en el texto
        // y genera recomendaciones específicas para esos gaps.
        private string GenerarRecomendacionesPorGaps(string textoNormalizado, int nivelActual)
        {
            // Nivel 5 ya es el máximo: usar plantillas genéricas
            if (nivelActual >= 5) return GenerarRecomendaciones(nivelActual);

            int nivelSiguiente = nivelActual + 1;
            string[] keywordsNivelSiguiente = nivelSiguiente switch
            {
                2 => _keywordsNivel2,
                3 => _keywordsNivel3,
                4 => _keywordsNivel4,
                5 => _keywordsNivel5,
                _ => Array.Empty<string>()
            };

            // Buscar gaps: keywords del nivel siguiente que NO aparecen en el texto
            var gaps = new List<string>();
            foreach (var kw in keywordsNivelSiguiente)
            {
                if (!textoNormalizado.Contains(kw))
                    gaps.Add(kw);
            }

            // Si por alguna razón no hay gaps (ya tienes todo del nivel siguiente),
            // usar las recomendaciones genéricas
            if (gaps.Count == 0) return GenerarRecomendaciones(nivelActual);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Para avanzar del nivel {nivelActual} al nivel {nivelSiguiente}, " +
                          $"el informe sugiere implementar las siguientes prácticas que aún no detectamos:");
            sb.AppendLine();

            // Tomar los primeros gaps relevantes (con descripción humana cuando exista)
            int incluidos = 0;
            int maxRecomendaciones = 6;
            foreach (var gap in gaps)
            {
                string descripcion = DescribirGap(gap);
                if (descripcion == null) continue;   // saltar gaps muy técnicos sin descripción

                sb.AppendLine($"- {descripcion}");
                incluidos++;
                if (incluidos >= maxRecomendaciones) break;
            }

            if (incluidos == 0)
            {
                // Fallback si ningún gap tenía descripción humana
                return GenerarRecomendaciones(nivelActual);
            }

            // Enriquecer con recomendaciones DESTILADAS del paquete activo (si existen)
            // aplicables al nivel siguiente. Máximo 3 para no saturar.
            var paquete = PaqueteActivo;
            if (paquete != null)
            {
                var extras = paquete.Recomendaciones
                    .Where(r => r.Nivel == nivelSiguiente.ToString())
                    .OrderByDescending(r => r.Frecuencia)
                    .Take(3)
                    .ToList();
                if (extras.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Prácticas destiladas del historial de análisis IA (recurrentes en dictámenes previos):");
                    foreach (var r in extras)
                        sb.AppendLine($"- {char.ToUpper(r.Texto[0]) + r.Texto.Substring(1)}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Estas recomendaciones provienen del análisis offline. Para un plan de mejora " +
                          $"más detallado y priorizado, repite el análisis con la IA cuando tengas conexión.");

            return sb.ToString();
        }

        // Diccionario de descripciones humanas para los keywords más relevantes
        private string? DescribirGap(string keyword)
        {
            return keyword switch
            {
                // Nivel 2
                "git" or "control de versiones" or "github" or "gitlab" or "bitbucket"
                    => "Implementar control de versiones con Git (GitHub, GitLab o similar) en todos los proyectos",
                "algunas pruebas" or "pruebas básicas" or "control básico"
                    => "Establecer pruebas unitarias básicas para componentes críticos",
                "respaldo manual"
                    => "Definir una política de respaldos periódicos (aunque sea manual)",
                "documentación básica"
                    => "Crear documentación básica de procesos y arquitectura",
                "metodología básica" or "scrum básico" or "kanban básico"
                    => "Adoptar una metodología ágil simple (Scrum o Kanban básico)",

                // Nivel 3
                "scrum"
                    => "Implementar Scrum como metodología formal (sprints, ceremonias, roles definidos)",
                "kanban"
                    => "Adoptar tableros Kanban para visualizar el flujo de trabajo",
                "metodología formal" or "metodología documentada"
                    => "Documentar formalmente la metodología de desarrollo en un manual interno",
                "code review obligatorio" or "revisiones de código obligatorias"
                    => "Hacer obligatorio el code review en todos los pull requests",
                "estándares documentados" or "convenciones de código"
                    => "Definir y documentar convenciones de código (linters, formato, naming)",
                "documentación de arquitectura"
                    => "Mantener documentación de arquitectura actualizada (diagramas C4, ADRs)",
                "iso 27001" or "iso 9001"
                    => "Considerar certificaciones ISO 27001 (seguridad) o 9001 (calidad)",
                "ambientes separados" or "staging"
                    => "Separar ambientes de desarrollo, staging y producción",
                "ci/cd básico"
                    => "Implementar un pipeline básico de CI/CD (compilar, testear, deployar)",
                "sprints"
                    => "Trabajar en sprints de 2 a 4 semanas con planning y review",
                "retrospectivas formales"
                    => "Realizar retrospectivas formales al cierre de cada sprint",
                "definition of done"
                    => "Definir y documentar una Definition of Done por equipo",

                // Nivel 4
                "métricas" or "métrica organizacional" or "indicador cuantitativo"
                    => "Medir indicadores cuantitativos de procesos (no solo intuitivos)",
                "kpi" or "kpis"
                    => "Establecer KPIs organizacionales documentados",
                "velocity"
                    => "Medir velocity de los equipos para planificar capacidad",
                "cycle time"
                    => "Medir cycle time (de inicio a entrega) para detectar cuellos de botella",
                "lead time"
                    => "Medir lead time (de pedido a entrega) para el cliente",
                "cobertura de pruebas"
                    => "Medir y mejorar la cobertura de pruebas automatizadas",
                "defect density"
                    => "Llevar registro de defect density para detectar áreas problemáticas",
                "sla" or "slo" or "sli"
                    => "Definir SLI/SLO/SLA para servicios críticos",
                "monitoreo activo" or "grafana" or "datadog"
                    => "Implementar monitoreo activo con dashboards (Grafana, Datadog, etc.)",
                "sonarqube"
                    => "Integrar análisis de calidad de código continuo (SonarQube o similar)",
                "story points" or "planning poker"
                    => "Estimar trabajo con story points y planning poker",

                // Nivel 5
                "a/b testing" or "experimentación" or "experimentos en producción"
                    => "Implementar plataforma de A/B testing y experimentación sistemática",
                "chaos engineering" or "gremlin"
                    => "Adoptar Chaos Engineering para validar resiliencia del sistema",
                "machine learning" or "ml predictivo"
                    => "Explorar ML predictivo para optimizar procesos",
                "mejora continua" or "auto-optimización"
                    => "Automatizar ciclos de mejora continua basados en métricas",
                "pruebas mutacionales" or "stryker"
                    => "Implementar pruebas mutacionales para validar la calidad de los tests",
                "feature flags"
                    => "Adoptar feature flags para releases controlados",
                "canary releases" or "blue-green deployment"
                    => "Implementar canary releases o blue-green deployment",
                "bug bounty"
                    => "Considerar un programa de bug bounty para vulnerabilidades",

                _ => null   // gap sin descripción → se ignora
            };
        }

        private string GenerarRecomendaciones(int nivel)
        {
            switch (nivel)
            {
                case 1:
                    return "- Implementar control de versiones (Git) en todos los proyectos.\n" +
                              "- Documentar procesos básicos de desarrollo.\n" +
                              "- Adoptar una metodología ágil simple (Scrum o Kanban).\n" +
                              "- Iniciar prácticas básicas de revisión de código.";
                case 2:
                    return "- Formalizar la metodología de desarrollo.\n" +
                              "- Establecer estándares organizacionales documentados.\n" +
                              "- Implementar revisiones de código obligatorias.\n" +
                              "- Definir y separar ambientes de desarrollo y producción.";
                case 3:
                    return "- Comenzar a medir indicadores cuantitativos (velocity, cycle time).\n" +
                              "- Implementar dashboards de métricas organizacionales.\n" +
                              "- Establecer SLI/SLO para servicios críticos.\n" +
                              "- Aumentar cobertura de pruebas automatizadas.";
                case 4:
                    return "- Implementar plataforma de experimentación (A/B testing).\n" +
                              "- Automatizar la mejora continua basada en datos.\n" +
                              "- Adoptar chaos engineering para mejorar resiliencia.\n" +
                              "- Investigar oportunidades de ML predictivo.";
                case 5:
                    return "- Mantener cultura de innovación a través de R&D continuo.\n" +
                              "- Explorar frontera de IA generativa para desarrollo asistido.\n" +
                              "- Compartir conocimiento mediante publicaciones y open source.";
                default: return "- Información insuficiente para generar recomendaciones específicas.";
            }
        }
    }
}