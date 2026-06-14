using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Vistas
{
    public partial class VistaEmpresas : UserControl
    {
        // Paneles principales
        private Panel panelHeader = null!;
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
                Size = new Size(700, 20),
                BackColor = Color.Transparent
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

            ReposicionarBotonHeader();
        }

        private void ReposicionarBotonHeader()
        {
            if (panelHeader == null || btnNuevaEmpresa == null) return;

            btnNuevaEmpresa.Location = new Point(
                panelHeader.Width - btnNuevaEmpresa.Width - 20, 25);
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

                MessageBox.Show(
                    $"Empresa '{modal.EmpresaGuardada.Nombre}' registrada exitosamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
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
                MessageBox.Show($"Error al cargar empresas: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AgregarTarjetaEmpresa(Modelos.Empresa empresa)
        {
            bool esActiva = Estado.EstadoApp.EmpresaActivaId == empresa.Id;
            Color colorFondo = esActiva ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;

            // === Tarjeta principal ===
            var tarjeta = new Panel
            {
                Size = new Size(290, 240),
                BackColor = colorFondo,
                Margin = new Padding(0, 0, 18, 18),
                Cursor = Cursors.Hand,
                Padding = new Padding(20),
                Tag = empresa.Id
            };

            // Esquinas redondeadas
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 20);

            // === Badge "ACTIVA" ===
            if (esActiva)
            {
                var badge = new Label
                {
                    Text = "ACTIVA",
                    Font = new Font("Segoe UI", 7, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    BackColor = Paleta.VerdeBrillante,
                    Size = new Size(55, 18),
                    Location = new Point(tarjeta.Width - 75, 12),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(0)
                };
                var pathBadge = new System.Drawing.Drawing2D.GraphicsPath();
                pathBadge.AddArc(0, 0, 18, 18, 90, 180);
                pathBadge.AddArc(badge.Width - 18, 0, 18, 18, 270, 180);
                pathBadge.CloseFigure();
                badge.Region = new Region(pathBadge);
                tarjeta.Controls.Add(badge);
            }

            // === Nombre de la empresa ===
            var lblNombre = new Label
            {
                Text = empresa.Nombre,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(20, 20),
                Size = new Size(250, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblNombre);

            // === RIF ===
            var lblRif = new Label
            {
                Text = $"RIF: {empresa.Rif}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(20, 44),
                Size = new Size(250, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblRif);

            // === Separador ===
            var separador = new Panel
            {
                BackColor = Color.FromArgb(80, 255, 255, 255),
                Location = new Point(20, 70),
                Size = new Size(250, 1)
            };
            tarjeta.Controls.Add(separador);

            // === Datos de la empresa ===
            int yDato = 80;
            AgregarLineaDato(tarjeta, "Sector:", empresa.Sector, yDato); yDato += 22;
            AgregarLineaDato(tarjeta, "Empleados:", empresa.CantidadEmpleados.ToString(), yDato); yDato += 22;
            AgregarLineaDato(tarjeta, "Teléfono:", string.IsNullOrEmpty(empresa.Telefono) ? "—" : empresa.Telefono, yDato); yDato += 22;
            AgregarLineaDato(tarjeta, "Registro:", empresa.FechaRegistro.ToString("dd/MM/yyyy"), yDato);

            // === Cálculo de posición simétrica de los botones ===
            int anchoBoton = 115;
            int gapEntreBotones = 10;
            int anchoTotal = (anchoBoton * 2) + gapEntreBotones;
            int margenLateral = (tarjeta.ClientSize.Width - anchoTotal) / 2;
            int yBotones = 175;

            // === BOTÓN PRIMARIO (Panel + Label) ===
            string textoBotonPrimario = esActiva ? "Ir al chat" : "Seleccionar";

            var btnPrimario = new Panel
            {
                BackColor = esActiva ? Paleta.VerdeGrisaceoOscuro : Paleta.MoradoClaro,
                Location = new Point(margenLateral, yBotones),
                Size = new Size(anchoBoton, 30),
                Cursor = Cursors.Hand
            };

            var lblBtnPrimario = new Label
            {
                Text = textoBotonPrimario,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnPrimario.Controls.Add(lblBtnPrimario);

            // Forma píldora
            var pathBtnP = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtnP.AddArc(0, 0, 30, 30, 90, 180);
            pathBtnP.AddArc(btnPrimario.Width - 30, 0, 30, 30, 270, 180);
            pathBtnP.CloseFigure();
            btnPrimario.Region = new Region(pathBtnP);

            // Hover effect
            Color colorNormalP = btnPrimario.BackColor;
            Color colorHoverP = esActiva ? Paleta.VerdeGrisaceo : Paleta.MoradoOscuroHover;
            btnPrimario.MouseEnter += (s, e) => btnPrimario.BackColor = colorHoverP;
            btnPrimario.MouseLeave += (s, e) => btnPrimario.BackColor = colorNormalP;
            lblBtnPrimario.MouseEnter += (s, e) => btnPrimario.BackColor = colorHoverP;
            lblBtnPrimario.MouseLeave += (s, e) => btnPrimario.BackColor = colorNormalP;

            // Click
            EventHandler primarioClick = (s, e) => OnBotonPrimarioClick(empresa);
            btnPrimario.Click += primarioClick;
            lblBtnPrimario.Click += primarioClick;

            tarjeta.Controls.Add(btnPrimario);

            // === BOTÓN "VER DETALLES" (Panel + Label) ===
            var btnDetalles = new Panel
            {
                BackColor = Color.FromArgb(60, 255, 255, 255),
                Location = new Point(margenLateral + anchoBoton + gapEntreBotones, yBotones),
                Size = new Size(anchoBoton, 30),
                Cursor = Cursors.Hand
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

            // Forma píldora
            var pathBtnD = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtnD.AddArc(0, 0, 30, 30, 90, 180);
            pathBtnD.AddArc(btnDetalles.Width - 30, 0, 30, 30, 270, 180);
            pathBtnD.CloseFigure();
            btnDetalles.Region = new Region(pathBtnD);

            // Hover effect
            Color colorNormalD = btnDetalles.BackColor;
            Color colorHoverD = Color.FromArgb(100, 255, 255, 255);
            btnDetalles.MouseEnter += (s, e) => btnDetalles.BackColor = colorHoverD;
            btnDetalles.MouseLeave += (s, e) => btnDetalles.BackColor = colorNormalD;
            lblBtnDetalles.MouseEnter += (s, e) => btnDetalles.BackColor = colorHoverD;
            lblBtnDetalles.MouseLeave += (s, e) => btnDetalles.BackColor = colorNormalD;

            // Click
            EventHandler detallesClick = (s, e) => OnVerDetallesClick(empresa);
            btnDetalles.Click += detallesClick;
            lblBtnDetalles.Click += detallesClick;

            tarjeta.Controls.Add(btnDetalles);

            flowEmpresas.Controls.Add(tarjeta);
        }

        // Helper para crear una línea de "etiqueta: valor"
        private void AgregarLineaDato(Panel padre, string etiqueta, string valor, int y)
        {
            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                Location = new Point(20, y),
                Size = new Size(100, 18),
                BackColor = Color.Transparent
            };
            padre.Controls.Add(lblEtiqueta);

            var lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(110, y),
                Size = new Size(160, 18),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            padre.Controls.Add(lblValor);
        }

        private void AgregarTarjetaNueva()
        {
            var tarjeta = new Panel
            {
                Size = new Size(290, 240),
                BackColor = Color.FromArgb(15, 143, 101, 203),  // Morado claro muy transparente
                Margin = new Padding(0, 0, 18, 18),
                Cursor = Cursors.Hand
            };

            // Esquinas redondeadas
            Paleta.AplicarBordeRedondeadoSuave(tarjeta, 20);

            // Círculo grande con "+"
            var circulo = new Panel
            {
                Size = new Size(70, 70),
                BackColor = Paleta.MoradoClaro,
                Location = new Point(110, 70)
            };
            var pathCirc = new System.Drawing.Drawing2D.GraphicsPath();
            pathCirc.AddEllipse(0, 0, circulo.Width, circulo.Height);
            circulo.Region = new Region(pathCirc);

            var lblMas = new Label
            {
                Text = "+",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            circulo.Controls.Add(lblMas);
            tarjeta.Controls.Add(circulo);

            // Texto
            var lblTexto = new Label
            {
                Text = "Registrar nueva empresa",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(40, 155),
                Size = new Size(210, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTexto);

            // Click en cualquier parte de la tarjeta → mismo evento que el botón del header
            EventHandler clickHandler = (s, e) => BtnNuevaEmpresa_Click(s, e);
            tarjeta.Click += clickHandler;
            circulo.Click += clickHandler;
            lblMas.Click += clickHandler;
            lblTexto.Click += clickHandler;

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

                // Notificación amigable
                MessageBox.Show(
                    $"Empresa '{empresa.Nombre}' seleccionada como activa.\n\n" +
                    "Ahora puedes ir al chat para iniciar o continuar su evaluación.",
                    "Empresa activa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

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

            // === Buscador (izquierda) ===
            var panelBuscador = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Location = new Point(5, 8),
                Size = new Size(290, 32),
                Padding = new Padding(12, 5, 10, 5)
            };
            var pathB = new System.Drawing.Drawing2D.GraphicsPath();
            pathB.AddArc(0, 0, 22, 22, 90, 180);
            pathB.AddArc(panelBuscador.Width - 22, 0, 22, 22, 270, 180);
            pathB.CloseFigure();
            panelBuscador.Region = new Region(pathB);

            var lblLupa = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 9),
                Dock = DockStyle.Left,
                Width = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            panelBuscador.Controls.Add(lblLupa);

            txtBuscar = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoOscuro,
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                PlaceholderText = "Buscar empresa..."
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
            int xTab = 310;

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