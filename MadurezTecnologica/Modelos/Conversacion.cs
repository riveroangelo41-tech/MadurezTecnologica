namespace MadurezTecnologica.Modelos
{
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