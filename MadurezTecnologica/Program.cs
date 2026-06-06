using MadurezTecnologica.Datos;

namespace MadurezTecnologica
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);  // ← agregar esta línea
            ApplicationConfiguration.Initialize();
            Application.Run(new MadurezTecnologica.Presentacion.FormMain());
        }
    }
}