namespace MadurezTecnologica.Modelos
{
    public class Mensaje
    {
        public int Id { get; set; }

        public int ConversacionId { get; set; }

        public string Remitente { get; set; } = "";

        public string Contenido { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public int Orden { get; set; }


    }
}