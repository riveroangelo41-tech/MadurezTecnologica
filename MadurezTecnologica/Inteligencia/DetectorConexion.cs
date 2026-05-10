using System.Net.Http;
using System.Net.NetworkInformation;

namespace MadurezTecnologica.Inteligencia
{
    public enum ModoOperacion // metodo para determinar el estado de la conexión a internet
    {
        Online, // Interner disponible
        OfflineSinRed, //No hay conexión de red
        OfflineForzado //el usuario ha forzado el modo offline


    }

    public class DetectorConexion // metodo para detectar el estado de la conexión a internet
    {

        private static readonly HttpClient _httpClient = new HttpClient()
        {

            Timeout = TimeSpan.FromSeconds(5) // tiempo de espera para la solicitud HTTP

        };

        public static bool _modoOfflineForzado = false; // variable para almacenar el estado del modo offline forzado

        public static void ActivarModoOfflineForzado() // metodo para activar el modo offline forzado
        {
            _modoOfflineForzado = true;
        }

        public static void DesactivarModoOfflineForzado() // metodo para desactivar el modo offline forzado
        {
            _modoOfflineForzado = false;
        }

        public static bool EstarForzadoOffline() // metodo para verificar si el modo offline forzado está activo
        {  return _modoOfflineForzado;
        
        
        
        
        }
           
        public async Task <bool> HayInternet()
        {
            //Nivel 1: Verificacion rapida de la terjeta de internet
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return false; // No hay conexión de red
            }

            //Nivel 2: Intentar hacer una solicitud HTTP real a  un servidor confiable para verificar la conectividad a internet
            try
            {
                var response = await _httpClient.GetAsync("https://www.google.com/generate_204");
                return response.IsSuccessStatusCode; // Si la respuesta es exitosa, hay conexión a internet
            }
            catch
            {
                return false; // Si ocurre una excepción, no hay conexión a internet
            }



        }

        public async Task<ModoOperacion> DetectarModo() // metodo para detectar el modo de operación actual
        {
            if (_modoOfflineForzado)

            {
                return ModoOperacion.OfflineForzado; // Si el modo offline forzado está activo, retornar ese modo



            }

            bool hayred = await HayInternet(); // Verificar si hay conexión a internet
            if  (!hayred)
            {
                return ModoOperacion.OfflineSinRed; // Si no hay conexión de red, retornar ese modo



            }

            return ModoOperacion.Online; // Si hay conexión a internet, retornar el modo online


        }

        public string DescribirModo(ModoOperacion modo) // metodo para describir el modo de operación actual en la interfaz
        {

            switch (modo)
            {
                case ModoOperacion.Online:
                    return "Modo online — Claude Sonnet 4.6 disponible";
                case ModoOperacion.OfflineSinRed:
                    return "Modo offline — sin conexión a internet";
                case ModoOperacion.OfflineForzado:
                    return "Modo offline — activado manualmente por el usuario";
                default:
                    return "Modo desconocido";



            }




        }







    }
    
        
    





}
  