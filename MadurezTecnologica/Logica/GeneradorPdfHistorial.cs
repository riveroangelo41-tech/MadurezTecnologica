using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Logica
{
    // Genera un PDF estructurado con el historial completo de diagnósticos de una empresa.
    // A diferencia de GeneradorPdfResultados (que hace snapshot visual), este produce un
    // reporte formal multi-página con: portada + un diagnóstico por sección detallada
    // (nivel CMMI, fecha, resumen, fortalezas, debilidades, riesgos, recomendaciones).
    public static class GeneradorPdfHistorial
    {
        // Colores corporativos (reutilizados en varios lados del PDF)
        private static readonly XColor COLOR_MORADO = XColor.FromArgb(91, 44, 111);       // #5B2C6F
        private static readonly XColor COLOR_MORADO_CLARO = XColor.FromArgb(240, 232, 245); // fondo suave
        private static readonly XColor COLOR_GRIS = XColor.FromArgb(120, 115, 112);
        private static readonly XColor COLOR_GRIS_CLARO = XColor.FromArgb(220, 210, 230);
        private static readonly XColor COLOR_TEXTO = XColor.FromArgb(50, 45, 45);

        // Márgenes de página en puntos (A4 = 595 × 842)
        private const double MARGEN_H = 50;
        private const double MARGEN_V_TOP = 60;
        private const double MARGEN_V_BOTTOM = 60;

        // Fuentes reusables — se crean una vez por Generar()
        private static XFont? _fontTituloGrande;
        private static XFont? _fontTitulo;
        private static XFont? _fontSubtitulo;
        private static XFont? _fontSeccion;
        private static XFont? _fontBody;
        private static XFont? _fontBodyBold;
        private static XFont? _fontMeta;

        public static string Generar(
            Empresa empresa,
            Conversacion conversacion,
            List<Diagnostico> diagnosticos,
            string rutaSalida)
        {
            if (empresa == null) throw new ArgumentNullException(nameof(empresa));
            if (conversacion == null) throw new ArgumentNullException(nameof(conversacion));
            if (diagnosticos == null) diagnosticos = new List<Diagnostico>();

            // Reutilizar el font resolver del otro generador (ya registrado si se
            // usó el PDF de Resultados; si no, se registra ahora).
            AsegurarFontResolver();

            // Crear fuentes
            _fontTituloGrande = new XFont("Segoe UI", 22, XFontStyleEx.Bold);
            _fontTitulo       = new XFont("Segoe UI", 15, XFontStyleEx.Bold);
            _fontSubtitulo    = new XFont("Segoe UI", 12, XFontStyleEx.Bold);
            _fontSeccion      = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            _fontBody         = new XFont("Segoe UI", 10);
            _fontBodyBold     = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            _fontMeta         = new XFont("Segoe UI", 9);

            using var pdf = new PdfDocument();
            pdf.Info.Title    = $"Historial de diagnósticos — {empresa.Nombre}";
            pdf.Info.Author   = "Sistema de Evaluación de Madurez Tecnológica";
            pdf.Info.Subject  = "Reporte de historial de diagnósticos CMMI";
            pdf.Info.Creator  = "MadurezTecnologica";

            // ---- PÁGINA 1: PORTADA ----
            DibujarPortada(pdf, empresa, conversacion, diagnosticos);

            // ---- PÁGINAS SIGUIENTES: UN DIAGNÓSTICO POR SECCIÓN ----
            var pagina = pdf.AddPage();
            pagina.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(pagina);
            double y = MARGEN_V_TOP;

            for (int i = 0; i < diagnosticos.Count; i++)
            {
                var d = diagnosticos[i];
                y = DibujarDiagnostico(pdf, ref pagina, ref gfx, y, d, i + 1, diagnosticos.Count, empresa.Nombre);
            }

            // Si no hay diagnósticos, mostrar mensaje
            if (diagnosticos.Count == 0)
            {
                DibujarTextoSimple(gfx, "No hay diagnósticos registrados para esta empresa.",
                    _fontBody!, XBrushes.Gray, MARGEN_H, y);
            }

            // CRÍTICO: liberar el XGraphics de la última página ANTES de numerar.
            // NumerarPaginas crea un nuevo XGraphics sobre cada página existente y
            // PDFsharp exige que no exista otro XGraphics activo sobre la misma página.
            // Sin este Dispose, tira el error "An XGraphics object already exists...".
            gfx.Dispose();

            // Numeración de páginas al final
            NumerarPaginas(pdf, empresa.Nombre);

            pdf.Save(rutaSalida);
            return rutaSalida;
        }

        // ==============================================================
        // PORTADA
        // ==============================================================
        private static void DibujarPortada(PdfDocument pdf, Empresa empresa,
            Conversacion conversacion, List<Diagnostico> diagnosticos)
        {
            var pagina = pdf.AddPage();
            pagina.Size = PdfSharp.PageSize.A4;
            using var gfx = XGraphics.FromPdfPage(pagina);

            double ancho = pagina.Width;
            double alto = pagina.Height;

            // Banda superior morada decorativa (200 pt de alto)
            gfx.DrawRectangle(new XSolidBrush(COLOR_MORADO), 0, 0, ancho, 220);

            // Título grande
            var brushBlanco = XBrushes.White;
            gfx.DrawString("REPORTE DE",
                new XFont("Segoe UI", 18, XFontStyleEx.Regular), brushBlanco,
                new XRect(0, 80, ancho, 30), XStringFormats.TopCenter);
            gfx.DrawString("MADUREZ TECNOLÓGICA",
                new XFont("Segoe UI", 26, XFontStyleEx.Bold), brushBlanco,
                new XRect(0, 110, ancho, 40), XStringFormats.TopCenter);
            gfx.DrawString("Historial completo de diagnósticos",
                new XFont("Segoe UI", 12), brushBlanco,
                new XRect(0, 165, ancho, 20), XStringFormats.TopCenter);

            // === Tarjeta de empresa (centrada) ===
            double cardY = 280;
            double cardW = ancho - 2 * 60;
            double cardX = 60;
            double cardH = 200;

            var brushCardBg = new XSolidBrush(XColor.FromArgb(248, 244, 250));
            var penCardBorder = new XPen(COLOR_GRIS_CLARO, 0.8);
            gfx.DrawRoundedRectangle(penCardBorder, brushCardBg,
                cardX, cardY, cardW, cardH, 16, 16);

            // Título de la tarjeta
            var brushMorado = new XSolidBrush(COLOR_MORADO);
            gfx.DrawString("EMPRESA EVALUADA", new XFont("Segoe UI", 9, XFontStyleEx.Bold),
                new XSolidBrush(COLOR_GRIS),
                new XRect(cardX + 24, cardY + 20, cardW - 48, 15), XStringFormats.TopLeft);

            gfx.DrawString(empresa.Nombre ?? "—", new XFont("Segoe UI", 20, XFontStyleEx.Bold),
                brushMorado,
                new XRect(cardX + 24, cardY + 38, cardW - 48, 30), XStringFormats.TopLeft);

            // Datos de empresa en 2 columnas
            var brushGris = new XSolidBrush(COLOR_GRIS);
            var brushTexto = new XSolidBrush(COLOR_TEXTO);
            double dataY = cardY + 90;
            double dataX1 = cardX + 24;
            double dataX2 = cardX + cardW / 2 + 10;

            DibujarCampo(gfx, "RIF",       empresa.Rif ?? "—",                              dataX1, dataY);
            DibujarCampo(gfx, "Sector",    empresa.Sector ?? "—",                          dataX1, dataY + 40);
            DibujarCampo(gfx, "Empleados", empresa.CantidadEmpleados.ToString(),           dataX2, dataY);
            DibujarCampo(gfx, "Teléfono",  empresa.Telefono ?? "—",                        dataX2, dataY + 40);

            // === Resumen del reporte ===
            double resY = cardY + cardH + 40;
            gfx.DrawString("RESUMEN DEL REPORTE", new XFont("Segoe UI", 10, XFontStyleEx.Bold),
                brushMorado, new XRect(60, resY, ancho - 120, 15), XStringFormats.TopLeft);

            int nIntermedios = diagnosticos.Count(d => !d.EsFinal);
            int nFinales = diagnosticos.Count(d => d.EsFinal);
            int nivelUltimo = diagnosticos.OrderByDescending(d => d.FechaGeneracion).FirstOrDefault()?.NivelMadurez ?? 0;

            double statsY = resY + 30;
            DibujarStatBox(gfx, "Total de diagnósticos", diagnosticos.Count.ToString(),
                60, statsY, (ancho - 140) / 3, 70);
            DibujarStatBox(gfx, "Intermedios / Finales", $"{nIntermedios} / {nFinales}",
                60 + (ancho - 140) / 3 + 10, statsY, (ancho - 140) / 3, 70);
            DibujarStatBox(gfx, "Nivel CMMI actual", nivelUltimo > 0 ? nivelUltimo.ToString() : "—",
                60 + 2 * ((ancho - 140) / 3 + 10), statsY, (ancho - 140) / 3, 70);

            // === Pie: fecha de generación y conversación ===
            gfx.DrawString(
                $"Conversación iniciada: {conversacion.FechaInicio:dd/MM/yyyy HH:mm}",
                _fontMeta!, brushGris,
                new XRect(60, statsY + 100, ancho - 120, 15), XStringFormats.TopLeft);
            gfx.DrawString(
                $"Reporte generado: {DateTime.Now:dd/MM/yyyy 'a las' HH:mm}",
                _fontMeta!, brushGris,
                new XRect(60, statsY + 118, ancho - 120, 15), XStringFormats.TopLeft);

            // Pie final centrado
            gfx.DrawString("Sistema de Evaluación de Madurez Tecnológica para PYMES",
                new XFont("Segoe UI", 9, XFontStyleEx.Italic), brushGris,
                new XRect(0, alto - 40, ancho, 15), XStringFormats.TopCenter);
        }

        private static void DibujarCampo(XGraphics gfx, string etiqueta, string valor, double x, double y)
        {
            gfx.DrawString(etiqueta.ToUpper(), new XFont("Segoe UI", 8, XFontStyleEx.Bold),
                new XSolidBrush(COLOR_GRIS), new XPoint(x, y));
            gfx.DrawString(valor, new XFont("Segoe UI", 11),
                new XSolidBrush(COLOR_TEXTO), new XPoint(x, y + 15));
        }

        private static void DibujarStatBox(XGraphics gfx, string etiqueta, string valor,
            double x, double y, double w, double h)
        {
            var brushBg = new XSolidBrush(XColor.FromArgb(245, 241, 249));
            gfx.DrawRoundedRectangle(brushBg, x, y, w, h, 10, 10);
            gfx.DrawString(etiqueta.ToUpper(), new XFont("Segoe UI", 8, XFontStyleEx.Bold),
                new XSolidBrush(COLOR_GRIS), new XRect(x, y + 10, w, 15), XStringFormats.TopCenter);
            gfx.DrawString(valor, new XFont("Segoe UI", 20, XFontStyleEx.Bold),
                new XSolidBrush(COLOR_MORADO), new XRect(x, y + 28, w, 30), XStringFormats.TopCenter);
        }

        // ==============================================================
        // BLOQUE DE UN DIAGNÓSTICO
        // Devuelve la nueva y actual (para encadenar el siguiente diagnóstico).
        // ==============================================================
        private static double DibujarDiagnostico(PdfDocument pdf, ref PdfPage pagina, ref XGraphics gfx,
            double y, Diagnostico d, int indice, int total, string empresaNombre)
        {
            double anchoUtil = pagina.Width - 2 * MARGEN_H;

            // Salto de página si no cabe el encabezado (mínimo 100 pt de espacio)
            if (y + 120 > pagina.Height - MARGEN_V_BOTTOM)
            {
                (pagina, gfx) = NuevaPagina(pdf, gfx);
                y = MARGEN_V_TOP;
            }

            // === Encabezado del diagnóstico ===
            // Banda morada con: "Diagnóstico N/M — Nivel CMMI X" + badge (INTERMEDIO/FINAL)
            var brushBanda = new XSolidBrush(COLOR_MORADO);
            gfx.DrawRoundedRectangle(brushBanda, MARGEN_H, y, anchoUtil, 46, 10, 10);

            string tituloBanda = $"Diagnóstico {indice} de {total}";
            gfx.DrawString(tituloBanda, _fontTitulo!, XBrushes.White,
                new XPoint(MARGEN_H + 20, y + 20));

            gfx.DrawString($"Nivel CMMI: {d.NivelMadurez}   ·   {d.FechaGeneracion:dd/MM/yyyy HH:mm}",
                new XFont("Segoe UI", 10), XBrushes.White,
                new XPoint(MARGEN_H + 20, y + 36));

            // Badge (INTERMEDIO / FINAL) a la derecha
            string badgeTexto = d.EsFinal ? "FINAL" : "INTERMEDIO";
            var brushBadge = new XSolidBrush(d.EsFinal
                ? XColor.FromArgb(70, 180, 100)
                : XColor.FromArgb(150, 130, 200));
            double badgeW = 90;
            gfx.DrawRoundedRectangle(brushBadge,
                MARGEN_H + anchoUtil - badgeW - 12, y + 13, badgeW, 20, 10, 10);
            gfx.DrawString(badgeTexto, new XFont("Segoe UI", 9, XFontStyleEx.Bold),
                XBrushes.White,
                new XRect(MARGEN_H + anchoUtil - badgeW - 12, y + 15, badgeW, 20),
                XStringFormats.TopCenter);

            y += 60;

            // === Secciones del diagnóstico ===
            y = DibujarSeccionTexto(pdf, ref pagina, ref gfx, y, "RESUMEN", d.ResumenEmpresa);
            y = DibujarSeccionTexto(pdf, ref pagina, ref gfx, y, "FORTALEZAS", d.Fortalezas);
            y = DibujarSeccionTexto(pdf, ref pagina, ref gfx, y, "DEBILIDADES", d.Debilidades);
            y = DibujarSeccionTexto(pdf, ref pagina, ref gfx, y, "RIESGOS", d.Riesgos);
            y = DibujarSeccionTexto(pdf, ref pagina, ref gfx, y, "RECOMENDACIONES", d.Recomendaciones);

            // Espaciado entre diagnósticos (línea decorativa)
            y += 12;
            if (y < pagina.Height - MARGEN_V_BOTTOM - 20)
            {
                var pen = new XPen(COLOR_GRIS_CLARO, 0.5);
                gfx.DrawLine(pen, MARGEN_H + 100, y, pagina.Width - MARGEN_H - 100, y);
                y += 20;
            }

            return y;
        }

        // Dibuja una sección "TÍTULO" + texto. Se pagina si es necesario.
        private static double DibujarSeccionTexto(PdfDocument pdf, ref PdfPage pagina, ref XGraphics gfx,
            double y, string titulo, string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido)) return y;

            // Salto de página si el título no cabe (mínimo 60 pt)
            if (y + 60 > pagina.Height - MARGEN_V_BOTTOM)
            {
                (pagina, gfx) = NuevaPagina(pdf, gfx);
                y = MARGEN_V_TOP;
            }

            // Título con marker morado a la izquierda
            var brushMorado = new XSolidBrush(COLOR_MORADO);
            gfx.DrawRoundedRectangle(brushMorado, MARGEN_H, y + 2, 4, 16, 2, 2);
            gfx.DrawString(titulo, _fontSeccion!, brushMorado,
                new XPoint(MARGEN_H + 12, y + 3));

            y += 24;

            // Cuerpo con word wrap y paginación automática
            y = DibujarTextoConWrap(pdf, ref pagina, ref gfx, y, contenido.Trim(),
                _fontBody!, new XSolidBrush(COLOR_TEXTO), MARGEN_H);

            return y + 10;   // espacio inferior antes de la próxima sección
        }

        // Dibuja texto con word-wrap dentro del ancho útil, manejando paginación
        // cuando se llega al fondo de la página.
        private static double DibujarTextoConWrap(PdfDocument pdf, ref PdfPage pagina, ref XGraphics gfx,
            double y, string texto, XFont font, XBrush brush, double x)
        {
            double anchoUtil = pagina.Width - 2 * MARGEN_H;
            double lineHeight = font.Height * 1.2;

            // Dividir en líneas lógicas (respetando saltos de línea originales del contenido)
            string[] parrafos = texto.Split(new[] { '\n' }, StringSplitOptions.None);

            foreach (var parrafo in parrafos)
            {
                string p = parrafo.TrimEnd('\r');
                if (string.IsNullOrEmpty(p))
                {
                    y += lineHeight * 0.5;   // línea vacía = espacio
                    continue;
                }

                // Word wrap manual
                var palabras = p.Split(' ');
                var lineaActual = new System.Text.StringBuilder();

                foreach (var palabra in palabras)
                {
                    string prueba = lineaActual.Length == 0 ? palabra : lineaActual + " " + palabra;
                    double ancho = gfx.MeasureString(prueba, font).Width;

                    if (ancho > anchoUtil && lineaActual.Length > 0)
                    {
                        // Fin de línea → dibujar la línea actual
                        if (y + lineHeight > pagina.Height - MARGEN_V_BOTTOM)
                        {
                            (pagina, gfx) = NuevaPagina(pdf, gfx);
                            y = MARGEN_V_TOP;
                        }
                        gfx.DrawString(lineaActual.ToString(), font, brush, new XPoint(x, y + font.Height * 0.85));
                        y += lineHeight;
                        lineaActual.Clear();
                        lineaActual.Append(palabra);
                    }
                    else
                    {
                        if (lineaActual.Length > 0) lineaActual.Append(' ');
                        lineaActual.Append(palabra);
                    }
                }

                // Última línea del párrafo
                if (lineaActual.Length > 0)
                {
                    if (y + lineHeight > pagina.Height - MARGEN_V_BOTTOM)
                    {
                        (pagina, gfx) = NuevaPagina(pdf, gfx);
                        y = MARGEN_V_TOP;
                    }
                    gfx.DrawString(lineaActual.ToString(), font, brush, new XPoint(x, y + font.Height * 0.85));
                    y += lineHeight;
                }
            }

            return y;
        }

        // Simple: dibuja texto sin word-wrap (para líneas cortas conocidas)
        private static void DibujarTextoSimple(XGraphics gfx, string texto, XFont font, XBrush brush,
            double x, double y)
        {
            gfx.DrawString(texto, font, brush, new XPoint(x, y + font.Height * 0.85));
        }

        // ==============================================================
        // GESTIÓN DE PÁGINAS
        // ==============================================================
        private static (PdfPage pagina, XGraphics gfx) NuevaPagina(PdfDocument pdf, XGraphics gfxViejo)
        {
            gfxViejo.Dispose();
            var pag = pdf.AddPage();
            pag.Size = PdfSharp.PageSize.A4;
            var g = XGraphics.FromPdfPage(pag);
            return (pag, g);
        }

        // Al final, agrega "Página X de N" y el nombre de empresa en el pie de cada página
        // (excepto la portada, que ya tiene su propio pie).
        private static void NumerarPaginas(PdfDocument pdf, string empresaNombre)
        {
            int totalPag = pdf.PageCount;
            for (int i = 1; i < totalPag; i++)   // saltamos la portada (índice 0)
            {
                var pag = pdf.Pages[i];
                using var gfx = XGraphics.FromPdfPage(pag, XGraphicsPdfPageOptions.Append);
                var brushGris = new XSolidBrush(COLOR_GRIS);
                var fontPie = new XFont("Segoe UI", 8);

                double ancho = pag.Width;

                // Izquierda: nombre de empresa
                gfx.DrawString(empresaNombre, fontPie, brushGris,
                    new XPoint(MARGEN_H, pag.Height - 25));

                // Derecha: página X de N
                string pageText = $"Página {i + 1} de {totalPag}";
                var tam = gfx.MeasureString(pageText, fontPie);
                gfx.DrawString(pageText, fontPie, brushGris,
                    new XPoint(ancho - MARGEN_H - tam.Width, pag.Height - 25));

                // Línea separadora sutil sobre el pie
                var penLinea = new XPen(COLOR_GRIS_CLARO, 0.4);
                gfx.DrawLine(penLinea, MARGEN_H, pag.Height - 40, ancho - MARGEN_H, pag.Height - 40);
            }
        }

        // Asegura que PDFsharp tenga un IFontResolver registrado (para poder crear XFont).
        // Reusa la clase WindowsFontResolver definida en GeneradorPdfResultados.cs.
        private static readonly object _lockResolver = new object();
        private static bool _resolverRegistrado = false;
        private static void AsegurarFontResolver()
        {
            if (_resolverRegistrado) return;
            lock (_lockResolver)
            {
                if (_resolverRegistrado) return;
                if (GlobalFontSettings.FontResolver == null)
                    GlobalFontSettings.FontResolver = new WindowsFontResolver();
                _resolverRegistrado = true;
            }
        }
    }
}
