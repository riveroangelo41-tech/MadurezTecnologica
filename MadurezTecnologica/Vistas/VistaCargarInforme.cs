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
        private Estilos.IndicadorModoConexion _indicadorConexion = null!;
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

        // Orquestador del análisis
        private Logica.OrquestadorAnalisis _orquestador = null!;

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

            _orquestador = new Logica.OrquestadorAnalisis();
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
                Font = new Font("Segoe UI Emoji", 18, FontStyle.Bold),
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

            _indicadorConexion = new Estilos.IndicadorModoConexion
            {
                Size = new Size(175, 36)
            };
            panelHeader.Controls.Add(_indicadorConexion);

            panelHeader.Resize += (s, e) =>
            {
                if (_indicadorConexion != null)
                    _indicadorConexion.Location = new Point(
                        panelHeader.Width - _indicadorConexion.Width - 20, 25);
            };
            if (_indicadorConexion != null)
                _indicadorConexion.Location = new Point(
                    panelHeader.Width - _indicadorConexion.Width - 20, 25);
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
                Height = 80,
                BackColor = Paleta.LilaInput,
                Padding = new Padding(20, 15, 20, 15),
                Margin = new Padding(0, 0, 0, 16)
            };
            panelBannerEmpresa.Resize += (s, e) =>
                Paleta.AplicarBordeRedondeadoSuave(panelBannerEmpresa, 14);
            panelContenido.Controls.Add(panelBannerEmpresa);

            // Barra de acento lateral como Panel hijo (no Paint, para que NO se recorte con la región redondeada)
            var barraAcento = new Panel
            {
                Size = new Size(5, 50),
                Location = new Point(8, 15),
                BackColor = Paleta.MoradoOscuro
            };
            barraAcento.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(barraAcento, 2);
            panelBannerEmpresa.Controls.Add(barraAcento);

            // Avatar circular con inicial de la empresa
            var avatar = new Panel
            {
                Size = new Size(46, 46),
                Location = new Point(22, 16),
                BackColor = Paleta.MoradoOscuro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAv);

            var lblInicial = new Label
            {
                Name = "lblInicialEmpresa",
                Text = "?",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
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
                Location = new Point(82, 14),
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            panelBannerEmpresa.Controls.Add(lblLabel);

            // Nombre de la empresa
            lblNombreEmpresa = new Label
            {
                Text = "Sin empresa seleccionada — ve a 'Empresas' para elegir una",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(82, 32),
                Size = new Size(700, 22),
                BackColor = Color.Transparent
            };
            panelBannerEmpresa.Controls.Add(lblNombreEmpresa);

            // Subtítulo con detalles (RIF, sector) — se muestra solo cuando hay empresa
            var lblDetalles = new Label
            {
                Name = "lblDetallesEmpresa",
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(125, 120, 117),
                Location = new Point(82, 53),
                Size = new Size(700, 18),
                BackColor = Color.Transparent
            };
            panelBannerEmpresa.Controls.Add(lblDetalles);
        }
        // ===================================================
        // BOTÓN "ANALIZAR CON IA" (abajo, centrado)
        // ===================================================
        private Label _lblHintAnalizar = null!;

        private void CrearBotonAnalizar()
        {
            var panelBotonContenedor = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(0, 6, 0, 10)
            };
            panelContenido.Controls.Add(panelBotonContenedor);

            btnAnalizar = new Panel
            {
                BackColor = Color.FromArgb(195, 190, 200),
                Size = new Size(260, 48),
                Cursor = Cursors.Default
            };
            btnAnalizar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnAnalizar, 24);

            lblBtnAnalizar = new Label
            {
                Text = "Analizar con IA",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Padding = new Padding(24, 0, 0, 0)  // deja espacio para el icono a la izquierda
            };
            btnAnalizar.Controls.Add(lblBtnAnalizar);

            // Icono a la izquierda del texto (nube subiendo → "enviar al análisis IA")
            var imgAnalizar = CargadorIconos.ObtenerRedimensionado(CargadorIconos.Analizar, 22, 22);
            if (imgAnalizar != null)
            {
                var picAnalizar = new PictureBox
                {
                    Image = imgAnalizar,
                    Size = new Size(22, 22),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                btnAnalizar.Controls.Add(picAnalizar);
                picAnalizar.BringToFront();
                // Centrado vertical + colocado a la izquierda del texto centrado
                void RecolocarPic() =>
                    picAnalizar.Location = new Point(28, (btnAnalizar.Height - picAnalizar.Height) / 2);
                btnAnalizar.Resize += (s, e) => RecolocarPic();
                RecolocarPic();
                // El clic sobre el icono también dispara el análisis
                picAnalizar.Click += async (s, e) => await OnAnalizarClick();
                picAnalizar.MouseEnter += (s, e) => btnAnalizar.BackColor = _btnAnalizarHabilitado ? Paleta.MoradoOscuroHover : btnAnalizar.BackColor;
                picAnalizar.MouseLeave += (s, e) => btnAnalizar.BackColor = _btnAnalizarHabilitado ? Paleta.MoradoOscuro : btnAnalizar.BackColor;
            }

            panelBotonContenedor.Controls.Add(btnAnalizar);

            // Hint debajo del botón — con margin extra para que respire
            _lblHintAnalizar = new Label
            {
                Text = "Selecciona una empresa y sube un PDF para activar el análisis",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 145, 142),
                AutoSize = false,
                Height = 20,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 0)
            };
            panelBotonContenedor.Controls.Add(_lblHintAnalizar);

            // Centrar horizontalmente al redimensionar
            panelBotonContenedor.Resize += (s, e) =>
            {
                btnAnalizar.Location = new Point(
                    (panelBotonContenedor.Width - btnAnalizar.Width) / 2,
                    8);
            };
            // Centrado inicial
            panelBotonContenedor.HandleCreated += (s, e) =>
            {
                panelBotonContenedor.BeginInvoke(new Action(() =>
                {
                    btnAnalizar.Location = new Point(
                        (panelBotonContenedor.Width - btnAnalizar.Width) / 2,
                        8);
                }));
            };

            // Click → analizar con IA
            EventHandler analizarClick = async (s, e) => await OnAnalizarClick();
            btnAnalizar.Click += analizarClick;
            lblBtnAnalizar.Click += analizarClick;

            // Hover: solo se ilumina cuando el botón está habilitado.
            void AplicarHoverAnalizar()
            {
                if (_btnAnalizarHabilitado) btnAnalizar.BackColor = Paleta.MoradoOscuroHover;
            }
            void QuitarHoverAnalizar()
            {
                if (_btnAnalizarHabilitado) btnAnalizar.BackColor = Paleta.MoradoOscuro;
            }
            btnAnalizar.MouseEnter += (s, e) => AplicarHoverAnalizar();
            btnAnalizar.MouseLeave += (s, e) => QuitarHoverAnalizar();
            lblBtnAnalizar.MouseEnter += (s, e) => AplicarHoverAnalizar();
            lblBtnAnalizar.MouseLeave += (s, e) => QuitarHoverAnalizar();
        }

        // Actualiza el hint según qué falta para habilitar el análisis
        private void ActualizarHintAnalizar()
        {
            if (_lblHintAnalizar == null) return;

            bool hayEmpresa = Estado.EstadoApp.EmpresaActivaId != null;
            bool hayArchivo = _archivoSeleccionado != null;

            if (hayEmpresa && hayArchivo)
            {
                _lblHintAnalizar.Text = "✓  Todo listo — haz clic para analizar";
                _lblHintAnalizar.ForeColor = ColorTranslator.FromHtml("#4A8F6F");
            }
            else if (!hayEmpresa && !hayArchivo)
            {
                _lblHintAnalizar.Text = "Selecciona una empresa y sube un PDF para activar el análisis";
                _lblHintAnalizar.ForeColor = Color.FromArgb(150, 145, 142);
            }
            else if (!hayEmpresa)
            {
                _lblHintAnalizar.Text = "Falta seleccionar una empresa";
                _lblHintAnalizar.ForeColor = ColorTranslator.FromHtml("#D4841C");
            }
            else
            {
                _lblHintAnalizar.Text = "Falta cargar un PDF";
                _lblHintAnalizar.ForeColor = ColorTranslator.FromHtml("#D4841C");
            }
        }
        // ===================================================
        // TARJETAS (esqueleto - sin contenido detallado todavía)
        // ===================================================
        private Panel CrearTarjetaDescarga()
        {
            var tarjeta = new Panel
            {
                Size = new Size(420, 410),
                BackColor = ColorTranslator.FromHtml("#F9F5FF"),
                Padding = new Padding(30, 30, 30, 30)
            };
            tarjeta.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);

            // Sombra inferior sutil para dar elevación
            tarjeta.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(Color.FromArgb(15, Paleta.MoradoOscuro));
                e.Graphics.FillRectangle(brush, 4, tarjeta.Height - 3, tarjeta.Width - 8, 3);
            };

            // Número del paso (badge "1") — DENTRO de la tarjeta para que no se recorte
            var numero = new Panel
            {
                Size = new Size(34, 34),
                Location = new Point(18, 18),
                BackColor = Paleta.MoradoOscuro
            };
            var pathNum = new System.Drawing.Drawing2D.GraphicsPath();
            pathNum.AddEllipse(0, 0, 34, 34);
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
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(62, 22),
                Size = new Size(320, 30),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitulo);

            // Descripción
            var lblDesc = new Label
            {
                Text = "Genera la plantilla Word y entrégala a la empresa para que la complete con su información.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(115, 110, 108),
                Location = new Point(24, 62),
                Size = new Size(370, 40),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblDesc);

            // === ICONO WORD CON HALO CIRCULAR ===
            // Mismo estilo visual que el icono PDF de la zona de abajo: un halo
            // circular morado claro como marco, con el icono real centrado dentro.
            // Así ambas tarjetas (plantilla Word y PDF cargado) tienen consistencia visual.
            var iconoWordHalo = new Panel
            {
                Size = new Size(90, 90),
                Location = new Point((420 - 90) / 2, 135),
                BackColor = Color.Transparent
            };
            iconoWordHalo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var halo = new SolidBrush(Color.FromArgb(40, Paleta.MoradoClaro));
                g.FillEllipse(halo, 0, 0, 90, 90);
            };
            tarjeta.Controls.Add(iconoWordHalo);

            // Icono real de Word (PNG embebido) centrado dentro del halo.
            // Si el recurso falla, cae al panel azul con la "W" tradicional.
            var imgWord = CargadorIconos.ObtenerRedimensionado(CargadorIconos.Word, 52, 52);
            if (imgWord != null)
            {
                var picWord = new PictureBox
                {
                    Image = imgWord,
                    Size = new Size(52, 52),
                    Location = new Point((90 - 52) / 2, (90 - 52) / 2),  // centrado en el halo 90×90
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                iconoWordHalo.Controls.Add(picWord);
            }
            else
            {
                var iconoWord = new Panel
                {
                    Size = new Size(72, 72),
                    Location = new Point(9, 9),
                    BackColor = ColorTranslator.FromHtml("#2B579A")
                };
                var pathIcono = new System.Drawing.Drawing2D.GraphicsPath();
                pathIcono.AddEllipse(0, 0, 72, 72);
                iconoWord.Region = new Region(pathIcono);

                var lblWordLetra = new Label
                {
                    Text = "W",
                    Font = new Font("Segoe UI", 32, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                iconoWord.Controls.Add(lblWordLetra);
                iconoWordHalo.Controls.Add(iconoWord);
            }

            // === INFO BAJO EL ICONO ===
            var lblInfo = new Label
            {
                Text = "📋  11 secciones · procesos, infraestructura, calidad y seguridad",
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = Color.FromArgb(95, 90, 88),
                Location = new Point(20, 255),
                Size = new Size(380, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblInfo);

            // === BOTÓN "DESCARGAR PLANTILLA WORD" ===
            var btnDescargar = new Panel
            {
                Size = new Size(240, 44),
                Location = new Point((420 - 240) / 2, 295),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            btnDescargar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnDescargar, 22);

            var lblBtnDescargar = new Label
            {
                Text = "📥   Descargar plantilla Word",
                Font = new Font("Segoe UI Emoji", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnDescargar.Controls.Add(lblBtnDescargar);

            // Hover + Press
            Color colorNormal = Paleta.MoradoOscuro;
            Color colorHover = Paleta.MoradoOscuroHover;
            Color colorPress = Color.FromArgb(60, 40, 90);
            btnDescargar.MouseEnter += (s, e) => btnDescargar.BackColor = colorHover;
            btnDescargar.MouseLeave += (s, e) => btnDescargar.BackColor = colorNormal;
            lblBtnDescargar.MouseEnter += (s, e) => btnDescargar.BackColor = colorHover;
            lblBtnDescargar.MouseLeave += (s, e) => btnDescargar.BackColor = colorNormal;
            btnDescargar.MouseDown += (s, e) => btnDescargar.BackColor = colorPress;
            btnDescargar.MouseUp += (s, e) => btnDescargar.BackColor = colorHover;
            lblBtnDescargar.MouseDown += (s, e) => btnDescargar.BackColor = colorPress;
            lblBtnDescargar.MouseUp += (s, e) => btnDescargar.BackColor = colorHover;

            EventHandler descargarClick = (s, e) => OnDescargarPlantillaClick();
            btnDescargar.Click += descargarClick;
            lblBtnDescargar.Click += descargarClick;

            tarjeta.Controls.Add(btnDescargar);

            // Formato info
            var lblFormato = new Label
            {
                Text = "Formato: .docx  ·  ~45 KB",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(155, 150, 148),
                Location = new Point(40, 358),
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
                Size = new Size(420, 410),
                BackColor = Color.White,
                Padding = new Padding(30, 30, 30, 30)
            };
            tarjeta.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);

            tarjeta.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(Color.FromArgb(15, Paleta.MoradoOscuro));
                e.Graphics.FillRectangle(brush, 4, tarjeta.Height - 3, tarjeta.Width - 8, 3);
            };

            // Número "2"
            var numero = new Panel
            {
                Size = new Size(34, 34),
                Location = new Point(18, 18),
                BackColor = Paleta.MoradoOscuro
            };
            var pathNum = new System.Drawing.Drawing2D.GraphicsPath();
            pathNum.AddEllipse(0, 0, 34, 34);
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
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(62, 22),
                Size = new Size(320, 30),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitulo);

            // Descripción
            var lblDesc = new Label
            {
                Text = "Una vez llenada por la empresa, conviértela a PDF y súbela aquí para el análisis.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(115, 110, 108),
                Location = new Point(24, 62),
                Size = new Size(370, 40),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblDesc);

            // === ZONA DROP ===
            zonaDrop = CrearZonaDrop();
            zonaDrop.Location = new Point(15, 115);
            tarjeta.Controls.Add(zonaDrop);

            // === ZONA "ARCHIVO CARGADO" ===
            zonaArchivo = CrearZonaArchivo();
            zonaArchivo.Location = new Point(15, 115);
            zonaArchivo.Visible = false;
            tarjeta.Controls.Add(zonaArchivo);

            return tarjeta;
        }

        // ===================================================
        // ZONA DROP (cuando NO hay archivo cargado)
        // ===================================================
        private Panel CrearZonaDrop()
        {
            Color colorBordeNormal = Paleta.MoradoClaro;
            Color colorFondoNormal = ColorTranslator.FromHtml("#FAF7FF");
            Color colorBordeActivo = Paleta.MoradoOscuro;
            Color colorFondoActivo = ColorTranslator.FromHtml("#EFE6FF");

            Color bordeActual = colorBordeNormal;

            var zona = new Panel
            {
                Size = new Size(390, 260),
                BackColor = colorFondoNormal,
                AllowDrop = true,
                Cursor = Cursors.Hand
            };

            zona.Paint += (s, e) =>
            {
                using var pen = new Pen(bordeActual, 2.5f)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash,
                    DashPattern = new float[] { 6, 4 }
                };
                var rect = new Rectangle(2, 2, zona.Width - 5, zona.Height - 5);
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

            zona.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(zona, 14);
            Paleta.AplicarBordeRedondeadoSuave(zona, 14);

            // Drag visual feedback — cambiar color del borde al hacer drag over
            zona.DragEnter += (s, e) =>
            {
                bordeActual = colorBordeActivo;
                zona.BackColor = colorFondoActivo;
                zona.Invalidate();
            };
            zona.DragLeave += (s, e) =>
            {
                bordeActual = colorBordeNormal;
                zona.BackColor = colorFondoNormal;
                zona.Invalidate();
            };
            zona.DragDrop += (s, e) =>
            {
                bordeActual = colorBordeNormal;
                zona.BackColor = colorFondoNormal;
                zona.Invalidate();
            };

            // Hover sutil
            zona.MouseEnter += (s, e) =>
            {
                if (bordeActual != colorBordeActivo)
                {
                    zona.BackColor = Color.FromArgb(247, 240, 255);
                    zona.Invalidate();
                }
            };
            zona.MouseLeave += (s, e) =>
            {
                if (bordeActual != colorBordeActivo)
                {
                    zona.BackColor = colorFondoNormal;
                    zona.Invalidate();
                }
            };

            // Icono PDF circular más prominente con halo translúcido
            var iconoPdfHalo = new Panel
            {
                Size = new Size(90, 90),
                Location = new Point((390 - 90) / 2, 30),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            iconoPdfHalo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var halo = new SolidBrush(Color.FromArgb(40, Paleta.MoradoClaro));
                g.FillEllipse(halo, 0, 0, 90, 90);
            };
            zona.Controls.Add(iconoPdfHalo);

            // Icono PDF real (PNG embebido). Se muestra centrado dentro del halo
            // morado circular para conservar el marco de color del sistema. Si el
            // recurso no se puede cargar, cae al dibujo custom de la hoja como antes.
            Control iconoPdfInterno;
            var imgPdfGrande = CargadorIconos.ObtenerRedimensionado(CargadorIconos.Pdf, 52, 52);
            if (imgPdfGrande != null)
            {
                var picPdfGrande = new PictureBox
                {
                    Image = imgPdfGrande,
                    Size = new Size(52, 52),
                    Location = new Point((90 - 52) / 2, (90 - 52) / 2),  // centrado dentro del halo 90×90
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                iconoPdfHalo.Controls.Add(picPdfGrande);
                iconoPdfInterno = picPdfGrande;
            }
            else
            {
                // Fallback: el diseño original con hoja dibujada
                var iconoPdf = new Panel
                {
                    Size = new Size(72, 72),
                    Location = new Point(9, 9),
                    BackColor = Paleta.MoradoClaro,
                    Cursor = Cursors.Hand
                };
                var pathIcono = new System.Drawing.Drawing2D.GraphicsPath();
                pathIcono.AddEllipse(0, 0, 72, 72);
                iconoPdf.Region = new Region(pathIcono);
                iconoPdf.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int cx = 36, cy = 36;
                    int w = 26, h = 32;
                    int x = cx - w / 2;
                    int y = cy - h / 2;
                    using var brushHoja = new SolidBrush(Color.White);
                    var hoja = new System.Drawing.Drawing2D.GraphicsPath();
                    hoja.AddLine(x, y, x + w - 8, y);
                    hoja.AddLine(x + w - 8, y, x + w, y + 8);
                    hoja.AddLine(x + w, y + 8, x + w, y + h);
                    hoja.AddLine(x + w, y + h, x, y + h);
                    hoja.CloseFigure();
                    g.FillPath(brushHoja, hoja);
                    using var brushDoble = new SolidBrush(Color.FromArgb(170, Paleta.MoradoClaro));
                    var doble = new System.Drawing.Drawing2D.GraphicsPath();
                    doble.AddLine(x + w - 8, y, x + w - 8, y + 8);
                    doble.AddLine(x + w - 8, y + 8, x + w, y + 8);
                    doble.CloseFigure();
                    g.FillPath(brushDoble, doble);
                    using var penLineas = new Pen(Paleta.MoradoClaro, 1.5f);
                    int xL = x + 4;
                    int wL = w - 8;
                    g.DrawLine(penLineas, xL, y + 14, xL + wL, y + 14);
                    g.DrawLine(penLineas, xL, y + 19, xL + wL, y + 19);
                    g.DrawLine(penLineas, xL, y + 24, xL + wL - 6, y + 24);
                };
                iconoPdfHalo.Controls.Add(iconoPdf);
                iconoPdfInterno = iconoPdf;
            }

            // Texto principal
            var lblTexto = new Label
            {
                Text = "Arrastra el PDF aquí",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 130),
                Size = new Size(350, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            zona.Controls.Add(lblTexto);

            // Texto secundario
            var lblTexto2 = new Label
            {
                Text = "o haz clic para seleccionarlo",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(130, 125, 122),
                Location = new Point(20, 152),
                Size = new Size(350, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            zona.Controls.Add(lblTexto2);

            // Botón "Seleccionar archivo"
            var btnSeleccionar = new Panel
            {
                Size = new Size(180, 40),
                Location = new Point((390 - 180) / 2, 185),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            btnSeleccionar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnSeleccionar, 20);

            var lblBtnSel = new Label
            {
                Text = "📂  Seleccionar archivo",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnSeleccionar.Controls.Add(lblBtnSel);

            // Hover + press del botón
            Color btnNormal = Paleta.MoradoOscuro;
            Color btnHover = Paleta.MoradoOscuroHover;
            Color btnPress = Color.FromArgb(60, 40, 90);
            btnSeleccionar.MouseEnter += (s, e) => btnSeleccionar.BackColor = btnHover;
            btnSeleccionar.MouseLeave += (s, e) => btnSeleccionar.BackColor = btnNormal;
            lblBtnSel.MouseEnter += (s, e) => btnSeleccionar.BackColor = btnHover;
            lblBtnSel.MouseLeave += (s, e) => btnSeleccionar.BackColor = btnNormal;
            btnSeleccionar.MouseDown += (s, e) => btnSeleccionar.BackColor = btnPress;
            btnSeleccionar.MouseUp += (s, e) => btnSeleccionar.BackColor = btnHover;
            lblBtnSel.MouseDown += (s, e) => btnSeleccionar.BackColor = btnPress;
            lblBtnSel.MouseUp += (s, e) => btnSeleccionar.BackColor = btnHover;

            // Click del botón
            EventHandler clickSel = (s, e) => OnSeleccionarArchivoClick();
            btnSeleccionar.Click += clickSel;
            lblBtnSel.Click += clickSel;
            zona.Controls.Add(btnSeleccionar);

            // Info de formato
            var lblFormato = new Label
            {
                Text = "📄  PDF  ·  Máximo 10 MB",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 145, 142),
                Location = new Point(20, 232),
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
            lblTexto2.Click += clickZona;
            iconoPdfHalo.Click += clickZona;
            iconoPdfInterno.Click += clickZona;

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

            // Icono PDF pequeño a la izquierda del nombre del archivo
            var imgPdfChico = CargadorIconos.ObtenerRedimensionado(CargadorIconos.Pdf, 22, 22);
            if (imgPdfChico != null)
            {
                var picPdfChico = new PictureBox
                {
                    Image = imgPdfChico,
                    Size = new Size(22, 22),
                    Location = new Point(150, 105),   // se reajusta al mostrar el nombre real
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                zona.Controls.Add(picPdfChico);
                picPdfChico.BringToFront();
                // Reposiciona el icono a la izquierda del texto centrado del label
                void CentrarIconoPdf()
                {
                    if (string.IsNullOrEmpty(lblNombreArchivo.Text)) return;
                    var tam = TextRenderer.MeasureText(lblNombreArchivo.Text, lblNombreArchivo.Font);
                    int xTexto = lblNombreArchivo.Left + (lblNombreArchivo.Width - tam.Width) / 2;
                    int xIcono = xTexto - picPdfChico.Width - 8;
                    if (xIcono < 5) xIcono = 5;
                    picPdfChico.Location = new Point(xIcono, lblNombreArchivo.Top + 1);
                }
                lblNombreArchivo.TextChanged += (s, e) => CentrarIconoPdf();
                CentrarIconoPdf();
            }

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

            // Si había un archivo cargado, limpiarlo porque era de otra empresa
            if (_archivoSeleccionado != null)
            {
                QuitarArchivo();
            }
        }

        // ===================================================
        // CARGAR DATOS DE EMPRESA ACTIVA
        // ===================================================
        private void CargarEmpresaActiva()
        {
            int? empresaId = Estado.EstadoApp.EmpresaActivaId;

            var lblDetalles = panelBannerEmpresa.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Name == "lblDetallesEmpresa");

            if (empresaId == null)
            {
                lblNombreEmpresa.Text = "Sin empresa seleccionada";
                if (lblDetalles != null)
                    lblDetalles.Text = "Ve a 'Empresas' en el menú lateral para seleccionar una.";

                var avatar = panelBannerEmpresa.Controls.OfType<Panel>()
                    .FirstOrDefault(p => p.Controls.OfType<Label>().Any());
                if (avatar != null)
                {
                    avatar.BackColor = Color.FromArgb(180, 175, 175);
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
                if (lblDetalles != null) lblDetalles.Text = "";
                return;
            }

            lblNombreEmpresa.Text = empresa.Nombre;
            if (lblDetalles != null)
                lblDetalles.Text = $"RIF: {empresa.Rif}   ·   Sector: {empresa.Sector}   ·   Empleados: {empresa.CantidadEmpleados}";

            var avatarReal = panelBannerEmpresa.Controls.OfType<Panel>()
                .FirstOrDefault(p => p.Controls.OfType<Label>().Any());
            if (avatarReal != null)
            {
                avatarReal.BackColor = Paleta.MoradoOscuro;
                var lblInicial = avatarReal.Controls.OfType<Label>().FirstOrDefault();
                if (lblInicial != null)
                    lblInicial.Text = empresa.Nombre.Length > 0
                        ? empresa.Nombre[0].ToString().ToUpper()
                        : "?";
            }

            // Actualizar hint del botón ya que cambió la empresa
            ActualizarHintAnalizar();
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

                bool abrirCarpeta = Estilos.MensajeApp.Confirmar(
                    $"Plantilla generada correctamente en:\n\n{rutaGenerada}\n\n¿Deseas abrir la carpeta?",
                    "Plantilla generada",
                    this.FindForm());

                if (abrirCarpeta)
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
                Estilos.MensajeApp.Error(
                    $"Error al generar la plantilla:\n\n{ex.Message}",
                    "Error",
                    this.FindForm());
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
            // === VALIDACIÓN 1: empresa activa obligatoria ===
            if (Estado.EstadoApp.EmpresaActivaId == null)
            {
                Estilos.MensajeApp.Advertencia(
                    "Debes seleccionar una empresa antes de cargar un PDF.\n\n" +
                    "Ve a la sección 'Empresas' y haz clic en 'Seleccionar' en la empresa que quieras analizar.",
                    "Sin empresa seleccionada",
                    this.FindForm());
                return;
            }

            // === VALIDACIÓN 2: la empresa NO debe tener análisis previo ===
            int empresaId = Estado.EstadoApp.EmpresaActivaId.Value;
            var repoConv = new Datos.RepositorioConversacion();
            var convExistente = repoConv.ObtenerUltimaPorEmpresa(empresaId);

            if (convExistente != null)
            {
                var repoEmpresa = new Datos.RepositorioEmpresa();
                var emp = repoEmpresa.ObtenerPorId(empresaId);
                Estilos.MensajeApp.Info(
                    $"La empresa '{emp?.Nombre}' ya tiene un análisis previo del {convExistente.FechaInicio:dd/MM/yyyy}.\n\n" +
                    "Solo se permite UN análisis por empresa para no confundir a la IA con datos contradictorios.\n\n" +
                    "Si deseas re-analizar esta empresa, ve a 'Historial' en el menú lateral y elimina " +
                    "los diagnósticos existentes. Cuando borres todos los diagnósticos, la conversación " +
                    "se eliminará automáticamente y podrás cargar un nuevo informe.",
                    "Análisis previo detectado",
                    this.FindForm());
                return;
            }

            // === Validar extensión ===
            if (!rutaArchivo.ToLower().EndsWith(".pdf"))
            {
                Estilos.MensajeApp.Advertencia("Solo se aceptan archivos PDF.",
                    "Formato no válido", this.FindForm());
                return;
            }

            // Validar tamaño (máximo 10 MB)
            var info = new FileInfo(rutaArchivo);
            long maxBytes = 10 * 1024 * 1024;
            if (info.Length > maxBytes)
            {
                Estilos.MensajeApp.Advertencia(
                    $"El archivo excede el tamaño máximo de 10 MB.\n\nTamaño actual: {info.Length / 1024.0 / 1024.0:F2} MB",
                    "Archivo demasiado grande",
                    this.FindForm());
                return;
            }

            // Validar que sea PDF legible (usando el GestorInforme que ya existe)
            var gestor = new Logica.GestorInforme();
            if (!gestor.EsPdfValido(rutaArchivo))
            {
                Estilos.MensajeApp.Advertencia("El archivo no parece ser un PDF válido o está corrupto.",
                    "PDF inválido", this.FindForm());
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
        private bool _btnAnalizarHabilitado = false;

        private void HabilitarBotonAnalizar(bool habilitar)
        {
            _btnAnalizarHabilitado = habilitar;

            if (habilitar)
            {
                btnAnalizar.BackColor = Paleta.MoradoOscuro;
                btnAnalizar.Cursor = Cursors.Hand;
                lblBtnAnalizar.Cursor = Cursors.Hand;
            }
            else
            {
                btnAnalizar.BackColor = Color.FromArgb(195, 190, 200);
                btnAnalizar.Cursor = Cursors.Default;
                lblBtnAnalizar.Cursor = Cursors.Default;
            }

            ActualizarHintAnalizar();
        }

        // ===================================================
        // ANÁLISIS CON IA (botón "Analizar con IA")
        // ===================================================
        private async Task OnAnalizarClick()
        {
            // === Validaciones (red de seguridad) ===
            if (_archivoSeleccionado == null)
            {
                Estilos.MensajeApp.Info("Primero carga un archivo PDF.",
                    "Sin archivo", this.FindForm());
                return;
            }

            if (Estado.EstadoApp.EmpresaActivaId == null)
            {
                Estilos.MensajeApp.Advertencia("Debes seleccionar una empresa antes de analizar.",
                    "Sin empresa", this.FindForm());
                return;
            }

            // Obtener la empresa de la BD
            var repo = new Datos.RepositorioEmpresa();
            var empresa = repo.ObtenerPorId(Estado.EstadoApp.EmpresaActivaId.Value);
            if (empresa == null)
            {
                Estilos.MensajeApp.Error("La empresa seleccionada no se encontró en la base de datos.",
                    "Empresa no encontrada", this.FindForm());
                return;
            }

            // Offline efectivo: forzado por el usuario O sin conexión detectada.
            bool modoOffline = Inteligencia.DetectorConexion.EstaOffline();
            bool sinConexion = !Inteligencia.DetectorConexion.HayConexion;

            string mensajeConfirmacion;
            string tituloConfirmacion;
            if (modoOffline)
            {
                string encabezado = sinConexion
                    ? "⚠ SIN CONEXIÓN A INTERNET"
                    : "⚠ MODO OFFLINE FORZADO ACTIVO";
                string comoVolver = sinConexion
                    ? "El análisis con IA volverá a estar disponible cuando se restablezca la conexión."
                    : "Si quieres usar la IA, desactiva el modo offline desde el indicador del header.";

                mensajeConfirmacion =
                    $"{encabezado}\n\n" +
                    $"Se va a analizar el PDF para la empresa '{empresa.Nombre}' usando el " +
                    "MOTOR LOCAL (detección por palabras clave).\n\n" +
                    "Este motor es menos preciso que la IA. Funciona detectando palabras clave " +
                    "del informe y asignando un nivel CMMI aproximado.\n\n" +
                    $"{comoVolver}\n\n" +
                    "¿Deseas continuar con el análisis offline?";
                tituloConfirmacion = "Análisis con motor offline";
            }
            else
            {
                mensajeConfirmacion =
                    $"Se va a analizar el PDF para la empresa '{empresa.Nombre}'.\n\n" +
                    "Este proceso consultará a la IA y puede tomar entre 15 y 60 segundos.\n\n" +
                    "¿Deseas continuar?";
                tituloConfirmacion = "Confirmar análisis";
            }

            bool confirmado = Estilos.MensajeApp.Confirmar(
                mensajeConfirmacion,
                tituloConfirmacion,
                this.FindForm());

            if (!confirmado) return;

            // === Bloquear UI y mostrar estado "Analizando" ===
            lblBtnAnalizar.Text = "Analizando...";
            HabilitarBotonAnalizar(false);

            // Mostrar diálogo de carga bloqueante con botón cancelar
            Logica.ResultadoAnalisis? resultado = null;
            bool cancelado = false;
            bool vpnApagada = false;

            try
            {
                // Ejecutar el análisis DENTRO del diálogo con progreso y cancelación
                var dialogo = new Estilos.DialogoCargando(
                    "Analizando informe",
                    "Iniciando análisis del PDF...");

                dialogo.Shown += async (s, e) =>
                {
                    try
                    {
                        resultado = await _orquestador.AnalizarInformePdf(
                            _archivoSeleccionado!,
                            empresa,
                            dialogo.Token,
                            mensaje => dialogo.ActualizarMensaje(mensaje));
                    }
                    catch (OperationCanceledException)
                    {
                        cancelado = true;
                    }
                    catch (Inteligencia.VpnRequeridaException)
                    {
                        // 403 de la API = VPN apagada. Marcar y avisar tras cerrar el diálogo.
                        vpnApagada = true;
                    }
                    catch (Exception ex)
                    {
                        Estilos.MensajeApp.Error(
                            $"Error inesperado durante el análisis:\n\n{ex.Message}",
                            "Error",
                            this.FindForm());
                    }
                    finally
                    {
                        if (!dialogo.IsDisposed) dialogo.Close();
                    }
                };

                dialogo.ShowDialog(this.FindForm());
                dialogo.Dispose();

                // VPN apagada (403 de la API): mensaje claro pidiendo encender la VPN.
                if (vpnApagada)
                {
                    Estilos.MensajeApp.Advertencia(
                        "🔒 La VPN está apagada.\n\n" +
                        "El análisis con IA no está disponible en tu región sin la VPN. " +
                        "Enciéndela e intenta analizar el informe de nuevo.\n\n" +
                        "Si no puedes usar la VPN ahora, activa el modo offline (indicador del " +
                        "header) para analizar con el motor local.",
                        "Se requiere VPN",
                        this.FindForm());
                    return;
                }

                if (cancelado || (resultado != null && resultado.Mensaje.Contains("cancelado")))
                {
                    // Distinguir: ¿se canceló porque se cayó la red, o porque el usuario lo canceló?
                    if (!Inteligencia.DetectorConexion.HayConexion)
                    {
                        Estilos.MensajeApp.Advertencia(
                            "Se perdió la conexión a internet durante el análisis, por lo que se canceló.\n\n" +
                            "El sistema pasó a modo offline. Vuelve a intentar y el análisis se hará con el " +
                            "motor local, o espera a que regrese la conexión para usar la IA.",
                            "Conexión perdida",
                            this.FindForm());
                    }
                    else
                    {
                        Estilos.MensajeApp.Info(
                            "El análisis fue cancelado. No se guardó ningún resultado.",
                            "Análisis cancelado",
                            this.FindForm());
                    }
                    return;
                }

                if (resultado == null)
                {
                    return;   // no debería pasar pero por seguridad
                }

                if (!resultado.Exitoso)
                {
                    Estilos.MensajeApp.Advertencia(
                        $"El análisis no pudo completarse:\n\n{resultado.Mensaje}",
                        "Análisis no exitoso",
                        this.FindForm());
                    return;
                }

                // Análisis exitoso → se creó una conversación nueva. Fijar la empresa
                // analizada como activa dispara la recarga automática del Chat (y demás
                // vistas), de modo que la conversación quede lista para enviar mensajes.
                if (resultado.EmpresaId.HasValue)
                    Estado.EstadoApp.EstablecerEmpresaActiva(resultado.EmpresaId.Value);
                Estado.EstadoApp.NotificarHistorialCambio();

                // === Éxito: mostrar diagnóstico en modal ===
                var diagResultado = resultado.Diagnostico;
                if (diagResultado != null)
                {
                    bool fueOffline = resultado.ModoUsado != Inteligencia.ModoOperacion.Online;
                    string motorUsado = fueOffline
                        ? "🔌 Motor OFFLINE (detección por palabras clave)"
                        : "🤖 IA (Claude)";

                    string mensajeExito =
                        $"¡Análisis completado con éxito!\n\n" +
                        $"Motor utilizado: {motorUsado}\n" +
                        $"Nivel CMMI detectado: {diagResultado.NivelMadurez}\n" +
                        $"Caracteres procesados: {resultado.CaracteresProcesados:N0}\n" +
                        $"Validación: {resultado.MetodoValidacion}\n\n" +
                        "El diagnóstico se ha guardado en la base de datos.\n" +
                        "Haz clic en Aceptar para ver el reporte completo.";

                    if (fueOffline)
                        mensajeExito +=
                            "\n\n💡 Cuando tengas conexión, podrás repetir el análisis con la IA " +
                            "para obtener un resultado más preciso y detallado.";

                    string titulo = fueOffline
                        ? "Análisis offline completado"
                        : "Análisis exitoso";

                    Estilos.MensajeApp.Exito(mensajeExito, titulo, this.FindForm());
                    MostrarModalDiagnostico(diagResultado);
                }

                QuitarArchivo();
            }
            catch (OperationCanceledException)
            {
                cancelado = true;
                Estilos.MensajeApp.Info(
                    "El análisis fue cancelado. No se guardó ningún resultado.",
                    "Análisis cancelado",
                    this.FindForm());
            }
            catch (Exception ex)
            {
                Estilos.MensajeApp.Error(
                    $"Error inesperado durante el análisis:\n\n{ex.Message}",
                    "Error",
                    this.FindForm());
            }
            finally
            {
                // Restaurar UI
                lblBtnAnalizar.Text = "Analizar con IA";
                HabilitarBotonAnalizar(_archivoSeleccionado != null);
                this.Cursor = Cursors.Default;
            }
        }

        // ===================================================
        // MODAL DE DIAGNÓSTICO (mismo diseño que VistaHistorial y VistaChat)
        // ===================================================
        private void MostrarModalDiagnostico(Modelos.Diagnostico diag)
        {
            var form = new Form
            {
                Size = new Size(620, 580),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Paleta.GrisClaro,
                ShowInTaskbar = false
            };
            form.Load += (s, e) => Paleta.AplicarBordeRedondeadoSuave(form, 16);

            form.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 175, 195), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, form.Width - 1, form.Height - 1);
            };

            // === HEADER MORADO ===
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Paleta.MoradoOscuro
            };

            string tipo = diag.EsFinal ? "📋 Diagnóstico Final" : "📝 Diagnóstico Intermedio";
            var lblTipo = new Label
            {
                Text = tipo,
                Font = new Font("Segoe UI Emoji", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(20, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblTipo);

            string primerFrase = diag.ResumenEmpresa.Split('.').FirstOrDefault()?.Trim() ?? "";
            if (primerFrase.Length > 50) primerFrase = primerFrase[..50] + "…";
            var lblFechaHeader = new Label
            {
                Text = $"📅 {diag.FechaGeneracion:dd/MM/yyyy · HH:mm}   ·   {primerFrase}",
                Font = new Font("Segoe UI Emoji", 8),
                ForeColor = Color.FromArgb(195, 190, 220),
                Location = new Point(22, 46),
                Size = new Size(form.Width - 150, 18),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblFechaHeader);

            string nivelTexto = diag.NivelMadurez > 0 ? $"Nivel {diag.NivelMadurez}" : "Sin nivel";
            var lblNivel = new Label
            {
                Text = nivelTexto,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                BackColor = Color.White,
                Size = new Size(70, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(form.Width - 145, 14)
            };
            var pathBadge = new System.Drawing.Drawing2D.GraphicsPath();
            pathBadge.AddArc(0, 0, 26, 26, 90, 180);
            pathBadge.AddArc(70 - 26, 0, 26, 26, 270, 180);
            pathBadge.CloseFigure();
            lblNivel.Region = new Region(pathBadge);
            header.Controls.Add(lblNivel);

            var btnCerrar = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 195, 220),
                Size = new Size(35, 35),
                Location = new Point(form.Width - 50, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCerrar.Click += (s, e) => form.Close();
            btnCerrar.MouseEnter += (s, e) => btnCerrar.ForeColor = Color.White;
            btnCerrar.MouseLeave += (s, e) => btnCerrar.ForeColor = Color.FromArgb(200, 195, 220);
            header.Controls.Add(btnCerrar);

            // Drag para mover el form
            bool arrastrando = false;
            Point puntoInicio = Point.Empty;
            EventHandler<MouseEventArgs> down = (s, e) => { arrastrando = true; puntoInicio = e.Location; };
            EventHandler<MouseEventArgs> move = (s, e) =>
            {
                if (arrastrando)
                    form.Location = new Point(
                        form.Location.X + e.X - puntoInicio.X,
                        form.Location.Y + e.Y - puntoInicio.Y);
            };
            EventHandler<MouseEventArgs> up = (s, e) => arrastrando = false;

            header.MouseDown += (s, e) => down(s, e);
            header.MouseMove += (s, e) => move(s, e);
            header.MouseUp += (s, e) => up(s, e);
            lblTipo.MouseDown += (s, e) => down(s, e);
            lblTipo.MouseMove += (s, e) => move(s, e);
            lblTipo.MouseUp += (s, e) => up(s, e);

            // === CONTENIDO SCROLLABLE ===
            var contenido = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 12, 20, 15),
                BackColor = Paleta.GrisClaro
            };
            form.Controls.Add(contenido);
            form.Controls.Add(header);

            var colorResumen = Paleta.MoradoOscuro;
            var colorFortalezas = ColorTranslator.FromHtml("#4A8F6F");
            var colorDebilidades = ColorTranslator.FromHtml("#D4841C");
            var colorRiesgos = ColorTranslator.FromHtml("#C13F3F");
            var colorRecomendaciones = ColorTranslator.FromHtml("#4A7FB5");

            int y = 8;
            y = AgregarSeccionDetalle(contenido, "📄  RESUMEN", diag.ResumenEmpresa, y, colorResumen);
            y = AgregarSeccionDetalle(contenido, "✅  FORTALEZAS", diag.Fortalezas, y, colorFortalezas);
            y = AgregarSeccionDetalle(contenido, "⚠️  DEBILIDADES", diag.Debilidades, y, colorDebilidades);
            y = AgregarSeccionDetalle(contenido, "🔴  RIESGOS", diag.Riesgos, y, colorRiesgos);
            y = AgregarSeccionDetalle(contenido, "💡  RECOMENDACIONES", diag.Recomendaciones, y, colorRecomendaciones);

            form.ShowDialog(this.FindForm());
        }

        private int AgregarSeccionDetalle(Panel parent, string titulo, string contenido, int y, Color colorAccento)
        {
            if (string.IsNullOrWhiteSpace(contenido)) return y;

            var fondoCard = Color.FromArgb(
                252 + (int)((colorAccento.R - 252) * 0.03),
                250 + (int)((colorAccento.G - 250) * 0.03),
                255 + (int)((colorAccento.B - 255) * 0.03));

            var lblContenido = new Label
            {
                Text = contenido,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(60, 58, 55),
                Location = new Point(18, 30),
                MaximumSize = new Size(parent.ClientSize.Width - 90, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            int cardHeight = 30 + lblContenido.PreferredHeight + 15;

            var card = new Panel
            {
                Location = new Point(5, y),
                Size = new Size(parent.ClientSize.Width - 35, cardHeight),
                BackColor = fondoCard
            };
            card.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(card, 10);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(colorAccento);
                e.Graphics.FillRectangle(brush, 0, 10, 4, card.Height - 20);
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI Emoji", 7.5f, FontStyle.Bold),
                ForeColor = colorAccento,
                Location = new Point(18, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblContenido);

            parent.Controls.Add(card);
            return card.Bottom + 8;
        }

    }
}