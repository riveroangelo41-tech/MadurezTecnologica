using System.Drawing.Drawing2D;

namespace MadurezTecnologica.Estilos
{
    public static class Paleta
    {
        // Colores principales
        public static readonly Color MoradoOscuro = ColorTranslator.FromHtml("#53377B");
        public static readonly Color MoradoClaro = ColorTranslator.FromHtml("#8F65CB");
        public static readonly Color VerdeGrisaceo = ColorTranslator.FromHtml("#63918B");
        public static readonly Color VerdeBrillante = ColorTranslator.FromHtml("#74FF14");
        public static readonly Color GrisClaro = ColorTranslator.FromHtml("#B2ACA9");

        // Textos
        public static readonly Color TextoBlanco = Color.White;
        public static readonly Color TextoOscuro = ColorTranslator.FromHtml("#423F3E");

        // Variantes
        public static readonly Color MoradoOscuroHover = ColorTranslator.FromHtml("#6B4A95");
        public static readonly Color VerdeGrisaceoOscuro = ColorTranslator.FromHtml("#527670");  

        // DIBUJO CON ANTIALIASING

        public static void AplicarBordeRedondeadoSuave(Control control, int radio)
        {
            if (control.Width <= 0 || control.Height <= 0) return;

            var path = new GraphicsPath();
            path.AddArc(0, 0, radio, radio, 180, 90);
            path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
            path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
            path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
            path.CloseFigure();

            control.Region = new Region(path);

            // Forzar redibujado con antialiasing
            control.Paint -= AntialiasingPaint;
            control.Paint += AntialiasingPaint;
        }

        private static void AntialiasingPaint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        }
    }
}