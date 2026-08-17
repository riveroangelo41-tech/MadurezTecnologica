using System.Reflection;

namespace MadurezTecnologica.Estilos
{
    // Carga los iconos PNG que van embebidos dentro del .exe (marcados como
    // EmbeddedResource en el csproj). Cachea el Image por nombre para que
    // pedirlo varias veces no re-lea el stream cada vez.
    public static class CargadorIconos
    {
        private static readonly Dictionary<string, Image> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        // Nombres canónicos de los iconos (sin extensión).
        // Coinciden con los archivos en Recursos/Iconos/.
        public const string App        = "app";
        public const string Inicio     = "inicio";
        public const string Empresas   = "empresas";
        public const string Cargar     = "cargar_informe";
        public const string Chat       = "chat";
        public const string Resultados = "resultados";
        public const string Historial  = "historial";
        public const string Analizar   = "analizar";
        public const string Word       = "word";
        public const string Pdf        = "pdf";

        // Devuelve el Image original tal como está embebido (respeta transparencia).
        // Devuelve null si el recurso no existe (para no tumbar la app).
        public static Image? Obtener(string nombre)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(nombre, out var img)) return img;

                var asm = Assembly.GetExecutingAssembly();
                string? recurso = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("." + nombre + ".png", StringComparison.OrdinalIgnoreCase));

                if (recurso == null) return null;

                using var stream = asm.GetManifestResourceStream(recurso);
                if (stream == null) return null;

                // Copiar el stream a memoria: Image.FromStream requiere que el stream
                // permanezca vivo durante toda la vida del Image, y los streams de
                // recursos embebidos no siempre soportan eso. La copia lo garantiza.
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                var imgLoaded = Image.FromStream(ms);
                _cache[nombre] = imgLoaded;
                return imgLoaded;
            }
        }

        // Devuelve una copia REDIMENSIONADA (nueva Bitmap) del icono al tamaño
        // pedido, útil para los botones del sidebar donde no queremos que el
        // PNG grande se muestre a su tamaño original.
        public static Image? ObtenerRedimensionado(string nombre, int ancho, int alto)
        {
            var original = Obtener(nombre);
            if (original == null) return null;

            var bmp = new Bitmap(ancho, alto);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(original, 0, 0, ancho, alto);
            }
            return bmp;
        }

        // Carga el icono .ico embebido en el ejecutable para usarlo como Icon
        // de una ventana (Form.Icon). Usa el .ico del ApplicationIcon, que ya
        // contiene múltiples resoluciones (16, 32, 48, 64, 128, 256).
        public static Icon? ObtenerIconoApp()
        {
            try
            {
                // El .ico se embebe automáticamente como recurso Win32 al usar
                // ApplicationIcon en el csproj. Se recupera con ExtractAssociatedIcon
                // del propio .exe.
                string? exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    return Icon.ExtractAssociatedIcon(exePath);
            }
            catch { /* si algo falla, la ventana queda con el icono por defecto */ }
            return null;
        }
    }
}
