using System.Drawing.Drawing2D;

namespace MadurezTecnologica.Estilos
{
    public enum TipoMensaje
    {
        Info,
        Exito,
        Advertencia,
        Error,
        Pregunta
    }

    public static class MensajeApp
    {
        public static void Info(string mensaje, string titulo = "Información", IWin32Window? owner = null)
            => Mostrar(titulo, mensaje, TipoMensaje.Info, owner);

        public static void Exito(string mensaje, string titulo = "Operación exitosa", IWin32Window? owner = null)
            => Mostrar(titulo, mensaje, TipoMensaje.Exito, owner);

        public static void Advertencia(string mensaje, string titulo = "Advertencia", IWin32Window? owner = null)
            => Mostrar(titulo, mensaje, TipoMensaje.Advertencia, owner);

        public static void Error(string mensaje, string titulo = "Error", IWin32Window? owner = null)
            => Mostrar(titulo, mensaje, TipoMensaje.Error, owner);

        public static bool Confirmar(string mensaje, string titulo = "Confirmación", IWin32Window? owner = null)
            => MostrarConRespuesta(titulo, mensaje, TipoMensaje.Pregunta, owner);

        // === IMPLEMENTACIÓN ===

        private static void Mostrar(string titulo, string mensaje, TipoMensaje tipo, IWin32Window? owner)
        {
            using var form = CrearFormBase(titulo, mensaje, tipo, mostrarBotonNo: false);
            form.ShowDialog(owner);
        }

        private static bool MostrarConRespuesta(string titulo, string mensaje, TipoMensaje tipo, IWin32Window? owner)
        {
            using var form = CrearFormBase(titulo, mensaje, tipo, mostrarBotonNo: true);
            return form.ShowDialog(owner) == DialogResult.Yes;
        }

        private static Form CrearFormBase(string titulo, string mensaje, TipoMensaje tipo, bool mostrarBotonNo)
        {
            var (colorAccento, icono) = ObtenerColorYIcono(tipo);

            var form = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                ShowInTaskbar = false,
                Size = new Size(460, 240),
                MinimumSize = new Size(460, 200)
            };

            // Borde sutil del form completo
            form.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(220, 215, 225), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, form.Width - 1, form.Height - 1);
            };

            // === HEADER CON BARRA DE ACENTO ===
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = colorAccento
            };
            form.Controls.Add(header);

            // === ICONO CIRCULAR ===
            var panelIcono = new Panel
            {
                Size = new Size(56, 56),
                Location = new Point(28, 28),
                BackColor = Color.FromArgb(
                    Math.Min(255, colorAccento.R + (255 - colorAccento.R) * 88 / 100),
                    Math.Min(255, colorAccento.G + (255 - colorAccento.G) * 88 / 100),
                    Math.Min(255, colorAccento.B + (255 - colorAccento.B) * 88 / 100))
            };
            var pathIcono = new GraphicsPath();
            pathIcono.AddEllipse(0, 0, panelIcono.Width, panelIcono.Height);
            panelIcono.Region = new Region(pathIcono);
            form.Controls.Add(panelIcono);

            var lblIcono = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = colorAccento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            panelIcono.Controls.Add(lblIcono);

            // === TÍTULO ===
            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(100, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            form.Controls.Add(lblTitulo);

            // === MENSAJE ===
            var lblMensaje = new Label
            {
                Text = mensaje,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(80, 75, 85),
                Location = new Point(100, 58),
                MaximumSize = new Size(form.Width - 130, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            form.Controls.Add(lblMensaje);

            // Ajustar altura del form al contenido — en Load (antes de mostrar)
            form.Load += (s, e) =>
            {
                lblMensaje.PerformLayout();
                int altoTextoEstimado = lblMensaje.PreferredSize.Height;

                int altoMinimo = 200;
                // 58 (top del lblMensaje) + textoEstimado + 20 padding + 64 (footer)
                int altoNecesario = Math.Max(altoMinimo, 58 + altoTextoEstimado + 30 + 64);
                form.Height = altoNecesario;

                // Reaplicar borde redondeado tras el cambio de tamaño
                Paleta.AplicarBordeRedondeadoSuave(form, 14);
            };

            // === BOTONES (parte inferior) ===
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(250, 248, 252),
                Padding = new Padding(20, 14, 20, 14)
            };
            panelBotones.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 230, 240), 1);
                e.Graphics.DrawLine(pen, 0, 0, panelBotones.Width, 0);
            };
            form.Controls.Add(panelBotones);

            // Botón principal (Aceptar o Sí)
            var btnPrincipal = CrearBoton(
                mostrarBotonNo ? "  Sí, continuar  " : "  Aceptar  ",
                colorAccento, Color.White, esPrincipal: true);
            btnPrincipal.Click += (s, e) =>
            {
                form.DialogResult = mostrarBotonNo ? DialogResult.Yes : DialogResult.OK;
                form.Close();
            };
            panelBotones.Controls.Add(btnPrincipal);
            btnPrincipal.Dock = DockStyle.Right;

            // Botón secundario (No / Cancelar) si es confirmación
            if (mostrarBotonNo)
            {
                var btnNo = CrearBoton("  Cancelar  ",
                    Color.FromArgb(240, 237, 245),
                    Color.FromArgb(80, 75, 90),
                    esPrincipal: false);
                btnNo.Click += (s, e) =>
                {
                    form.DialogResult = DialogResult.No;
                    form.Close();
                };
                btnNo.Margin = new Padding(0, 0, 12, 0);
                panelBotones.Controls.Add(btnNo);
                btnNo.Dock = DockStyle.Right;
            }

            // === BOTÓN CERRAR (X) ===
            var btnCerrar = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 155, 165),
                Size = new Size(32, 32),
                Location = new Point(form.Width - 42, 12),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCerrar.Click += (s, e) =>
            {
                form.DialogResult = mostrarBotonNo ? DialogResult.No : DialogResult.Cancel;
                form.Close();
            };
            btnCerrar.MouseEnter += (s, e) => btnCerrar.ForeColor = Paleta.TextoOscuro;
            btnCerrar.MouseLeave += (s, e) => btnCerrar.ForeColor = Color.FromArgb(160, 155, 165);
            form.Controls.Add(btnCerrar);
            btnCerrar.BringToFront();

            // ESC cierra el form
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    form.DialogResult = mostrarBotonNo ? DialogResult.No : DialogResult.Cancel;
                    form.Close();
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    form.DialogResult = mostrarBotonNo ? DialogResult.Yes : DialogResult.OK;
                    form.Close();
                }
            };

            return form;
        }

        private static Button CrearBoton(string texto, Color colorFondo, Color colorTexto, bool esPrincipal)
        {
            var btn = new Button
            {
                Text = texto,
                Font = new Font("Segoe UI", 9.5f, esPrincipal ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = colorTexto,
                BackColor = colorFondo,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 36),
                Cursor = Cursors.Hand,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(18, 6, 18, 6)
            };
            btn.FlatAppearance.BorderSize = 0;

            if (esPrincipal)
            {
                btn.FlatAppearance.MouseOverBackColor = OscurecerColor(colorFondo, 0.12);
                btn.FlatAppearance.MouseDownBackColor = OscurecerColor(colorFondo, 0.25);
            }
            else
            {
                btn.FlatAppearance.MouseOverBackColor = OscurecerColor(colorFondo, 0.08);
            }

            btn.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btn, 18);
            return btn;
        }

        private static Color OscurecerColor(Color c, double factor)
        {
            return Color.FromArgb(
                (int)Math.Max(0, c.R * (1 - factor)),
                (int)Math.Max(0, c.G * (1 - factor)),
                (int)Math.Max(0, c.B * (1 - factor)));
        }

        private static (Color colorAccento, string icono) ObtenerColorYIcono(TipoMensaje tipo)
        {
            return tipo switch
            {
                TipoMensaje.Info => (Paleta.MoradoOscuro, "ℹ"),
                TipoMensaje.Exito => (ColorTranslator.FromHtml("#4A8F6F"), "✓"),
                TipoMensaje.Advertencia => (ColorTranslator.FromHtml("#D4841C"), "⚠"),
                TipoMensaje.Error => (ColorTranslator.FromHtml("#C13F3F"), "✕"),
                TipoMensaje.Pregunta => (Paleta.MoradoOscuro, "?"),
                _ => (Paleta.MoradoOscuro, "ℹ")
            };
        }
    }
}
