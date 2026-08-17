using System.Drawing.Drawing2D;

namespace MadurezTecnologica.Estilos
{
    // Wrapper estático sobre ToolTip nativo con OwnerDraw:
    // aparece tras 500ms al hacer hover, fondo morado oscuro, texto blanco.
    // Uso: TooltipEstilizado.Aplicar(control, "descripción del dato");
    public static class TooltipEstilizado
    {
        private static readonly ToolTip _instancia = CrearInstancia();
        private const int AnchoMaximo = 320;

        private static ToolTip CrearInstancia()
        {
            var tt = new ToolTip
            {
                OwnerDraw = true,
                InitialDelay = 500,
                ReshowDelay = 200,
                AutoPopDelay = 12000,
                UseAnimation = false,
                UseFading = false,
                ShowAlways = true
            };
            tt.Popup += MedirTooltip;
            tt.Draw += DibujarTooltip;
            return tt;
        }

        public static void Aplicar(Control control, string texto)
        {
            _instancia.SetToolTip(control, texto);
        }

        // Aplica el mismo tooltip a varios controles a la vez (útil para tarjetas
        // compuestas donde el hover puede caer sobre cualquier hijo).
        public static void AplicarACascada(string texto, params Control[] controles)
        {
            foreach (var c in controles)
                if (c != null) _instancia.SetToolTip(c, texto);
        }

        private static void MedirTooltip(object? sender, PopupEventArgs e)
        {
            string texto = _instancia.GetToolTip(e.AssociatedControl) ?? "";
            using var font = new Font("Segoe UI", 8.5f);
            var size = TextRenderer.MeasureText(
                texto,
                font,
                new Size(AnchoMaximo, int.MaxValue),
                TextFormatFlags.WordBreak);
            e.ToolTipSize = new Size(size.Width + 22, size.Height + 18);
        }

        private static void DibujarTooltip(object? sender, DrawToolTipEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = e.Bounds;

            // Fondo morado oscuro sólido, cubriendo TODO el bounds.
            // (No usamos esquinas redondeadas porque el popup nativo dejaría
            // asomar el color de Info amarillo en las esquinas.)
            using (var brushFondo = new SolidBrush(Paleta.MoradoOscuro))
                g.FillRectangle(brushFondo, rect);

            // Borde interior sutil de 1px en un tono más claro
            using (var penBorde = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                g.DrawRectangle(penBorde, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

            using var font = new Font("Segoe UI", 8.5f);
            var textRect = new Rectangle(
                rect.X + 11,
                rect.Y + 9,
                rect.Width - 22,
                rect.Height - 18);
            TextRenderer.DrawText(
                g,
                e.ToolTipText,
                font,
                textRect,
                Color.White,
                TextFormatFlags.WordBreak | TextFormatFlags.LeftAndRightPadding);
        }
    }
}
