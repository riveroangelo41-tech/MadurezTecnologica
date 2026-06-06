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
        private int _empresaIdActiva = 1;  // TEMPORAL: hardcoded
        private int? _conversacionActivaId = null;
        private MadurezTecnologica.Logica.GestorConversacion _gestorConv = null!;
        private MadurezTecnologica.Datos.RepositorioConversacion _repoConv = null!;
        private MadurezTecnologica.Datos.RepositorioEmpresa _repoEmpresa = null!;

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

            ConfigurarControl();
            CrearPanelChat();
            CrearPanelConversaciones();
            CrearHeader();
            CargarConversacionesDeEmpresa();

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
                Text = "+ Nueva conversación",
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

        // ===================================================
        // PANEL DE CONVERSACIONES (izquierda)
        // ===================================================

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
                Text = "Conversaciones",
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
                PlaceholderText = "Buscar conversación..."
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

        // ===================================================
        // PANEL DE CHAT (centro/derecha)
        // ===================================================

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

        // ===================================================
        // CARGA DE CONVERSACIONES Y MENSAJES
        // ===================================================

        private void CargarConversacionesDeEmpresa()
        {
            flowConversaciones.SuspendLayout();
            flowConversaciones.Controls.Clear();

            try
            {
                var todas = _repoConv.ObtenerTodas();
                var conversaciones = todas.Where(c => c.EmpresaId == _empresaIdActiva)
                                          .OrderByDescending(c => c.FechaInicio)
                                          .ToList();

                if (conversaciones.Count == 0)
                {
                    var lblVacio = new Label
                    {
                        Text = "Aún no hay conversaciones para esta empresa.",
                        Font = new Font("Segoe UI", 9, FontStyle.Italic),
                        ForeColor = Paleta.TextoOscuro,
                        Size = new Size(280, 60),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    flowConversaciones.Controls.Add(lblVacio);
                    return;
                }

                foreach (var conv in conversaciones)
                {
                    var repoMensaje = new MadurezTecnologica.Datos.RepositorioMensaje();
                    var mensajes = repoMensaje.ObtenerPorConversacion(conv.Id);
                    string preview = mensajes.FirstOrDefault(m => m.Remitente == "Usuario")?.Contenido
                                  ?? mensajes.FirstOrDefault()?.Contenido
                                  ?? "(Sin mensajes)";
                    if (preview.Length > 60) preview = preview.Substring(0, 60) + "...";

                    string titulo = $"Conversación #{conv.Id}";
                    string hora = conv.FechaInicio.ToString("dd/MM HH:mm");

                    AgregarTarjetaConversacion(conv.Id, titulo, preview, hora, seleccionada: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar conversaciones: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                flowConversaciones.ResumeLayout(true);
            }
        }


        private void AgregarTarjetaConversacion(int conversacionId, string titulo, string preview, string hora, bool seleccionada)
        {
            Color colorFondo = seleccionada ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;
            Color colorTexto = Paleta.TextoBlanco;

            var tarjeta = new Panel
            {
                Size = new Size(280, 90),
                BackColor = colorFondo,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 10, 12, 10),
                Tag = conversacionId
            };

            var pathT = new System.Drawing.Drawing2D.GraphicsPath();
            int r = 18;
            pathT.AddArc(0, 0, r, r, 180, 90);
            pathT.AddArc(tarjeta.Width - r, 0, r, r, 270, 90);
            pathT.AddArc(tarjeta.Width - r, tarjeta.Height - r, r, r, 0, 90);
            pathT.AddArc(0, tarjeta.Height - r, r, r, 90, 90);
            pathT.CloseFigure();
            tarjeta.Region = new Region(pathT);

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = colorTexto,
                Location = new Point(12, 10),
                Size = new Size(255, 18),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitulo);

            var lblPreview = new Label
            {
                Text = preview,
                Font = new Font("Segoe UI", 8),
                ForeColor = colorTexto,
                Location = new Point(12, 32),
                Size = new Size(255, 30),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblPreview);

            var lblHora = new Label
            {
                Text = hora,
                Font = new Font("Segoe UI", 8),
                ForeColor = colorTexto,
                Location = new Point(180, 65),
                Size = new Size(90, 16),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            tarjeta.Controls.Add(lblHora);

            EventHandler clickHandler = (s, e) => SeleccionarConversacion(conversacionId);
            tarjeta.Click += clickHandler;
            lblTitulo.Click += clickHandler;
            lblPreview.Click += clickHandler;
            lblHora.Click += clickHandler;

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

            var empresa = _repoEmpresa.ObtenerPorId(_empresaIdActiva);
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
                flowMensajes.ResumeLayout(false);
                flowMensajes.Visible = true;        
                flowMensajes.PerformLayout();

                if (flowMensajes.Controls.Count > 0)
                {
                    flowMensajes.ScrollControlIntoView(flowMensajes.Controls[flowMensajes.Controls.Count - 1]);
                }
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

            // === Fila como FlowLayoutPanel (avatar + burbuja se ordenan solos) ===
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

            // === Avatar circular ===
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

            // === Burbuja ===
            var burbuja = new Panel
            {
                BackColor = colorFondo,
                Padding = new Padding(15, 12, 15, 12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MaximumSize = new Size(anchoMaxBurbuja, 0),
                Margin = new Padding(0)
            };

            var flowInterno = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            var lblMensaje = new Label
            {
                Text = contenido,
                Font = new Font("Segoe UI", 10),
                ForeColor = colorTexto,
                AutoSize = true,
                MaximumSize = new Size(anchoMaxBurbuja - 40, 0),
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

            // === Agregar al flow layout en el orden correcto ===
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

            if (_conversacionActivaId == null)
            {
                MessageBox.Show("Selecciona una conversación primero.",
                    "Sin conversación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AgregarBurbuja("Usuario", texto, DateTime.Now);
            txtEntrada.Clear();

            btnEnviar.Enabled = false;
            txtEntrada.Enabled = false;

            try
            {
                string respuesta = await _gestorConv.EnviarMensajeUsuario(_conversacionActivaId.Value, texto);
                AgregarBurbuja("IA", respuesta, DateTime.Now);

                if (flowMensajes.Controls.Count > 0)
                {
                    flowMensajes.ScrollControlIntoView(flowMensajes.Controls[flowMensajes.Controls.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar mensaje: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
    }
}