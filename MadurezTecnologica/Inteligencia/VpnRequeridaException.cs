namespace MadurezTecnologica.Inteligencia
{
    // Se lanza cuando la API de Claude responde 403 (Forbidden / "Request not allowed"),
    // lo que en la práctica significa que el servicio está bloqueado en la región y se
    // necesita la VPN encendida para acceder. La UI la captura para mostrar un mensaje
    // claro pidiendo encender la VPN, en vez del error genérico.
    public class VpnRequeridaException : Exception
    {
        public VpnRequeridaException()
            : base("El servicio de IA no está disponible sin la VPN (respuesta 403 de la API).")
        {
        }
    }
}
