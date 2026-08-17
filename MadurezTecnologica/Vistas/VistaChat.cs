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
        private Estilos.IndicadorModoConexion panelIndicadorConexion = null!;
        private Label _lblTituloHeader = null!;
        private Label _lblSubtituloHeader = null!;
        private Panel _panelSugerenciasOffline = null!;
        private Panel _btnTogglePersonalizado = null!;
        private Label _lblTextoTogglePersonalizado = null!;
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
        private bool _enviandoMensaje = false;

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

                // Suscribirse a los eventos globales de estado
                Estado.EstadoApp.EmpresaActivaCambio += OnEmpresaActivaCambio;
                Estado.EstadoApp.HistorialCambio += OnHistorialCambio;

                this.BeginInvoke(new Action(() => CargarEvaluacionesDeEmpresa()));
            };

            // Desuscribirse cuando la vista se destruya para evitar memory leaks
            this.HandleDestroyed += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio -= OnEmpresaActivaCambio;
                Estado.EstadoApp.HistorialCambio -= OnHistorialCambio;
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

            _lblTituloHeader = new Label
            {
                Text = "Asistente de IA",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 15),
                Size = new Size(400, 30),
                BackColor = Color.Transparent
            };
            panelHeader.Controls.Add(_lblTituloHeader);

            _lblSubtituloHeader = new Label
            {
                Text = "Consulta, analiza y recibe recomendaciones inteligentes",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 48),
                Size = new Size(450, 20),
                BackColor = Color.Transparent
            };
            panelHeader.Controls.Add(_lblSubtituloHeader);

            // Suscribirse al evento global para actualizar el header al cambiar modo
            Inteligencia.DetectorConexion.ModoCambio += ActualizarHeaderSegunModo;
            HandleDestroyed += (s, e) => Inteligencia.DetectorConexion.ModoCambio -= ActualizarHeaderSegunModo;
            ActualizarHeaderSegunModo();

            panelIndicadorConexion = new Estilos.IndicadorModoConexion
            {
                Size = new Size(175, 36)
            };
            panelHeader.Controls.Add(panelIndicadorConexion);

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
            txtBuscarConversacion.TextChanged += (s, e) => FiltrarEvaluaciones(txtBuscarConversacion.Text);
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

            // Header dentro del panelChat (más profesional)
            var panelHeaderChat = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 0)
            };
            panelChat.Controls.Add(panelHeaderChat);

            // Línea inferior tipo separador
            panelHeaderChat.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 230, 225), 1);
                e.Graphics.DrawLine(pen, 0, panelHeaderChat.Height - 1,
                    panelHeaderChat.Width, panelHeaderChat.Height - 1);
            };

            // Mini avatar circular morado claro con inicial
            var avatarHeader = new Panel
            {
                Size = new Size(46, 46),
                Location = new Point(8, 12),
                BackColor = Paleta.MoradoClaro
            };
            var pathAvHd = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvHd.AddEllipse(0, 0, avatarHeader.Width, avatarHeader.Height);
            avatarHeader.Region = new Region(pathAvHd);
            panelHeaderChat.Controls.Add(avatarHeader);

            var lblInicialEmpresa = new Label
            {
                Text = "C",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatarHeader.Controls.Add(lblInicialEmpresa);

            lblHeaderEmpresa = new Label
            {
                Text = "Selecciona una conversación",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(64, 12),
                Size = new Size(600, 25),
                BackColor = Color.Transparent
            };
            panelHeaderChat.Controls.Add(lblHeaderEmpresa);

            lblHeaderInfo = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(130, 125, 120),
                Location = new Point(64, 39),
                Size = new Size(600, 18),
                BackColor = Color.Transparent
            };
            panelHeaderChat.Controls.Add(lblHeaderInfo);

            // === PANEL DE ENTRADA — versión pulida ===
            panelEntrada = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 95,
                BackColor = Color.White,
                Padding = new Padding(18, 14, 18, 14)
            };
            panelChat.Controls.Add(panelEntrada);

            // === PANEL DE SUGERENCIAS OFFLINE (chips arriba del input) ===
            _panelSugerenciasOffline = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 140,
                BackColor = Color.FromArgb(252, 250, 246),
                Padding = new Padding(0),
                Visible = false
            };

            // Borde superior sutil tipo separador
            _panelSugerenciasOffline.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 228, 218), 1);
                e.Graphics.DrawLine(pen, 0, 0, _panelSugerenciasOffline.Width, 0);
            };

            panelChat.Controls.Add(_panelSugerenciasOffline);

            CrearChipsSugeridosOffline();

            // Wrapper del textbox con borde dinámico (focus state)
            var panelTxtWrapper = new Panel
            {
                BackColor = Paleta.LilaInput,
                Dock = DockStyle.Fill,
                Padding = new Padding(50, 16, 60, 16)
            };
            panelEntrada.Controls.Add(panelTxtWrapper);

            Color bordeFocus = Paleta.MoradoOscuro;
            Color bordeNormal = Color.FromArgb(225, 220, 230);
            bool tieneFoco = false;

            panelTxtWrapper.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(tieneFoco ? bordeFocus : bordeNormal, tieneFoco ? 2 : 1);
                int inset = tieneFoco ? 1 : 0;
                g.DrawRectangle(pen, inset, inset,
                    panelTxtWrapper.Width - 1 - inset * 2,
                    panelTxtWrapper.Height - 1 - inset * 2);
            };

            panelTxtWrapper.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(panelTxtWrapper, 22);

            // Icono ✎ a la izquierda
            var lblIconoChat = new Label
            {
                Text = "✎",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(18, 18),
                Size = new Size(28, 28),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelTxtWrapper.Controls.Add(lblIconoChat);

            txtEntrada = new TextBox
            {
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Paleta.TextoOscuro,
                BackColor = Paleta.LilaInput,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Multiline = true,
                PlaceholderText = "Escribe tu mensaje... (Enter para enviar, Shift+Enter para nueva línea)"
            };
            txtEntrada.KeyDown += TxtEntrada_KeyDown;
            txtEntrada.GotFocus += (s, e) =>
            {
                tieneFoco = true;
                lblIconoChat.ForeColor = Paleta.MoradoClaro;
                panelTxtWrapper.Invalidate();
            };
            txtEntrada.LostFocus += (s, e) =>
            {
                tieneFoco = false;
                lblIconoChat.ForeColor = Paleta.MoradoOscuro;
                panelTxtWrapper.Invalidate();
            };
            panelTxtWrapper.Controls.Add(txtEntrada);
            lblIconoChat.BringToFront();

            // Contador de caracteres dentro del wrapper, alineado a la derecha
            var lblContador = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(140, 135, 130),
                AutoSize = false,
                Size = new Size(50, 20),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            panelTxtWrapper.Controls.Add(lblContador);
            lblContador.BringToFront();

            panelTxtWrapper.Resize += (s, e) =>
            {
                lblContador.Location = new Point(
                    panelTxtWrapper.Width - lblContador.Width - 14,
                    panelTxtWrapper.Height - lblContador.Height - 6);
            };

            txtEntrada.TextChanged += (s, e) =>
            {
                int len = txtEntrada.Text.Length;
                lblContador.Text = len.ToString();
                lblContador.ForeColor = len > 1500
                    ? Color.FromArgb(193, 63, 63)
                    : Color.FromArgb(140, 135, 130);
            };

            // === BOTÓN ENVIAR ===
            btnEnviar = new Button
            {
                Text = "Enviar",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 50),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                Margin = new Padding(12, 0, 0, 0)
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            btnEnviar.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 40, 90);
            btnEnviar.Click += BtnEnviar_Click;
            panelEntrada.Controls.Add(btnEnviar);
            btnEnviar.BringToFront();
            btnEnviar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnEnviar, 24);

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
                var conversacion = _repoConv.ObtenerUltimaPorEmpresa(_empresaIdActiva.Value);

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
                Estilos.MensajeApp.Error($"Error al cargar evaluaciones: {ex.Message}",
                    "Error", this.FindForm());
            }
            finally
            {
                flowConversaciones.ResumeLayout(true);
            }
        }


        private void FiltrarEvaluaciones(string filtro)
        {
            string busqueda = filtro?.Trim().ToLowerInvariant() ?? "";

            flowConversaciones.SuspendLayout();
            try
            {
                foreach (Control ctrl in flowConversaciones.Controls)
                {
                    if (ctrl is not Panel tarjeta) continue;

                    if (string.IsNullOrEmpty(busqueda))
                    {
                        tarjeta.Visible = true;
                        continue;
                    }

                    // Concatena el texto de todos los labels de la tarjeta
                    var textoTarjeta = string.Join(" ",
                        tarjeta.Controls.OfType<Label>().Select(l => l.Text ?? ""));

                    tarjeta.Visible = textoTarjeta.ToLowerInvariant().Contains(busqueda);
                }
            }
            finally
            {
                flowConversaciones.ResumeLayout(true);
            }
        }

        private void AgregarTarjetaDiagnostico(Modelos.Diagnostico diag)
        {
            // Vuelven los colores originales: morado para FINAL, verde para INTERMEDIO
            Color colorFondoCard = diag.EsFinal ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;
            Color colorFondoHover = diag.EsFinal
                ? Paleta.MoradoOscuroHover
                : Paleta.VerdeGrisaceoOscuro;
            Color colorAccentoBarra = diag.EsFinal
                ? Paleta.MoradoClaro
                : Paleta.VerdeBrillante;
            Color colorTextoPrincipal = Color.White;
            Color colorTextoSubtle = Color.FromArgb(220, 215, 210);

            var tarjeta = new Panel
            {
                Size = new Size(280, 110),
                BackColor = colorFondoCard,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                Tag = diag.Id
            };
            tarjeta.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(tarjeta, 12);

            // Pinta: barra acento lateral + sombra inferior sutil
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using var barBrush = new SolidBrush(colorAccentoBarra);
                g.FillRectangle(barBrush, 0, 14, 4, tarjeta.Height - 28);

                using var shadowBrush = new SolidBrush(Color.FromArgb(30, Color.Black));
                g.FillRectangle(shadowBrush, 2, tarjeta.Height - 2, tarjeta.Width - 4, 2);
            };

            // Línea 1: Fecha
            var lblFecha = new Label
            {
                Text = $"📅 {diag.FechaGeneracion:dd/MM/yyyy  ·  HH:mm}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = colorTextoPrincipal,
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            tarjeta.Controls.Add(lblFecha);

            // Badge FINAL/INTERMEDIO (estilo pill, fondo blanco translúcido)
            string textoBadge = diag.EsFinal ? "FINAL" : "INTERMEDIO";
            int anchoBadge = diag.EsFinal ? 50 : 75;
            var lblBadge = new Label
            {
                Text = textoBadge,
                Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
                ForeColor = colorFondoCard,
                BackColor = Color.White,
                Size = new Size(anchoBadge, 18),
                Location = new Point(280 - anchoBadge - 14, 13),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblBadge.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(lblBadge, 9);
            tarjeta.Controls.Add(lblBadge);

            // Línea 2: Nivel CMMI con destacado
            var lblNivel = new Label
            {
                Text = $"Nivel CMMI: {diag.NivelMadurez}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = colorTextoPrincipal,
                Location = new Point(16, 38),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            tarjeta.Controls.Add(lblNivel);

            // Línea 3: Resumen breve
            string resumen = diag.ResumenEmpresa.Split('.').FirstOrDefault()?.Trim() ?? diag.ResumenEmpresa;
            if (resumen.Length > 75) resumen = resumen.Substring(0, 75) + "…";

            var lblResumen = new Label
            {
                Text = resumen,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Italic),
                ForeColor = colorTextoSubtle,
                Location = new Point(16, 62),
                Size = new Size(250, 36),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                AutoEllipsis = true
            };
            tarjeta.Controls.Add(lblResumen);

            // Hover effect — se oscurece un poco
            tarjeta.MouseEnter += (s, e) => tarjeta.BackColor = colorFondoHover;
            tarjeta.MouseLeave += (s, e) =>
            {
                if (!tarjeta.ClientRectangle.Contains(tarjeta.PointToClient(Cursor.Position)))
                    tarjeta.BackColor = colorFondoCard;
            };
            foreach (Control child in new Control[] { lblFecha, lblNivel, lblResumen })
            {
                child.MouseEnter += (s, e) => tarjeta.BackColor = colorFondoHover;
                child.MouseLeave += (s, e) =>
                {
                    if (!tarjeta.ClientRectangle.Contains(tarjeta.PointToClient(Cursor.Position)))
                        tarjeta.BackColor = colorFondoCard;
                };
            }

            EventHandler clickHandler = (s, e) => MostrarModalDiagnostico(diag);
            tarjeta.Click += clickHandler;
            lblFecha.Click += clickHandler;
            lblNivel.Click += clickHandler;
            lblResumen.Click += clickHandler;
            lblBadge.Click += clickHandler;

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
                    // El mensaje "INFORME" (texto crudo del PDF) es solo contexto para la IA,
                    // no se muestra en el chat.
                    if (msg.Remitente == "INFORME") continue;

                    AgregarBurbuja(msg.Remitente, msg.Contenido, msg.Timestamp);
                }
            }
            catch (Exception ex)
            {
                Estilos.MensajeApp.Error($"Error cargando mensajes: {ex.Message}",
                    "Error", this.FindForm());
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
            Color colorTextoHora = esIA
                ? Color.FromArgb(200, 190, 230)
                : Color.FromArgb(210, 230, 220);
            Color colorTextoNombre = esIA
                ? Color.FromArgb(220, 210, 245)
                : Color.FromArgb(225, 240, 230);

            int anchoDisponible = flowMensajes.ClientSize.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200;
            int anchoMaxBurbuja = (int)(anchoDisponible * 0.72);

            // Fila como FlowLayoutPanel
            var fila = new FlowLayoutPanel
            {
                FlowDirection = esIA ? FlowDirection.LeftToRight : FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
                Width = flowMensajes.ClientSize.Width - 30,
                Height = 60,
                Margin = new Padding(0, 6, 0, 12),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Avatar circular con borde sutil
            var avatar = new Panel
            {
                Size = new Size(44, 44),
                BackColor = esIA ? Paleta.MoradoClaro : Paleta.VerdeGrisaceoOscuro,
                Margin = new Padding(0, 4, 12, 0)
            };
            var pathAvatar = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvatar.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAvatar);

            var lblInicial = new Label
            {
                Text = esIA ? "IA" : "Tú",
                Font = new Font("Segoe UI", esIA ? 11 : 11, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);

            // Burbuja
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
                Padding = new Padding(20, 12, 20, 12),
                MaximumSize = new Size(anchoMaxBurbuja, 0),
                Margin = new Padding(0)
            };

            // Nombre del remitente arriba del mensaje
            var lblNombre = new Label
            {
                Text = esIA ? "Asistente IA" : "Tú",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = colorTextoNombre,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            flowInterno.Controls.Add(lblNombre);

            var lblMensaje = new Label
            {
                Text = LimpiadorTexto.LimpiarMarkdown(contenido),
                Font = new Font("Segoe UI", 10),
                ForeColor = colorTexto,
                AutoSize = true,
                MaximumSize = new Size(anchoMaxBurbuja - 50, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            flowInterno.Controls.Add(lblMensaje);

            // Línea con hora + checkmark sutil
            var panelMeta = new FlowLayoutPanel
            {
                FlowDirection = esIA ? FlowDirection.LeftToRight : FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(0)
            };

            var lblHora = new Label
            {
                Text = $"🕐 {timestamp:HH:mm}",
                Font = new Font("Segoe UI", 7),
                ForeColor = colorTextoHora,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            panelMeta.Controls.Add(lblHora);

            if (!esIA)
            {
                var lblCheck = new Label
                {
                    Text = "  ✓",
                    Font = new Font("Segoe UI", 7, FontStyle.Bold),
                    ForeColor = colorTextoHora,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0)
                };
                panelMeta.Controls.Add(lblCheck);
            }

            flowInterno.Controls.Add(panelMeta);

            burbuja.Controls.Add(flowInterno);

            // Aplicar bordes redondeados — diferentes en cada esquina según remitente
            burbuja.HandleCreated += (s, e) =>
            {
                burbuja.BeginInvoke(new Action(() =>
                {
                    if (burbuja.Width > 0 && burbuja.Height > 0)
                    {
                        AplicarBurbujaConCola(burbuja, esIA);
                    }
                }));
            };

            burbuja.Resize += (s, e) =>
            {
                if (burbuja.Width > 0 && burbuja.Height > 0)
                    AplicarBurbujaConCola(burbuja, esIA);
            };

            fila.Controls.Add(avatar);
            fila.Controls.Add(burbuja);

            fila.HandleCreated += (s, e) =>
            {
                fila.BeginInvoke(new Action(() =>
                {
                    fila.Height = Math.Max(burbuja.PreferredSize.Height, 50) + 12;
                }));
            };

            flowMensajes.Controls.Add(fila);
        }

        // Da forma de burbuja de chat: una esquina "puntiaguda" en el lado del avatar
        private void AplicarBurbujaConCola(Panel burbuja, bool esIA)
        {
            int radio = 18;
            int radioPequeno = 6;

            var path = new System.Drawing.Drawing2D.GraphicsPath();

            if (esIA)
            {
                // IA está a la izquierda → esquina superior-izquierda más pequeña (cola)
                path.AddArc(0, 0, radioPequeno, radioPequeno, 180, 90);
                path.AddArc(burbuja.Width - radio, 0, radio, radio, 270, 90);
                path.AddArc(burbuja.Width - radio, burbuja.Height - radio, radio, radio, 0, 90);
                path.AddArc(0, burbuja.Height - radio, radio, radio, 90, 90);
            }
            else
            {
                // Usuario está a la derecha → esquina superior-derecha más pequeña (cola)
                path.AddArc(0, 0, radio, radio, 180, 90);
                path.AddArc(burbuja.Width - radioPequeno, 0, radioPequeno, radioPequeno, 270, 90);
                path.AddArc(burbuja.Width - radio, burbuja.Height - radio, radio, radio, 0, 90);
                path.AddArc(0, burbuja.Height - radio, radio, radio, 90, 90);
            }
            path.CloseFigure();
            burbuja.Region = new Region(path);
        }

        // Versión especial de AgregarBurbuja que devuelve el Label para que podamos actualizarlo
        private Label AgregarBurbujaStreaming()
        {
            int anchoDisponible = flowMensajes.ClientSize.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200;
            int anchoMaxBurbuja = (int)(anchoDisponible * 0.72);

            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Width = flowMensajes.ClientSize.Width - 30,
                Height = 60,
                Margin = new Padding(0, 6, 0, 12),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var avatar = new Panel
            {
                Size = new Size(44, 44),
                BackColor = Paleta.MoradoClaro,
                Margin = new Padding(0, 4, 12, 0)
            };
            var pathAvatar = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvatar.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAvatar);

            var lblInicial = new Label
            {
                Text = "IA",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
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
                Padding = new Padding(20, 12, 20, 12),
                MaximumSize = new Size(anchoMaxBurbuja, 0),
                Margin = new Padding(0)
            };

            // Nombre "Asistente IA"
            var lblNombre = new Label
            {
                Text = "Asistente IA",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 210, 245),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            flowInterno.Controls.Add(lblNombre);

            // Label del mensaje (vacío al inicio, se llena con el streaming)
            var lblMensaje = new Label
            {
                Text = "▍",   // cursor parpadeante inicial
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoBlanco,
                AutoSize = true,
                MinimumSize = new Size(60, 22),
                MaximumSize = new Size(anchoMaxBurbuja - 50, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            flowInterno.Controls.Add(lblMensaje);

            // Hora con ícono
            var lblHora = new Label
            {
                Text = $"🕐 {DateTime.Now:HH:mm}",
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.FromArgb(200, 190, 230),
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
                        AplicarBurbujaConCola(burbuja, esIA: true);
                }));
            };
            burbuja.Resize += (s, e) =>
            {
                if (burbuja.Width > 0 && burbuja.Height > 0)
                    AplicarBurbujaConCola(burbuja, esIA: true);
            };

            fila.Controls.Add(avatar);
            fila.Controls.Add(burbuja);

            fila.HandleCreated += (s, e) =>
            {
                fila.BeginInvoke(new Action(() =>
                {
                    fila.Height = Math.Max(burbuja.PreferredSize.Height, 50) + 12;
                }));
            };

            flowMensajes.Controls.Add(fila);

            return lblMensaje;
        }

        private void TxtEntrada_KeyDown(object? sender, KeyEventArgs e)
        {
            // Enter solo → enviar mensaje
            // Shift+Enter → salto de línea (comportamiento por defecto del TextBox multiline)
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                if (_enviandoMensaje) return;   // bloquear envíos múltiples
                BtnEnviar_Click(sender, EventArgs.Empty);
            }
        }

        private void EstablecerEstadoEnvio(bool enviando)
        {
            _enviandoMensaje = enviando;

            if (enviando)
            {
                btnEnviar.Text = "⏳ Esperando...";
                btnEnviar.BackColor = Color.FromArgb(155, 145, 175);
                btnEnviar.Cursor = Cursors.Default;
                btnEnviar.Enabled = false;

                txtEntrada.Enabled = false;
                txtEntrada.BackColor = Color.FromArgb(245, 243, 248);
                txtEntrada.PlaceholderText = "Esperando respuesta del Asistente IA...";
            }
            else
            {
                btnEnviar.Text = "Enviar";
                btnEnviar.BackColor = Paleta.MoradoOscuro;
                btnEnviar.Cursor = Cursors.Hand;
                btnEnviar.Enabled = true;

                txtEntrada.Enabled = true;
                txtEntrada.BackColor = Paleta.LilaInput;
                txtEntrada.PlaceholderText = "Escribe tu mensaje... (Enter para enviar, Shift+Enter para nueva línea)";
                txtEntrada.Focus();
            }
        }

        private async void BtnEnviar_Click(object? sender, EventArgs e)
        {
            // Bloqueo de envíos múltiples mientras la IA está respondiendo
            if (_enviandoMensaje) return;

            string texto = txtEntrada.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                Estilos.MensajeApp.Advertencia("Escribe un mensaje antes de enviar.",
                    "Mensaje vacío", this.FindForm());
                return;
            }

            if (_empresaIdActiva == null)
            {
                Estilos.MensajeApp.Info("Primero selecciona una empresa en la sección 'Empresas'.",
                    "Sin empresa", this.FindForm());
                return;
            }

            if (_conversacionActivaId == null)
            {
                Estilos.MensajeApp.Info("Selecciona una conversación primero.",
                    "Sin conversación", this.FindForm());
                return;
            }

            // 1. Mostrar mensaje del usuario
            AgregarBurbuja("Usuario", texto, DateTime.Now);
            txtEntrada.Clear();
            ScrollAlFinal();

            // 2. Mostrar indicador "escribiendo..." y bloquear UI
            MostrarIndicadorEscribiendo();
            EstablecerEstadoEnvio(true);

            Label? lblBurbujaIA = null;
            var respuestaAcumulada = new System.Text.StringBuilder();

            // Timer CONTINUO que scrollea cada 80ms mientras dura el stream.
            // Forza el recálculo en CASCADA: burbuja → fila → flowMensajes,
            // y solo después scrollea. Sin esto, el FlowLayoutPanel padre
            // no sabe la altura real de las filas y el scroll queda corto.
            var timerScrollContinuo = new System.Windows.Forms.Timer { Interval = 80 };
            timerScrollContinuo.Tick += (s, ev) =>
            {
                if (lblBurbujaIA != null)
                {
                    // Cadena: lblBurbujaIA → flowInterno → burbuja → fila
                    var burbuja = lblBurbujaIA.Parent?.Parent;
                    var fila = burbuja?.Parent;

                    if (burbuja != null && fila != null)
                    {
                        burbuja.PerformLayout();
                        int alturaNecesaria = burbuja.PreferredSize.Height + 12;
                        if (fila.Height != alturaNecesaria)
                            fila.Height = alturaNecesaria;
                    }
                }

                flowMensajes.PerformLayout();
                flowMensajes.AutoScrollPosition = new Point(0, int.MaxValue);
            };

            // Timer del cursor parpadeante mientras llega el stream
            var timerCursor = new System.Windows.Forms.Timer { Interval = 500 };
            bool cursorVisible = true;
            timerCursor.Tick += (s, ev) =>
            {
                if (lblBurbujaIA == null) return;
                cursorVisible = !cursorVisible;
                string textoActual = LimpiadorTexto.LimpiarMarkdown(respuestaAcumulada.ToString());
                lblBurbujaIA.Text = cursorVisible ? textoActual + " ▍" : textoActual + "  ";
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
                        timerCursor.Start();
                        timerScrollContinuo.Start();
                        primerChunk = false;
                    }

                    respuestaAcumulada.Append(chunk);

                    // Actualizar el texto INMEDIATAMENTE (no esperar a un timer)
                    if (lblBurbujaIA != null)
                    {
                        string textoLimpio = LimpiadorTexto.LimpiarMarkdown(respuestaAcumulada.ToString());
                        lblBurbujaIA.Text = textoLimpio + (cursorVisible ? " ▍" : "  ");
                    }
                }

                // Stream terminado: detener timers y dejar el texto final SIN cursor
                timerCursor.Stop();
                timerScrollContinuo.Stop();

                if (lblBurbujaIA != null)
                {
                    lblBurbujaIA.Text = LimpiadorTexto.LimpiarMarkdown(respuestaAcumulada.ToString());

                    // Scroll FINAL — forzar layout completo y scrollear con reintentos
                    flowMensajes.PerformLayout();
                    ScrollAlFinal();
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación: si fue por caída de red, avisar; si no, silencioso.
                timerCursor.Stop();
                timerScrollContinuo.Stop();
                OcultarIndicadorEscribiendo();

                if (!Inteligencia.DetectorConexion.HayConexion)
                {
                    Estilos.MensajeApp.Advertencia(
                        "Se perdió la conexión a internet durante la respuesta.\n\n" +
                        "El sistema pasó a modo offline. Vuelve a enviar tu mensaje y se " +
                        "responderá con el motor local, o espera a que regrese la conexión.",
                        "Conexión perdida", this.FindForm());
                }
            }
            catch (Inteligencia.VpnRequeridaException)
            {
                timerCursor.Stop();
                timerScrollContinuo.Stop();
                OcultarIndicadorEscribiendo();
                Estilos.MensajeApp.Advertencia(
                    "🔒 La VPN está apagada.\n\n" +
                    "El asistente de IA no está disponible en tu región sin la VPN. " +
                    "Enciéndela e intenta enviar tu mensaje de nuevo.\n\n" +
                    "Mientras tanto, puedes activar el modo offline (indicador del header) " +
                    "para responder con el motor local.",
                    "Se requiere VPN", this.FindForm());
            }
            catch (Exception ex)
            {
                timerCursor.Stop();
                timerScrollContinuo.Stop();
                OcultarIndicadorEscribiendo();
                Estilos.MensajeApp.Error($"Error al enviar mensaje: {ex.Message}",
                    "Error", this.FindForm());
            }
            finally
            {
                timerCursor.Dispose();
                timerScrollContinuo.Dispose();
                EstablecerEstadoEnvio(false);
            }
        }


        // UTILIDAD


        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            Paleta.AplicarBordeRedondeadoSuave(panel, radio);
        }
        private Panel? _filaEscribiendo = null;

        private System.Windows.Forms.Timer? _timerPuntosEscribiendo = null;

        private void MostrarIndicadorEscribiendo()
        {
            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Width = flowMensajes.ClientSize.Width - 30,
                Height = 60,
                Margin = new Padding(0, 6, 0, 12),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Avatar de la IA — mismo estilo que las burbujas reales
            var avatar = new Panel
            {
                Size = new Size(44, 44),
                BackColor = Paleta.MoradoClaro,
                Margin = new Padding(0, 4, 12, 0)
            };
            var pathAvatar = new System.Drawing.Drawing2D.GraphicsPath();
            pathAvatar.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAvatar);

            var lblInicial = new Label
            {
                Text = "IA",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);

            // Burbuja con animación de puntos
            var burbuja = new Panel
            {
                BackColor = Paleta.MoradoOscuro,
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            var contenedor = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(22, 14, 22, 14),
                Margin = new Padding(0)
            };

            var lblEscribiendo = new Label
            {
                Text = "Asistente IA está escribiendo",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 210, 245),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0)
            };
            contenedor.Controls.Add(lblEscribiendo);

            // Label de los 3 puntos animados
            var lblPuntos = new Label
            {
                Text = "   ●",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0)
            };
            contenedor.Controls.Add(lblPuntos);

            burbuja.Controls.Add(contenedor);

            burbuja.HandleCreated += (s, e) =>
            {
                burbuja.BeginInvoke(new Action(() =>
                {
                    if (burbuja.Width > 0 && burbuja.Height > 0)
                        AplicarBurbujaConCola(burbuja, esIA: true);
                }));
            };
            burbuja.Resize += (s, e) =>
            {
                if (burbuja.Width > 0 && burbuja.Height > 0)
                    AplicarBurbujaConCola(burbuja, esIA: true);
            };

            fila.Controls.Add(avatar);
            fila.Controls.Add(burbuja);

            fila.HandleCreated += (s, e) =>
            {
                fila.BeginInvoke(new Action(() =>
                {
                    fila.Height = Math.Max(burbuja.PreferredSize.Height, 50) + 12;
                }));
            };

            flowMensajes.Controls.Add(fila);
            _filaEscribiendo = fila;

            // Animar los puntos: ● → ● ● → ● ● ● → repeat
            _timerPuntosEscribiendo?.Stop();
            _timerPuntosEscribiendo?.Dispose();
            _timerPuntosEscribiendo = new System.Windows.Forms.Timer { Interval = 400 };
            int fase = 0;
            _timerPuntosEscribiendo.Tick += (s, e) =>
            {
                fase = (fase + 1) % 3;
                lblPuntos.Text = fase switch
                {
                    0 => "   ●",
                    1 => "   ● ●",
                    _ => "   ● ● ●"
                };
            };
            _timerPuntosEscribiendo.Start();

            ScrollAlFinal();
        }

        private void OcultarIndicadorEscribiendo()
        {
            _timerPuntosEscribiendo?.Stop();
            _timerPuntosEscribiendo?.Dispose();
            _timerPuntosEscribiendo = null;

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

            // Las burbujas usan AutoSize + HandleCreated + BeginInvoke,
            // así que sus alturas se calculan en varios ticks del message loop.
            // Usamos un Timer con varios reintentos para asegurar que cuando
            // hagamos el scroll, TODAS las burbujas ya tengan su altura final.
            int intentos = 0;
            int maxIntentos = 8;
            var timer = new System.Windows.Forms.Timer { Interval = 30 };

            timer.Tick += (s, e) =>
            {
                EjecutarScrollAlFinal();
                intentos++;
                if (intentos >= maxIntentos)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void EjecutarScrollAlFinal()
        {
            if (flowMensajes.Controls.Count == 0) return;

            // Forzar refresh del layout para que las alturas estén actualizadas
            flowMensajes.PerformLayout();

            // Calculamos el scroll máximo usando el VerticalScroll del FlowLayoutPanel
            int scrollMax = flowMensajes.VerticalScroll.Maximum;

            // Backup: si VerticalScroll no está actualizado, usamos Bottom del último control
            var ultimo = flowMensajes.Controls[flowMensajes.Controls.Count - 1];
            int yObjetivoFallback = ultimo.Bottom + 100;

            int yFinal = Math.Max(scrollMax, yObjetivoFallback);

            flowMensajes.AutoScrollPosition = new Point(0, yFinal);
        }

        private async void BtnGenerarEvaluacion_Click(object? sender, EventArgs e)
        {
            if (_empresaIdActiva == null)
            {
                Estilos.MensajeApp.Info("Primero selecciona una empresa en la sección 'Empresas'.",
                    "Sin empresa", this.FindForm());
                return;
            }

            if (_conversacionActivaId == null)
            {
                Estilos.MensajeApp.Info("Primero inicia una conversación con la empresa.",
                    "Sin conversación", this.FindForm());
                return;
            }

            bool confirmado = Estilos.MensajeApp.Confirmar(
                "¿Generar una nueva evaluación considerando toda la conversación actual?\n\n" +
                "Esto tomará algunos segundos.",
                "Confirmar generación",
                this.FindForm());

            if (!confirmado) return;

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
                Estilos.MensajeApp.Info(ex.Message, "No es posible aún", this.FindForm());
            }
            catch (Inteligencia.VpnRequeridaException)
            {
                Estilos.MensajeApp.Advertencia(
                    "🔒 La VPN está apagada.\n\n" +
                    "Generar la evaluación con IA no está disponible en tu región sin la VPN. " +
                    "Enciéndela e inténtalo de nuevo.\n\n" +
                    "También puedes activar el modo offline (indicador del header) para generarla " +
                    "con el motor local.",
                    "Se requiere VPN", this.FindForm());
            }
            catch (Exception ex)
            {
                Estilos.MensajeApp.Error($"Error al generar la evaluación: {ex.Message}",
                    "Error", this.FindForm());
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
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(20, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblTipo);

            // Subtítulo con fecha + preview del resumen
            string primerFrase = diag.ResumenEmpresa.Split('.').FirstOrDefault()?.Trim() ?? "";
            if (primerFrase.Length > 50) primerFrase = primerFrase[..50] + "…";
            var lblFechaHeader = new Label
            {
                Text = $"📅 {diag.FechaGeneracion:dd/MM/yyyy · HH:mm}   ·   {primerFrase}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(195, 190, 220),
                Location = new Point(22, 46),
                Size = new Size(form.Width - 150, 18),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblFechaHeader);

            // Badge "Nivel X"
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

            // Botón cerrar
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

            // Colores por sección (mismos que VistaHistorial para consistencia)
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

            // Fondo de la tarjeta: blanco con toque sutil del color de acento
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

            // Barra de acento lateral izquierda
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(colorAccento);
                e.Graphics.FillRectangle(brush, 0, 10, 4, card.Height - 20);
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
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
        private void ActualizarHeaderSegunModo()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ActualizarHeaderSegunModo));
                return;
            }

            if (_lblTituloHeader == null || _lblSubtituloHeader == null) return;

            // Offline efectivo: forzado por el usuario O sin conexión detectada.
            bool offline = Inteligencia.DetectorConexion.EstaOffline();

            if (offline)
            {
                _lblTituloHeader.Text = "🔌 Asistente Offline";
                _lblSubtituloHeader.Text = "Modo offline — respuestas básicas por plantillas locales";
                _lblTituloHeader.ForeColor = Paleta.VerdeGrisaceoOscuro;
            }
            else
            {
                _lblTituloHeader.Text = "Asistente de IA";
                _lblSubtituloHeader.Text = "Consulta, analiza y recibe recomendaciones inteligentes";
                _lblTituloHeader.ForeColor = Paleta.TextoOscuro;
            }

            // Mostrar/ocultar el panel de sugerencias offline
            if (_panelSugerenciasOffline != null)
                _panelSugerenciasOffline.Visible = offline;

            // En modo offline: ocultar la barra de escribir por defecto (queda solo los chips)
            // En modo online: la barra siempre está visible
            if (panelEntrada != null)
                panelEntrada.Visible = !offline;

            // Resetear el botón toggle al estado inicial
            if (_lblTextoTogglePersonalizado != null)
                _lblTextoTogglePersonalizado.Text = "✎  Escribir personalizado";
        }

        private void AlternarBarraEscribir()
        {
            if (panelEntrada == null || _lblTextoTogglePersonalizado == null) return;

            bool estaVisible = panelEntrada.Visible;
            panelEntrada.Visible = !estaVisible;

            _lblTextoTogglePersonalizado.Text = panelEntrada.Visible
                ? "✕  Ocultar barra"
                : "✎  Escribir personalizado";

            if (panelEntrada.Visible)
                txtEntrada?.Focus();
        }

        private void CrearChipsSugeridosOffline()
        {
            // === HEADER con icono naranja + título descriptivo ===
            var headerSugerencias = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.Transparent,
                Padding = new Padding(22, 12, 22, 0)
            };
            _panelSugerenciasOffline.Controls.Add(headerSugerencias);

            var iconoCirculo = new Panel
            {
                Size = new Size(24, 24),
                Location = new Point(20, 8),
                BackColor = Paleta.VerdeGrisaceoOscuro
            };
            var pathIcon = new System.Drawing.Drawing2D.GraphicsPath();
            pathIcon.AddEllipse(0, 0, 24, 24);
            iconoCirculo.Region = new Region(pathIcon);
            var lblIcon = new Label
            {
                Text = "💡",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconoCirculo.Controls.Add(lblIcon);
            headerSugerencias.Controls.Add(iconoCirculo);

            var lblTitulo = new Label
            {
                Text = "PREGUNTAS SUGERIDAS",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Paleta.VerdeGrisaceoOscuro,
                Location = new Point(54, 13),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            headerSugerencias.Controls.Add(lblTitulo);

            var lblHint = new Label
            {
                Text = "Clic en cualquier chip para enviarlo al asistente offline",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(125, 135, 132),
                Location = new Point(212, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            headerSugerencias.Controls.Add(lblHint);

            // === BOTÓN TOGGLE "ESCRIBIR PERSONALIZADO" ===
            _btnTogglePersonalizado = new Panel
            {
                Size = new Size(180, 28),
                BackColor = Paleta.VerdeGrisaceoOscuro,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnTogglePersonalizado.Resize += (s, e) =>
                Paleta.AplicarBordeRedondeadoSuave(_btnTogglePersonalizado, 14);

            _lblTextoTogglePersonalizado = new Label
            {
                Text = "✎  Escribir personalizado",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _btnTogglePersonalizado.Controls.Add(_lblTextoTogglePersonalizado);

            Color colorBtnNormal = Paleta.VerdeGrisaceoOscuro;
            Color colorBtnHover = Color.FromArgb(60, 95, 88);
            Color colorBtnPress = Color.FromArgb(45, 75, 70);

            _btnTogglePersonalizado.MouseEnter += (s, e) => _btnTogglePersonalizado.BackColor = colorBtnHover;
            _btnTogglePersonalizado.MouseLeave += (s, e) => _btnTogglePersonalizado.BackColor = colorBtnNormal;
            _lblTextoTogglePersonalizado.MouseEnter += (s, e) => _btnTogglePersonalizado.BackColor = colorBtnHover;
            _lblTextoTogglePersonalizado.MouseLeave += (s, e) => _btnTogglePersonalizado.BackColor = colorBtnNormal;
            _btnTogglePersonalizado.MouseDown += (s, e) => _btnTogglePersonalizado.BackColor = colorBtnPress;
            _btnTogglePersonalizado.MouseUp += (s, e) => _btnTogglePersonalizado.BackColor = colorBtnHover;

            EventHandler toggleClick = (s, e) => AlternarBarraEscribir();
            _btnTogglePersonalizado.Click += toggleClick;
            _lblTextoTogglePersonalizado.Click += toggleClick;

            headerSugerencias.Controls.Add(_btnTogglePersonalizado);
            headerSugerencias.Resize += (s, e) =>
            {
                _btnTogglePersonalizado.Location = new Point(
                    headerSugerencias.Width - _btnTogglePersonalizado.Width - 20, 4);
            };
            // Posición inicial
            _btnTogglePersonalizado.Location = new Point(
                headerSugerencias.Width - _btnTogglePersonalizado.Width - 20, 4);

            // === FLOW CON LOS CHIPS ===
            var flowChips = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false,
                Padding = new Padding(22, 8, 22, 12)
            };
            _panelSugerenciasOffline.Controls.Add(flowChips);
            flowChips.BringToFront();

            // (texto del chip, pregunta que se envía al chat, icono)
            var sugerencias = new (string label, string pregunta, string icono)[]
            {
                ("Mi nivel CMMI",      "¿Cuál es mi nivel CMMI?",          "📊"),
                ("Fortalezas",         "¿Cuáles son mis fortalezas?",      "✅"),
                ("Debilidades",        "¿Cuáles son mis debilidades?",     "⚠️"),
                ("Riesgos",            "¿Qué riesgos tengo?",              "🔴"),
                ("Recomendaciones",    "¿Qué recomendaciones tengo?",      "💡"),
                ("Resumen",            "Hazme un resumen del diagnóstico", "📄"),
                ("Ayuda",              "¿Qué puedo preguntarte?",          "❓")
            };

            foreach (var (label, pregunta, icono) in sugerencias)
            {
                var chip = CrearChipSugerido(icono, label, pregunta);
                flowChips.Controls.Add(chip);
            }
        }

        private Panel CrearChipSugerido(string icono, string texto, string preguntaAEnviar)
        {
            Color colorBorde = Color.FromArgb(165, 190, 183);
            Color colorBordeHover = Paleta.VerdeGrisaceoOscuro;
            Color colorFondoNormal = Color.White;
            Color colorFondoHover = Color.FromArgb(228, 242, 238);
            Color colorFondoPress = Color.FromArgb(198, 224, 216);
            Color colorTexto = Color.FromArgb(50, 70, 65);
            Color colorTextoHover = Paleta.VerdeGrisaceoOscuro;

            int radio = 20;

            // Label con icono + texto
            var lblContenido = new Label
            {
                Text = $"{icono}   {texto}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colorTexto,
                AutoSize = true,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Padding = new Padding(20, 10, 22, 10)
            };

            // Estado actual (capturado por closures)
            Color fondoActual = colorFondoNormal;
            Color bordeActual = colorBorde;

            // Chip wrapper — el BackColor coincide con el del FlowChips padre para que
            // las esquinas "fuera del path" se vean transparentes contra el fondo crema.
            var chip = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(252, 250, 246),  // mismo color del padre
                Margin = new Padding(0, 0, 10, 10),
                Cursor = Cursors.Hand,
                Padding = new Padding(0)
            };

            System.Drawing.Drawing2D.GraphicsPath ConstruirPath(int w, int h)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                int r = Math.Min(radio, Math.Min(w, h) / 2);
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(w - r - 1, 0, r, r, 270, 90);
                path.AddArc(w - r - 1, h - r - 1, r, r, 0, 90);
                path.AddArc(0, h - r - 1, r, r, 90, 90);
                path.CloseFigure();
                return path;
            }

            // Paint dibuja TODO: fondo redondeado + borde.
            // Sin Region → sin colapso de AutoSize.
            chip.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using var path = ConstruirPath(chip.Width, chip.Height);
                using var brushFondo = new SolidBrush(fondoActual);
                g.FillPath(brushFondo, path);
                using var pen = new Pen(bordeActual, 1.5f);
                g.DrawPath(pen, path);
            };

            chip.Controls.Add(lblContenido);

            void AplicarHover()
            {
                fondoActual = colorFondoHover;
                bordeActual = colorBordeHover;
                lblContenido.ForeColor = colorTextoHover;
                chip.Invalidate();
            }
            void QuitarHover()
            {
                if (chip.ClientRectangle.Contains(chip.PointToClient(Cursor.Position))) return;
                fondoActual = colorFondoNormal;
                bordeActual = colorBorde;
                lblContenido.ForeColor = colorTexto;
                chip.Invalidate();
            }
            void AplicarPress()
            {
                fondoActual = colorFondoPress;
                chip.Invalidate();
            }

            foreach (Control ctrl in new Control[] { chip, lblContenido })
            {
                ctrl.MouseEnter += (s, e) => AplicarHover();
                ctrl.MouseLeave += (s, e) => QuitarHover();
                ctrl.MouseDown += (s, e) => AplicarPress();
                ctrl.MouseUp += (s, e) => AplicarHover();
                ctrl.Click += (s, e) => EnviarSugerencia(preguntaAEnviar);
            }

            return chip;
        }

        private void EnviarSugerencia(string pregunta)
        {
            if (_enviandoMensaje) return;
            if (_empresaIdActiva == null || _conversacionActivaId == null) return;

            txtEntrada.Text = pregunta;
            BtnEnviar_Click(this, EventArgs.Empty);
        }

        private void OnEmpresaActivaCambio()
        {
            _empresaIdActiva = Estado.EstadoApp.EmpresaActivaId;
            _conversacionActivaId = null;   // Reset porque cambiamos de empresa
            CargarEvaluacionesDeEmpresa();
        }

        // Se dispara cuando se elimina historial en otra vista. Recarga las evaluaciones
        // de la empresa activa para reflejar el cambio al instante (sin cambiar de empresa).
        private void OnHistorialCambio()
        {
            // No interrumpir un envío/streaming en curso (rompería la burbuja en progreso).
            if (_enviandoMensaje) return;

            _empresaIdActiva = Estado.EstadoApp.EmpresaActivaId;
            _conversacionActivaId = null;   // la conversación pudo haberse eliminado
            CargarEvaluacionesDeEmpresa();
        }



    }
}