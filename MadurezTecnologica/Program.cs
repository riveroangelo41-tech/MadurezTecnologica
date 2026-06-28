using MadurezTecnologica.Datos;

namespace MadurezTecnologica
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            ApplicationConfiguration.Initialize();

            // Crear las tablas de la BD si no existen (idempotente)
            BaseDatos.Inicializar();

            Application.Run(new MadurezTecnologica.Presentacion.FormMain());
        }
    }
}