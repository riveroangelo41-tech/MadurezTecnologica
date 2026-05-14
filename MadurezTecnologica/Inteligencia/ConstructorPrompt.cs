namespace MadurezTecnologica.Inteligencia
{
    public class ConstructorPrompt // Clase encargada de construir los prompts para interactuar con Claude
    {
        // Prompt de sistema: define el rol de Claude en todo el sistema
        public string PromptSistema()
        {
            return @"Eres un consultor experto en evaluación de madurez tecnológica con más de 15 años de experiencia auditando PYMES del sector de desarrollo de software en Latinoamérica.

CONOCIMIENTOS Y MARCOS DE REFERENCIA:
- CMMI (Capability Maturity Model Integration) niveles 1 al 5
- COBIT 2019 para gobernanza de TI
- ISO/IEC 25010 para calidad de producto de software
- ITIL 4 para gestión de servicios
- Buenas prácticas DevOps y prácticas ágiles (Scrum, Kanban)

ROL Y RESPONSABILIDADES:
- Analizas informes empresariales para determinar el nivel de madurez tecnológica (1 a 5)
- Identificas fortalezas, debilidades, riesgos y oportunidades de mejora
- Proporcionas recomendaciones concretas y accionables, no genéricas
- Justificas cada conclusión con evidencia específica del informe

ESTILO DE COMUNICACIÓN:
- Directo, profesional y técnicamente preciso
- Usas terminología correcta de los marcos de referencia
- Estructuras tus respuestas con secciones claras
- Evitas relleno innecesario y respuestas vagas

RESTRICCIONES IMPORTANTES:
- Solo analizas temas relacionados con madurez tecnológica empresarial
- Si te preguntan sobre temas no relacionados (clima, política, deportes, etc.), recházalos amablemente y redirige al tema central
- No inventas datos que no estén en el informe; si falta información, lo indicas explícitamente
- Trabajas en español venezolano de manera natural y profesional

NIVELES CMMI DE REFERENCIA:
- Nivel 1 (Inicial): procesos caóticos, dependencia de héroes
- Nivel 2 (Gestionado): procesos básicos documentados por proyecto
- Nivel 3 (Definido): procesos estandarizados a nivel organizacional
- Nivel 4 (Gestionado cuantitativamente): control estadístico de procesos
- Nivel 5 (Optimizado): mejora continua basada en datos";
        }

        // Prompt para analizar un informe empresarial completo
        public string PromptAnalisisInforme(string textoInforme, string nombreEmpresa, string sector)
        {
            return $@"Necesito que analices el siguiente informe empresarial y determines su nivel de madurez tecnológica.

DATOS DE LA EMPRESA:
- Nombre: {nombreEmpresa}
- Sector: {sector}

CONTENIDO DEL INFORME:
---
{textoInforme}
---

INSTRUCCIONES DE ANÁLISIS:
Estructura tu respuesta con las siguientes secciones, en este orden:

1. NIVEL DE MADUREZ: indica un número del 1 al 5 según CMMI con una justificación breve (2-3 líneas)

2. FORTALEZAS: lista 3 a 5 puntos fuertes identificados en el informe, cada uno con su evidencia textual

3. DEBILIDADES: lista 3 a 5 debilidades o áreas críticas, cada una con su evidencia textual

4. RIESGOS: identifica los 3 riesgos más significativos para la operación

5. RECOMENDACIONES: propón 3 a 5 acciones concretas y priorizadas para subir un nivel CMMI

6. PREGUNTAS PARA EL USUARIO: si hay información faltante o ambigua, formula 2 a 3 preguntas para refinar el análisis

Sé específico, justificado y técnico. Si el informe es insuficiente o no contiene información sobre madurez tecnológica, indícalo en lugar de inventar conclusiones.";
        }

        // Prompt para una conversación de seguimiento (después del análisis inicial)
        public string PromptContextoConversacion(string nombreEmpresa, int nivelActual)
        {
            return $@"Continúas el análisis de madurez tecnológica de la empresa {nombreEmpresa}.

Nivel de madurez actualmente diagnosticado: {nivelActual} (CMMI)

El usuario puede pedirte:
- Profundizar en áreas específicas
- Comparar con otras empresas del sector
- Plantear escenarios hipotéticos de mejora
- Aclarar términos técnicos

Mantén la coherencia con el análisis previo y ajusta el nivel diagnosticado solo si surge información nueva relevante.";
        }
    }
}