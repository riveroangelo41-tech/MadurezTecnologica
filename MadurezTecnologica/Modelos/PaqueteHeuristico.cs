namespace MadurezTecnologica.Modelos
{
    // Un indicador destilado: un término asociado a un nivel CMMI con su peso y soporte.
    public class IndicadorDestilado
    {
        public string Termino { get; set; } = "";
        public int Nivel { get; set; }
        public double Peso { get; set; }        // asociacion(t,n) ∈ [0,1]
        public int Soporte { get; set; }        // nº de informes de ese nivel donde apareció
    }

    // Una recomendación destilada: frase frecuente en los dictámenes de Claude.
    public class RecomendacionDestilada
    {
        public string Nivel { get; set; } = "";     // "1".."5"
        public string Texto { get; set; } = "";     // frase ya normalizada
        public int Frecuencia { get; set; }
    }

    // Paquete completo de heurísticas destiladas para una versión.
    // Se serializa a JSON y se persiste en la tabla PaquetesHeuristicos.
    public class PaqueteHeuristico
    {
        public int Version { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public int NumDictamenes { get; set; }
        public string HashCorpus { get; set; } = "";

        // Indicadores destilados agrupados por nivel (1..5)
        public List<IndicadorDestilado> Indicadores { get; set; } = new();

        // Recomendaciones destiladas por nivel
        public List<RecomendacionDestilada> Recomendaciones { get; set; } = new();

        // Métricas de la comparación antes/después (evaluación en conjunto val)
        public double ExactitudBase { get; set; }
        public double ExactitudDestilada { get; set; }
        public double F1MacroBase { get; set; }
        public double F1MacroDestilada { get; set; }

        // Estado del ciclo de vida: "candidato" | "activo" | "retirado"
        public string Estado { get; set; } = "candidato";
    }
}
