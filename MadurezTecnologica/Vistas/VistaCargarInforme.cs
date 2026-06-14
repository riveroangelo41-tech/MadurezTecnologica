using MadurezTecnologica.Estilos;
using System.Runtime.InteropServices;

namespace MadurezTecnologica.Vistas
{
    public partial class VistaCargarInforme : UserControl
    {

        // === Soporte de drag-and-drop con UAC (Windows UIPI) ===
        [DllImport("user32.dll")]
        private static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);
        private const uint WM_DROPFILES = 0x0233;
        private const uint WM_COPYDATA = 0x004A;
        private const uint WM_COPYGLOBALDATA = 0x0049;
        private const uint MSGFLT_ADD = 1;

        // Paneles principales
        private Panel panelHeader = null!;
        private Panel panelContenido = null!;

        // Header
        private Label lblTitulo = null!;
        private Label lblSubtitulo = null!;

        // Banner de empresa activa
        private Panel panelBannerEmpresa = null!;
        private Label lblNombreEmpresa = null!;

        // Contenedor de las dos tarjetas
        private Panel panelTarjetas = null!;
        private Panel tarjetaDescargar = null!;
        private Panel tarjetaSubir = null!;

        // Botón final "Analizar con IA"
        private Panel btnAnalizar = null!;
        private Label lblBtnAnalizar = null!;

        // Estado del archivo cargado (para más adelante)
        private string? _archivoSeleccionado = null;

        // Controles del estado de la tarjeta 2 (zona drop / archivo cargado)
        private Panel zonaDrop = null!;
        private Panel zonaArchivo = null!;
        private Label lblNombreArchivo = null!;
        private Label lblTamanoArchivo = null!;
        public VistaCargarInforme()
        {
            InitializeComponent();

            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();

            ConfigurarControl();
            CrearPanelContenido();
            CrearHeader();

            // Suscribirse al evento del estado global
            this.Load += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio += OnEmpresaActivaCambio;
                this.BeginInvoke(new Action(() => CargarEmpresaActiva()));
            };
            this.HandleDestroyed += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio -= OnEmpresaActivaCambio;
            };

            // Permitir drag-and-drop incluso si la app corre con UAC elevado
            try
            {
                ChangeWindowMessageFilter(WM_DROPFILES, MSGFLT_ADD);
                ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ADD);
                ChangeWindowMessageFilter(WM_COPYGLOBALDATA, MSGFLT_ADD);
            }
            catch { /* no es crítico si falla */ }

        }

        // ===================================================
        // CONFIGURACIÓN GENERAL
        // ===================================================
        private void ConfigurarControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.GrisClaro;
        }

        // ===================================================
        // HEADER (arriba)
        // ===================================================
        private void CrearHeader()
        {
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Paleta.GrisClaro,
                Padding = new Padding(20, 15, 20, 10)
            };
            Controls.Add(panelHeader);

            // Avatar circular con icono PDF
            var picAvatar = new Panel
            {
                Size = new Size(50, 50),
                Location = new Point(10, 15),
                BackColor = Paleta.MoradoClaro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, picAvatar.Width, picAvatar.Height);
            picAvatar.Region = new Region(pathAv);

            var lblIcono = new Label
            {
                Text = "📄",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            picAvatar.Controls.Add(lblIcono);
            panelHeader.Controls.Add(picAvatar);

            // Título
            lblTitulo = new Label
            {
                Text = "Cargar Informe",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 15),
                Size = new Size(500, 30),
                BackColor = Color.Transparent
            };
            panelHeader.Controls.Add(lblTitulo);

            // Subtítulo
            lblSubtitulo = new Label
            {
                Text = "Genera la plantilla, llénala con tu empresa y súbela para analizar con IA",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 48),
                Size = new Size(700, 20),
                BackColor = Color.Transparent
            };
            panelHeader.Controls.Add(lblSubtitulo);
        }

        // ===================================================
        // PANEL CENTRAL CONTENEDOR (blanco, redondeado)
        // ===================================================
        private void CrearPanelContenido()
        {
            panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30, 22, 30, 28)
            };
            Controls.Add(panelContenido);
            panelContenido.Resize += (s, e) => AplicarBordeRedondeado(panelContenido, 25);

            // 1) Banner empresa (arriba) - Dock Top
            CrearBannerEmpresa();

            // 2) Botón "Analizar con IA" (abajo) - Dock Bottom
            CrearBotonAnalizar();

            // 3) Contenedor de tarjetas (centro) - Dock Fill
            panelTarjetas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0, 20, 0, 20)
            };
            panelContenido.Controls.Add(panelTarjetas);

            // Re-centrar las tarjetas cuando el panel cambie de tamaño
            panelTarjetas.Resize += (s, e) => CentrarTarjetas();

            // Crear las dos tarjetas
            tarjetaDescargar = CrearTarjetaDescarga();
            tarjetaSubir = CrearTarjetaSubida();

            panelTarjetas.Controls.Add(tarjetaDescargar);
            panelTarjetas.Controls.Add(tarjetaSubir);

            // Aplicar centrado inicial (se hace después de Layout para tener Width real)
            panelTarjetas.HandleCreated += (s, e) =>
            {
                panelTarjetas.BeginInvoke(new Action(() => CentrarTarjetas()));
            };

            // Asegurar el orden de capas
            panelTarjetas.BringToFront();
        }

        // ===================================================
        // UTILIDADES
        // ===================================================
        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            Paleta.AplicarBordeRedondeadoSuave(panel, radio);
        }

        // ===================================================
        // BANNER DE EMPRESA ACTIVA (arriba del contenido)
        // ===================================================
        private void CrearBannerEmpresa()
        {
            panelBannerEmpresa = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Padding = new Padding(15, 10, 15, 10)
            };
            panelContenido.Controls.Add(panelBannerEmpresa);

            // Barra morada del lado izquierdo (visual de "acento")
            var barraIzq = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Paleta.MoradoOscuro
            };
            panelBannerEmpresa.Controls.Add(barraIzq);

            // Avatar circular con inicial de la empresa
            var avatar = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(20, 12),
                BackColor = Paleta.MoradoOscuro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAv);

            var lblInicial = new Label
            {
                Name = "lblInicialEmpresa",
                Text = "?",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);
            panelBannerEmpresa.Controls.Add(avatar);

            // Etiqueta "EMPRESA SELECCIONADA"
            var lblLabel = new Label
            {
                Text = "EMPRESA SELECCIONADA",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 125, 122),
                Location = new Point(65, 8),
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            panelBannerEmpresa.Controls.Add(lblLabel);

            // Nombre de la empresa
            lblNombreEmpresa = new Label
            {
                Text = "Sin empresa seleccionada",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(65, 26),
                Size = new Size(700, 22),
                BackColor = Color.Transparent
            };
            panelBannerEmpresa.Controls.Add(lblNombreEmpresa);
        }
        // ===================================================
        // BOTÓN "ANALIZAR CON IA" (abajo, centrado)
        // ===================================================
        private void CrearBotonAnalizar()
        {
            var panelBotonContenedor = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.White
            };
            panelContenido.Controls.Add(panelBotonContenedor);

            btnAnalizar = new Panel
            {
                BackColor = Color.FromArgb(200, 200, 200),  // gris (deshabilitado)
                Size = new Size(220, 48),
                Cursor = Cursors.Default
            };

            lblBtnAnalizar = new Label
            {
                Text = "⚡  Analizar con IA",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            btnAnalizar.Controls.Add(lblBtnAnalizar);

            // Forma píldora
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 48, 48, 90, 180);
            pathBtn.AddArc(btnAnalizar.Width - 48, 0, 48, 48, 270, 180);
            pathBtn.CloseFigure();
            btnAnalizar.Region = new Region(pathBtn);

            panelBotonContenedor.Controls.Add(btnAnalizar);

            // Centrar horizontalmente al redimensionar
            panelBotonContenedor.Resize += (s, e) =>
            {
                btnAnalizar.Location = new Point(
                    (panelBotonContenedor.Width - btnAnalizar.Width) / 2,
                    (panelBotonContenedor.Height - btnAnalizar.Height) / 2);
            };
            // Centrado inicial
            panelBotonContenedor.HandleCreated += (s, e) =>
            {
                panelBotonContenedor.BeginInvoke(new Action(() =>
                {
                    btnAnalizar.Location = new Point(
                        (panelBotonContenedor.Width - btnAnalizar.Width) / 2,
                        (panelBotonContenedor.Height - btnAnalizar.Height) / 2);
                }));
            };

            // El click lo conectaremos en la Tarea 5
        }
        // ===================================================
        // TARJETAS (esqueleto - sin contenido detallado todavía)
        // ===================================================
        private Panel CrearTarjetaDescarga()
        {
            var tarjeta = new Panel
            {
                Size = new Size(420, 380),
                BackColor = ColorTranslator.FromHtml("#F9F5FF"),
                Padding = new Padding(28, 28, 28, 28)
            };
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 20);

            // Número del paso (badge "1")
            var numero = new Panel
            {
                Size = new Size(32, 32),
                Location = new Point(20, -8),
                BackColor = Paleta.MoradoOscuro
            };
            var pathNum = new System.Drawing.Drawing2D.GraphicsPath();
            pathNum.AddEllipse(0, 0, 32, 32);
            numero.Region = new Region(pathNum);

            var lblNum = new Label
            {
                Text = "1",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            numero.Controls.Add(lblNum);
            tarjeta.Controls.Add(numero);

            // Título
            var lblTitulo = new Label
            {
                Text = "Descargar plantilla",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 35),
                Size = new Size(360, 26),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitulo);

            // Descripción
            var lblDesc = new Label
            {
                Text = "Genera la plantilla Word y entrégala a la empresa para que la complete con su información.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 115, 112),
                Location = new Point(20, 64),
                Size = new Size(360, 40),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblDesc);

            // === ICONO WORD GRANDE ===
            var iconoWord = new Panel
            {
                Size = new Size(90, 90),
                Location = new Point((420 - 90) / 2, 130),  // centrado horizontalmente
                BackColor = ColorTranslator.FromHtml("#2B579A")  // azul Word
            };
            // Redondear el cuadrado en 14px
            var pathIcono = new System.Drawing.Drawing2D.GraphicsPath();
            int rIcono = 14;
            pathIcono.AddArc(0, 0, rIcono * 2, rIcono * 2, 180, 90);
            pathIcono.AddArc(iconoWord.Width - rIcono * 2, 0, rIcono * 2, rIcono * 2, 270, 90);
            pathIcono.AddArc(iconoWord.Width - rIcono * 2, iconoWord.Height - rIcono * 2, rIcono * 2, rIcono * 2, 0, 90);
            pathIcono.AddArc(0, iconoWord.Height - rIcono * 2, rIcono * 2, rIcono * 2, 90, 90);
            pathIcono.CloseFigure();
            iconoWord.Region = new Region(pathIcono);

            var lblWordLetra = new Label
            {
                Text = "W",
                Font = new Font("Segoe UI", 42, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconoWord.Controls.Add(lblWordLetra);
            tarjeta.Controls.Add(iconoWord);

            // === INFO BAJO EL ICONO ===
            var lblInfo = new Label
            {
                Text = "11 secciones sobre procesos, infraestructura,\ncalidad y seguridad",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 95, 92),
                Location = new Point(40, 230),
                Size = new Size(340, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblInfo);

            // === BOTÓN "DESCARGAR PLANTILLA WORD" (Panel + Label) ===
            var btnDescargar = new Panel
            {
                Size = new Size(220, 40),
                Location = new Point((420 - 220) / 2, 280),  // centrado horizontalmente
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 40, 40, 90, 180);
            pathBtn.AddArc(btnDescargar.Width - 40, 0, 40, 40, 270, 180);
            pathBtn.CloseFigure();
            btnDescargar.Region = new Region(pathBtn);

            var lblBtnDescargar = new Label
            {
                Text = "📥  Descargar plantilla Word",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnDescargar.Controls.Add(lblBtnDescargar);

            // Hover
            Color colorNormal = Paleta.MoradoOscuro;
            Color colorHover = Paleta.MoradoOscuroHover;
            btnDescargar.MouseEnter += (s, e) => btnDescargar.BackColor = colorHover;
            btnDescargar.MouseLeave += (s, e) => btnDescargar.BackColor = colorNormal;
            lblBtnDescargar.MouseEnter += (s, e) => btnDescargar.BackColor = colorHover;
            lblBtnDescargar.MouseLeave += (s, e) => btnDescargar.BackColor = colorNormal;

            // Click → descargar plantilla
            EventHandler descargarClick = (s, e) => OnDescargarPlantillaClick();
            btnDescargar.Click += descargarClick;
            lblBtnDescargar.Click += descargarClick;

            tarjeta.Controls.Add(btnDescargar);

            // Formato info
            var lblFormato = new Label
            {
                Text = "Formato: .docx",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(160, 155, 152),
                Location = new Point(40, 330),
                Size = new Size(340, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblFormato);

            return tarjeta;
        }

        private Panel CrearTarjetaSubida()
        {
            var tarjeta = new Panel
            {
                Size = new Size(420, 380),
                BackColor = Color.White,
                Padding = new Padding(28, 28, 28, 28)
            };
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 20);

            // Número "2"
            var numero = new Panel
            {
                Size = new Size(32, 32),
                Location = new Point(20, -8),
                BackColor = Paleta.MoradoOscuro
            };
            var pathNum = new System.Drawing.Drawing2D.GraphicsPath();
            pathNum.AddEllipse(0, 0, 32, 32);
            numero.Region = new Region(pathNum);

            var lblNum = new Label
            {
                Text = "2",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            numero.Controls.Add(lblNum);
            tarjeta.Controls.Add(numero);

            // Título
            var lblTitulo = new Label
            {
                Text = "Subir plantilla completada",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 35),
                Size = new Size(360, 26),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitulo);

            // Descripción
            var lblDesc = new Label
            {
                Text = "Una vez llenada por la empresa, conviértela a PDF y súbela aquí para el análisis.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 115, 112),
                Location = new Point(20, 64),
                Size = new Size(360, 40),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblDesc);

            // === ZONA DROP (visible inicialmente, cuando NO hay archivo) ===
            zonaDrop = CrearZonaDrop();
            zonaDrop.Location = new Point(15, 120);
            tarjeta.Controls.Add(zonaDrop);

            // === ZONA "ARCHIVO CARGADO" (oculta inicialmente) ===
            zonaArchivo = CrearZonaArchivo();
            zonaArchivo.Location = new Point(15, 120);
            zonaArchivo.Visible = false;
            tarjeta.Controls.Add(zonaArchivo);

            return tarjeta;
        }

        // ===================================================
        // ZONA DROP (cuando NO hay archivo cargado)
        // ===================================================
        private Panel CrearZonaDrop()
        {
            var zona = new Panel
            {
                Size = new Size(390, 230),
                BackColor = ColorTranslator.FromHtml("#FAF7FF"),
                AllowDrop = true,
                Cursor = Cursors.Hand
            };

            // Borde punteado morado (dibujado con Paint)
            zona.Paint += (s, e) =>
            {
                using var pen = new Pen(Paleta.MoradoClaro, 2.5f)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                };
                var rect = new Rectangle(1, 1, zona.Width - 3, zona.Height - 3);
                // Esquinas redondeadas
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                int r = 14;
                path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            };

            // Aplicar borde redondeado (para el fondo)
            Paleta.AplicarBordeRedondeadoSuave(zona, 14);

            // Icono PDF circular
            var iconoPdf = new Panel
            {
                Size = new Size(70, 70),
                Location = new Point((390 - 70) / 2, 25),
                BackColor = ColorTranslator.FromHtml("#F0EDF5")
            };
            var pathIcono = new System.Drawing.Drawing2D.GraphicsPath();
            pathIcono.AddEllipse(0, 0, 70, 70);
            iconoPdf.Region = new Region(pathIcono);

            var lblPdfIcono = new Label
            {
                Text = "📄",
                Font = new Font("Segoe UI", 28),
                ForeColor = Paleta.MoradoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconoPdf.Controls.Add(lblPdfIcono);
            zona.Controls.Add(iconoPdf);

            // Texto principal
            var lblTexto = new Label
            {
                Text = "Arrastra el PDF aquí o haz clic para seleccionarlo",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 115, 112),
                Location = new Point(20, 105),
                Size = new Size(350, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            zona.Controls.Add(lblTexto);

            // Botón "Seleccionar archivo"
            var btnSeleccionar = new Panel
            {
                Size = new Size(160, 36),
                Location = new Point((390 - 160) / 2, 135),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 36, 36, 90, 180);
            pathBtn.AddArc(btnSeleccionar.Width - 36, 0, 36, 36, 270, 180);
            pathBtn.CloseFigure();
            btnSeleccionar.Region = new Region(pathBtn);

            var lblBtnSel = new Label
            {
                Text = "Seleccionar archivo",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnSeleccionar.Controls.Add(lblBtnSel);

            // Hover del botón
            btnSeleccionar.MouseEnter += (s, e) => btnSeleccionar.BackColor = Paleta.MoradoOscuroHover;
            btnSeleccionar.MouseLeave += (s, e) => btnSeleccionar.BackColor = Paleta.MoradoOscuro;
            lblBtnSel.MouseEnter += (s, e) => btnSeleccionar.BackColor = Paleta.MoradoOscuroHover;
            lblBtnSel.MouseLeave += (s, e) => btnSeleccionar.BackColor = Paleta.MoradoOscuro;

            // Click del botón
            EventHandler clickSel = (s, e) => OnSeleccionarArchivoClick();
            btnSeleccionar.Click += clickSel;
            lblBtnSel.Click += clickSel;
            zona.Controls.Add(btnSeleccionar);

            // Info de formato
            var lblFormato = new Label
            {
                Text = "PDF · Máximo 10 MB",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(160, 155, 152),
                Location = new Point(20, 185),
                Size = new Size(350, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            zona.Controls.Add(lblFormato);

            // === EVENTOS DRAG-AND-DROP ===
            zona.DragEnter += ZonaDrop_DragEnter;
            zona.DragOver += ZonaDrop_DragOver;
            zona.DragDrop += ZonaDrop_DragDrop;

            // Click en cualquier parte de la zona (no solo el botón) → abrir diálogo
            EventHandler clickZona = (s, e) => OnSeleccionarArchivoClick();
            lblTexto.Click += clickZona;
            iconoPdf.Click += clickZona;
            lblPdfIcono.Click += clickZona;

            return zona;
        }

        // ===================================================
        // ZONA "ARCHIVO CARGADO" (cuando SÍ hay archivo)
        // ===================================================
        private Panel CrearZonaArchivo()
        {
            var zona = new Panel
            {
                Size = new Size(390, 230),
                BackColor = ColorTranslator.FromHtml("#F9F5FF")
            };
            Paleta.AplicarBordeRedondeadoSuave(zona, 14);

            // Icono de check verde
            var iconoCheck = new Panel
            {
                Size = new Size(70, 70),
                Location = new Point((390 - 70) / 2, 25),
                BackColor = ColorTranslator.FromHtml("#E8F5E9")
            };
            var pathCheck = new System.Drawing.Drawing2D.GraphicsPath();
            pathCheck.AddEllipse(0, 0, 70, 70);
            iconoCheck.Region = new Region(pathCheck);

            var lblCheck = new Label
            {
                Text = "✓",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#2E7D32"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconoCheck.Controls.Add(lblCheck);
            zona.Controls.Add(iconoCheck);

            // Nombre del archivo
            lblNombreArchivo = new Label
            {
                Text = "archivo.pdf",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 105),
                Size = new Size(350, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            zona.Controls.Add(lblNombreArchivo);

            // Tamaño + páginas
            lblTamanoArchivo = new Label
            {
                Text = "0 KB · 0 páginas",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 115, 112),
                Location = new Point(20, 128),
                Size = new Size(350, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            zona.Controls.Add(lblTamanoArchivo);

            // Botón "Quitar archivo"
            var btnQuitar = new Panel
            {
                Size = new Size(140, 32),
                Location = new Point((390 - 140) / 2, 165),
                BackColor = Color.FromArgb(180, 180, 180),
                Cursor = Cursors.Hand
            };
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 32, 32, 90, 180);
            pathBtn.AddArc(btnQuitar.Width - 32, 0, 32, 32, 270, 180);
            pathBtn.CloseFigure();
            btnQuitar.Region = new Region(pathBtn);

            var lblBtnQuitar = new Label
            {
                Text = "✕  Quitar archivo",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnQuitar.Controls.Add(lblBtnQuitar);

            btnQuitar.MouseEnter += (s, e) => btnQuitar.BackColor = Color.FromArgb(140, 140, 140);
            btnQuitar.MouseLeave += (s, e) => btnQuitar.BackColor = Color.FromArgb(180, 180, 180);
            lblBtnQuitar.MouseEnter += (s, e) => btnQuitar.BackColor = Color.FromArgb(140, 140, 140);
            lblBtnQuitar.MouseLeave += (s, e) => btnQuitar.BackColor = Color.FromArgb(180, 180, 180);

            EventHandler quitarClick = (s, e) => QuitarArchivo();
            btnQuitar.Click += quitarClick;
            lblBtnQuitar.Click += quitarClick;
            zona.Controls.Add(btnQuitar);

            // Propagar AllowDrop a todos los hijos para que el evento se dispare
            // independiente de qué control esté bajo el cursor
            foreach (Control hijo in zona.Controls)
            {
                hijo.AllowDrop = true;
                hijo.DragEnter += ZonaDrop_DragEnter;
                hijo.DragOver += ZonaDrop_DragOver;
                hijo.DragDrop += ZonaDrop_DragDrop;

                // Si el hijo tiene sub-hijos (como el botón Panel+Label), también
                foreach (Control sub in hijo.Controls)
                {
                    sub.AllowDrop = true;
                    sub.DragEnter += ZonaDrop_DragEnter;
                    sub.DragOver += ZonaDrop_DragOver;
                    sub.DragDrop += ZonaDrop_DragDrop;
                }
            }

            return zona;

        }

        // ===================================================
        // CENTRADO HORIZONTAL Y VERTICAL DE LAS DOS TARJETAS
        // ===================================================
        private void CentrarTarjetas()
        {
            if (panelTarjetas == null || tarjetaDescargar == null || tarjetaSubir == null) return;
            if (panelTarjetas.ClientSize.Width <= 0) return;

            int anchoTarjeta = tarjetaDescargar.Width;
            int gap = 30;
            int anchoTotal = (anchoTarjeta * 2) + gap;

            int xInicio = (panelTarjetas.ClientSize.Width - anchoTotal) / 2;
            int yCentrado = (panelTarjetas.ClientSize.Height - tarjetaDescargar.Height) / 2;
            if (yCentrado < 10) yCentrado = 10;  // protección por si el panel es muy chico

            tarjetaDescargar.Location = new Point(xInicio, yCentrado);
            tarjetaSubir.Location = new Point(xInicio + anchoTarjeta + gap, yCentrado);
        }


        private void OnEmpresaActivaCambio()
        {
            CargarEmpresaActiva();
        }
        // ===================================================
        // CARGAR DATOS DE EMPRESA ACTIVA
        // ===================================================
        private void CargarEmpresaActiva()
        {
            int? empresaId = Estado.EstadoApp.EmpresaActivaId;

            if (empresaId == null)
            {
                lblNombreEmpresa.Text = "Sin empresa seleccionada — ve a 'Empresas' para elegir una";
                var avatar = panelBannerEmpresa.Controls.OfType<Panel>()
                    .FirstOrDefault(p => p.Controls.OfType<Label>().Any());
                if (avatar != null)
                {
                    var lblInicial = avatar.Controls.OfType<Label>().FirstOrDefault();
                    if (lblInicial != null) lblInicial.Text = "?";
                }
                return;
            }

            var repo = new Datos.RepositorioEmpresa();
            var empresa = repo.ObtenerPorId(empresaId.Value);
            if (empresa == null)
            {
                lblNombreEmpresa.Text = "Empresa no encontrada";
                return;
            }

            lblNombreEmpresa.Text = $"{empresa.Nombre} · RIF: {empresa.Rif}";

            var avatarReal = panelBannerEmpresa.Controls.OfType<Panel>()
                .FirstOrDefault(p => p.Controls.OfType<Label>().Any());
            if (avatarReal != null)
            {
                var lblInicial = avatarReal.Controls.OfType<Label>().FirstOrDefault();
                if (lblInicial != null)
                    lblInicial.Text = empresa.Nombre.Length > 0
                        ? empresa.Nombre[0].ToString().ToUpper()
                        : "?";
            }
        }

        // ===================================================
        // DESCARGAR PLANTILLA WORD
        // ===================================================
        private void OnDescargarPlantillaClick()
        {
            // Sugerir un nombre por defecto con timestamp
            string nombreSugerido = $"plantilla_madurez_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

            using var saveDialog = new SaveFileDialog
            {
                Title = "Guardar plantilla de evaluación",
                Filter = "Documento Word (*.docx)|*.docx",
                FileName = nombreSugerido,
                DefaultExt = "docx",
                AddExtension = true
            };

            if (saveDialog.ShowDialog(this.FindForm()) != DialogResult.OK) return;

            try
            {
                // Llamar al generador
                var generador = new Logica.GeneradorPlantilla();
                string rutaGenerada = generador.GenerarPlantilla(saveDialog.FileName);

                // Confirmar al usuario
                var respuesta = MessageBox.Show(
                    $"Plantilla generada correctamente en:\n\n{rutaGenerada}\n\n¿Deseas abrir la carpeta?",
                    "Plantilla generada",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (respuesta == DialogResult.Yes)
                {
                    // Abrir el explorador en la carpeta y seleccionar el archivo
                    string? carpeta = Path.GetDirectoryName(rutaGenerada);
                    if (!string.IsNullOrEmpty(carpeta) && Directory.Exists(carpeta))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{rutaGenerada}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al generar la plantilla:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ===================================================
        // EVENTOS DRAG-AND-DROP
        // ===================================================
        private void ZonaDrop_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Validación rápida: que haya al menos un archivo .pdf
                var archivos = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                bool hayPdf = archivos.Any(a =>
                    string.Equals(Path.GetExtension(a), ".pdf", StringComparison.OrdinalIgnoreCase));

                if (hayPdf)
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void ZonaDrop_DragOver(object? sender, DragEventArgs e)
        {
            // Mantener el efecto del DragEnter
            ZonaDrop_DragEnter(sender, e);
        }

        private void ZonaDrop_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] archivos = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (archivos.Length == 0) return;

            CargarArchivo(archivos[0]);
        }

        // ===================================================
        // SELECCIÓN DE ARCHIVO VÍA DIÁLOGO
        // ===================================================
        private void OnSeleccionarArchivoClick()
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "Seleccionar plantilla completada (PDF)",
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                Multiselect = false
            };

            if (openDialog.ShowDialog(this.FindForm()) != DialogResult.OK) return;

            CargarArchivo(openDialog.FileName);
        }

        // ===================================================
        // LÓGICA COMÚN: VALIDAR Y CARGAR UN ARCHIVO
        // ===================================================
        private void CargarArchivo(string rutaArchivo)
        {
            // Validar extensión
            if (!rutaArchivo.ToLower().EndsWith(".pdf"))
            {
                MessageBox.Show("Solo se aceptan archivos PDF.",
                    "Formato no válido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar tamaño (máximo 10 MB)
            var info = new FileInfo(rutaArchivo);
            long maxBytes = 10 * 1024 * 1024;
            if (info.Length > maxBytes)
            {
                MessageBox.Show(
                    $"El archivo excede el tamaño máximo de 10 MB.\n\nTamaño actual: {info.Length / 1024.0 / 1024.0:F2} MB",
                    "Archivo demasiado grande",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar que sea PDF legible (usando el GestorInforme que ya existe)
            var gestor = new Logica.GestorInforme();
            if (!gestor.EsPdfValido(rutaArchivo))
            {
                MessageBox.Show("El archivo no parece ser un PDF válido o está corrupto.",
                    "PDF inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Guardar referencia
            _archivoSeleccionado = rutaArchivo;

            // Actualizar UI
            lblNombreArchivo.Text = Path.GetFileName(rutaArchivo);
            int paginas = gestor.ContarPaginas(rutaArchivo);
            double mb = info.Length / 1024.0 / 1024.0;
            lblTamanoArchivo.Text = $"{mb:F2} MB · {paginas} página{(paginas != 1 ? "s" : "")}";

            // Cambiar visual: ocultar zona drop, mostrar zona archivo
            zonaDrop.Visible = false;
            zonaArchivo.Visible = true;

            // Habilitar botón "Analizar con IA"
            HabilitarBotonAnalizar(true);
        }

        // ===================================================
        // QUITAR ARCHIVO
        // ===================================================
        private void QuitarArchivo()
        {
            _archivoSeleccionado = null;
            zonaDrop.Visible = true;
            zonaArchivo.Visible = false;
            HabilitarBotonAnalizar(false);
        }

        // ===================================================
        // HABILITAR / DESHABILITAR BOTÓN "ANALIZAR CON IA"
        // ===================================================
        private void HabilitarBotonAnalizar(bool habilitar)
        {
            if (habilitar)
            {
                btnAnalizar.BackColor = Paleta.MoradoOscuro;
                btnAnalizar.Cursor = Cursors.Hand;
                lblBtnAnalizar.Cursor = Cursors.Hand;
            }
            else
            {
                btnAnalizar.BackColor = Color.FromArgb(200, 200, 200);
                btnAnalizar.Cursor = Cursors.Default;
                lblBtnAnalizar.Cursor = Cursors.Default;
            }
        }

    }
}