namespace MadurezTecnologica.Modelos
// Clase que representa un mensaje dentro de una conversación, con su remitente, contenido, timestamp y orden de aparición
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