using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    public class MotorOffline
    {
        
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

            // Contar matches de cada nivel
            int puntos1 = ContarMatches(textoNormalizado, _keywordsNivel1);
            int puntos2 = ContarMatches(textoNormalizado, _keywordsNivel2);
            int puntos3 = ContarMatches(textoNormalizado, _keywordsNivel3);
            int puntos4 = ContarMatches(textoNormalizado, _keywordsNivel4);
            int puntos5 = ContarMatches(textoNormalizado, _keywordsNivel5);

            // Determinar el nivel ganador
            int nivelDeterminado = DeterminarNivel(puntos1, puntos2, puntos3, puntos4, puntos5);

            // Construir diagnóstico estructurado
            var diagnostico = new Diagnostico
            {
                NivelMadurez = nivelDeterminado,
                ResumenEmpresa = GenerarResumenEmpresa(empresa, nivelDeterminado),
                Fortalezas = GenerarFortalezas(nivelDeterminado),
                Debilidades = GenerarDebilidades(nivelDeterminado),
                Riesgos = GenerarRiesgos(nivelDeterminado),
                Recomendaciones = GenerarRecomendaciones(nivelDeterminado),
                FechaGeneracion = DateTime.Now,
                EsFinal = false
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

        private int DeterminarNivel(int p1, int p2, int p3, int p4, int p5)
        {
            // Estrategia: el nivel con más matches gana
            // En caso de empate, gana el nivel MÁS BAJO (más conservador)

            int max = Math.Max(p1, Math.Max(p2, Math.Max(p3, Math.Max(p4, p5))));

            // Si nadie tiene matches, asumimos nivel 1 (caso peor)
            if (max == 0) return 1;

            if (p1 == max) return 1;
            if (p2 == max) return 2;
            if (p3 == max) return 3;
            if (p4 == max) return 4;
            return 5;
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