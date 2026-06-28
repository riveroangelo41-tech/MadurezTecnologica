using MadurezTecnologica.Inteligencia;

namespace MadurezTecnologica.Estilos
{
    /// <summary>
    /// Toggle visual clickeable que muestra el modo de operación actual (Online / Offline forzado)
    /// y permite alternarlo. Todas las instancias se mantienen sincronizadas mediante el evento
    /// estático DetectorConexion.ModoCambio.
    /// </summary>
    public class IndicadorModoConexion : UserControl
    {
        private Panel _puntoEstado = null!;
        private Label _lblEstado = null!;

        private static readonly Color VerdeConectado = ColorTranslator.FromHtml("#74FF14");
        private static readonly Color NaranjaOffline = ColorTranslator.FromHtml("#D4841C");

        public IndicadorModoConexion()
        {
            Width = 175;
            Height = 36;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Margin = new Padding(0);

            CrearControles();

            // Pintar el borde redondeado en lugar de usar Region (más confiable)
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = 18;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(Width - r * 2 - 1, Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();

                // Fondo blanco redondeado
                using var brushFondo = new SolidBrush(Color.White);
                g.FillPath(brushFondo, path);

                // Sombra/borde sutil
                using var pen = new Pen(Color.FromArgb(220, 215, 210), 1);
                g.DrawPath(pen, path);
            };

            // El BackColor debe coincidir con el del PADRE para que las esquinas se vean transparentes
            ParentChanged += (s, e) =>
            {
                if (Parent != null)
                    BackColor = Parent.BackColor;
            };

            Click += OnClick;
            ActualizarVisual();

            DetectorConexion.ModoCambio += ActualizarVisual;
            HandleDestroyed += (s, e) => DetectorConexion.ModoCambio -= ActualizarVisual;
        }

        private void CrearControles()
        {
            _puntoEstado = new Panel
            {
                Size = new Size(20, 20),
                Location = new Point(12, 8),
                BackColor = Color.Transparent
            };
            _puntoEstado.Paint += PintarPunto;
            _puntoEstado.Click += OnClick;
            Controls.Add(_puntoEstado);

            _lblEstado = new Label
            {
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(36, 10),
                Size = new Size(Width - 50, 16),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _lblEstado.Click += OnClick;
            Controls.Add(_lblEstado);
        }

        private void PintarPunto(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Color color = DetectorConexion.EstarForzadoOffline() ? NaranjaOffline : VerdeConectado;

            // Halo translúcido
            using var halo = new SolidBrush(Color.FromArgb(60, color));
            g.FillEllipse(halo, 0, 0, 20, 20);

            // Punto sólido en el centro
            using var solido = new SolidBrush(color);
            g.FillEllipse(solido, 5, 5, 10, 10);
        }

        private void OnClick(object? sender, EventArgs e)
        {
            DetectorConexion.AlternarModoOfflineForzado();

            // Avisar al usuario del cambio
            bool ahoraOffline = DetectorConexion.EstarForzadoOffline();
            var owner = FindForm();

            if (ahoraOffline)
            {
                MensajeApp.Advertencia(
                    "Activaste el modo offline forzado.\n\n" +
                    "Los análisis de PDF se procesarán con el motor local (detección por palabras clave) " +
                    "en lugar de la IA. Esto es útil cuando no hay conexión a internet, pero el análisis " +
                    "será menos preciso.\n\n" +
                    "Haz clic en el indicador nuevamente para volver a usar la IA.",
                    "Modo Offline activado",
                    owner);
            }
            else
            {
                MensajeApp.Exito(
                    "Volviste al modo conectado.\n\n" +
                    "Los próximos análisis de PDF se procesarán con la IA (Claude), " +
                    "siempre que haya conexión a internet disponible.",
                    "Modo Conectado",
                    owner);
            }
        }

        private void ActualizarVisual()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ActualizarVisual));
                return;
            }

            bool offline = DetectorConexion.EstarForzadoOffline();
            _lblEstado.Text = offline ? "Modo Offline" : "Conectado a IA";
            _lblEstado.ForeColor = offline
                ? ColorTranslator.FromHtml("#A6620D")
                : Paleta.TextoOscuro;
            _puntoEstado.Invalidate();
        }
    }
}
