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

        // Conexión real detectada por el monitor de fondo. Se asume conectado al inicio
        // hasta el primer chequeo. El monitor la actualiza cada X segundos.
        private static bool _hayConexionDetectada = true;
        public static bool HayConexion => _hayConexionDetectada;

        // Estado offline EFECTIVO: el usuario lo forzó O no hay conexión detectada.
        // Es la fuente de verdad que deben usar el chat, el análisis y la UI para
        // decidir si operan con la IA o con el motor local.
        public static bool EstaOffline() => _modoOfflineForzado || !_hayConexionDetectada;

        // Token que se CANCELA en el instante en que se pierde la conexión.
        // Toda petición a la IA (análisis de PDF o chat) enlaza su cancelación a este
        // token, de modo que si la red se cae a mitad de la petición, ésta se aborta.
        // Al recuperarse la conexión se genera un token nuevo y limpio.
        private static CancellationTokenSource _ctsConexion = new CancellationTokenSource();
        private static readonly object _ctsLock = new object();

        public static CancellationToken TokenConexion
        {
            get { lock (_ctsLock) { return _ctsConexion.Token; } }
        }

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

        // ===================================================
        // MONITOR DE CONEXIÓN EN SEGUNDO PLANO
        // Chequea periódicamente si hay internet y, si el estado cambia,
        // dispara ModoCambio para que toda la UI y la lógica se actualicen.
        // ===================================================
        private static System.Threading.Timer? _monitor;
        private static readonly DetectorConexion _detectorMonitor = new DetectorConexion();

        public static void IniciarMonitoreo(int intervaloSegundos = 15)
        {
            if (_monitor != null) return;   // ya está corriendo

            _monitor = new System.Threading.Timer(
                callback: _ => _ = VerificarConexionEnFondo(),
                state: null,
                dueTime: TimeSpan.Zero,                              // primer chequeo inmediato
                period: TimeSpan.FromSeconds(intervaloSegundos));    // luego cada X segundos
        }

        private static async Task VerificarConexionEnFondo()
        {
            try
            {
                // Forzar un chequeo real cada tick (ignorar el caché de 30s)
                InvalidarCacheConexion();
                bool hay = await _detectorMonitor.HayInternet();

                if (hay != _hayConexionDetectada)
                {
                    _hayConexionDetectada = hay;

                    lock (_ctsLock)
                    {
                        if (!hay)
                        {
                            // Se perdió la conexión → abortar cualquier petición en curso.
                            _ctsConexion.Cancel();
                        }
                        else
                        {
                            // Volvió la conexión → token nuevo para las próximas peticiones.
                            _ctsConexion.Dispose();
                            _ctsConexion = new CancellationTokenSource();
                        }
                    }

                    // Los handlers de la UI marshalizan al hilo de UI por su cuenta.
                    ModoCambio?.Invoke();
                }
            }
            catch
            {
                // El monitor nunca debe tumbar la app; si falla, se reintenta al próximo tick.
            }
        }

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
  