using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace MadurezTecnologica.Logica
{
    // Genera un PDF a partir de un snapshot visual del dashboard de Resultados.
    // Estrategia: captura la vista actual como bitmap (mismo mecanismo que usan
    // las animaciones de transición) y la incrusta en un PDF A4 vertical. Así el
    // PDF conserva 1:1 el diseño visual del dashboard (colores, tarjetas, KPIs)
    // sin tener que re-modelar la UI en primitivas PDF.
    public static class GeneradorPdfResultados
    {
        // PDFsharp 6 NO resuelve fuentes automáticamente — necesita un IFontResolver
        // global. Sin esto lanza "No appropriate font found for family name...".
        // Registramos un resolver simple que carga fuentes del sistema Windows
        // (%WINDIR%\Fonts). Se inicializa una sola vez con lock para thread-safety.
        private static readonly object _lockResolver = new object();
        private static bool _resolverConfigurado = false;

        private static void ConfigurarFontResolverSiHaceFalta()
        {
            if (_resolverConfigurado) return;
            lock (_lockResolver)
            {
                if (_resolverConfigurado) return;
                if (GlobalFontSettings.FontResolver == null)
                    GlobalFontSettings.FontResolver = new WindowsFontResolver();
                _resolverConfigurado = true;
            }
        }

        // Genera el PDF con la captura del panel indicado.
        //   panelDashboard: el control cuya captura se incrusta en el PDF (típicamente
        //                   el contenedor interno de la vista Resultados).
        //   rutaSalida:     ruta absoluta del .pdf a escribir.
        //   tituloPagina:   título mostrado en la parte superior del PDF.
        // Devuelve la ruta escrita.
        public static string Generar(Control panelDashboard, string rutaSalida, string tituloPagina = "Dashboard de Resultados")
        {
            if (panelDashboard == null) throw new ArgumentNullException(nameof(panelDashboard));
            if (panelDashboard.Width <= 0 || panelDashboard.Height <= 0)
                throw new InvalidOperationException("El panel del dashboard aún no tiene dimensiones válidas.");

            // PDFsharp 6 requiere IFontResolver — inicializar antes de crear cualquier XFont.
            ConfigurarFontResolverSiHaceFalta();

            // --- 1. Forzar repintado completo antes de capturar ---
            // Sin esto, los controles con Paint custom (KPIs, gráficos) pueden quedar
            // a medio pintar en el bitmap. Refresh+Update procesa la cola de WM_PAINT
            // inmediatamente, garantizando que la foto captura el estado final.
            panelDashboard.Refresh();
            panelDashboard.Update();

            // --- 2. Capturar el panel a un Bitmap (contenido COMPLETO, no solo lo visible) ---
            // Si el panel tiene AutoScroll o el contenido excede el viewport, un DrawToBitmap
            // simple solo captura la porción visible. CapturarPanelCompleto itera los hijos
            // para calcular el bounding box total y dibuja cada uno en su posición absoluta,
            // produciendo una imagen con TODO el dashboard.
            using var bmp = CapturarPanelCompleto(panelDashboard);

            // --- 3. Guardar la imagen en memoria como PNG (mejor calidad que JPG) ---
            using var msImagen = new MemoryStream();
            bmp.Save(msImagen, System.Drawing.Imaging.ImageFormat.Png);
            msImagen.Position = 0;

            // --- 4. Crear el documento PDF ---
            using var pdf = new PdfDocument();
            pdf.Info.Title = tituloPagina;
            pdf.Info.Author = "Sistema de Evaluación de Madurez Tecnológica";
            pdf.Info.Subject = "Reporte de resultados";
            pdf.Info.Creator = "MadurezTecnologica";

            var pagina = pdf.AddPage();
            // A4 vertical: 210 × 297 mm. PdfSharp por defecto usa A4 vertical.
            pagina.Size = PdfSharp.PageSize.A4;
            pagina.Orientation = PdfSharp.PageOrientation.Portrait;

            using var gfx = XGraphics.FromPdfPage(pagina);

            // Márgenes seguros (en puntos: 1 pt = 1/72 pulgada; A4 = 595×842 pt)
            const double MARGEN_H = 30;    // ~10 mm
            const double MARGEN_V_TOP = 60;    // ~21 mm — deja espacio para el encabezado
            const double MARGEN_V_BOTTOM = 50; // ~17 mm — deja espacio para el pie

            double anchoUtil = pagina.Width - (2 * MARGEN_H);
            double altoUtil = pagina.Height - MARGEN_V_TOP - MARGEN_V_BOTTOM;

            // --- 5. Dibujar encabezado (título + fecha) ---
            var brushMorado = new XSolidBrush(XColor.FromArgb(91, 44, 111));   // #5B2C6F
            var brushGris = new XSolidBrush(XColor.FromArgb(120, 115, 112));

            var fontTitulo = new XFont("Segoe UI", 14, XFontStyleEx.Bold);
            gfx.DrawString(tituloPagina, fontTitulo, brushMorado,
                new XPoint(MARGEN_H, 30));

            var fontFecha = new XFont("Segoe UI", 9);
            string fecha = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy 'a las' HH:mm");
            gfx.DrawString(fecha, fontFecha, brushGris,
                new XPoint(MARGEN_H, 48));

            // Línea horizontal separadora bajo el encabezado
            var penSeparador = new XPen(XColor.FromArgb(220, 210, 230), 0.5);
            gfx.DrawLine(penSeparador,
                MARGEN_H, MARGEN_V_TOP - 5,
                pagina.Width - MARGEN_H, MARGEN_V_TOP - 5);

            // --- 6. Incrustar la imagen del dashboard dentro del área útil ---
            // Se escala manteniendo proporciones para caber en el ancho o el alto.
            double proporcion = (double)bmp.Width / bmp.Height;
            double anchoDibujo = anchoUtil;
            double altoDibujo = anchoDibujo / proporcion;
            if (altoDibujo > altoUtil)
            {
                altoDibujo = altoUtil;
                anchoDibujo = altoDibujo * proporcion;
            }
            double xDibujo = MARGEN_H + (anchoUtil - anchoDibujo) / 2;   // centrado
            double yDibujo = MARGEN_V_TOP;

            using var ximg = XImage.FromStream(msImagen);
            gfx.DrawImage(ximg, xDibujo, yDibujo, anchoDibujo, altoDibujo);

            // --- 7. Pie de página ---
            var fontPie = new XFont("Segoe UI", 8);
            gfx.DrawString(
                "Sistema de Evaluación de Madurez Tecnológica — Página 1 de 1",
                fontPie, brushGris,
                new XRect(MARGEN_H, pagina.Height - 30, anchoUtil, 15),
                XStringFormats.Center);

            // --- 8. Guardar ---
            pdf.Save(rutaSalida);
            return rutaSalida;
        }

        // Captura el contenido COMPLETO de un panel scrollable como Bitmap.
        // A diferencia de un DrawToBitmap simple (que solo captura el viewport),
        // este método:
        //   1. Calcula el bounding box del contenido iterando los hijos.
        //   2. Resetea temporalmente el scroll a (0,0) para que las posiciones sean consistentes.
        //   3. Crea un bitmap con el tamaño TOTAL del contenido (no solo el visible).
        //   4. Dibuja cada hijo del panel en su posición absoluta original.
        //   5. Restaura el scroll a su posición previa.
        private static Bitmap CapturarPanelCompleto(Control panel)
        {
            // Guardar y resetear el scroll (para que las coordenadas de los hijos
            // reflejen su posición REAL, sin offset del scroll actual del usuario).
            Point scrollPrevio = Point.Empty;
            if (panel is ScrollableControl sc)
            {
                scrollPrevio = sc.AutoScrollPosition;
                sc.AutoScrollPosition = new Point(0, 0);
            }

            try
            {
                // Forzar layout y repintado antes de calcular el bounding
                panel.PerformLayout();
                panel.Refresh();
                panel.Update();

                // Calcular el tamaño TOTAL del contenido (bounding de todos los hijos)
                int maxX = panel.ClientSize.Width;
                int maxY = panel.ClientSize.Height;
                foreach (Control hijo in panel.Controls)
                {
                    if (!hijo.Visible) continue;
                    if (hijo.Right  > maxX) maxX = hijo.Right;
                    if (hijo.Bottom > maxY) maxY = hijo.Bottom;
                }

                // Margen inferior y derecho pequeño para respirar
                int ancho = maxX + 10;
                int alto  = maxY + 10;

                var bmp = new Bitmap(ancho, alto);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(panel.BackColor);
                }

                // Dibujar cada hijo en su posición absoluta original. DrawToBitmap
                // captura el control entero (incluidos sus hijos anidados), así
                // que los paneles complejos con muchos controles adentro se pintan
                // correctamente por dentro.
                foreach (Control hijo in panel.Controls)
                {
                    if (!hijo.Visible) continue;
                    // Forzar que el hijo también tenga su render actualizado
                    hijo.Refresh();
                    hijo.Update();
                    hijo.DrawToBitmap(bmp, new Rectangle(hijo.Left, hijo.Top, hijo.Width, hijo.Height));
                }

                return bmp;
            }
            finally
            {
                // Restaurar el scroll a la posición previa del usuario. Nota: en
                // ScrollableControl, AutoScrollPosition se setea con valores POSITIVOS
                // (indicando cuánto scrollear) y el sistema lo convierte a negativos
                // internamente. Por eso invertimos el signo al restaurar.
                if (panel is ScrollableControl sc2)
                {
                    sc2.AutoScrollPosition = new Point(-scrollPrevio.X, -scrollPrevio.Y);
                }
            }
        }
    }

    // Resolver simple para PDFsharp 6 que carga fuentes desde el directorio de fuentes
    // de Windows (%WINDIR%\Fonts). Cubre las fuentes que usamos en el PDF (Segoe UI
    // y variantes) con fallback a Arial si algo no está.
    internal class WindowsFontResolver : IFontResolver
    {
        private static readonly string CarpetaFuentes = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

        // Mapeo de familia+estilo → nombre del archivo .ttf en %WINDIR%\Fonts.
        // Solo incluimos las que usa este generador. Si se piden más adelante, se agregan.
        private static readonly Dictionary<string, string> _archivos =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Segoe UI (fuente principal del sistema en Win 10/11)
                ["segoeui#regular"]    = "segoeui.ttf",
                ["segoeui#bold"]       = "segoeuib.ttf",
                ["segoeui#italic"]     = "segoeuii.ttf",
                ["segoeui#bolditalic"] = "segoeuiz.ttf",
                // Arial (fallback universal — está en cualquier Windows)
                ["arial#regular"]      = "arial.ttf",
                ["arial#bold"]         = "arialbd.ttf",
                ["arial#italic"]       = "ariali.ttf",
                ["arial#bolditalic"]   = "arialbi.ttf"
            };

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string style = (isBold, isItalic) switch
            {
                (true, true)   => "bolditalic",
                (true, false)  => "bold",
                (false, true)  => "italic",
                _              => "regular"
            };

            // Normalizar el nombre: quitar espacios ("Segoe UI" → "segoeui")
            string familyNorm = familyName.Replace(" ", "").ToLowerInvariant();
            string faceName = $"{familyNorm}#{style}";

            // Fallback en cascada: si el estilo pedido no existe, probar regular;
            // si tampoco la familia, caer a Arial (que está en todo Windows).
            if (!_archivos.ContainsKey(faceName))
            {
                string faceRegular = $"{familyNorm}#regular";
                if (_archivos.ContainsKey(faceRegular)) faceName = faceRegular;
                else faceName = $"arial#{style}";
                if (!_archivos.ContainsKey(faceName)) faceName = "arial#regular";
            }

            return new FontResolverInfo(faceName);
        }

        public byte[]? GetFont(string faceName)
        {
            if (!_archivos.TryGetValue(faceName, out var archivo)) return null;

            string ruta = System.IO.Path.Combine(CarpetaFuentes, archivo);
            if (!System.IO.File.Exists(ruta)) return null;

            return System.IO.File.ReadAllBytes(ruta);
        }
    }
}
