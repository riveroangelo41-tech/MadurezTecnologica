using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Vistas
{
    public partial class VistaEmpresas : UserControl
    {
        // Paneles principales
        private Panel panelHeader = null!;
        private Estilos.IndicadorModoConexion _indicadorConexion = null!;
        private Panel panelContenido = null!;

        // Header
        private Label lblTitulo = null!;
        private Label lblSubtitulo = null!;
        private Button btnNuevaEmpresa = null!;



        // Repositorio
        private MadurezTecnologica.Datos.RepositorioEmpresa _repoEmpresa = null!;

        // Tipos de filtro disponibles 
        private enum TipoFiltro
        {
            Todas,
            Activa,
            SinEvaluar
        }

        // === Estado del filtrado ===
        private List<Modelos.Empresa> _empresasCache = new();
        private TipoFiltro _filtroActivo = TipoFiltro.Todas;
        private string _textoBusqueda = "";

        // === Controles de la barra de filtros ===
        private TextBox txtBuscar = null!;
        private Panel tabTodas = null!;
        private Panel tabActiva = null!;
        private Panel tabSinEvaluar = null!;
        private Label lblTabTodas = null!;
        private Label lblTabActiva = null!;
        private Label lblTabSinEvaluar = null!;

        // === Repositorios extra para detectar "sin evaluar" ===
        private MadurezTecnologica.Datos.RepositorioConversacion _repoConv = null!;
        private MadurezTecnologica.Datos.RepositorioDiagnostico _repoDiag = null!;


        public VistaEmpresas()
        {
            InitializeComponent();

            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();

            _repoEmpresa = new MadurezTecnologica.Datos.RepositorioEmpresa();

            _repoConv = new MadurezTecnologica.Datos.RepositorioConversacion();
            _repoDiag = new MadurezTecnologica.Datos.RepositorioDiagnostico();

            ConfigurarControl();
            CrearPanelContenido();
            CrearHeader();

            // Cargar empresas cuando el control esté listo (con BeginInvoke para esperar el tamaño)
            this.Load += (s, e) =>
            {
                // Suscribirse al evento de cambio de empresa
                Estado.EstadoApp.EmpresaActivaCambio += OnEmpresaActivaCambio;

                this.BeginInvoke(new Action(() => CargarEmpresas()));
            };

            // Desuscribirse al destruir el control (evitar memory leaks)
            this.HandleDestroyed += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio -= OnEmpresaActivaCambio;
            };
        }


        // CONFIGURACIÓN GENERAL


        private void ConfigurarControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.GrisClaro;
        }

       
        // HEADER (arriba)
        

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

            panelHeader.Resize += (s, e) => ReposicionarBotonHeader();

            // Avatar (círculo morado claro con icono de edificio)
            var picAvatar = new Panel
            {
                Size = new Size(50, 50),
                Location = new Point(10, 15),
                BackColor = Paleta.MoradoClaro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, picAvatar.Width, picAvatar.Height);
            picAvatar.Region = new Region(pathAv);

            // Letra "E" como placeholder hasta tener un icono real
            var lblIconoAvatar = new Label
            {
                Text = "E",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            picAvatar.Controls.Add(lblIconoAvatar);
            panelHeader.Controls.Add(picAvatar);

            // Título
            lblTitulo = new Label
            {
                Text = "Empresas registradas",
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
                Text = "Selecciona una empresa para iniciar o continuar su evaluación de madurez tecnológica",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 48),
                Size = new Size(560, 20),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            panelHeader.Controls.Add(lblSubtitulo);

            // Botón "+ Nueva empresa" (a la derecha)
            btnNuevaEmpresa = new Button
            {
                Text = "+ Nueva empresa",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 32),
                Cursor = Cursors.Hand
            };
            btnNuevaEmpresa.FlatAppearance.BorderSize = 0;
            btnNuevaEmpresa.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;

            // Esquinas redondeadas al botón
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 30, 30, 90, 180);
            pathBtn.AddArc(btnNuevaEmpresa.Width - 30, 0, 30, 30, 270, 180);
            pathBtn.CloseFigure();
            btnNuevaEmpresa.Region = new Region(pathBtn);

            btnNuevaEmpresa.Click += BtnNuevaEmpresa_Click;
            panelHeader.Controls.Add(btnNuevaEmpresa);

            _indicadorConexion = new Estilos.IndicadorModoConexion
            {
                Size = new Size(175, 36)
            };
            panelHeader.Controls.Add(_indicadorConexion);
            _indicadorConexion.BringToFront();

            ReposicionarBotonHeader();
        }

        private void ReposicionarBotonHeader()
        {
            if (panelHeader == null || btnNuevaEmpresa == null) return;

            btnNuevaEmpresa.Location = new Point(
                panelHeader.Width - btnNuevaEmpresa.Width - 20, 25);

            if (_indicadorConexion != null)
                _indicadorConexion.Location = new Point(
                    btnNuevaEmpresa.Left - _indicadorConexion.Width - 15, 25);
        }


        // PANEL CENTRAL CONTENEDOR (blanco, redondeado)


        private FlowLayoutPanel flowEmpresas = null!;
        private Label lblContador = null!;

        private void CrearPanelContenido()
        {
            panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(25, 20, 25, 20)
            };
            Controls.Add(panelContenido);
            panelContenido.Resize += (s, e) => AplicarBordeRedondeado(panelContenido, 25);

            // FlowLayoutPanel para las tarjetas (grid responsive)
            flowEmpresas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.White,
                Padding = new Padding(5)
            };
            panelContenido.Controls.Add(flowEmpresas);

            // Barra de filtros arriba del grid
            var barraFiltros = CrearBarraFiltros();
            panelContenido.Controls.Add(barraFiltros);

            flowEmpresas.BringToFront();

            // Double buffering
            typeof(FlowLayoutPanel).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, flowEmpresas, new object[] { true });
        }

        // ===================================================
        // EVENTOS
        // ===================================================

        private void BtnNuevaEmpresa_Click(object? sender, EventArgs e)
        {
            using var modal = new Presentacion.FormNuevaEmpresa();
            var resultado = modal.ShowDialog(this.FindForm());

            if (resultado == DialogResult.OK && modal.EmpresaGuardada != null)
            {
                // Recargar el grid para mostrar la nueva empresa
                CargarEmpresas();

                Estilos.MensajeApp.Exito(
                    $"Empresa '{modal.EmpresaGuardada.Nombre}' registrada exitosamente.",
                    "Empresa registrada",
                    this.FindForm());
            }
        }

        // ===================================================
        // UTILIDADES
        // ===================================================

        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            Paleta.AplicarBordeRedondeadoSuave(panel, radio);
        }

        private void CargarEmpresas()
        {
            try
            {
                _empresasCache = _repoEmpresa.ObtenerTodas();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                Estilos.MensajeApp.Error($"Error al cargar empresas: {ex.Message}",
                    "Error", this.FindForm());
            }
        }

        private void AgregarTarjetaEmpresa(Modelos.Empresa empresa)
        {
            bool esActiva = Estado.EstadoApp.EmpresaActivaId == empresa.Id;
            Color colorFondo = esActiva ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;
            Color colorFondoHover = esActiva ? Paleta.MoradoOscuroHover : Paleta.VerdeGrisaceoOscuro;

            // === Tarjeta principal ===
            var tarjeta = new Panel
            {
                Size = new Size(300, 270),
                BackColor = colorFondo,
                Margin = new Padding(0, 0, 18, 18),
                Cursor = Cursors.Hand,
                Padding = new Padding(0),
                Tag = empresa.Id
            };
            tarjeta.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);

            // Sombra inferior sutil
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brushSombra = new SolidBrush(Color.FromArgb(35, Color.Black));
                g.FillRectangle(brushSombra, 4, tarjeta.Height - 3, tarjeta.Width - 8, 3);
            };

            // === HEADER: avatar circular + nombre + RIF ===
            int headerHeight = 78;
            string inicial = empresa.Nombre.Length > 0 ? empresa.Nombre[0].ToString().ToUpper() : "?";

            // Avatar circular
            var avatar = new Panel
            {
                Size = new Size(48, 48),
                Location = new Point(20, 18),
                BackColor = Color.FromArgb(60, 255, 255, 255)
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, 48, 48);
            avatar.Region = new Region(pathAv);

            var lblInicial = new Label
            {
                Text = inicial,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);
            tarjeta.Controls.Add(avatar);

            // === Badge "ACTIVA" ===
            if (esActiva)
            {
                var badge = new Label
                {
                    Text = "● ACTIVA",
                    Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(50, 100, 50),
                    BackColor = Paleta.VerdeBrillante,
                    Size = new Size(62, 20),
                    Location = new Point(tarjeta.Width - 82, 22),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(0)
                };
                badge.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(badge, 10);
                Paleta.AplicarBordeRedondeadoSuave(badge, 10);
                tarjeta.Controls.Add(badge);
            }

            // === Nombre de la empresa ===
            int xText = 80;
            var lblNombre = new Label
            {
                Text = empresa.Nombre,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(xText, 22),
                Size = new Size(esActiva ? 140 : 200, 22),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            tarjeta.Controls.Add(lblNombre);

            // === RIF ===
            var lblRif = new Label
            {
                Text = empresa.Rif,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                Location = new Point(xText, 46),
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblRif);

            // === Separador horizontal ===
            var separador = new Panel
            {
                BackColor = Color.FromArgb(50, 255, 255, 255),
                Location = new Point(20, headerHeight),
                Size = new Size(260, 1)
            };
            tarjeta.Controls.Add(separador);

            // === Datos de la empresa (grid limpio) ===
            int yDato = headerHeight + 12;
            AgregarLineaDato(tarjeta, "📍  Sector", empresa.Sector, yDato); yDato += 21;
            AgregarLineaDato(tarjeta, "👥  Empleados", empresa.CantidadEmpleados.ToString(), yDato); yDato += 21;
            AgregarLineaDato(tarjeta, "📞  Teléfono", string.IsNullOrEmpty(empresa.Telefono) ? "—" : empresa.Telefono, yDato); yDato += 21;
            AgregarLineaDato(tarjeta, "📅  Registro", empresa.FechaRegistro.ToString("dd/MM/yyyy"), yDato);

            // === Cálculo de posición simétrica de los botones ===
            int anchoBoton = 120;
            int gapEntreBotones = 10;
            int anchoTotal = (anchoBoton * 2) + gapEntreBotones;
            int margenLateral = (tarjeta.ClientSize.Width - anchoTotal) / 2;
            int yBotones = 220;
            int altoBoton = 34;

            // === BOTÓN PRIMARIO — sólido contrastante con icono ===
            string textoBotonPrimario = esActiva ? "💬   Ir al chat" : "✓   Seleccionar";

            // Para empresa activa (morada): botón en VerdeBrillante (contrasta con morado)
            // Para empresa inactiva (verde): botón en MoradoClaro/lila (contrasta con verde)
            Color colorBtnFondo = esActiva ? Paleta.VerdeBrillante : Paleta.MoradoClaro;
            Color colorBtnFondoHover = esActiva
                ? Color.FromArgb(105, 220, 30)
                : Paleta.MoradoOscuro;
            Color colorBtnTexto = esActiva
                ? Color.FromArgb(40, 70, 30)
                : Color.White;

            var btnPrimario = new Panel
            {
                BackColor = colorBtnFondo,
                Location = new Point(margenLateral, yBotones),
                Size = new Size(anchoBoton, altoBoton),
                Cursor = Cursors.Hand
            };
            btnPrimario.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnPrimario, 17);
            Paleta.AplicarBordeRedondeadoSuave(btnPrimario, 17);

            var lblBtnPrimario = new Label
            {
                Text = textoBotonPrimario,
                Font = new Font("Segoe UI Emoji", 9, FontStyle.Bold),
                ForeColor = colorBtnTexto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnPrimario.Controls.Add(lblBtnPrimario);

            btnPrimario.MouseEnter += (s, e) => btnPrimario.BackColor = colorBtnFondoHover;
            btnPrimario.MouseLeave += (s, e) => btnPrimario.BackColor = colorBtnFondo;
            lblBtnPrimario.MouseEnter += (s, e) => btnPrimario.BackColor = colorBtnFondoHover;
            lblBtnPrimario.MouseLeave += (s, e) => btnPrimario.BackColor = colorBtnFondo;

            EventHandler primarioClick = (s, e) => OnBotonPrimarioClick(empresa);
            btnPrimario.Click += primarioClick;
            lblBtnPrimario.Click += primarioClick;

            tarjeta.Controls.Add(btnPrimario);

            // === BOTÓN "VER DETALLES" — outlined (borde blanco translúcido) ===
            var btnDetalles = new Panel
            {
                BackColor = Color.Transparent,
                Location = new Point(margenLateral + anchoBoton + gapEntreBotones, yBotones),
                Size = new Size(anchoBoton, altoBoton),
                Cursor = Cursors.Hand
            };

            Color bordeBtnNormal = Color.FromArgb(120, 255, 255, 255);
            Color bordeBtnHover = Color.White;
            Color bordeBtnActual = bordeBtnNormal;
            Color fondoBtnNormal = Color.Transparent;
            Color fondoBtnHover = Color.FromArgb(40, 255, 255, 255);

            btnDetalles.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = 17;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(btnDetalles.Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(btnDetalles.Width - r * 2 - 1, btnDetalles.Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, btnDetalles.Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();

                if (btnDetalles.BackColor != Color.Transparent)
                {
                    using var brushFondo = new SolidBrush(fondoBtnHover);
                    g.FillPath(brushFondo, path);
                }

                using var pen = new Pen(bordeBtnActual, 1.5f);
                g.DrawPath(pen, path);
            };

            var lblBtnDetalles = new Label
            {
                Text = "Ver detalles",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnDetalles.Controls.Add(lblBtnDetalles);

            void HoverDetalles()
            {
                bordeBtnActual = bordeBtnHover;
                btnDetalles.BackColor = fondoBtnHover;
                btnDetalles.Invalidate();
            }
            void QuitarHoverDetalles()
            {
                bordeBtnActual = bordeBtnNormal;
                btnDetalles.BackColor = fondoBtnNormal;
                btnDetalles.Invalidate();
            }

            btnDetalles.MouseEnter += (s, e) => HoverDetalles();
            btnDetalles.MouseLeave += (s, e) => QuitarHoverDetalles();
            lblBtnDetalles.MouseEnter += (s, e) => HoverDetalles();
            lblBtnDetalles.MouseLeave += (s, e) => QuitarHoverDetalles();

            EventHandler detallesClick = (s, e) => OnVerDetallesClick(empresa);
            btnDetalles.Click += detallesClick;
            lblBtnDetalles.Click += detallesClick;

            tarjeta.Controls.Add(btnDetalles);

            // === Hover de toda la tarjeta — sutil oscurecimiento ===
            tarjeta.MouseEnter += (s, e) =>
            {
                if (!tarjeta.ClientRectangle.Contains(tarjeta.PointToClient(Cursor.Position))) return;
                tarjeta.BackColor = colorFondoHover;
            };
            tarjeta.MouseLeave += (s, e) =>
            {
                if (!tarjeta.ClientRectangle.Contains(tarjeta.PointToClient(Cursor.Position)))
                    tarjeta.BackColor = colorFondo;
            };

            flowEmpresas.Controls.Add(tarjeta);
        }

        // Helper para crear una línea de "etiqueta: valor"
        private void AgregarLineaDato(Panel padre, string etiqueta, string valor, int y)
        {
            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = Color.FromArgb(210, 255, 255, 255),
                Location = new Point(20, y),
                Size = new Size(115, 18),
                BackColor = Color.Transparent
            };
            padre.Controls.Add(lblEtiqueta);

            var lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(135, y),
                Size = new Size(145, 18),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            };
            padre.Controls.Add(lblValor);
        }

        private void AgregarTarjetaNueva()
        {
            Color fondoNormal = Color.FromArgb(15, 143, 101, 203);
            Color fondoHover = Color.FromArgb(35, 143, 101, 203);
            Color bordeNormal = Color.FromArgb(140, 143, 101, 203);
            Color bordeHover = Paleta.MoradoOscuro;
            Color bordeActual = bordeNormal;

            var tarjeta = new Panel
            {
                Size = new Size(300, 270),
                BackColor = fondoNormal,
                Margin = new Padding(0, 0, 18, 18),
                Cursor = Cursors.Hand
            };
            tarjeta.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 18);

            // Border dashed pintado en Paint (más visible que las dashed normales de WinForms)
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(bordeActual, 2)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash,
                    DashPattern = new float[] { 6, 4 }
                };
                int r = 18;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(2, 2, r * 2, r * 2, 180, 90);
                path.AddArc(tarjeta.Width - r * 2 - 3, 2, r * 2, r * 2, 270, 90);
                path.AddArc(tarjeta.Width - r * 2 - 3, tarjeta.Height - r * 2 - 3, r * 2, r * 2, 0, 90);
                path.AddArc(2, tarjeta.Height - r * 2 - 3, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            };

            // Círculo grande con "+" — con halo translúcido detrás
            var circuloHalo = new Panel
            {
                Size = new Size(100, 100),
                Location = new Point((300 - 100) / 2, 70),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            circuloHalo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var halo = new SolidBrush(Color.FromArgb(40, Paleta.MoradoClaro));
                g.FillEllipse(halo, 0, 0, 100, 100);
            };
            tarjeta.Controls.Add(circuloHalo);

            var circulo = new Panel
            {
                Size = new Size(78, 78),
                Location = new Point(11, 11),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            var pathCirc = new System.Drawing.Drawing2D.GraphicsPath();
            pathCirc.AddEllipse(0, 0, circulo.Width, circulo.Height);
            circulo.Region = new Region(pathCirc);

            var lblMas = new Label
            {
                Text = "+",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            circulo.Controls.Add(lblMas);
            circuloHalo.Controls.Add(circulo);

            // Texto principal
            var lblTexto = new Label
            {
                Text = "Registrar nueva empresa",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(30, 188),
                Size = new Size(240, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTexto);

            // Subtítulo
            var lblHint = new Label
            {
                Text = "Click aquí para crear",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 110, 175),
                Location = new Point(30, 213),
                Size = new Size(240, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblHint);

            // Hover de toda la tarjeta
            void AplicarHover()
            {
                tarjeta.BackColor = fondoHover;
                bordeActual = bordeHover;
                tarjeta.Invalidate();
            }
            void QuitarHover()
            {
                if (tarjeta.ClientRectangle.Contains(tarjeta.PointToClient(Cursor.Position))) return;
                tarjeta.BackColor = fondoNormal;
                bordeActual = bordeNormal;
                tarjeta.Invalidate();
            }
            foreach (Control ctrl in new Control[] { tarjeta, circuloHalo, circulo, lblMas, lblTexto, lblHint })
            {
                ctrl.MouseEnter += (s, e) => AplicarHover();
                ctrl.MouseLeave += (s, e) => QuitarHover();
            }

            EventHandler clickHandler = (s, e) => BtnNuevaEmpresa_Click(s, e);
            tarjeta.Click += clickHandler;
            circuloHalo.Click += clickHandler;
            circulo.Click += clickHandler;
            lblMas.Click += clickHandler;
            lblTexto.Click += clickHandler;
            lblHint.Click += clickHandler;

            flowEmpresas.Controls.Add(tarjeta);
        }

        private void OnBotonPrimarioClick(Modelos.Empresa empresa)
        {
            bool esActiva = Estado.EstadoApp.EmpresaActivaId == empresa.Id;

            if (esActiva)
            {
                // Si ya es la activa, ir al chat
                IrAVistaChat();
            }
            else
            {
                // Si NO es la activa, seleccionarla como activa
                Estado.EstadoApp.EstablecerEmpresaActiva(empresa.Id);

                Estilos.MensajeApp.Exito(
                    $"Empresa '{empresa.Nombre}' seleccionada como activa.\n\n" +
                    "Ahora puedes ir al chat para iniciar o continuar su evaluación.",
                    "Empresa activa",
                    this.FindForm());

                // Recargar el grid para que la tarjeta se actualice (morada + badge ACTIVA)
                CargarEmpresas();
            }
        }

        private void IrAVistaChat()
        {
            // Buscar el FormMain padre y disparar el cambio de vista
            var formMain = this.FindForm() as MadurezTecnologica.Presentacion.FormMain;
            if (formMain != null)
            {
                formMain.NavegarAVistaChat();
            }
        }
        private void OnEmpresaActivaCambio()
        {
            // Recargar el grid para mostrar el badge ACTIVA en la nueva empresa
            CargarEmpresas();
        }

        private void OnVerDetallesClick(Modelos.Empresa empresa)
        {
            using var modal = new Presentacion.FormDetalleEmpresa(empresa);
            modal.ShowDialog(this.FindForm());
        }

        // =====================================================
        // BARRA DE FILTROS (buscador + tabs)
        // =====================================================
        private Panel CrearBarraFiltros()
        {
            var barra = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(5, 10, 5, 5)
            };

            // === Buscador (izquierda) con focus state ===
            var panelBuscador = new Panel
            {
                BackColor = Paleta.LilaInput,
                Location = new Point(5, 8),
                Size = new Size(310, 34),
                Padding = new Padding(36, 6, 14, 6)
            };
            panelBuscador.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(panelBuscador, 17);

            // Border dinámico (cambia con focus)
            Color bordeBuscadorNormal = Color.FromArgb(220, 215, 230);
            Color bordeBuscadorFocus = Paleta.MoradoOscuro;
            bool buscadorFocus = false;
            panelBuscador.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = 17;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(panelBuscador.Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(panelBuscador.Width - r * 2 - 1, panelBuscador.Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, panelBuscador.Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                using var pen = new Pen(buscadorFocus ? bordeBuscadorFocus : bordeBuscadorNormal,
                                        buscadorFocus ? 2 : 1);
                g.DrawPath(pen, path);
            };

            // Icono lupa con color dinámico
            var lblLupa = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 10),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(12, 8),
                Size = new Size(22, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            panelBuscador.Controls.Add(lblLupa);

            txtBuscar = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoOscuro,
                BackColor = Paleta.LilaInput,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                PlaceholderText = "Buscar empresa por nombre o RIF..."
            };
            txtBuscar.GotFocus += (s, e) =>
            {
                buscadorFocus = true;
                lblLupa.ForeColor = Paleta.MoradoClaro;
                panelBuscador.Invalidate();
            };
            txtBuscar.LostFocus += (s, e) =>
            {
                buscadorFocus = false;
                lblLupa.ForeColor = Paleta.MoradoOscuro;
                panelBuscador.Invalidate();
            };
            txtBuscar.TextChanged += (s, e) =>
            {
                _textoBusqueda = txtBuscar.Text.Trim();
                AplicarFiltros();
            };
            panelBuscador.Controls.Add(txtBuscar);
            txtBuscar.BringToFront();

            barra.Controls.Add(panelBuscador);

            // === Tabs (derecha) ===
            int xTab = 335;

            (tabTodas, lblTabTodas) = CrearTab("Todas", 0, TipoFiltro.Todas, xTab);
            xTab += tabTodas.Width + 8;
            barra.Controls.Add(tabTodas);

            (tabActiva, lblTabActiva) = CrearTab("Activa", 0, TipoFiltro.Activa, xTab);
            xTab += tabActiva.Width + 8;
            barra.Controls.Add(tabActiva);

            (tabSinEvaluar, lblTabSinEvaluar) = CrearTab("Sin evaluar", 0, TipoFiltro.SinEvaluar, xTab);
            barra.Controls.Add(tabSinEvaluar);

            return barra;
        }

        // Crea un tab tipo "pill" (Panel + Label)
        private (Panel panel, Label label) CrearTab(string texto, int count, TipoFiltro tipo, int x)
        {
            bool esActivo = _filtroActivo == tipo;

            var panel = new Panel
            {
                BackColor = esActivo ? Paleta.MoradoOscuro : ColorTranslator.FromHtml("#E8E5EB"),
                Size = new Size(100, 32),
                Location = new Point(x, 8),
                Cursor = Cursors.Hand
            };

            var lbl = new Label
            {
                Text = $"{texto} ({count})",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = esActivo ? Paleta.TextoBlanco : Paleta.TextoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(lbl);

            // Forma píldora
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, 32, 32, 90, 180);
            path.AddArc(panel.Width - 32, 0, 32, 32, 270, 180);
            path.CloseFigure();
            panel.Region = new Region(path);

            // Click → cambiar filtro activo
            EventHandler tabClick = (s, e) =>
            {
                _filtroActivo = tipo;
                AplicarFiltros();
            };
            panel.Click += tabClick;
            lbl.Click += tabClick;

            return (panel, lbl);
        }

        // Actualiza los colores de los tabs según _filtroActivo
        private void ActualizarVisualTabs()
        {
            void Refrescar(Panel p, Label l, TipoFiltro tipo)
            {
                bool esActivo = _filtroActivo == tipo;
                p.BackColor = esActivo ? Paleta.MoradoOscuro : ColorTranslator.FromHtml("#E8E5EB");
                l.ForeColor = esActivo ? Paleta.TextoBlanco : Paleta.TextoOscuro;
            }
            Refrescar(tabTodas, lblTabTodas, TipoFiltro.Todas);
            Refrescar(tabActiva, lblTabActiva, TipoFiltro.Activa);
            Refrescar(tabSinEvaluar, lblTabSinEvaluar, TipoFiltro.SinEvaluar);
        }
        private void ActualizarContadoresTabs(int totalTodas, int totalActiva, int totalSinEvaluar)
        {
            lblTabTodas.Text = $"Todas ({totalTodas})";
            lblTabActiva.Text = $"Activa ({totalActiva})";
            lblTabSinEvaluar.Text = $"Sin evaluar ({totalSinEvaluar})";
        }

        private void AplicarFiltros()
        {
            ActualizarVisualTabs();

            if (flowEmpresas == null) return;

            flowEmpresas.SuspendLayout();
            flowEmpresas.Controls.Clear();

            try
            {
                // === Detectar qué empresas tienen diagnósticos generados ===
                // Cargamos conversaciones y diagnósticos UNA sola vez para eficiencia
                var todasConversaciones = _repoConv.ObtenerTodas();
                var empresasConDiagnostico = new HashSet<int>();

                foreach (var conv in todasConversaciones)
                {
                    var diagnosticos = _repoDiag.ObtenerHistorialPorConversacion(conv.Id);
                    if (diagnosticos.Count > 0)
                    {
                        empresasConDiagnostico.Add(conv.EmpresaId);
                    }
                }

                // === Calcular contadores TOTALES (sin importar el texto de búsqueda) ===
                int totalTodas = _empresasCache.Count;
                int totalActiva = Estado.EstadoApp.EmpresaActivaId.HasValue ? 1 : 0;
                int totalSinEvaluar = _empresasCache.Count(e => !empresasConDiagnostico.Contains(e.Id));
                ActualizarContadoresTabs(totalTodas, totalActiva, totalSinEvaluar);

                // === Aplicar filtro de tab + buscador ===
                var empresasFiltradas = _empresasCache.Where(emp =>
                {
                    // Filtro por tab activo
                    switch (_filtroActivo)
                    {
                        case TipoFiltro.Activa:
                            if (Estado.EstadoApp.EmpresaActivaId != emp.Id) return false;
                            break;
                        case TipoFiltro.SinEvaluar:
                            if (empresasConDiagnostico.Contains(emp.Id)) return false;
                            break;
                    }

                    // Filtro por texto de búsqueda
                    if (!string.IsNullOrEmpty(_textoBusqueda))
                    {
                        string busqueda = _textoBusqueda.ToLower();
                        bool coincide =
                            emp.Nombre.ToLower().Contains(busqueda) ||
                            emp.Rif.ToLower().Contains(busqueda) ||
                            (emp.Sector ?? "").ToLower().Contains(busqueda);
                        if (!coincide) return false;
                    }

                    return true;
                }).ToList();

                // === Renderizar resultado ===
                if (empresasFiltradas.Count == 0)
                {
                    var lblSinResultados = new Label
                    {
                        Text = string.IsNullOrEmpty(_textoBusqueda)
                            ? "No hay empresas en este filtro."
                            : $"No se encontraron empresas que coincidan con '{_textoBusqueda}'.",
                        Font = new Font("Segoe UI", 10, FontStyle.Italic),
                        ForeColor = Paleta.TextoOscuro,
                        Size = new Size(600, 60),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent
                    };
                    flowEmpresas.Controls.Add(lblSinResultados);
                }
                else
                {
                    foreach (var empresa in empresasFiltradas)
                    {
                        AgregarTarjetaEmpresa(empresa);
                    }
                }

                // Tarjeta "+ Nueva empresa" solo cuando estamos viendo "Todas"
                if (_filtroActivo == TipoFiltro.Todas)
                {
                    AgregarTarjetaNueva();
                }
            }
            finally
            {
                flowEmpresas.ResumeLayout(true);
            }
        }



    }
}