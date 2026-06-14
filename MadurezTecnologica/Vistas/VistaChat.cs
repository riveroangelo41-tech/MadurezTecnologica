using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Vistas
{
    public partial class VistaChat : UserControl
    {

        // Paneles principales
        private Panel panelHeader = null!;
        private Panel panelConversaciones = null!;
        private Panel panelChat = null!;

        // Header
        private Label lblTituloEmpresa = null!;
        private Label lblInfoEmpresa = null!;
        private Panel panelIndicadorConexion = null!;
        private Label lblIndicadorConexion = null!;
        private Button btnNuevaConversacion = null!;

        // Lista de conversaciones
        private Label lblTituloConversaciones = null!;
        private TextBox txtBuscarConversacion = null!;
        private FlowLayoutPanel flowConversaciones = null!;

        // Área de mensajes
        private FlowLayoutPanel flowMensajes = null!;
        private Panel panelEntrada = null!;
        private TextBox txtEntrada = null!;
        private Button btnEnviar = null!;
        private Label lblHeaderEmpresa = null!;
        private Label lblHeaderInfo = null!;

        // Estado del chat
        private int? _empresaIdActiva = null;  // se obtiene de EstadoApp en runtime
        private int? _conversacionActivaId = null;
        private MadurezTecnologica.Logica.GestorConversacion _gestorConv = null!;
        private MadurezTecnologica.Datos.RepositorioConversacion _repoConv = null!;
        private MadurezTecnologica.Datos.RepositorioEmpresa _repoEmpresa = null!;

        private MadurezTecnologica.Datos.RepositorioDiagnostico _repoDiagnostico = null!;

        public VistaChat()
        {
            InitializeComponent();

            // Activar doble buffer en TODO el control (esto es lo crítico)
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();

            _gestorConv = new MadurezTecnologica.Logica.GestorConversacion();
            _repoConv = new MadurezTecnologica.Datos.RepositorioConversacion();
            _repoEmpresa = new MadurezTecnologica.Datos.RepositorioEmpresa();
            _repoDiagnostico = new MadurezTecnologica.Datos.RepositorioDiagnostico();

            ConfigurarControl();
            CrearPanelChat();
            CrearPanelConversaciones();
            CrearHeader();
            this.Load += (s, e) =>
            {
                // Obtener empresa activa del estado global
                _empresaIdActiva = Estado.EstadoApp.EmpresaActivaId;

                // Suscribirse al evento de cambio de empresa
                Estado.EstadoApp.EmpresaActivaCambio += OnEmpresaActivaCambio;

                this.BeginInvoke(new Action(() => CargarEvaluacionesDeEmpresa()));
            };

            // Desuscribirse cuando la vista se destruya para evitar memory leaks
            this.HandleDestroyed += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio -= OnEmpresaActivaCambio;
            };

            // Activar double buffering en el flow de mensajes
            typeof(FlowLayoutPanel).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flowMensajes, new object[] { true });

            // También en el flow de conversaciones
            typeof(FlowLayoutPanel).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flowConversaciones, new object[] { true });
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
        // HEADER (arriba, info de la empresa)
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

            panelHeader.Resize += (s, e) => ReposicionarBotonesHeader();

            var picAvatar = new Panel
            {
                Size = new Size(50, 50),
                Location = new Point(10, 15),
                BackColor = Paleta.MoradoClaro
            };
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, picAvatar.Width, picAvatar.Height);
            picAvatar.Region = new Region(path);
            panelHeader.Controls.Add(picAvatar);

            var lblTitulo = new Label
            {
                Text = "Asistente de IA",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 15),
                Size = new Size(400, 30),
                BackColor = Color.Transparent
            };
            panelHeader.Controls.Add(lblTitulo);

            var lblSubtitulo = new Label
            {
                Text = "Consulta, analiza y recibe recomendaciones inteligentes",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 48),
                Size = new Size(450, 20),
                BackColor = Color.Transparent
            };
            panelHeader.Controls.Add(lblSubtitulo);

            panelIndicadorConexion = new Panel
            {
                Size = new Size(150, 32),
                BackColor = Color.White
            };
            var pathInd = new System.Drawing.Drawing2D.GraphicsPath();
            pathInd.AddArc(0, 0, 30, 30, 90, 180);
            pathInd.AddArc(panelIndicadorConexion.Width - 30, 0, 30, 30, 270, 180);
            pathInd.CloseFigure();
            panelIndicadorConexion.Region = new Region(pathInd);
            panelHeader.Controls.Add(panelIndicadorConexion);

            var puntoVerde = new Panel
            {
                Size = new Size(12, 12),
                Location = new Point(12, 10),
                BackColor = Paleta.VerdeBrillante
            };
            var pathPunto = new System.Drawing.Drawing2D.GraphicsPath();
            pathPunto.AddEllipse(0, 0, 12, 12);
            puntoVerde.Region = new Region(pathPunto);
            panelIndicadorConexion.Controls.Add(puntoVerde);

            lblIndicadorConexion = new Label
            {
                Text = "Conectado a IA",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(30, 8),
                Size = new Size(115, 16),
                BackColor = Color.Transparent
            };
            panelIndicadorConexion.Controls.Add(lblIndicadorConexion);

            btnNuevaConversacion = new Button
            {
                Text = "+ Generar evaluación",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(165, 32),
                Cursor = Cursors.Hand
            };
            btnNuevaConversacion.FlatAppearance.BorderSize = 0;
            btnNuevaConversacion.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 30, 30, 90, 180);
            pathBtn.AddArc(btnNuevaConversacion.Width - 30, 0, 30, 30, 270, 180);
            pathBtn.CloseFigure();
            btnNuevaConversacion.Region = new Region(pathBtn);
            btnNuevaConversacion.Click += BtnGenerarEvaluacion_Click;
            panelHeader.Controls.Add(btnNuevaConversacion);

            ReposicionarBotonesHeader();
        }

        private void ReposicionarBotonesHeader()
        {
            if (panelHeader == null || btnNuevaConversacion == null || panelIndicadorConexion == null)
                return;

            btnNuevaConversacion.Location = new Point(
                panelHeader.Width - btnNuevaConversacion.Width - 20, 25);

            panelIndicadorConexion.Location = new Point(
                btnNuevaConversacion.Left - panelIndicadorConexion.Width - 15, 25);
        }

        
        // PANEL DE CONVERSACIONES (izquierda)
     

        private void CrearPanelConversaciones()
        {
            panelConversaciones = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = ColorTranslator.FromHtml("#A8A2A0"),
                Padding = new Padding(15, 0, 10, 15),
                Margin = new Padding(0, 10, 10, 10)
            };
            Controls.Add(panelConversaciones);
            panelConversaciones.Resize += (s, e) => AplicarBordeRedondeado(panelConversaciones, 25);

            lblTituloConversaciones = new Label
            {
                Text = "Evaluaciones",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                BackColor = Color.Transparent
            };
            panelConversaciones.Controls.Add(lblTituloConversaciones);

            var panelBuscador = new Panel
            {
                BackColor = Color.White,
                Location = new Point(15, 40),
                Size = new Size(280, 34),
                Padding = new Padding(10, 5, 10, 5)
            };
            panelConversaciones.Controls.Add(panelBuscador);

            var pathBuscador = new System.Drawing.Drawing2D.GraphicsPath();
            int rB = 16;
            pathBuscador.AddArc(0, 0, rB, rB, 180, 90);
            pathBuscador.AddArc(panelBuscador.Width - rB, 0, rB, rB, 270, 90);
            pathBuscador.AddArc(panelBuscador.Width - rB, panelBuscador.Height - rB, rB, rB, 0, 90);
            pathBuscador.AddArc(0, panelBuscador.Height - rB, rB, rB, 90, 90);
            pathBuscador.CloseFigure();
            panelBuscador.Region = new Region(pathBuscador);

            txtBuscarConversacion = new TextBox
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                PlaceholderText = "Buscar evaluación..."
            };
            panelBuscador.Controls.Add(txtBuscarConversacion);

            flowConversaciones = new FlowLayoutPanel
            {
                Location = new Point(15, 80),
                Size = new Size(290, 500),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = ColorTranslator.FromHtml("#A8A2A0"),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            panelConversaciones.Controls.Add(flowConversaciones);
        }

       
        // PANEL DE CHAT (centro/derecha)
        

        private void CrearPanelChat()
        {
            panelChat = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };
            Controls.Add(panelChat);
            panelChat.Resize += (s, e) => AplicarBordeRedondeado(panelChat, 25);

            // Header dentro del panelChat
            var panelHeaderChat = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 10)
            };
            panelChat.Controls.Add(panelHeaderChat);

            lblHeaderEmpresa = new Label
            {
                Text = "Selecciona una conversación",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(15, 5),
                Size = new Size(600, 25),
                BackColor = Color.Transparent
            };
            panelHeaderChat.Controls.Add(lblHeaderEmpresa);

            lblHeaderInfo = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(15, 32),
                Size = new Size(600, 18),
                BackColor = Color.Transparent
            };
            panelHeaderChat.Controls.Add(lblHeaderInfo);

            // Panel de entrada (abajo)
            panelEntrada = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 15)
            };
            panelChat.Controls.Add(panelEntrada);

            var panelTxtWrapper = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10)
            };
            panelEntrada.Controls.Add(panelTxtWrapper);
            panelTxtWrapper.Resize += (s, e) => AplicarBordeRedondeado(panelTxtWrapper, 22);

            txtEntrada = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoOscuro,
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Multiline = true,
                PlaceholderText = "Escribe tu mensaje..."
            };
            txtEntrada.KeyDown += TxtEntrada_KeyDown;
            panelTxtWrapper.Controls.Add(txtEntrada);

            btnEnviar = new Button
            {
                Text = "Enviar",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(90, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            btnEnviar.Click += BtnEnviar_Click;
            panelEntrada.Controls.Add(btnEnviar);
            btnEnviar.BringToFront();
            var pathBtn = new System.Drawing.Drawing2D.GraphicsPath();
            pathBtn.AddArc(0, 0, 20, 20, 180, 90);
            pathBtn.AddArc(btnEnviar.Width - 20, 0, 20, 20, 270, 90);
            pathBtn.AddArc(btnEnviar.Width - 20, btnEnviar.Height - 20, 20, 20, 0, 90);
            pathBtn.AddArc(0, btnEnviar.Height - 20, 20, 20, 90, 90);
            pathBtn.CloseFigure();
            btnEnviar.Region = new Region(pathBtn);

            flowMensajes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 30, 10)
            };
            panelChat.Controls.Add(flowMensajes);

            flowMensajes.BringToFront();
        }


        // CARGA DE CONVERSACIONES Y MENSAJES


        private void CargarEvaluacionesDeEmpresa()
        {
            flowConversaciones.SuspendLayout();
            flowConversaciones.Controls.Clear();

            if (_empresaIdActiva == null)
            {
                var lblSinEmpresa = new Label
                {
                    Text = "No hay empresa seleccionada.\n\nVe a 'Empresas' y selecciona una empresa para empezar.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = Paleta.TextoOscuro,
                    Size = new Size(280, 100),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                flowConversaciones.Controls.Add(lblSinEmpresa);

                lblHeaderEmpresa.Text = "Sin empresa seleccionada";
                lblHeaderInfo.Text = "";
                flowMensajes.Controls.Clear();

                flowConversaciones.ResumeLayout(true);
                return;
            }

            try
            {
                var todas = _repoConv.ObtenerTodas();
                var conversacion = todas.FirstOrDefault(c => c.EmpresaId == _empresaIdActiva.Value);

                if (conversacion == null)
                {
                    var lblVacio = new Label
                    {
                        Text = "Esta empresa no tiene conversación iniciada todavía.",
                        Font = new Font("Segoe UI", 9, FontStyle.Italic),
                        ForeColor = Paleta.TextoOscuro,
                        Size = new Size(280, 60),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    flowConversaciones.Controls.Add(lblVacio);

                    var empresaSinConv = _repoEmpresa.ObtenerPorId(_empresaIdActiva.Value);
                    lblHeaderEmpresa.Text = empresaSinConv?.Nombre ?? "Empresa";
                    lblHeaderInfo.Text = $"Sector: {empresaSinConv?.Sector ?? "—"}   |   Empleados: {empresaSinConv?.CantidadEmpleados ?? 0}";
                    flowMensajes.Controls.Clear();

                    return;
                }

                _conversacionActivaId = conversacion.Id;
                var empresa = _repoEmpresa.ObtenerPorId(_empresaIdActiva.Value);
                lblHeaderEmpresa.Text = empresa?.Nombre ?? "Empresa";
                lblHeaderInfo.Text = $"Sector: {empresa?.Sector ?? "—"}   |   Empleados: {empresa?.CantidadEmpleados ?? 0}";
                CargarMensajes(conversacion.Id);

                var diagnosticos = _repoDiagnostico.ObtenerHistorialPorConversacion(conversacion.Id)
                                                   .OrderByDescending(d => d.FechaGeneracion)
                                                   .ToList();

                if (diagnosticos.Count == 0)
                {
                    var lblSinEvaluaciones = new Label
                    {
                        Text = "Aún no se ha generado ninguna evaluación.\n\nUsa el botón '+ Generar evaluación' del header.",
                        Font = new Font("Segoe UI", 9, FontStyle.Italic),
                        ForeColor = Paleta.TextoOscuro,
                        Size = new Size(280, 80),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    flowConversaciones.Controls.Add(lblSinEvaluaciones);
                    return;
                }

                foreach (var diag in diagnosticos)
                {
                    AgregarTarjetaDiagnostico(diag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar evaluaciones: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                flowConversaciones.ResumeLayout(true);
            }
        }


        private void AgregarTarjetaDiagnostico(Modelos.Diagnostico diag)
        {
            // Color según si es diagnóstico final o intermedio
            Color colorFondo = diag.EsFinal ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;
            Color colorTexto = Paleta.TextoBlanco;

            var tarjeta = new Panel
            {
                Size = new Size(280, 100),
                BackColor = colorFondo,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 10, 12, 10),
                Tag = diag.Id  // guardamos el Id por si necesitamos identificarla
            };

            // Esquinas redondeadas
            var pathT = new System.Drawing.Drawing2D.GraphicsPath();
            int r = 18;
            pathT.AddArc(0, 0, r, r, 180, 90);
            pathT.AddArc(tarjeta.Width - r, 0, r, r, 270, 90);
            pathT.AddArc(tarjeta.Width - r, tarjeta.Height - r, r, r, 0, 90);
            pathT.AddArc(0, tarjeta.Height - r, r, r, 90, 90);
            pathT.CloseFigure();
            tarjeta.Region = new Region(pathT);

            // Línea 1: Fecha + badge "FINAL"
            var lblFecha = new Label
            {
                Text = diag.FechaGeneracion.ToString("dd/MM/yyyy"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = colorTexto,
                Location = new Point(12, 10),
                Size = new Size(150, 18),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblFecha);

            if (diag.EsFinal)
            {
                var lblFinal = new Label
                {
                    Text = "FINAL",
                    Font = new Font("Segoe UI", 7, FontStyle.Bold),
                    ForeColor = Paleta.VerdeBrillante,
                    Location = new Point(225, 12),
                    Size = new Size(45, 16),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleRight
                };
                tarjeta.Controls.Add(lblFinal);
            }

            // Línea 2: Nivel CMMI
            var lblNivel = new Label
            {
                Text = $"Nivel CMMI: {diag.NivelMadurez}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = colorTexto,
                Location = new Point(12, 32),
                Size = new Size(255, 20),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblNivel);

            // Línea 3: Resumen breve
            string resumen = diag.ResumenEmpresa;
            if (resumen.Length > 70) resumen = resumen.Substring(0, 70) + "...";

            var lblResumen = new Label
            {
                Text = resumen,
                Font = new Font("Segoe UI", 8),
                ForeColor = colorTexto,
                Location = new Point(12, 55),
                Size = new Size(255, 35),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblResumen);

            // Click → abrir modal con el reporte completo
            EventHandler clickHandler = (s, e) => MostrarModalDiagnostico(diag);
            tarjeta.Click += clickHandler;
            lblFecha.Click += clickHandler;
            lblNivel.Click += clickHandler;
            lblResumen.Click += clickHandler;

            flowConversaciones.Controls.Add(tarjeta);
        }

        private void SeleccionarConversacion(int conversacionId)
        {
            _conversacionActivaId = conversacionId;

            foreach (Control ctrl in flowConversaciones.Controls)
            {
                if (ctrl is Panel panel && panel.Tag is int id)
                {
                    panel.BackColor = (id == conversacionId)
                        ? Paleta.MoradoOscuro
                        : Paleta.VerdeGrisaceo;
                }
            }
            if (_empresaIdActiva == null) return;
            var empresa = _repoEmpresa.ObtenerPorId(_empresaIdActiva.Value);
            lblHeaderEmpresa.Text = empresa?.Nombre ?? "Empresa";
            lblHeaderInfo.Text = $"Sector: {empresa?.Sector ?? "—"}   |   Empleados: {empresa?.CantidadEmpleados ?? 0}";

            CargarMensajes(conversacionId);
        }

        private void CargarMensajes(int conversacionId)
        {
            flowMensajes.Visible = false;          
            flowMensajes.SuspendLayout();
            flowMensajes.Controls.Clear();

            try
            {
                var repoMensaje = new MadurezTecnologica.Datos.RepositorioMensaje();
                var mensajes = repoMensaje.ObtenerPorConversacion(conversacionId);

                foreach (var msg in mensajes)
                {
                    AgregarBurbuja(msg.Remitente, msg.Contenido, msg.Timestamp);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando mensajes: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                flowMensajes.ResumeLayout(true);
                flowMensajes.Visible = true;
                flowMensajes.PerformLayout();

                ScrollAlFinal();
            }
        }
        private void AgregarBurbuja(string remitente, string contenido, DateTime timestamp)
        {
            bool esIA = remitente == "IA";
            Color colorFondo = esIA ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;
            Color colorTexto = Paleta.TextoBlanco;

            int anchoDisponible = flowMensajes.ClientSize.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200;
            int anchoMaxBurbuja = (int)(anchoDisponible * 0.65);

            // Fila como FlowLayoutPanel (avatar + burbuja se ordenan solos) 
            var fila = new FlowLayoutPanel
            {
                FlowDirection = esIA ? FlowDirection.LeftToRight : FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
                Width = flowMensajes.ClientSize.Width - 30,
                Height = 50, // se ajusta después
                Margin = new Padding(0, 5, 0, 10),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Avatar circular 
            var avatar = new Panel
            {
                Size = new Size(40, 40),
                BackColor = esIA ? Paleta.MoradoClaro : Paleta.VerdeGrisaceoOscuro,
                Margin = new Padding(0, 5, 10, 0)
            };
            var pathAvatar = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvatar.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAvatar);

            var lblInicial = new Label
            {
                Text = esIA ? "C" : "T",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);

            //  Burbuja 
            var burbuja = new Panel
            {
                BackColor = colorFondo,
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            var flowInterno = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(25, 15, 25, 15),
                MaximumSize = new Size(anchoMaxBurbuja, 0),
                Margin = new Padding(0)
            };

            var lblMensaje = new Label
            {
                Text = LimpiadorTexto.LimpiarMarkdown(contenido),
                Font = new Font("Segoe UI", 10),
                ForeColor = colorTexto,
                AutoSize = true,
                MaximumSize = new Size(anchoMaxBurbuja - 60, 0),  
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            flowInterno.Controls.Add(lblMensaje);

            var lblHora = new Label
            {
                Text = timestamp.ToString("HH:mm"),
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 0)
            };
            flowInterno.Controls.Add(lblHora);

            burbuja.Controls.Add(flowInterno);

            burbuja.HandleCreated += (s, e) =>
            {
                burbuja.BeginInvoke(new Action(() =>
                {
                    if (burbuja.Width > 0 && burbuja.Height > 0)
                    {
                        Paleta.AplicarBordeRedondeadoSuave(burbuja, 18);
                    }
                }));
            };

            // Agregar al flow layout en el orden correcto 
            fila.Controls.Add(avatar);
            fila.Controls.Add(burbuja);

            // Ajustar la altura de la fila cuando la burbuja tenga su tamaño final
            fila.HandleCreated += (s, e) =>
            {
                fila.BeginInvoke(new Action(() =>
                {
                    fila.Height = Math.Max(burbuja.PreferredSize.Height, 50) + 10;
                }));
            };

            flowMensajes.Controls.Add(fila);
        }

        // Versión especial de AgregarBurbuja que devuelve el Label para que podamos actualizarlo
        private Label AgregarBurbujaStreaming()
        {
            int anchoDisponible = flowMensajes.ClientSize.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200;
            int anchoMaxBurbuja = (int)(anchoDisponible * 0.65);

            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Width = flowMensajes.ClientSize.Width - 30,
                Height = 50,
                Margin = new Padding(0, 5, 0, 10),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Avatar de Claude
            var avatar = new Panel
            {
                Size = new Size(40, 40),
                BackColor = Paleta.MoradoClaro,
                Margin = new Padding(0, 5, 10, 0)
            };
            var pathAvatar = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvatar.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAvatar);

            var lblInicial = new Label
            {
                Text = "C",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);

            var burbuja = new Panel
            {
                BackColor = Paleta.MoradoOscuro,
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            var flowInterno = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(25, 15, 25, 15),
                MaximumSize = new Size(anchoMaxBurbuja, 0),
                Margin = new Padding(0)
            };

            var lblMensaje = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoBlanco,
                AutoSize = true,
                MinimumSize = new Size(anchoMaxBurbuja - 60, 22),  // ← ancho mínimo desde el inicio
                MaximumSize = new Size(anchoMaxBurbuja - 60, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            flowInterno.Controls.Add(lblMensaje);

            var lblHora = new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 0)
            };
            flowInterno.Controls.Add(lblHora);

            burbuja.Controls.Add(flowInterno);

            burbuja.HandleCreated += (s, e) =>
            {
                burbuja.BeginInvoke(new Action(() =>
                {
                    if (burbuja.Width > 0 && burbuja.Height > 0)
                        Paleta.AplicarBordeRedondeadoSuave(burbuja, 18);
                }));
            };

            fila.Controls.Add(avatar);
            fila.Controls.Add(burbuja);

            fila.HandleCreated += (s, e) =>
            {
                fila.BeginInvoke(new Action(() =>
                {
                    fila.Height = Math.Max(burbuja.PreferredSize.Height, 50) + 10;
                }));
            };

            flowMensajes.Controls.Add(fila);

            return lblMensaje;   // el label para actualizarlo
        }

        private void TxtEntrada_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnEnviar_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnEnviar_Click(object? sender, EventArgs e)
        {
            string texto = txtEntrada.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("Escribe un mensaje antes de enviar.",
                    "Mensaje vacío", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_empresaIdActiva == null)
            {
                MessageBox.Show("Primero selecciona una empresa en la sección 'Empresas'.",
                    "Sin empresa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_conversacionActivaId == null)
            {
                MessageBox.Show("Selecciona una conversación primero.",
                    "Sin conversación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 1. Mostrar mensaje del usuario
            AgregarBurbuja("Usuario", texto, DateTime.Now);
            txtEntrada.Clear();
            ScrollAlFinal();

            // 2. Mostrar indicador "escribiendo..."
            MostrarIndicadorEscribiendo();

            btnEnviar.Enabled = false;
            txtEntrada.Enabled = false;

            Label? lblBurbujaIA = null;
            var respuestaAcumulada = new System.Text.StringBuilder();

            var timerActualizacion = new System.Windows.Forms.Timer { Interval = 100 };
            timerActualizacion.Tick += (s, ev) =>
            {
                if (lblBurbujaIA != null && respuestaAcumulada.Length > 0)
                {
                    lblBurbujaIA.Text = LimpiadorTexto.LimpiarMarkdown(respuestaAcumulada.ToString());
                    ScrollAlFinal();
                }
            };

            try
            {
                bool primerChunk = true;

                await foreach (var chunk in _gestorConv.EnviarMensajeUsuarioStream(_conversacionActivaId.Value, texto))
                {
                    if (primerChunk)
                    {
                        OcultarIndicadorEscribiendo();
                        lblBurbujaIA = AgregarBurbujaStreaming();
                        timerActualizacion.Start();
                        primerChunk = false;
                    }
                    respuestaAcumulada.Append(chunk);
                }

                // Stream terminado: detener timer y reemplazar burbuja temporal por definitiva
                timerActualizacion.Stop();

                if (lblBurbujaIA != null)
                {
                    Control? filaTemporal = lblBurbujaIA.Parent?.Parent?.Parent;
                    if (filaTemporal != null)
                    {
                        flowMensajes.Controls.Remove(filaTemporal);
                        filaTemporal.Dispose();
                    }
                    AgregarBurbuja("IA", respuestaAcumulada.ToString(), DateTime.Now);
                    ScrollAlFinal();
                }
            }
            catch (Exception ex)
            {
                timerActualizacion.Stop();
                OcultarIndicadorEscribiendo();
                MessageBox.Show($"Error al enviar mensaje: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                timerActualizacion.Dispose();
                btnEnviar.Enabled = true;
                txtEntrada.Enabled = true;
                txtEntrada.Focus();
            }
        }


        // UTILIDAD


        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            Paleta.AplicarBordeRedondeadoSuave(panel, radio);
        }
        private Panel? _filaEscribiendo = null;

        private void MostrarIndicadorEscribiendo()
        {
            int anchoDisponible = flowMensajes.ClientSize.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200;

            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Width = flowMensajes.ClientSize.Width - 30,
                Height = 60,
                Margin = new Padding(0, 5, 0, 10),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Avatar de Claude
            var avatar = new Panel
            {
                Size = new Size(40, 40),
                BackColor = Paleta.MoradoClaro,
                Margin = new Padding(0, 5, 10, 0)
            };
            var pathAvatar = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvatar.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAvatar);

            var lblInicial = new Label
            {
                Text = "C",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);

            // Burbuja con "Claude está escribiendo..."
            var burbuja = new Panel
            {
                BackColor = Paleta.MoradoOscuro,
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            var lblEscribiendo = new Label
            {
                Text = "Claude está escribiendo...",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Paleta.TextoBlanco,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(25, 15, 25, 15),
                Margin = new Padding(0)
            };
            burbuja.Controls.Add(lblEscribiendo);

            burbuja.HandleCreated += (s, e) =>
            {
                burbuja.BeginInvoke(new Action(() =>
                {
                    if (burbuja.Width > 0 && burbuja.Height > 0)
                    {
                        Paleta.AplicarBordeRedondeadoSuave(burbuja, 18);
                    }
                }));
            };

            fila.Controls.Add(avatar);
            fila.Controls.Add(burbuja);

            fila.HandleCreated += (s, e) =>
            {
                fila.BeginInvoke(new Action(() =>
                {
                    fila.Height = Math.Max(burbuja.PreferredSize.Height, 50) + 10;
                }));
            };

            flowMensajes.Controls.Add(fila);
            _filaEscribiendo = fila;

            ScrollAlFinal();   //reemplaza el ScrollControlIntoView
        }

        private void OcultarIndicadorEscribiendo()
        {
            if (_filaEscribiendo != null)
            {
                flowMensajes.Controls.Remove(_filaEscribiendo);
                _filaEscribiendo.Dispose();
                _filaEscribiendo = null;
            }
        }


        private void ScrollAlFinal()
        {
            if (flowMensajes.Controls.Count == 0) return;

            // Calcular la posición vertical máxima
            int alturaTotal = flowMensajes.Controls.OfType<Control>().Sum(c => c.Height + c.Margin.Vertical);

            // Forzar el scroll al valor máximo permitido
            flowMensajes.AutoScrollPosition = new Point(0, alturaTotal);
        }

        private async void BtnGenerarEvaluacion_Click(object? sender, EventArgs e)
        {
            if (_empresaIdActiva == null)
            {
                MessageBox.Show("Primero selecciona una empresa en la sección 'Empresas'.",
                    "Sin empresa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_conversacionActivaId == null)
            {
                MessageBox.Show("Primero inicia una conversación con la empresa.",
                    "Sin conversación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Confirmar
            var confirmacion = MessageBox.Show(
                "¿Generar una nueva evaluación considerando toda la conversación actual?\n\n" +
                "Esto tomará algunos segundos.",
                "Confirmar generación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes) return;

            // Deshabilitar controles
            btnNuevaConversacion.Enabled = false;
            btnNuevaConversacion.Text = "Generando...";
            btnEnviar.Enabled = false;
            txtEntrada.Enabled = false;

            try
            {
                var diagnosticoNuevo = await _gestorConv.RegenerarDiagnosticoFinal(_conversacionActivaId.Value);

                // Recargar las evaluaciones del panel lateral
                CargarEvaluacionesDeEmpresa();

                // Mostrar el modal con el diagnóstico recién generado
                MostrarModalDiagnostico(diagnosticoNuevo);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "No es posible aún",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la evaluación: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnNuevaConversacion.Enabled = true;
                btnNuevaConversacion.Text = "+ Generar evaluación";
                btnEnviar.Enabled = true;
                txtEntrada.Enabled = true;
                txtEntrada.Focus();
            }
        }
        private void MostrarModalDiagnostico(Modelos.Diagnostico diag)
        {
            using var modal = new Presentacion.FormDiagnostico(diag);
            modal.ShowDialog(this.FindForm());
        }
        private void OnEmpresaActivaCambio()
        {
            _empresaIdActiva = Estado.EstadoApp.EmpresaActivaId;
            _conversacionActivaId = null;   // Reset porque cambiamos de empresa
            CargarEvaluacionesDeEmpresa();
        }



    }
}