using System.Text;
using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Vistas
{
    public partial class VistaHistorial : UserControl
    {
        // === CONTROLES PRINCIPALES ===
        private Panel _panelHeader = null!;
        private Estilos.IndicadorModoConexion _indicadorConexion = null!;
        private Panel _panelContenido = null!;
        private Label _lblTitulo = null!;
        private Label _lblSubtitulo = null!;

        // === BANNER CON BOTONES (una sola fila) ===
        private Panel _panelBanner = null!;
        private Label _lblNombreEmpresa = null!;
        private Label _lblDetallesEmpresa = null!;
        private Label _lblInicialEmpresa = null!;
        private Panel _btnIrAlChat = null!;
        private Label _lblBtnIrAlChat = null!;
        private Panel _btnExportar = null!;
        private Label _lblBtnExportar = null!;
        private Panel _btnEliminar = null!;
        private Label _lblBtnEliminar = null!;
        private bool _botonesHabilitados = false;

        private static readonly Color RojoSuave = ColorTranslator.FromHtml("#C13F3F");
        private static readonly Color RojoOscuro = ColorTranslator.FromHtml("#A02828");
        private static readonly Color GrisDeshabilitado = Color.FromArgb(200, 200, 200);

        // === ESTADÍSTICAS (grid 2×2 de tarjetas) ===
        private Panel _panelEstadisticas = null!;
        private Panel _cardDiagnosticos = null!;
        private Panel _cardMensajes = null!;
        private Panel _cardInicio = null!;
        private Panel _cardActividad = null!;
        private Label _lblValorDiagnosticos = null!;
        private Label _lblSubDiagnosticos = null!;
        private Label _lblValorMensajes = null!;
        private Label _lblSubMensajes = null!;
        private Label _lblValorInicio = null!;
        private Label _lblSubInicio = null!;
        private Label _lblValorActividad = null!;
        private Label _lblSubActividad = null!;

        private static readonly Color CardBgNormal = Color.FromArgb(248, 246, 252);
        private static readonly Color CardBgHover = Color.FromArgb(237, 232, 247);

        // === FILTROS TEMPORALES ===
        private Panel _panelFiltros = null!;
        private Panel _chipTodo = null!;
        private Panel _chipSemana = null!;
        private Panel _chipMes = null!;
        private Panel _chipPersonalizado = null!;
        private string _filtroActivo = "todo";
        private Panel _panelRangoFechas = null!;
        private DateTimePicker _dtpDesde = null!;
        private DateTimePicker _dtpHasta = null!;

        // === TIMELINE VERTICAL ===
        private Panel _panelTimeline = null!;
        private Label _lblSinTimeline = null!;
        private List<Panel> _tarjetasTimeline = new List<Panel>();

        private static readonly Color HoverChipInactivo = Color.FromArgb(225, 220, 235);
        private static readonly Color ColorValorVerde = ColorTranslator.FromHtml("#5A8F7B");

        // Repositorios
        private Datos.RepositorioEmpresa _repoEmpresa = null!;
        private Datos.RepositorioConversacion _repoConversacion = null!;
        private Datos.RepositorioDiagnostico _repoDiagnostico = null!;
        private Datos.RepositorioMensaje _repoMensaje = null!;

        public VistaHistorial()
        {
            InitializeComponent();

            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();

            _repoEmpresa = new Datos.RepositorioEmpresa();
            _repoConversacion = new Datos.RepositorioConversacion();
            _repoDiagnostico = new Datos.RepositorioDiagnostico();
            _repoMensaje = new Datos.RepositorioMensaje();

            ConfigurarControl();

            CrearPanelContenido();
            CrearTimeline();
            CrearFiltrosTemporales();
            CrearEstadisticas();
            CrearBannerConBotones();
            CrearHeader();

            this.Load += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio += OnEmpresaActivaCambio;
                this.BeginInvoke(new Action(() => CargarEmpresaActiva()));
            };
            this.HandleDestroyed += (s, e) =>
            {
                Estado.EstadoApp.EmpresaActivaCambio -= OnEmpresaActivaCambio;
            };
        }

        private void ConfigurarControl()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Paleta.GrisClaro;
        }

        // ===================================================
        // HEADER
        // ===================================================
        private void CrearHeader()
        {
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Paleta.GrisClaro,
                Padding = new Padding(20, 15, 20, 10)
            };
            this.Controls.Add(_panelHeader);

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
                Text = "📜",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            picAvatar.Controls.Add(lblIcono);
            _panelHeader.Controls.Add(picAvatar);

            _lblTitulo = new Label
            {
                Text = "Historial de Evaluación",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 15),
                Size = new Size(500, 30),
                BackColor = Color.Transparent
            };
            _panelHeader.Controls.Add(_lblTitulo);

            _lblSubtitulo = new Label
            {
                Text = "Cronología completa de diagnósticos generados para la empresa activa",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 48),
                Size = new Size(700, 20),
                BackColor = Color.Transparent
            };
            _panelHeader.Controls.Add(_lblSubtitulo);

            _indicadorConexion = new Estilos.IndicadorModoConexion
            {
                Size = new Size(175, 36)
            };
            _panelHeader.Controls.Add(_indicadorConexion);

            _panelHeader.Resize += (s, e) =>
            {
                if (_indicadorConexion != null)
                    _indicadorConexion.Location = new Point(
                        _panelHeader.Width - _indicadorConexion.Width - 20, 25);
            };
            if (_indicadorConexion != null)
                _indicadorConexion.Location = new Point(
                    _panelHeader.Width - _indicadorConexion.Width - 20, 25);
        }

        // ===================================================
        // PANEL CENTRAL CONTENEDOR (blanco, redondeado)
        // ===================================================
        private void CrearPanelContenido()
        {
            _panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(25, 15, 25, 20),
                AutoScroll = true
            };
            this.Controls.Add(_panelContenido);
            _panelContenido.Resize += (s, e) =>
                Paleta.AplicarBordeRedondeadoSuave(_panelContenido, 25);
        }

        // ===================================================
        // BANNER CON BOTONES (empresa a la izq, botones a la der)
        // ===================================================
        private void CrearBannerConBotones()
        {
            _panelBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Paleta.LilaInput
            };
            _panelContenido.Controls.Add(_panelBanner);
            _panelBanner.Resize += (s, e) =>
            {
                Paleta.AplicarBordeRedondeadoSuave(_panelBanner, 16);
                ReposicionarBotonesBanner();
            };

            // Avatar circular con inicial
            var avatar = new Panel
            {
                Size = new Size(50, 50),
                Location = new Point(15, 15),
                BackColor = Paleta.MoradoOscuro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, 50, 50);
            avatar.Region = new Region(pathAv);

            _lblInicialEmpresa = new Label
            {
                Text = "?",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(_lblInicialEmpresa);
            _panelBanner.Controls.Add(avatar);

            var lblLabel = new Label
            {
                Text = "EMPRESA ACTIVA",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 125, 122),
                Location = new Point(75, 12),
                Size = new Size(200, 14),
                BackColor = Color.Transparent
            };
            _panelBanner.Controls.Add(lblLabel);

            _lblNombreEmpresa = new Label
            {
                Text = "Sin empresa seleccionada",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(75, 27),
                Size = new Size(500, 20),
                BackColor = Color.Transparent
            };
            _panelBanner.Controls.Add(_lblNombreEmpresa);

            _lblDetallesEmpresa = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 115, 112),
                Location = new Point(75, 49),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            _panelBanner.Controls.Add(_lblDetallesEmpresa);

            // 3 botones (se posicionan a la derecha con ReposicionarBotonesBanner)
            int anchoBtn = 125;
            int altoBtn = 30;

            (_btnIrAlChat, _lblBtnIrAlChat) = CrearBotonAccion(
                "💬 Ir al chat", Paleta.MoradoOscuro, Paleta.MoradoOscuroHover,
                Point.Empty, new Size(anchoBtn, altoBtn));
            _panelBanner.Controls.Add(_btnIrAlChat);

            (_btnExportar, _lblBtnExportar) = CrearBotonAccion(
                "📥 Exportar todo", Paleta.VerdeGrisaceo, Paleta.VerdeGrisaceoOscuro,
                Point.Empty, new Size(anchoBtn + 10, altoBtn));
            _panelBanner.Controls.Add(_btnExportar);

            (_btnEliminar, _lblBtnEliminar) = CrearBotonAccion(
                "🗑 Eliminar", RojoSuave, RojoOscuro,
                Point.Empty, new Size(anchoBtn - 10, altoBtn));
            _panelBanner.Controls.Add(_btnEliminar);

            EventHandler chatClick = (s, e) => OnIrAlChatClick();
            _btnIrAlChat.Click += chatClick;
            _lblBtnIrAlChat.Click += chatClick;

            EventHandler exportarClick = (s, e) => OnExportarClick();
            _btnExportar.Click += exportarClick;
            _lblBtnExportar.Click += exportarClick;

            EventHandler eliminarClick = (s, e) => OnEliminarClick();
            _btnEliminar.Click += eliminarClick;
            _lblBtnEliminar.Click += eliminarClick;

            ActualizarEstadoBotones(false);
        }

        private void ReposicionarBotonesBanner()
        {
            if (_panelBanner == null || _btnEliminar == null) return;

            int gap = 8;
            int y = 25;
            int derecha = _panelBanner.ClientSize.Width - 15;

            _btnEliminar.Location = new Point(derecha - _btnEliminar.Width, y);
            derecha -= _btnEliminar.Width + gap;

            _btnExportar.Location = new Point(derecha - _btnExportar.Width, y);
            derecha -= _btnExportar.Width + gap;

            _btnIrAlChat.Location = new Point(derecha - _btnIrAlChat.Width, y);
        }

        private (Panel btn, Label lbl) CrearBotonAccion(
            string texto, Color colorNormal, Color colorHover,
            Point ubicacion, Size tamano)
        {
            var btn = new Panel
            {
                BackColor = colorNormal,
                Location = ubicacion,
                Size = tamano,
                Cursor = Cursors.Hand
            };

            var lbl = new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.Controls.Add(lbl);

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, tamano.Height, tamano.Height, 90, 180);
            path.AddArc(tamano.Width - tamano.Height, 0, tamano.Height, tamano.Height, 270, 180);
            path.CloseFigure();
            btn.Region = new Region(path);

            btn.MouseEnter += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorHover; };
            btn.MouseLeave += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorNormal; };
            lbl.MouseEnter += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorHover; };
            lbl.MouseLeave += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorNormal; };

            var colorPress = Color.FromArgb(
                Math.Max(0, colorHover.R - 30),
                Math.Max(0, colorHover.G - 30),
                Math.Max(0, colorHover.B - 30));
            btn.MouseDown += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorPress; };
            btn.MouseUp += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorHover; };
            lbl.MouseDown += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorPress; };
            lbl.MouseUp += (s, e) => { if (_botonesHabilitados) btn.BackColor = colorHover; };

            return (btn, lbl);
        }

        private void ActualizarEstadoBotones(bool habilitar)
        {
            _botonesHabilitados = habilitar;

            if (habilitar)
            {
                _btnIrAlChat.BackColor = Paleta.MoradoOscuro;
                _btnIrAlChat.Cursor = Cursors.Hand;
                _lblBtnIrAlChat.Cursor = Cursors.Hand;

                _btnExportar.BackColor = Paleta.VerdeGrisaceo;
                _btnExportar.Cursor = Cursors.Hand;
                _lblBtnExportar.Cursor = Cursors.Hand;

                _btnEliminar.BackColor = RojoSuave;
                _btnEliminar.Cursor = Cursors.Hand;
                _lblBtnEliminar.Cursor = Cursors.Hand;
            }
            else
            {
                _btnIrAlChat.BackColor = GrisDeshabilitado;
                _btnIrAlChat.Cursor = Cursors.Default;
                _lblBtnIrAlChat.Cursor = Cursors.Default;

                _btnExportar.BackColor = GrisDeshabilitado;
                _btnExportar.Cursor = Cursors.Default;
                _lblBtnExportar.Cursor = Cursors.Default;

                _btnEliminar.BackColor = GrisDeshabilitado;
                _btnEliminar.Cursor = Cursors.Default;
                _lblBtnEliminar.Cursor = Cursors.Default;
            }
        }

        // ===================================================
        // ACCIONES DE LOS BOTONES
        // ===================================================
        private void OnIrAlChatClick()
        {
            if (!_botonesHabilitados) return;

            int? empresaId = Estado.EstadoApp.EmpresaActivaId;
            if (empresaId == null) return;

            var conv = _repoConversacion.ObtenerUltimaPorEmpresa(empresaId.Value);
            if (conv == null)
            {
                Estilos.MensajeApp.Info(
                    "Esta empresa no tiene análisis aún.\n\n" +
                    "Ve a 'Cargar Informe' para subir un PDF y generar el primer análisis.",
                    "Sin análisis",
                    this.FindForm());
                return;
            }

            var formMain = this.FindForm() as Presentacion.FormMain;
            formMain?.NavegarAVistaChat();
        }

        private void OnExportarClick()
        {
            if (!_botonesHabilitados) return;

            int? empresaId = Estado.EstadoApp.EmpresaActivaId;
            if (empresaId == null) return;

            var empresa = _repoEmpresa.ObtenerPorId(empresaId.Value);
            var conv = _repoConversacion.ObtenerUltimaPorEmpresa(empresaId.Value);
            if (empresa == null || conv == null) return;

            var diagnosticos = _repoDiagnostico.ObtenerHistorialPorConversacion(conv.Id)
                                               .OrderBy(d => d.FechaGeneracion)
                                               .ToList();

            // === EXPORTAR COMO PDF (formato principal) ===
            // El PDF es un reporte formal multi-página: portada con datos de empresa
            // y un diagnóstico por sección con nivel CMMI, resumen, fortalezas,
            // debilidades, riesgos y recomendaciones.
            var dialogPdf = new SaveFileDialog
            {
                Title = "Exportar historial de diagnósticos como PDF",
                Filter = "Archivo PDF (*.pdf)|*.pdf|Archivo de texto (*.txt)|*.txt",
                FileName = $"Historial_{empresa.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = "pdf",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialogPdf.ShowDialog() != DialogResult.OK) return;

            // Decidir formato según extensión elegida en el diálogo
            bool esPdf = dialogPdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            if (esPdf)
            {
                try
                {
                    Cursor previoCursor = this.Cursor;
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        Logica.GeneradorPdfHistorial.Generar(empresa, conv, diagnosticos, dialogPdf.FileName);
                    }
                    finally
                    {
                        this.Cursor = previoCursor;
                    }

                    var abrir = MessageBox.Show(
                        this.FindForm(),
                        $"PDF guardado en:\n{dialogPdf.FileName}\n\n¿Deseas abrirlo ahora?",
                        "Exportación completada",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (abrir == DialogResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = dialogPdf.FileName,
                                UseShellExecute = true
                            });
                        }
                        catch { /* si no hay app asociada, el archivo ya está guardado */ }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this.FindForm(),
                        $"No se pudo generar el PDF:\n\n{ex.Message}",
                        "Error al generar PDF",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return;
            }

            // === Fallback: exportar como TXT (formato legado, si el usuario elige .txt) ===
            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════════════════════════════════════════");
            sb.AppendLine("       REPORTE DE MADUREZ TECNOLÓGICA");
            sb.AppendLine("       Sistema de Evaluación para PYMES");
            sb.AppendLine("══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"  Empresa:    {empresa.Nombre}");
            sb.AppendLine($"  RIF:        {empresa.Rif}");
            sb.AppendLine($"  Sector:     {empresa.Sector}");
            sb.AppendLine($"  Empleados:  {empresa.CantidadEmpleados}");
            sb.AppendLine($"  Fecha:      {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine("──────────────────────────────────────────────────────────────");
            sb.AppendLine($"  Conversación iniciada: {conv.FechaInicio:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"  Diagnósticos generados: {diagnosticos.Count}");
            sb.AppendLine("──────────────────────────────────────────────────────────────");

            for (int i = 0; i < diagnosticos.Count; i++)
            {
                var d = diagnosticos[i];
                sb.AppendLine();
                sb.AppendLine($"  ┌─ DIAGNÓSTICO {i + 1} de {diagnosticos.Count} " +
                              $"({(d.EsFinal ? "FINAL" : "INTERMEDIO")}) ─────────────────────");
                sb.AppendLine($"  │  Nivel de madurez: {d.NivelMadurez}");
                sb.AppendLine($"  │  Fecha: {d.FechaGeneracion:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"  └──────────────────────────────────────────────────────");
                sb.AppendLine();

                if (!string.IsNullOrWhiteSpace(d.ResumenEmpresa))
                {
                    sb.AppendLine("  📄 RESUMEN");
                    sb.AppendLine($"  {d.ResumenEmpresa}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(d.Fortalezas))
                {
                    sb.AppendLine("  ✅ FORTALEZAS");
                    sb.AppendLine($"  {d.Fortalezas}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(d.Debilidades))
                {
                    sb.AppendLine("  ⚠️ DEBILIDADES");
                    sb.AppendLine($"  {d.Debilidades}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(d.Riesgos))
                {
                    sb.AppendLine("  🔴 RIESGOS");
                    sb.AppendLine($"  {d.Riesgos}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(d.Recomendaciones))
                {
                    sb.AppendLine("  💡 RECOMENDACIONES");
                    sb.AppendLine($"  {d.Recomendaciones}");
                    sb.AppendLine();
                }

                sb.AppendLine("──────────────────────────────────────────────────────────────");
            }

            sb.AppendLine();
            sb.AppendLine("  Generado por: Sistema de Madurez Tecnológica para PYMES");
            sb.AppendLine($"  Exportado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

            // El usuario ya eligió una ruta .txt en el diálogo del principio de este
            // método, así que aquí solo escribimos el archivo con el texto generado.
            try
            {
                File.WriteAllText(dialogPdf.FileName, sb.ToString(), Encoding.UTF8);
                Estilos.MensajeApp.Exito(
                    $"Reporte exportado exitosamente.\n\n📁 {dialogPdf.FileName}",
                    "Exportación completada",
                    this.FindForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this.FindForm(),
                    $"No se pudo guardar el archivo:\n\n{ex.Message}",
                    "Error al exportar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnEliminarClick()
        {
            if (!_botonesHabilitados) return;

            int? empresaId = Estado.EstadoApp.EmpresaActivaId;
            if (empresaId == null) return;

            var conv = _repoConversacion.ObtenerUltimaPorEmpresa(empresaId.Value);
            if (conv == null) return;

            var diagnosticos = _repoDiagnostico.ObtenerHistorialPorConversacion(conv.Id)
                                               .OrderByDescending(d => d.FechaGeneracion)
                                               .ToList();
            if (diagnosticos.Count == 0) return;

            MostrarDialogoEliminar(diagnosticos, conv.Id);
        }

        private void MostrarDialogoEliminar(List<Modelos.Diagnostico> diagnosticos, int convId)
        {
            var form = new Form
            {
                Text = "",
                Size = new Size(540, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.FromArgb(240, 237, 235),
                ShowInTaskbar = false
            };

            var RojoAccento = Color.FromArgb(193, 63, 63);
            var RojoOscuroBtn = Color.FromArgb(150, 45, 45);
            var RojoSuaveFondo = Color.FromArgb(253, 245, 245);
            var CardNormal = Color.FromArgb(255, 254, 253);
            var CardHover = Color.FromArgb(252, 248, 248);
            var CardChecked = Color.FromArgb(255, 248, 247);

            // === HEADER ROJO CON GRADIENTE VISUAL ===
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = RojoAccento
            };

            header.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(Color.FromArgb(25, Color.Black));
                e.Graphics.FillRectangle(brush, 0, header.Height - 3, header.Width, 3);
            };

            var iconoCirculo = new Panel
            {
                Size = new Size(42, 42),
                Location = new Point(20, 19),
                BackColor = Color.FromArgb(170, 50, 50)
            };
            iconoCirculo.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(iconoCirculo, 21);

            var lblIcono = new Label
            {
                Text = "🗑",
                Font = new Font("Segoe UI", 14),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconoCirculo.Controls.Add(lblIcono);

            var lblTitulo = new Label
            {
                Text = "Eliminar diagnósticos",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(75, 19),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var lblSubtitulo = new Label
            {
                Text = $"Selecciona los diagnósticos que deseas eliminar  ·  {diagnosticos.Count} disponibles",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(240, 210, 210),
                Location = new Point(75, 44),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var btnCerrar = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 200, 200),
                Size = new Size(40, 40),
                Location = new Point(form.Width - 50, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCerrar.Click += (s, e) => form.Close();
            btnCerrar.MouseEnter += (s, e) => btnCerrar.ForeColor = Color.White;
            btnCerrar.MouseLeave += (s, e) => btnCerrar.ForeColor = Color.FromArgb(230, 200, 200);

            header.Controls.AddRange(new Control[] { iconoCirculo, lblTitulo, lblSubtitulo, btnCerrar });

            Point dragOffset = Point.Empty;
            header.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragOffset = e.Location; };
            header.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    form.Location = new Point(
                        form.Location.X + e.X - dragOffset.X,
                        form.Location.Y + e.Y - dragOffset.Y);
            };

            // === CONTENIDO SCROLLABLE ===
            var contenido = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(240, 237, 235),
                Padding = new Padding(18, 15, 18, 15)
            };

            // Panel envolvente — crea el "doble margen" y profundidad
            var panelLista = new Panel
            {
                Location = new Point(18, 15),
                BackColor = Color.FromArgb(248, 246, 244),
                Padding = new Padding(12, 12, 12, 12)
            };
            panelLista.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(panelLista, 14);

            // Pinta una sombra sutil interior alrededor del panel envolvente
            panelLista.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var shadow = new SolidBrush(Color.FromArgb(15, Color.Black));
                g.FillRectangle(shadow, 0, panelLista.Height - 3, panelLista.Width, 3);
                using var topLight = new SolidBrush(Color.FromArgb(20, Color.White));
                g.FillRectangle(topLight, 0, 0, panelLista.Width, 1);
            };

            // Label de sección dentro del panel envolvente
            var lblSeccion = new Label
            {
                Text = "DIAGNÓSTICOS DISPONIBLES",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 130, 125),
                Location = new Point(8, 3),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelLista.Controls.Add(lblSeccion);

            var checks = new List<(CheckBox chk, Modelos.Diagnostico diag, Panel card)>();
            int y = 22;

            foreach (var diag in diagnosticos)
            {
                var card = new Panel
                {
                    Location = new Point(4, y),
                    Size = new Size(460, 85),
                    BackColor = CardNormal,
                    Cursor = Cursors.Hand
                };
                card.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(card, 12);

                var colorAccento = diag.EsFinal ? Paleta.MoradoOscuro : Paleta.MoradoClaro;

                var chk = new CheckBox
                {
                    Location = new Point(16, 32),
                    Size = new Size(18, 18),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                card.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using var barBrush = new SolidBrush(colorAccento);
                    g.FillRectangle(barBrush, 0, 12, 4, card.Height - 24);

                    if (chk.Checked)
                    {
                        using var pen = new Pen(RojoAccento, 2.5f);
                        g.DrawRectangle(pen, 1, 1, card.Width - 3, card.Height - 3);

                        using var checkBrush = new SolidBrush(Color.FromArgb(8, RojoAccento));
                        g.FillRectangle(checkBrush, 0, 0, card.Width, card.Height);
                    }

                    using var shadowBrush = new SolidBrush(Color.FromArgb(12, Color.Black));
                    g.FillRectangle(shadowBrush, 2, card.Height - 2, card.Width - 4, 2);
                };

                string tipo = diag.EsFinal ? "Diagnóstico Final" : "Diagnóstico Intermedio";
                string icono = diag.EsFinal ? "📋" : "📝";
                var lblTipo = new Label
                {
                    Text = $"{icono}  {tipo}",
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(42, 12),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                var lblNivel = new Label
                {
                    Text = $"Nivel {diag.NivelMadurez}",
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = colorAccento,
                    Size = new Size(60, 22),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(385, 14)
                };
                lblNivel.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(lblNivel, 11);

                var lblFecha = new Label
                {
                    Text = $"📅  {diag.FechaGeneracion:dd/MM/yyyy  ·  HH:mm}",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(130, 125, 120),
                    Location = new Point(42, 38),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                string preview = diag.ResumenEmpresa.Split('.').FirstOrDefault()?.Trim() ?? "";
                if (preview.Length > 60) preview = preview[..60] + "…";
                var lblPreview = new Label
                {
                    Text = preview,
                    Font = new Font("Segoe UI", 7.8f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(155, 150, 145),
                    Location = new Point(42, 58),
                    Size = new Size(340, 18),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    AutoEllipsis = true
                };

                chk.CheckedChanged += (s, e) =>
                {
                    card.BackColor = chk.Checked ? CardChecked : CardNormal;
                    card.Invalidate();
                };

                card.MouseEnter += (s, e) => { if (!chk.Checked) card.BackColor = CardHover; };
                card.MouseLeave += (s, e) =>
                {
                    if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
                        card.BackColor = chk.Checked ? CardChecked : CardNormal;
                };

                EventHandler toggleCheck = (s, e) => chk.Checked = !chk.Checked;
                card.Click += toggleCheck;
                lblTipo.Click += toggleCheck;
                lblFecha.Click += toggleCheck;
                lblPreview.Click += toggleCheck;

                foreach (Control child in new Control[] { lblTipo, lblFecha, lblPreview })
                {
                    child.MouseEnter += (s, e) => { if (!chk.Checked) card.BackColor = CardHover; };
                    child.MouseLeave += (s, e) =>
                    {
                        if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
                            card.BackColor = chk.Checked ? CardChecked : CardNormal;
                    };
                }

                card.Controls.AddRange(new Control[] { chk, lblTipo, lblNivel, lblFecha, lblPreview });
                panelLista.Controls.Add(card);
                checks.Add((chk, diag, card));

                y += 98;
            }

            panelLista.Height = y + 8;
            panelLista.Width = 480;
            contenido.Controls.Add(panelLista);

            // === FOOTER ===
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.White
            };

            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(230, 225, 220));
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            var lblContador = new Label
            {
                Text = "Ninguno seleccionado",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(160, 155, 150),
                Location = new Point(22, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var btnCancelar = new Panel
            {
                Size = new Size(85, 34),
                Location = new Point(form.Width - 295, 15),
                BackColor = Color.FromArgb(240, 237, 235),
                Cursor = Cursors.Hand
            };
            btnCancelar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnCancelar, 10);

            var lblCancelar = new Label
            {
                Text = "Cancelar",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(120, 115, 110),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCancelar.Controls.Add(lblCancelar);
            btnCancelar.Click += (s, e) => form.Close();
            lblCancelar.Click += (s, e) => form.Close();
            btnCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = Color.FromArgb(230, 225, 222);
            btnCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = Color.FromArgb(240, 237, 235);
            lblCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = Color.FromArgb(230, 225, 222);
            lblCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = Color.FromArgb(240, 237, 235);

            var btnEliminar = new Panel
            {
                Size = new Size(185, 36),
                Location = new Point(form.Width - 205, 14),
                BackColor = Color.FromArgb(210, 195, 195),
                Cursor = Cursors.Default
            };
            btnEliminar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnEliminar, 12);

            var lblBtnEliminar = new Label
            {
                Text = "🗑  Eliminar seleccionados",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            btnEliminar.Controls.Add(lblBtnEliminar);

            foreach (var (chk, _, _) in checks)
            {
                chk.CheckedChanged += (s, e) =>
                {
                    int seleccionados = checks.Count(c => c.chk.Checked);
                    lblContador.Text = seleccionados == 0
                        ? "Ninguno seleccionado"
                        : $"🔴 {seleccionados} seleccionado{(seleccionados != 1 ? "s" : "")}";
                    lblContador.ForeColor = seleccionados > 0
                        ? RojoAccento
                        : Color.FromArgb(160, 155, 150);

                    bool haySeleccion = seleccionados > 0;
                    btnEliminar.BackColor = haySeleccion ? RojoAccento : Color.FromArgb(210, 195, 195);
                    btnEliminar.Cursor = haySeleccion ? Cursors.Hand : Cursors.Default;
                    lblBtnEliminar.Text = haySeleccion
                        ? $"🗑  Eliminar ({seleccionados})"
                        : "🗑  Eliminar seleccionados";
                };
            }

            btnEliminar.MouseEnter += (s, e) =>
            {
                if (checks.Any(c => c.chk.Checked))
                    btnEliminar.BackColor = RojoOscuroBtn;
            };
            btnEliminar.MouseLeave += (s, e) =>
            {
                btnEliminar.BackColor = checks.Any(c => c.chk.Checked)
                    ? RojoAccento : Color.FromArgb(210, 195, 195);
            };
            lblBtnEliminar.MouseEnter += (s, e) =>
            {
                if (checks.Any(c => c.chk.Checked))
                    btnEliminar.BackColor = RojoOscuroBtn;
            };
            lblBtnEliminar.MouseLeave += (s, e) =>
            {
                btnEliminar.BackColor = checks.Any(c => c.chk.Checked)
                    ? RojoAccento : Color.FromArgb(210, 195, 195);
            };

            EventHandler eliminarClick = (s, e) =>
            {
                var seleccionados = checks.Where(c => c.chk.Checked).Select(c => c.diag).ToList();
                if (seleccionados.Count == 0) return;

                bool esTodo = seleccionados.Count == diagnosticos.Count;
                string mensaje = esTodo
                    ? $"¿Eliminar los {seleccionados.Count} diagnósticos?\n\n" +
                      "Al eliminar todos, también se borrará la conversación completa y sus mensajes.\n\n" +
                      "Esta acción no se puede deshacer."
                    : $"¿Eliminar {seleccionados.Count} diagnóstico{(seleccionados.Count > 1 ? "s" : "")}?\n\n" +
                      "Esta acción no se puede deshacer.";

                bool confirmado = Estilos.MensajeApp.Confirmar(mensaje, "Confirmar eliminación", form);
                if (!confirmado) return;

                if (esTodo)
                {
                    _repoDiagnostico.EliminarPorConversacion(convId);
                    _repoMensaje.EliminarPorConversacion(convId);
                    _repoConversacion.Eliminar(convId);
                }
                else
                {
                    foreach (var d in seleccionados)
                        _repoDiagnostico.EliminarPorId(d.Id);
                }

                form.Close();
                CargarEmpresaActiva();

                // Avisar a las demás vistas (Chat, Resultados) que el historial cambió,
                // para que se refresquen al instante sin tener que cambiar de empresa.
                Estado.EstadoApp.NotificarHistorialCambio();
            };
            btnEliminar.Click += eliminarClick;
            lblBtnEliminar.Click += eliminarClick;

            footer.Controls.AddRange(new Control[] { lblContador, btnCancelar, btnEliminar });

            form.Controls.Add(contenido);
            form.Controls.Add(footer);
            form.Controls.Add(header);

            form.ShowDialog();
        }

        // ===================================================
        // ESTADÍSTICAS (bandas horizontales apiladas)
        // ===================================================
        private void CrearEstadisticas()
        {
            _panelEstadisticas = new Panel
            {
                Dock = DockStyle.Top,
                Height = 215,
                BackColor = Color.White
            };
            _panelContenido.Controls.Add(_panelEstadisticas);

            (_cardDiagnosticos, _lblValorDiagnosticos, _lblSubDiagnosticos) =
                CrearTarjetaEstadistica("📊", "DIAGNÓSTICOS GENERADOS", "—", "—", Paleta.MoradoOscuro);
            (_cardMensajes, _lblValorMensajes, _lblSubMensajes) =
                CrearTarjetaEstadistica("💬", "MENSAJES INTERCAMBIADOS", "—", "—", Paleta.MoradoOscuro);
            (_cardInicio, _lblValorInicio, _lblSubInicio) =
                CrearTarjetaEstadistica("📅", "INICIO DE CONVERSACIÓN", "—", "—", ColorValorVerde);
            (_cardActividad, _lblValorActividad, _lblSubActividad) =
                CrearTarjetaEstadistica("⏱", "ÚLTIMA ACTIVIDAD", "—", "—", ColorValorVerde);

            _panelEstadisticas.Controls.AddRange(new Control[] {
                _cardDiagnosticos, _cardMensajes, _cardInicio, _cardActividad
            });

            _panelEstadisticas.Resize += (s, e) => ReposicionarTarjetas();
        }

        private void ReposicionarTarjetas()
        {
            if (_panelEstadisticas == null || _cardDiagnosticos == null) return;

            int pad = 15, gap = 12, cardH = 90;
            int cardW = (_panelEstadisticas.ClientSize.Width - 2 * pad - gap) / 2;

            _cardDiagnosticos.Bounds = new Rectangle(pad, pad, cardW, cardH);
            _cardMensajes.Bounds = new Rectangle(pad + cardW + gap, pad, cardW, cardH);
            _cardInicio.Bounds = new Rectangle(pad, pad + cardH + gap, cardW, cardH);
            _cardActividad.Bounds = new Rectangle(pad + cardW + gap, pad + cardH + gap, cardW, cardH);
        }

        private (Panel card, Label lblValor, Label lblSub) CrearTarjetaEstadistica(
            string icono, string titulo, string valorInicial, string subInicial, Color colorAccento)
        {
            var card = new Panel { BackColor = CardBgNormal };
            card.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(card, 14);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(colorAccento);
                e.Graphics.FillRectangle(brush, 0, 15, 4, card.Height - 30);
            };

            var iconBg = Color.FromArgb(
                CardBgNormal.R + (int)((colorAccento.R - CardBgNormal.R) * 0.15),
                CardBgNormal.G + (int)((colorAccento.G - CardBgNormal.G) * 0.15),
                CardBgNormal.B + (int)((colorAccento.B - CardBgNormal.B) * 0.15));

            var iconCircle = new Panel
            {
                Size = new Size(28, 28),
                Location = new Point(16, 10),
                BackColor = iconBg
            };
            var pathIcon = new System.Drawing.Drawing2D.GraphicsPath();
            pathIcon.AddEllipse(0, 0, 28, 28);
            iconCircle.Region = new Region(pathIcon);

            var lblIcono = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconCircle.Controls.Add(lblIcono);
            card.Controls.Add(iconCircle);

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 125, 120),
                Location = new Point(50, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitulo);

            var lblValor = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = colorAccento,
                Location = new Point(14, 40),
                Size = new Size(280, 26),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblValor);

            var lblSub = new Label
            {
                Text = subInicial,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(150, 145, 140),
                Location = new Point(16, 68),
                Size = new Size(280, 14),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblSub);

            AplicarHoverRecursivo(card, card);
            return (card, lblValor, lblSub);
        }

        private void AplicarHoverRecursivo(Control ctrl, Panel card)
        {
            ctrl.MouseEnter += (s, e) => card.BackColor = CardBgHover;
            ctrl.MouseLeave += (s, e) =>
            {
                if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
                    card.BackColor = CardBgNormal;
            };
            foreach (Control child in ctrl.Controls)
                AplicarHoverRecursivo(child, card);
        }

        private void AplicarClickRecursivo(Control ctrl, EventHandler handler)
        {
            ctrl.Click += handler;
            ctrl.Cursor = Cursors.Hand;
            foreach (Control child in ctrl.Controls)
                AplicarClickRecursivo(child, handler);
        }

        private void ActualizarEstadisticas()
        {
            int? empresaId = Estado.EstadoApp.EmpresaActivaId;

            if (empresaId == null)
            {
                _lblValorDiagnosticos.Text = "—";  _lblSubDiagnosticos.Text = "";
                _lblValorMensajes.Text = "—";       _lblSubMensajes.Text = "";
                _lblValorInicio.Text = "—";         _lblSubInicio.Text = "";
                _lblValorActividad.Text = "—";      _lblSubActividad.Text = "";
                return;
            }

            var conv = _repoConversacion.ObtenerUltimaPorEmpresa(empresaId.Value);
            if (conv == null)
            {
                _lblValorDiagnosticos.Text = "0";   _lblSubDiagnosticos.Text = "Sin diagnósticos aún";
                _lblValorMensajes.Text = "0";        _lblSubMensajes.Text = "Sin mensajes aún";
                _lblValorInicio.Text = "—";          _lblSubInicio.Text = "";
                _lblValorActividad.Text = "—";       _lblSubActividad.Text = "";
                return;
            }

            // Diagnósticos
            var diagnosticos = _repoDiagnostico.ObtenerHistorialPorConversacion(conv.Id);
            int total = diagnosticos.Count;
            int finales = diagnosticos.Count(d => d.EsFinal);
            int intermedios = total - finales;
            _lblValorDiagnosticos.Text = total.ToString();
            _lblSubDiagnosticos.Text = $"{intermedios} intermedio{(intermedios != 1 ? "s" : "")} + {finales} final{(finales != 1 ? "es" : "")}";

            // Mensajes
            var mensajes = _repoMensaje.ObtenerPorConversacion(conv.Id);
            int totalMsg = mensajes.Count;
            int delUsuario = mensajes.Count(m => m.Remitente == "Usuario");
            int deIA = mensajes.Count(m => m.Remitente == "IA");
            _lblValorMensajes.Text = totalMsg.ToString();
            _lblSubMensajes.Text = $"{delUsuario} del usuario · {deIA} IA";

            // Inicio de conversación
            _lblValorInicio.Text = conv.FechaInicio.ToString("dd/MM/yyyy");
            _lblSubInicio.Text = FormatearTiempoRelativo(conv.FechaInicio);

            // Última actividad
            if (mensajes.Count > 0)
            {
                var ultimo = mensajes.Last();
                _lblValorActividad.Text = FormatearTiempoRelativo(ultimo.Timestamp);
                _lblSubActividad.Text = ultimo.Timestamp.ToString("dd/MM/yyyy · HH:mm");
            }
            else
            {
                _lblValorActividad.Text = conv.FechaInicio.ToString("dd/MM/yyyy");
                _lblSubActividad.Text = "";
            }
        }

        private string FormatearTiempoRelativo(DateTime fecha)
        {
            var diff = DateTime.Now - fecha;
            if (diff.TotalMinutes < 1) return "Justo ahora";
            if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours}h";
            if (diff.TotalDays < 30) return $"Hace {(int)diff.TotalDays} días";
            return fecha.ToString("dd/MM/yyyy");
        }

        // ===================================================
        // FILTROS TEMPORALES
        // ===================================================
        private void CrearFiltrosTemporales()
        {
            _panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White
            };
            _panelContenido.Controls.Add(_panelFiltros);

            // Etiqueta "RANGO:"
            var lblRango = new Label
            {
                Text = "RANGO:",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 130),
                Location = new Point(15, 16),
                Size = new Size(55, 18),
                BackColor = Color.Transparent
            };
            _panelFiltros.Controls.Add(lblRango);

            int x = 75;
            int altoChip = 30;
            int gap = 8;

            _chipTodo = CrearChipFiltro("📅 Todo el historial", x, altoChip, true);
            x += _chipTodo.Width + gap;

            _chipSemana = CrearChipFiltro("Última semana", x, altoChip, false);
            x += _chipSemana.Width + gap;

            _chipMes = CrearChipFiltro("Último mes", x, altoChip, false);
            x += _chipMes.Width + gap;

            _chipPersonalizado = CrearChipFiltro("📅 Personalizado", x, altoChip, false);

            _panelFiltros.Controls.AddRange(new Control[] {
                _chipTodo, _chipSemana, _chipMes, _chipPersonalizado
            });

            // Panel de rango de fechas (visible solo con "Personalizado")
            _panelRangoFechas = new Panel
            {
                Location = new Point(15, 48),
                Size = new Size(560, 48),
                Visible = false,
                BackColor = Paleta.LilaInput
            };
            _panelRangoFechas.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(_panelRangoFechas, 12);

            var lblDesdeIcon = new Label
            {
                Text = "Desde",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(14, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _dtpDesde = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd / MM / yyyy",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(58, 11),
                Size = new Size(145, 28),
                Value = DateTime.Now.AddMonths(-1),
                CalendarMonthBackground = Color.White,
                CalendarForeColor = Paleta.TextoOscuro
            };

            var lblSeparador = new Panel
            {
                Size = new Size(30, 2),
                Location = new Point(212, 23),
                BackColor = Paleta.MoradoClaro
            };

            var lblHastaIcon = new Label
            {
                Text = "Hasta",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(250, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _dtpHasta = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd / MM / yyyy",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(294, 11),
                Size = new Size(145, 28),
                Value = DateTime.Now,
                CalendarMonthBackground = Color.White,
                CalendarForeColor = Paleta.TextoOscuro
            };

            var btnAplicar = new Panel
            {
                Size = new Size(95, 30),
                Location = new Point(452, 9),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            btnAplicar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnAplicar, 15);

            var lblAplicar = new Label
            {
                Text = "✓  Aplicar",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnAplicar.Controls.Add(lblAplicar);

            btnAplicar.MouseEnter += (s, e) => btnAplicar.BackColor = Paleta.MoradoOscuroHover;
            btnAplicar.MouseLeave += (s, e) => btnAplicar.BackColor = Paleta.MoradoOscuro;
            lblAplicar.MouseEnter += (s, e) => btnAplicar.BackColor = Paleta.MoradoOscuroHover;
            lblAplicar.MouseLeave += (s, e) => btnAplicar.BackColor = Paleta.MoradoOscuro;

            btnAplicar.MouseDown += (s, e) => btnAplicar.BackColor = Color.FromArgb(60, 40, 90);
            btnAplicar.MouseUp += (s, e) => btnAplicar.BackColor = Paleta.MoradoOscuroHover;
            lblAplicar.MouseDown += (s, e) => btnAplicar.BackColor = Color.FromArgb(60, 40, 90);
            lblAplicar.MouseUp += (s, e) => btnAplicar.BackColor = Paleta.MoradoOscuroHover;

            EventHandler aplicarClick = (s, e) => ActualizarTimeline();
            btnAplicar.Click += aplicarClick;
            lblAplicar.Click += aplicarClick;

            _panelRangoFechas.Controls.AddRange(new Control[] {
                lblDesdeIcon, _dtpDesde, lblSeparador, lblHastaIcon, _dtpHasta, btnAplicar
            });
            _panelFiltros.Controls.Add(_panelRangoFechas);

            ConfigurarClickChip(_chipTodo, "todo");
            ConfigurarClickChip(_chipSemana, "semana");
            ConfigurarClickChip(_chipMes, "mes");
            ConfigurarClickChip(_chipPersonalizado, "personalizado");
        }

        private Panel CrearChipFiltro(string texto, int x, int alto, bool activo)
        {
            var font = new Font("Segoe UI", 8.5f);
            int anchoTexto = TextRenderer.MeasureText(texto, font).Width;
            int ancho = anchoTexto + 20;

            var chip = new Panel
            {
                Size = new Size(ancho, alto),
                Location = new Point(x, 10),
                BackColor = activo ? Paleta.MoradoOscuro : Paleta.LilaInput,
                Cursor = Cursors.Hand
            };

            var lbl = new Label
            {
                Text = texto,
                Font = font,
                ForeColor = activo ? Paleta.TextoBlanco : Paleta.MoradoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            chip.Controls.Add(lbl);

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, alto, alto, 90, 180);
            path.AddArc(ancho - alto, 0, alto, alto, 270, 180);
            path.CloseFigure();
            chip.Region = new Region(path);

            chip.MouseEnter += (s, e) => { if (chip.BackColor == Paleta.LilaInput) chip.BackColor = HoverChipInactivo; };
            chip.MouseLeave += (s, e) => { if (chip.BackColor == HoverChipInactivo) chip.BackColor = Paleta.LilaInput; };
            lbl.MouseEnter += (s, e) => { if (chip.BackColor == Paleta.LilaInput) chip.BackColor = HoverChipInactivo; };
            lbl.MouseLeave += (s, e) => { if (chip.BackColor == HoverChipInactivo) chip.BackColor = Paleta.LilaInput; };

            return chip;
        }

        private void ConfigurarClickChip(Panel chip, string filtro)
        {
            EventHandler click = (s, e) => SeleccionarFiltro(filtro);
            chip.Click += click;
            chip.Controls.OfType<Label>().First().Click += click;
        }

        private void SeleccionarFiltro(string filtro)
        {
            _filtroActivo = filtro;

            ActualizarChipVisual(_chipTodo, filtro == "todo");
            ActualizarChipVisual(_chipSemana, filtro == "semana");
            ActualizarChipVisual(_chipMes, filtro == "mes");
            ActualizarChipVisual(_chipPersonalizado, filtro == "personalizado");

            bool mostrarRango = filtro == "personalizado";
            _panelRangoFechas.Visible = mostrarRango;
            _panelFiltros.Height = mostrarRango ? 105 : 50;

            if (filtro != "personalizado")
                ActualizarTimeline();
        }

        private void ActualizarChipVisual(Panel chip, bool activo)
        {
            chip.BackColor = activo ? Paleta.MoradoOscuro : Paleta.LilaInput;
            var lbl = chip.Controls.OfType<Label>().FirstOrDefault();
            if (lbl != null)
                lbl.ForeColor = activo ? Paleta.TextoBlanco : Paleta.MoradoOscuro;
        }

        private List<Modelos.Diagnostico> FiltrarPorRangoTemporal(List<Modelos.Diagnostico> diagnosticos)
        {
            DateTime ahora = DateTime.Now;

            return _filtroActivo switch
            {
                "semana" => diagnosticos.Where(d => d.FechaGeneracion >= ahora.AddDays(-7)).ToList(),
                "mes" => diagnosticos.Where(d => d.FechaGeneracion >= ahora.AddMonths(-1)).ToList(),
                "personalizado" => diagnosticos.Where(d =>
                    d.FechaGeneracion.Date >= _dtpDesde.Value.Date &&
                    d.FechaGeneracion.Date <= _dtpHasta.Value.Date).ToList(),
                _ => diagnosticos
            };
        }

        // ===================================================
        // TIMELINE VERTICAL
        // ===================================================
        private void CrearTimeline()
        {
            _panelTimeline = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White
            };
            _panelContenido.Controls.Add(_panelTimeline);

            var lblTitulo = new Label
            {
                Text = "CRONOLOGÍA DE DIAGNÓSTICOS",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 125, 120),
                Location = new Point(15, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _panelTimeline.Controls.Add(lblTitulo);

            _lblSinTimeline = new Label
            {
                Text = "Sin diagnósticos generados aún.\nUsa el chat para generar una evaluación.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 155, 150),
                Location = new Point(15, 35),
                Size = new Size(400, 40),
                BackColor = Color.Transparent,
                Visible = true
            };
            _panelTimeline.Controls.Add(_lblSinTimeline);

            // Pinta la línea vertical y los nodos circulares
            _panelTimeline.Paint += PintarLineaTimeline;
        }

        private void PintarLineaTimeline(object? sender, PaintEventArgs e)
        {
            if (_tarjetasTimeline.Count == 0) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int lineX = 90;
            int topY = _tarjetasTimeline.First().Top + 12;
            int bottomY = _tarjetasTimeline.Last().Top + 12;

            // Línea vertical punteada morada
            using var pen = new Pen(Color.FromArgb(200, 195, 215), 2)
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
            };
            e.Graphics.DrawLine(pen, lineX, topY, lineX, bottomY);

            // Círculos en cada nodo
            foreach (var card in _tarjetasTimeline)
            {
                bool esFinal = card.Tag is true;
                int cy = card.Top + 12;

                if (esFinal)
                {
                    // Círculo sólido morado para diagnóstico final
                    using var brush = new SolidBrush(Paleta.MoradoOscuro);
                    e.Graphics.FillEllipse(brush, lineX - 7, cy - 7, 14, 14);
                }
                else
                {
                    // Anillo verde para diagnóstico intermedio
                    using var brush = new SolidBrush(Paleta.VerdeGrisaceo);
                    e.Graphics.FillEllipse(brush, lineX - 6, cy - 6, 12, 12);
                    using var inner = new SolidBrush(Color.White);
                    e.Graphics.FillEllipse(inner, lineX - 3, cy - 3, 6, 6);
                }
            }
        }

        private Panel CrearTarjetaTimeline(Modelos.Diagnostico diag, int yPos)
        {
            string tipo = diag.EsFinal ? "Diagnóstico Final" : "Diagnóstico Intermedio";
            string iconoTipo = diag.EsFinal ? "📋" : "📝";
            var colorBorde = diag.EsFinal ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo;

            var card = new Panel
            {
                Location = new Point(110, yPos),
                Size = new Size(_panelTimeline.ClientSize.Width - 130, 75),
                BackColor = CardBgNormal,
                Tag = diag.EsFinal
            };
            card.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(card, 10);

            // Acento superior (línea de color en el top)
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(colorBorde);
                e.Graphics.FillRectangle(brush, 12, 0, card.Width - 24, 3);
            };

            // Tipo + icono
            var lblTipo = new Label
            {
                Text = $"{iconoTipo}  {tipo}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = colorBorde,
                Location = new Point(12, 8),
                Size = new Size(250, 18),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTipo);

            // Badge nivel CMMI
            string nivelTexto = diag.NivelMadurez > 0
                ? $"Nivel {diag.NivelMadurez}"
                : "Sin nivel";
            var lblNivel = new Label
            {
                Text = nivelTexto,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = colorBorde,
                Size = new Size(55, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblNivel.Location = new Point(card.Width - lblNivel.Width - 12, 8);
            var pathBadge = new System.Drawing.Drawing2D.GraphicsPath();
            pathBadge.AddArc(0, 0, 18, 18, 90, 180);
            pathBadge.AddArc(55 - 18, 0, 18, 18, 270, 180);
            pathBadge.CloseFigure();
            lblNivel.Region = new Region(pathBadge);
            card.Controls.Add(lblNivel);

            // Resumen (truncado)
            string resumen = diag.ResumenEmpresa.Length > 80
                ? diag.ResumenEmpresa[..80] + "…"
                : diag.ResumenEmpresa;
            if (string.IsNullOrWhiteSpace(resumen)) resumen = "Sin resumen disponible";

            var lblResumen = new Label
            {
                Text = resumen,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(100, 95, 90),
                Location = new Point(12, 30),
                Size = new Size(card.Width - 30, 16),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblResumen);

            // Fecha
            var lblFecha = new Label
            {
                Text = diag.FechaGeneracion.ToString("dd/MM/yyyy · HH:mm"),
                Font = new Font("Segoe UI", 7f),
                ForeColor = Color.FromArgb(155, 150, 145),
                Location = new Point(12, 50),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblFecha);

            // Fecha al costado izquierdo de la línea (en el panel padre)
            var lblFechaLinea = new Label
            {
                Text = diag.FechaGeneracion.ToString("dd/MM\nHH:mm"),
                Font = new Font("Segoe UI", 7f),
                ForeColor = Color.FromArgb(140, 135, 130),
                Location = new Point(15, yPos + 2),
                Size = new Size(60, 28),
                TextAlign = ContentAlignment.TopRight,
                BackColor = Color.Transparent
            };
            _panelTimeline.Controls.Add(lblFechaLinea);

            // Hover
            AplicarHoverRecursivo(card, card);

            card.Cursor = Cursors.Hand;
            EventHandler click = (s, e) => MostrarDetalleDiagnostico(diag);
            AplicarClickRecursivo(card, click);

            return card;
        }

        private void ActualizarTimeline()
        {
            // Limpiar tarjetas anteriores
            foreach (var card in _tarjetasTimeline)
            {
                _panelTimeline.Controls.Remove(card);
                card.Dispose();
            }
            _tarjetasTimeline.Clear();

            // Limpiar labels de fecha que estén a la izquierda
            var fechasViejas = _panelTimeline.Controls.OfType<Label>()
                .Where(l => l.Location.X == 15 && l.Location.Y > 30)
                .ToList();
            foreach (var f in fechasViejas)
            {
                _panelTimeline.Controls.Remove(f);
                f.Dispose();
            }

            int? empresaId = Estado.EstadoApp.EmpresaActivaId;
            if (empresaId == null)
            {
                _lblSinTimeline.Visible = true;
                _panelTimeline.Height = 80;
                _panelTimeline.Invalidate();
                return;
            }

            var conv = _repoConversacion.ObtenerUltimaPorEmpresa(empresaId.Value);
            if (conv == null)
            {
                _lblSinTimeline.Visible = true;
                _panelTimeline.Height = 80;
                _panelTimeline.Invalidate();
                return;
            }

            var diagnosticos = _repoDiagnostico.ObtenerHistorialPorConversacion(conv.Id)
                                               .OrderByDescending(d => d.FechaGeneracion)
                                               .ToList();

            diagnosticos = FiltrarPorRangoTemporal(diagnosticos);

            if (diagnosticos.Count == 0)
            {
                _lblSinTimeline.Visible = true;
                _panelTimeline.Height = 80;
                _panelTimeline.Invalidate();
                return;
            }

            _lblSinTimeline.Visible = false;

            int y = 35;
            int cardHeight = 75;
            int gap = 15;

            foreach (var diag in diagnosticos)
            {
                var card = CrearTarjetaTimeline(diag, y);
                _panelTimeline.Controls.Add(card);
                _tarjetasTimeline.Add(card);
                y += cardHeight + gap;
            }

            _panelTimeline.Height = y + 10;
            _panelTimeline.Invalidate();
        }

        // ===================================================
        // DETALLE DE DIAGNÓSTICO (diálogo modal)
        // ===================================================
        private void MostrarDetalleDiagnostico(Modelos.Diagnostico diag)
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

            // === HEADER MORADO (más alto, con fecha integrada) ===
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

            // Subtítulo con fecha dentro del header
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

            // Botón cerrar (X)
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
            // IMPORTANTE: en WinForms el Dock=Fill se agrega PRIMERO,
            // luego el Dock=Top. Si se invierte, el Fill se extiende
            // detrás del Top y la parte superior del contenido queda oculta.
            var contenido = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 12, 20, 15),
                BackColor = Paleta.GrisClaro
            };
            form.Controls.Add(contenido);
            form.Controls.Add(header);

            // Colores por sección
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

            // Fondo de la tarjeta: blanco con toque del color de acento
            var fondoCard = Color.FromArgb(
                252 + (int)((colorAccento.R - 252) * 0.03),
                250 + (int)((colorAccento.G - 250) * 0.03),
                255 + (int)((colorAccento.B - 255) * 0.03));

            // Label del contenido (se crea primero para medir su alto)
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

            // Alto de la tarjeta: título (25) + contenido + padding inferior
            int cardHeight = 30 + lblContenido.PreferredHeight + 15;

            var card = new Panel
            {
                Location = new Point(5, y),
                Size = new Size(parent.ClientSize.Width - 35, cardHeight),
                BackColor = fondoCard
            };
            card.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(card, 10);

            // Barra de acento lateral izquierda (4px)
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(colorAccento);
                e.Graphics.FillRectangle(brush, 0, 10, 4, card.Height - 20);
            };

            // Título de la sección con color de acento
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

            // Agregar contenido a la tarjeta
            card.Controls.Add(lblContenido);

            parent.Controls.Add(card);
            return card.Bottom + 8;
        }

        // ===================================================
        // OBSERVER: REACCIONAR AL CAMBIO DE EMPRESA ACTIVA
        // ===================================================
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
                _lblNombreEmpresa.Text = "Sin empresa seleccionada";
                _lblDetallesEmpresa.Text = "Ve a 'Empresas' para elegir una y ver su historial";
                _lblInicialEmpresa.Text = "?";
                ActualizarEstadoBotones(false);
                ActualizarEstadisticas();
                ActualizarTimeline();
                return;
            }

            var repo = new Datos.RepositorioEmpresa();
            var empresa = repo.ObtenerPorId(empresaId.Value);
            if (empresa == null)
            {
                _lblNombreEmpresa.Text = "Empresa no encontrada";
                _lblDetallesEmpresa.Text = "";
                _lblInicialEmpresa.Text = "?";
                ActualizarEstadoBotones(false);
                ActualizarEstadisticas();
                ActualizarTimeline();
                return;
            }

            _lblNombreEmpresa.Text = empresa.Nombre;

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(empresa.Sector)) partes.Add(empresa.Sector);
            partes.Add($"RIF: {empresa.Rif}");
            if (!string.IsNullOrWhiteSpace(empresa.Direccion)) partes.Add(empresa.Direccion);
            if (empresa.CantidadEmpleados > 0) partes.Add($"{empresa.CantidadEmpleados} empleados");

            _lblDetallesEmpresa.Text = string.Join("  ·  ", partes);

            _lblInicialEmpresa.Text = empresa.Nombre.Length > 0
                ? empresa.Nombre[0].ToString().ToUpper()
                : "?";

            ActualizarEstadoBotones(true);
            ActualizarEstadisticas();
            ActualizarTimeline();
        }

        private void InitializeComponent()
        {
        }
    }
}
