namespace MadurezTecnologica.Modelos
// Clase que representa el diagnóstico generado por la IA para una conversación específica, con sus resultados y recomendaciones
{
    public class Diagnostico
    {
        public int Id { get; set; }

        public int ConversacionId { get; set; }
        public string ResumenEmpresa { get; set; } = "";

        public int NivelMadurez { get; set; }

        public string Fortalezas { get; set; } = "";

        public string Debilidades { get; set; } = "";

        public string Riesgos { get; set; } = "";

        public string Recomendaciones { get; set; } = "";

        public DateTime FechaGeneracion { get; set; }
        
        public bool EsFinal { get; set; }


    }
}