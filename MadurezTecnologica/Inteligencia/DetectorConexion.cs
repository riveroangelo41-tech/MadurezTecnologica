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

        // Evento global que se dispara cuando el modo cambia.
        // Todos los IndicadorModoConexion se suscriben para mantenerse sincronizados.
        public static event Action? ModoCambio;

        public static void ActivarModoOfflineForzado()
        {
            if (_modoOfflineForzado) return;
            _modoOfflineForzado = true;
            InvalidarCacheConexion();
            ModoCambio?.Invoke();
        }

        public static void DesactivarModoOfflineForzado()
        {
            if (!_modoOfflineForzado) return;
            _modoOfflineForzado = false;
            InvalidarCacheConexion();
            ModoCambio?.Invoke();
        }

        public static void AlternarModoOfflineForzado()
        {
            _modoOfflineForzado = !_modoOfflineForzado;
            InvalidarCacheConexion();
            ModoCambio?.Invoke();
        }

        public static bool EstarForzadoOffline() => _modoOfflineForzado;
           
        // Caché del resultado de HayInternet: evita golpear la red en cada llamada.
        // Las llamadas dentro del TTL devuelven el último resultado sin consultar.
        private static bool? _cacheHayInternet = null;
        private static DateTime _cacheHayInternetTimestamp = DateTime.MinValue;
        private static readonly TimeSpan _cacheTTL = TimeSpan.FromSeconds(30);
        private static readonly object _cacheLock = new object();

        public static void InvalidarCacheConexion()
        {
            lock (_cacheLock)
            {
                _cacheHayInternet = null;
                _cacheHayInternetTimestamp = DateTime.MinValue;
            }
        }

        public async Task<bool> HayInternet()
        {
            // 1. Si el caché está fresco, devolver el resultado guardado
            lock (_cacheLock)
            {
                if (_cacheHayInternet.HasValue &&
                    DateTime.Now - _cacheHayInternetTimestamp < _cacheTTL)
                {
                    return _cacheHayInternet.Value;
                }
            }

            // 2. Caché expirado o no inicializado → consultar realmente
            bool resultado;

            // Nivel 1: Verificación rápida de la tarjeta de internet
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                resultado = false;
            }
            else
            {
                // Nivel 2: Solicitud HTTP a un servidor confiable
                try
                {
                    var response = await _httpClient.GetAsync("https://www.google.com/generate_204");
                    resultado = response.IsSuccessStatusCode;
                }
                catch
                {
                    resultado = false;
                }
            }

            // 3. Guardar en caché para los próximos 30 segundos
            lock (_cacheLock)
            {
                _cacheHayInternet = resultado;
                _cacheHayInternetTimestamp = DateTime.Now;
            }

            return resultado;
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
  