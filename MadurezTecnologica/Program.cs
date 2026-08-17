using MadurezTecnologica.Datos;

namespace MadurezTecnologica
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // DPI awareness (PerMonitorV2) se configura en el .csproj mediante
            // <ApplicationHighDpiMode>. ApplicationConfiguration.Initialize aplica
            // esa configuración al runtime junto con EnableVisualStyles y la fuente
            // por defecto — no hace falta SetHighDpiMode explícito.
            ApplicationConfiguration.Initialize();

            // Crear las tablas de la BD si no existen (idempotente)
            BaseDatos.Inicializar();

            // === LOGIN (RF-33) ===
            // Se exige autenticación antes de acceder al sistema. Si el usuario no se
            // autentica (cierra o cancela), la aplicación no arranca.
            using (var login = new MadurezTecnologica.Presentacion.FormLogin())
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return;   // acceso denegado → salir sin abrir el sistema
            }

            // Iniciar el monitor de conexión en segundo plano: detecta automáticamente
            // cuándo se pierde/recupera internet y pasa la app a modo offline u online.
            Inteligencia.DetectorConexion.IniciarMonitoreo();

            Application.Run(new MadurezTecnologica.Presentacion.FormMain());
        }
    }
}