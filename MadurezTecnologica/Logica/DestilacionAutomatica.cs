using System.IO;

namespace MadurezTecnologica.Logica
{
    // Dispara ciclos de destilación en segundo plano, después de cada análisis ONLINE
    // exitoso (cuando llega un dictamen IA nuevo al corpus).
    //
    // Características clave:
    //  - Fire-and-forget: no bloquea el hilo UI.
    //  - Non-reentrant: si ya hay una destilación en curso, la nueva se salta.
    //  - Fail-silent: cualquier excepción se atrapa (no puede romper el flujo del usuario).
    //  - Auditable: cada corrida deja línea en 'destilaciones.log' al lado del .exe,
    //    útil para trazabilidad y para respaldar la sección de la tesis en la defensa.
    public static class DestilacionAutomatica
    {
        // Semáforo simple para evitar dos corridas concurrentes
        private static int _corridaEnCurso = 0;

        private static readonly string _rutaLog =
            Path.Combine(AppContext.BaseDirectory, "destilaciones.log");

        public static void DispararEnBackground()
        {
            // Intenta marcar corrida en curso; si ya hay una, sale sin hacer nada
            if (System.Threading.Interlocked.CompareExchange(ref _corridaEnCurso, 1, 0) != 0)
            {
                EscribirLog("SKIP · ya hay una destilación en curso");
                return;
            }

            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var destilador = new Destilador();
                    var resultado = destilador.EjecutarCicloDestilacion();

                    string estado = resultado.Promovido
                        ? "PROMOVIDO"
                        : (resultado.Exitoso ? "CANDIDATO" : "SIN_DESTILAR");

                    string version = resultado.Paquete != null
                        ? $"v{resultado.Paquete.Version}"
                        : "-";

                    EscribirLog($"{estado} · {version} · {resultado.Mensaje}");
                }
                catch (Exception ex)
                {
                    // Nunca propagar: la destilación es un mecanismo silencioso
                    EscribirLog($"ERROR · {ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _corridaEnCurso, 0);
                }
            });
        }

        private static void EscribirLog(string mensaje)
        {
            try
            {
                string linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}";
                File.AppendAllText(_rutaLog, linea + Environment.NewLine);
            }
            catch { /* si el log falla, no hay nada más que hacer */ }
        }
    }
}
