using MadurezTecnologica.Estilos;
using MadurezTecnologica.Seguridad;

namespace MadurezTecnologica.Presentacion
{
    // Ventana de inicio de sesión (RF-33).
    // Valida usuario y contraseña contra appconfi.json (contraseña con hash SHA-256).
    // Solo si la autenticación es exitosa devuelve DialogResult.OK y se abre el sistema.
    public partial class FormLogin : Form
    {
        private TextBox _txtUsuario = null!;
        private TextBox _txtPassword = null!;
        private Label _lblError = null!;
        private Panel _btnEntrar = null!;
        private Label _lblOjo = null!;

        public FormLogin()
        {
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Size = new Size(780, 480);
            MinimumSize = new Size(780, 480);   // el login mantiene su tamaño de diseño
            // Icono de la ventana (aparece en la barra de tareas mientras se está en el login)
            var icoApp = CargadorIconos.ObtenerIconoApp();
            if (icoApp != null) this.Icon = icoApp;

            // Esquinas redondeadas de toda la ventana
            Load += (s, e) => Paleta.AplicarBordeRedondeadoSuave(this, 20);
            Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(210, 205, 215), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };

            CrearPanelIzquierdo();
            CrearPanelDerecho();
        }

        // ===================================================
        // PANEL IZQUIERDO — marca (morado)
        // ===================================================
        private void CrearPanelIzquierdo()
        {
            var panelIzq = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = Paleta.MoradoOscuro
            };
            Controls.Add(panelIzq);

            // Permitir arrastrar la ventana desde el panel morado
            HabilitarArrastre(panelIzq);

            // Logo del sistema: el cubo con diamante morado (PNG embebido).
            // Se muestra sin fondo circular para no competir con el color del diamante.
            var imgLogo = CargadorIconos.ObtenerRedimensionado(CargadorIconos.App, 90, 90);
            if (imgLogo != null)
            {
                var picLogo = new PictureBox
                {
                    Image = imgLogo,
                    Size = new Size(90, 90),
                    Location = new Point(40, 70),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                panelIzq.Controls.Add(picLogo);
            }
            else
            {
                // Fallback: círculo con el emoji original si el recurso no existe
                var logo = new Panel
                {
                    Size = new Size(70, 70),
                    Location = new Point(40, 80),
                    BackColor = Paleta.MoradoClaro
                };
                var pathLogo = new System.Drawing.Drawing2D.GraphicsPath();
                pathLogo.AddEllipse(0, 0, 70, 70);
                logo.Region = new Region(pathLogo);
                var lblLogo = new Label
                {
                    Text = "📊",
                    Font = new Font("Segoe UI Emoji", 26),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                logo.Controls.Add(lblLogo);
                panelIzq.Controls.Add(logo);
            }

            var lblTitulo = new Label
            {
                Text = "Sistema de Madurez",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(40, 168),
                Size = new Size(230, 30),
                BackColor = Color.Transparent
            };
            panelIzq.Controls.Add(lblTitulo);

            var lblSub = new Label
            {
                Text = "Tecnológica para PYMES",
                Font = new Font("Segoe UI", 11),
                ForeColor = ColorTranslator.FromHtml("#C9BEE0"),
                Location = new Point(42, 200),
                Size = new Size(230, 24),
                BackColor = Color.Transparent
            };
            panelIzq.Controls.Add(lblSub);

            var lblTagline = new Label
            {
                Text = "Evaluación de madurez tecnológica\nasistida por inteligencia artificial.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ColorTranslator.FromHtml("#B7AAD3"),
                Location = new Point(42, 260),
                Size = new Size(230, 44),
                BackColor = Color.Transparent
            };
            panelIzq.Controls.Add(lblTagline);

            var lblPie = new Label
            {
                Text = "© 2026 Angelo Rivero · Yelimar Sánchez",
                Font = new Font("Segoe UI", 8),
                ForeColor = ColorTranslator.FromHtml("#9C8FBC"),
                Location = new Point(42, 430),
                Size = new Size(240, 18),
                BackColor = Color.Transparent
            };
            panelIzq.Controls.Add(lblPie);
        }

        // ===================================================
        // PANEL DERECHO — formulario de login (blanco)
        // ===================================================
        private void CrearPanelDerecho()
        {
            var panelDer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(48, 40, 48, 40)
            };
            Controls.Add(panelDer);
            panelDer.BringToFront();

            // Botón cerrar (X)
            var btnCerrar = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 145, 145),
                Size = new Size(34, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCerrar.Location = new Point(Width - 300 - 44, 12);
            btnCerrar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnCerrar.MouseEnter += (s, e) => btnCerrar.ForeColor = Paleta.MoradoOscuro;
            btnCerrar.MouseLeave += (s, e) => btnCerrar.ForeColor = Color.FromArgb(150, 145, 145);
            panelDer.Controls.Add(btnCerrar);

            var lblBienvenida = new Label
            {
                Text = "Iniciar sesión",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(48, 70),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelDer.Controls.Add(lblBienvenida);

            var lblInstr = new Label
            {
                Text = "Ingresa tus credenciales para acceder al sistema.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(140, 135, 135),
                Location = new Point(50, 106),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelDer.Controls.Add(lblInstr);

            // === Campo Usuario ===
            var lblUsuario = new Label
            {
                Text = "USUARIO",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 135),
                Location = new Point(50, 150),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelDer.Controls.Add(lblUsuario);

            var wrapUsuario = CrearWrapperInput(new Point(50, 170), out _txtUsuario);
            _txtUsuario.PlaceholderText = "Nombre de usuario";
            panelDer.Controls.Add(wrapUsuario);

            // === Campo Contraseña ===
            var lblPassword = new Label
            {
                Text = "CONTRASEÑA",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 135),
                Location = new Point(50, 224),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelDer.Controls.Add(lblPassword);

            var wrapPassword = CrearWrapperInput(new Point(50, 244), out _txtPassword);
            _txtPassword.PlaceholderText = "Contraseña";
            _txtPassword.UseSystemPasswordChar = true;
            panelDer.Controls.Add(wrapPassword);

            // Ojo para mostrar/ocultar contraseña
            _lblOjo = new Label
            {
                Text = "👁",
                Font = new Font("Segoe UI Emoji", 11),
                ForeColor = Color.FromArgb(150, 145, 145),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _lblOjo.Click += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
            };
            wrapPassword.Controls.Add(_lblOjo);
            _lblOjo.BringToFront();
            wrapPassword.Resize += (s, e) =>
                _lblOjo.Location = new Point(wrapPassword.Width - 40, (wrapPassword.Height - 30) / 2);
            _lblOjo.Location = new Point(wrapPassword.Width - 40, (wrapPassword.Height - 30) / 2);

            // === Mensaje de error ===
            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTranslator.FromHtml("#C13F3F"),
                Location = new Point(50, 300),
                Size = new Size(340, 20),
                BackColor = Color.Transparent,
                Visible = false
            };
            panelDer.Controls.Add(_lblError);

            // === Botón Entrar ===
            _btnEntrar = new Panel
            {
                Location = new Point(50, 330),
                Size = new Size(340, 46),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            _btnEntrar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(_btnEntrar, 23);
            var lblEntrar = new Label
            {
                Text = "Entrar",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _btnEntrar.Controls.Add(lblEntrar);
            panelDer.Controls.Add(_btnEntrar);

            // Hover del botón
            _btnEntrar.MouseEnter += (s, e) => _btnEntrar.BackColor = Paleta.MoradoOscuroHover;
            _btnEntrar.MouseLeave += (s, e) => _btnEntrar.BackColor = Paleta.MoradoOscuro;
            lblEntrar.MouseEnter += (s, e) => _btnEntrar.BackColor = Paleta.MoradoOscuroHover;
            lblEntrar.MouseLeave += (s, e) => _btnEntrar.BackColor = Paleta.MoradoOscuro;

            // Click → intentar login
            _btnEntrar.Click += (s, e) => IntentarLogin();
            lblEntrar.Click += (s, e) => IntentarLogin();

            // Enter en cualquiera de los campos → login
            _txtUsuario.KeyDown += TxtKeyDown;
            _txtPassword.KeyDown += TxtKeyDown;

            _txtUsuario.Select();
        }

        // Crea un wrapper redondeado con un TextBox adentro (estilo consistente con la app)
        private Panel CrearWrapperInput(Point ubicacion, out TextBox txt)
        {
            var wrapper = new Panel
            {
                Location = ubicacion,
                Size = new Size(340, 44),
                BackColor = Paleta.LilaInput,
                Padding = new Padding(14, 11, 14, 11)
            };
            wrapper.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(wrapper, 22);

            txt = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = Paleta.TextoOscuro,
                BackColor = Paleta.LilaInput,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };
            wrapper.Controls.Add(txt);
            return wrapper;
        }

        private void TxtKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                IntentarLogin();
            }
        }

        // ===================================================
        // VALIDACIÓN
        // ===================================================
        private void IntentarLogin()
        {
            string usuario = _txtUsuario.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                MostrarError("Ingresa tu usuario y contraseña.");
                return;
            }

            if (Autenticador.Validar(usuario, password))
            {
                // Autenticación exitosa → dejar entrar al sistema
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MostrarError("Usuario o contraseña incorrectos.");
                _txtPassword.Clear();
                _txtPassword.Select();
            }
        }

        private void MostrarError(string mensaje)
        {
            _lblError.Text = "⚠  " + mensaje;
            _lblError.Visible = true;
        }

        // ===================================================
        // ARRASTRE DE LA VENTANA (FormBorderStyle.None)
        // ===================================================
        private void HabilitarArrastre(Control control)
        {
            bool arrastrando = false;
            Point inicio = Point.Empty;

            control.MouseDown += (s, e) => { arrastrando = true; inicio = e.Location; };
            control.MouseUp += (s, e) => arrastrando = false;
            control.MouseMove += (s, e) =>
            {
                if (arrastrando)
                    Location = new Point(Location.X + e.X - inicio.X, Location.Y + e.Y - inicio.Y);
            };
        }
    }
}
