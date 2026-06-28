using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Presentacion
{
    public partial class FormMain : Form
    {
        // Controles del menú lateral
        private Panel panelMenu = null!;
        private Panel panelCentral = null!;
        private Panel panelLogo = null!;
        private Label lblTitulo = null!;
        private Label lblSubtitulo = null!;

        // Botones del menú
        private Button btnInicio = null!;
        private Button btnEmpresas = null!;
        private Button btnCargarInforme = null!;
        private Button btnChat = null!;
        private Button btnResultados = null!;
        private Button btnHistorial = null!;

        // Botón actualmente activo
        private Button? botonActivo = null;

        public FormMain()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearMenuLateral();
            CrearPanelCentral();
            EstablecerBotonActivo(btnInicio);
        }

        private void ConfigurarFormulario()
        {
            Text = "Sistema de Evaluación de Madurez Tecnológica";
            Size = new Size(1280, 760);
            MinimumSize = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Paleta.GrisClaro;
            WindowState = FormWindowState.Maximized;
        }

        private void CrearMenuLateral()
        {
            panelMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Paleta.MoradoOscuro
            };
            Controls.Add(panelMenu);

            // Aplicar esquinas redondeadas y mantenerlas al redimensionar la ventana
            panelMenu.Resize += (s, e) => AplicarBordeRedondeado(panelMenu, 30);

            panelLogo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Paleta.MoradoOscuro
            };
            panelMenu.Controls.Add(panelLogo);

            // Logo placeholder (círculo morado claro)
            var picLogo = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(20, 25),
                BackColor = Paleta.MoradoClaro
            };
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, picLogo.Width, picLogo.Height);
            picLogo.Region = new Region(path);
            panelLogo.Controls.Add(picLogo);

            lblTitulo = new Label
            {
                Text = "Sistema de Madurez",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(75, 28),
                Size = new Size(170, 22),
                BackColor = Color.Transparent
            };
            panelLogo.Controls.Add(lblTitulo);

            lblSubtitulo = new Label
            {
                Text = "Tecnológica para PYMES",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(75, 50),
                Size = new Size(170, 18),
                BackColor = Color.Transparent
            };
            panelLogo.Controls.Add(lblSubtitulo);

            btnInicio = CrearBotonMenu("  Inicio", 150);
            btnEmpresas = CrearBotonMenu("  Empresas", 205);
            btnCargarInforme = CrearBotonMenu("  Cargar Informe", 260);
            btnChat = CrearBotonMenu("  Análisis con IA (Chat)", 315);
            btnResultados = CrearBotonMenu("  Resultados", 370);
            btnHistorial = CrearBotonMenu("  Historial", 425);

            btnInicio.Click += (s, e) => CambiarVista(btnInicio, "Inicio");
            btnEmpresas.Click += (s, e) => CambiarVista(btnEmpresas, "Empresas");
            btnCargarInforme.Click += (s, e) => CambiarVista(btnCargarInforme, "Cargar Informe");
            btnChat.Click += (s, e) => CambiarVista(btnChat, "Análisis con IA (Chat)");
            btnResultados.Click += (s, e) => CambiarVista(btnResultados, "Resultados");
            btnHistorial.Click += (s, e) => CambiarVista(btnHistorial, "Historial");
        }

        private Button CrearBotonMenu(string texto, int top) 
        {
            var btn = new Button
            {
                Text = texto,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(230, 45),
                Location = new Point(10, top),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            btn.FlatAppearance.MouseDownBackColor = Paleta.MoradoOscuroHover;
            panelMenu.Controls.Add(btn);
            return btn;
        }

        private void CrearPanelCentral()
        {
            panelCentral = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Paleta.GrisClaro,
                Padding = new Padding(20)
            };
            Controls.Add(panelCentral);

            panelCentral.BringToFront();
            panelMenu.SendToBack();

            var lblPlaceholder = new Label
            {
                Text = "Bienvenido al Sistema de Evaluación de Madurez Tecnológica",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelCentral.Controls.Add(lblPlaceholder);
        }

        private bool _animandoVista = false;
        private Button? _botonActivoActual = null;

        private void CambiarVista(Button botonClickeado, string nombreVista)
        {
            if (_animandoVista) return;

            // Si ya estoy en esa vista, no hago nada
            if (_botonActivoActual == botonClickeado) return;

            EstablecerBotonActivo(botonClickeado);
            _botonActivoActual = botonClickeado;

            Control nuevaVista = CrearVistaParaBoton(botonClickeado, nombreVista);

            if (panelCentral.Controls.Count == 0)
            {
                panelCentral.Controls.Add(nuevaVista);
                return;
            }

            Control vistaAnterior = panelCentral.Controls[0];
            AnimarTransicionVista(vistaAnterior, nuevaVista);
        }

        private Control CrearVistaParaBoton(Button botonClickeado, string nombreVista)
        {
            if (botonClickeado == btnChat)
                return new MadurezTecnologica.Vistas.VistaChat();

            if (botonClickeado == btnEmpresas)
                return new MadurezTecnologica.Vistas.VistaEmpresas();

            if (botonClickeado == btnCargarInforme)
                return new MadurezTecnologica.Vistas.VistaCargarInforme();

            if (botonClickeado == btnResultados)
                return new MadurezTecnologica.Vistas.VistaResultados();

            if (botonClickeado == btnHistorial)
                return new MadurezTecnologica.Vistas.VistaHistorial();

            return new Label
            {
                Text = $"Vista: {nombreVista}\n\n(Se construirá próximamente)",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void AnimarTransicionVista(Control vistaAnterior, Control nuevaVista)
        {
            _animandoVista = true;

            int anchoTotal = panelCentral.ClientSize.Width;
            int altoTotal = panelCentral.ClientSize.Height;

            panelCentral.SuspendLayout();

            vistaAnterior.Dock = DockStyle.None;
            vistaAnterior.Size = new Size(anchoTotal, altoTotal);
            vistaAnterior.Location = new Point(0, 0);

            nuevaVista.Dock = DockStyle.None;
            nuevaVista.Size = new Size(anchoTotal, altoTotal);
            nuevaVista.Location = new Point(anchoTotal, 0);

            panelCentral.Controls.Add(nuevaVista);
            panelCentral.ResumeLayout(false);

            int totalPasos = 30;
            int paso = 0;
            var cronometro = System.Diagnostics.Stopwatch.StartNew();
            int duracionMs = 320;

            var timer = new System.Windows.Forms.Timer { Interval = 8 };

            timer.Tick += (s, e) =>
            {
                paso++;
                double t = Math.Min(1.0, cronometro.ElapsedMilliseconds / (double)duracionMs);
                double eased = 1 - Math.Pow(1 - t, 5);
                int offset = (int)(anchoTotal * eased);

                vistaAnterior.SuspendLayout();
                nuevaVista.SuspendLayout();

                vistaAnterior.Location = new Point(-offset, 0);
                nuevaVista.Location = new Point(anchoTotal - offset, 0);

                vistaAnterior.ResumeLayout(false);
                nuevaVista.ResumeLayout(false);

                if (t >= 1.0 || paso >= totalPasos)
                {
                    timer.Stop();
                    timer.Dispose();
                    cronometro.Stop();

                    panelCentral.SuspendLayout();
                    panelCentral.Controls.Remove(vistaAnterior);
                    vistaAnterior.Dispose();

                    nuevaVista.Location = new Point(0, 0);
                    nuevaVista.Dock = DockStyle.Fill;
                    panelCentral.ResumeLayout(true);

                    _animandoVista = false;
                }
            };
            timer.Start();
        }

        private void EstablecerBotonActivo(Button botonNuevo)
        {
            if (botonActivo != null)
            {
                botonActivo.BackColor = Paleta.MoradoOscuro;
                botonActivo.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            }

            botonNuevo.BackColor = Paleta.MoradoClaro;
            botonNuevo.FlatAppearance.MouseOverBackColor = Paleta.MoradoClaro;
            botonActivo = botonNuevo;
        }
        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            if (panel.Width <= 0 || panel.Height <= 0) return;

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radio, radio, 180, 90);
            path.AddArc(panel.Width - radio, 0, radio, radio, 270, 90);
            path.AddArc(panel.Width - radio, panel.Height - radio, radio, radio, 0, 90);
            path.AddArc(0, panel.Height - radio, radio, radio, 90, 90);
            path.CloseFigure();
            panel.Region = new Region(path);
        }
        public void NavegarAVistaChat()
        {
            // Simulamos un click en el botón del menú lateral
            CambiarVista(btnChat, "Análisis con IA (Chat)");
        }
    }

    // Panel con doble buffer activado para animaciones sin parpadeo
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }
    }
}