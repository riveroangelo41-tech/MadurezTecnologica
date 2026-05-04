namespace MadurezTecnologica.Modelos
{
    // Clase que representa una conversación entre el usuario y la IA, con sus mensajes asociados
    public class Conversacion
    {
        public int Id { get; set; }

        public int EmpresaId { get; set; }

        public DateTime FechaInicio { get; set; }

        public string Estado { get; set; } = "activa";

        public string RutaInforme { get; set; } = "";

        public List<Mensaje> Mensajes { get; set; } = new List<Mensaje>();



    }
}