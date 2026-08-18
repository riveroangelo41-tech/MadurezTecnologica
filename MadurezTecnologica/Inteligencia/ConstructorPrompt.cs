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

8. MARCOS DE REFERENCIA APLICADOS:
[Lista breve (una línea por marco) de los marcos que USASTE EXPLÍCITAMENTE en este análisis y por qué. Solo menciona los que realmente aplicaste — si un marco no fue relevante para este informe, NO lo incluyas. Formato: ""- [Nombre del marco]: [1-2 líneas de justificación específica de por qué aplicó a este caso]"". Ejemplo válido: ""- CMMI: se usó para determinar el nivel de madurez de procesos (secciones 4 y 7 del informe)"". Ejemplo válido: ""- ISO/IEC 25010: no se aplicó porque el informe no describe atributos de calidad del producto"".]

9. CRITERIO PARA DETERMINAR EL NIVEL CMMI:
[Explicación BREVE (3 a 5 líneas máximo) del criterio concreto que usaste para asignar el nivel CMMI de la sección 2. Menciona las evidencias específicas del informe que soportan el nivel elegido y por qué NO se asignó un nivel inmediatamente superior o inferior. Sé directo y técnico, sin relleno.]

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

        // Prompt para validar si el sector del informe corresponde al sector registrado
        // de la empresa. Se usa como capa adicional de coherencia (RF derivado de
        // la corrección del tutor: no aceptar informes cuyo sector no encaja con lo
        // que el usuario registró). Distinto de PromptValidacionCoherencia, que solo
        // valida nombre; este valida ENCAJE TEMÁTICO del contenido con el sector.
        public string PromptValidacionSector(string textoInforme, string sectorRegistrado)
        {
            string textoRecortado = textoInforme.Length > 2500
                ? textoInforme.Substring(0, 2500) + "..."
                : textoInforme;

            return $@"Analiza el siguiente fragmento de un informe empresarial y determina si el TEMA del informe corresponde al sector indicado.

SECTOR REGISTRADO POR EL USUARIO: {sectorRegistrado}

FRAGMENTO DEL INFORME:
---
{textoRecortado}
---

Criterio de evaluación:
- Responde SI si el informe habla claramente de una empresa que opera en el sector registrado, o si el sector registrado es lo suficientemente cercano al tema del informe como para no ser una inconsistencia grave.
- Responde NO solo si hay una inconsistencia CLARA entre el sector registrado y el contenido del informe (por ejemplo: sector 'Videojuegos' pero el informe habla exclusivamente de un ERP corporativo, o sector 'HealthTech' pero el informe describe una tienda de ropa).
- Sé tolerante: la mayoría de las empresas de software tienen actividades transversales (usan ERPs, tienen sitios web, aplican ciberseguridad). Marca inconsistencia solo si es evidente.

Si respondes NO, agrega en una segunda línea el sector que SÍ describe el informe, en formato:
NO
Sector detectado: [nombre del sector]

Si respondes SI, responde SOLO la palabra SI, sin nada más.";
        }

        // Prompt para detectar cuántos empleados menciona el informe. Se usa para
        // comparar con el número registrado en la empresa y detectar inconsistencias.
        // Se le pide a Claude que devuelva SOLO el número (o -1 si no lo puede
        // determinar con confianza), para parseo fácil.
        public string PromptDetectarEmpleados(string textoInforme)
        {
            string textoRecortado = textoInforme.Length > 3000
                ? textoInforme.Substring(0, 3000) + "..."
                : textoInforme;

            return $@"Lee el siguiente fragmento de un informe empresarial y determina cuántos empleados tiene la empresa según el informe.

FRAGMENTO DEL INFORME:
---
{textoRecortado}
---

Instrucciones estrictas:
- Busca menciones explícitas al número de empleados, trabajadores, personal, planta, plantilla, colaboradores o equipo.
- Si el informe menciona un rango (por ejemplo ""entre 20 y 30 empleados""), responde con el promedio redondeado.
- Ignora otros números que NO se refieran a empleados (años en el mercado, número de oficinas, número de clientes, etc.).
- Responde EXCLUSIVAMENTE con el número entero, sin explicaciones, sin puntuación, sin texto adicional.
- Si NO puedes determinar con confianza el número de empleados, responde exactamente: -1

Ejemplos de respuesta válida:
50
120
-1";
        }
    }
}