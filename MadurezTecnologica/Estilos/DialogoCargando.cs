using System.Drawing.Drawing2D;

namespace MadurezTecnologica.Estilos
{
    /// <summary>
    /// Diálogo modal con barra de progreso animada indeterminada y botón cancelar.
    /// Bloquea la interacción con el resto de la app mientras se ejecuta la operación.
    /// El caller pasa una Task&lt;T&gt; y el diálogo se cierra automáticamente al terminar.
    /// </summary>
    public class DialogoCargando : Form
    {
        private System.Windows.Forms.Timer _timerAnimacion = null!;
        private float _progresoAnimado = 0f;
        private Panel _barraProgreso = null!;
        private Label _lblEstado = null!;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public CancellationToken Token => _cts.Token;
        public bool FueCancelado { get; private set; }

        public DialogoCargando(string titulo, string mensaje)
        {
            ConfigurarForm();
            CrearContenido(titulo, mensaje);
            IniciarAnimacion();
        }

        private void ConfigurarForm()
        {
            Text = "";
            Size = new Size(480, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;
            ShowInTaskbar = false;
            ControlBox = false;

            Load += (s, e) => Paleta.AplicarBordeRedondeadoSuave(this, 14);

            Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(195, 188, 210), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        private void CrearContenido(string titulo, string mensaje)
        {
            // === HEADER MORADO ===
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Paleta.MoradoOscuro
            };
            Controls.Add(header);

            // Círculo con icono de reloj/analizando
            var iconoCirculo = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(20, 15),
                BackColor = Paleta.MoradoClaro
            };
            var pathIcon = new GraphicsPath();
            pathIcon.AddEllipse(0, 0, 40, 40);
            iconoCirculo.Region = new Region(pathIcon);

            var lblIcon = new Label
            {
                Text = "⚡",
                Font = new Font("Segoe UI Emoji", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconoCirculo.Controls.Add(lblIcon);
            header.Controls.Add(iconoCirculo);

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(72, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblTitulo);

            // === CUERPO ===
            _lblEstado = new Label
            {
                Text = mensaje,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(80, 75, 85),
                Location = new Point(24, 90),
                Size = new Size(430, 40),
                BackColor = Color.Transparent
            };
            Controls.Add(_lblEstado);

            // === BARRA DE PROGRESO INDETERMINADA ===
            var contenedorBarra = new Panel
            {
                Location = new Point(24, 138),
                Size = new Size(430, 8),
                BackColor = Color.FromArgb(232, 226, 240)
            };
            contenedorBarra.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(contenedorBarra, 4);
            Paleta.AplicarBordeRedondeadoSuave(contenedorBarra, 4);
            Controls.Add(contenedorBarra);

            _barraProgreso = new Panel
            {
                Location = new Point(-90, 0),
                Size = new Size(90, 8),
                BackColor = Paleta.MoradoOscuro
            };
            _barraProgreso.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(_barraProgreso, 4);
            Paleta.AplicarBordeRedondeadoSuave(_barraProgreso, 4);
            contenedorBarra.Controls.Add(_barraProgreso);

            // === TEXTO DEBAJO DE LA BARRA ===
            var lblHint = new Label
            {
                Text = "No cierres la ventana ni cambies de pestaña hasta que termine.",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 135, 145),
                Location = new Point(24, 160),
                Size = new Size(430, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            Controls.Add(lblHint);

            // === BOTÓN CANCELAR ===
            var btnCancelar = new Panel
            {
                Size = new Size(130, 36),
                Location = new Point((Width - 130) / 2, 200),
                BackColor = Color.FromArgb(230, 225, 232),
                Cursor = Cursors.Hand
            };
            btnCancelar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnCancelar, 18);
            Paleta.AplicarBordeRedondeadoSuave(btnCancelar, 18);

            var lblCancelar = new Label
            {
                Text = "✕  Cancelar",
                Font = new Font("Segoe UI Emoji", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(90, 80, 100),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCancelar.Controls.Add(lblCancelar);

            Color colorNormal = Color.FromArgb(230, 225, 232);
            Color colorHover = Color.FromArgb(215, 208, 220);
            btnCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = colorHover;
            btnCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = colorNormal;
            lblCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = colorHover;
            lblCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = colorNormal;

            EventHandler cancelarClick = (s, e) =>
            {
                FueCancelado = true;
                _cts.Cancel();
                lblCancelar.Text = "⏳  Cancelando...";
                btnCancelar.Enabled = false;
                _lblEstado.Text = "Cancelando la operación, por favor espera...";
            };
            btnCancelar.Click += cancelarClick;
            lblCancelar.Click += cancelarClick;

            Controls.Add(btnCancelar);
        }

        private void IniciarAnimacion()
        {
            _timerAnimacion = new System.Windows.Forms.Timer { Interval = 25 };
            _timerAnimacion.Tick += (s, e) =>
            {
                if (_barraProgreso == null || _barraProgreso.Parent == null) return;

                _progresoAnimado += 4f;
                int anchoContenedor = _barraProgreso.Parent.Width;

                // La barra se mueve de izquierda a derecha continuamente
                int posX = (int)_progresoAnimado - _barraProgreso.Width;
                if (posX > anchoContenedor)
                {
                    _progresoAnimado = 0;
                    posX = -_barraProgreso.Width;
                }
                _barraProgreso.Location = new Point(posX, 0);
            };
            _timerAnimacion.Start();
        }

        /// <summary>
        /// Actualiza el texto de estado mientras se ejecuta la operación.
        /// Se puede llamar desde cualquier hilo.
        /// </summary>
        public void ActualizarMensaje(string mensaje)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ActualizarMensaje(mensaje)));
                return;
            }
            if (_lblEstado != null) _lblEstado.Text = mensaje;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timerAnimacion?.Stop();
            _timerAnimacion?.Dispose();
            _cts?.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Ejecuta una operación async mostrando el diálogo. El diálogo se cierra
        /// automáticamente cuando la operación termine (con éxito, error o cancelación).
        /// Devuelve el resultado, o el default si fue cancelado.
        /// </summary>
        public static async Task<T?> EjecutarConDialogo<T>(
            IWin32Window owner,
            string titulo,
            string mensaje,
            Func<CancellationToken, Task<T>> operacion)
        {
            using var dialogo = new DialogoCargando(titulo, mensaje);
            T? resultado = default;
            Exception? excepcion = null;

            // Ejecutar la operación después de mostrar el diálogo
            dialogo.Shown += async (s, e) =>
            {
                try
                {
                    resultado = await operacion(dialogo.Token);
                }
                catch (OperationCanceledException)
                {
                    // Cancelación normal, no propagar
                }
                catch (Exception ex)
                {
                    excepcion = ex;
                }
                finally
                {
                    if (!dialogo.IsDisposed)
                        dialogo.Close();
                }
            };

            dialogo.ShowDialog(owner);

            if (excepcion != null) throw excepcion;
            return resultado;
        }
    }
}
