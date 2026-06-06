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
            panelCentral = new Panel
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

        private void CambiarVista(Button botonClickeado, string nombreVista)
        {
            EstablecerBotonActivo(botonClickeado);

            panelCentral.Controls.Clear();

            // Si el usuario clickeó "Análisis con IA (Chat)", cargamos VistaChat
            if (botonClickeado == btnChat)
            {
                var vistaChat = new MadurezTecnologica.Vistas.VistaChat();
                panelCentral.Controls.Add(vistaChat);
                return;
            }

            // Resto de vistas: placeholder por ahora
            var lbl = new Label
            {
                Text = $"Vista: {nombreVista}\n\n(Se construirá próximamente)",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelCentral.Controls.Add(lbl);
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
    }
}