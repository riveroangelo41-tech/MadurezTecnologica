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

REGLAS ESTRICTAS DE FORMATO (obligatorias):
- NO uses formato Markdown: nada de **negritas**, ## títulos, tablas, líneas separadoras (---)
- NO uses emojis ni íconos decorativos (🔴, ✅, etc.)
- NO añadas texto introductorio antes de cada sección
- Cada sección debe iniciar DIRECTAMENTE con el número y título indicados
- Las listas usan guión simple ""-"" al inicio de cada elemento
- Texto plano, profesional, sin adornos visuales

ESTRUCTURA OBLIGATORIA DE LA RESPUESTA (sigue exactamente este formato):

1. RESUMEN DE LA EMPRESA:
[Párrafo de 4 a 6 líneas describiendo qué hace la empresa, su tamaño, sector, productos principales y características generales que identificaste del informe. Sé descriptivo y objetivo.]

2. NIVEL DE MADUREZ: [número del 1 al 5]
[Justificación de 2 a 3 líneas en texto plano]

3. FORTALEZAS:
- [Fortaleza 1]: [Evidencia textual del informe]
- [Fortaleza 2]: [Evidencia textual del informe]
- [Fortaleza 3]: [Evidencia textual del informe]

4. DEBILIDADES:
- [Debilidad 1]: [Evidencia textual del informe]
- [Debilidad 2]: [Evidencia textual del informe]
- [Debilidad 3]: [Evidencia textual del informe]

5. RIESGOS:
- [Riesgo 1]: [Descripción del impacto y probabilidad]
- [Riesgo 2]: [Descripción del impacto y probabilidad]
- [Riesgo 3]: [Descripción del impacto y probabilidad]

6. RECOMENDACIONES:
- [Recomendación 1]: [Acción concreta y priorizada]
- [Recomendación 2]: [Acción concreta y priorizada]
- [Recomendación 3]: [Acción concreta y priorizada]

7. PREGUNTAS PARA EL USUARIO:
- [Pregunta 1]
- [Pregunta 2]

Sé específico, justificado y técnico. Si el informe es insuficiente, indícalo en la sección correspondiente en lugar de inventar conclusiones.";
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

        // Prompt para validar si un texto corresponde a una empresa determinada
        public string PromptValidacionCoherencia(string textoInforme, string nombreEmpresa, string sector)
        {
            // Recortar el texto si es muy largo para ahorrar tokens
            string textoRecortado = textoInforme.Length > 1500
                ? textoInforme.Substring(0, 1500) + "..."
                : textoInforme;

            return $@"Analiza el siguiente fragmento de texto y determina si corresponde a un informe empresarial de la empresa indicada.

EMPRESA REGISTRADA: {nombreEmpresa}
SECTOR: {sector}

FRAGMENTO DEL INFORME:
---
{textoRecortado}
---

Responde EXCLUSIVAMENTE con una sola palabra, sin explicaciones ni puntuación adicional:
- SI (si el texto claramente corresponde a la empresa indicada)
- NO (si el texto corresponde a una empresa diferente, o no es un informe empresarial)";
        }


    }
}