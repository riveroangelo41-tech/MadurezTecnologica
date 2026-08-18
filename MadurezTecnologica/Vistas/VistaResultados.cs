using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Vistas
{
    // Estructura simple para representar un sector con su valor y color
    public class SectorDato
    {
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; }
        public Color Color { get; set; }
    }

    public partial class VistaResultados : UserControl
    {
        // Paneles principales
        private Panel panelHeader = null!;
        private Estilos.IndicadorModoConexion _indicadorConexion = null!;
        private Panel panelContenido = null!;
        // Botón "Descargar PDF" del header: captura el dashboard actual como imagen
        // y genera un PDF A4 vertical de 1 página.
        private Panel _btnDescargarPdf = null!;

        // Header
        private Label lblTitulo = null!;
        private Label lblSubtitulo = null!;

        // KPIs principales (referencias para luego actualizarles los datos reales)
        private Label lblKpiEmpresas = null!;
        private Label lblKpiEvaluaciones = null!;
        private Label lblKpiNivelPromedio = null!;
        private Label lblKpiNivelPromedioSub = null!;   // "Gestionado", "Definido"...
        private Label lblKpiEsteMes = null!;

        // KPIs secundarios
        private Label lblKpiEmpresasEvaluadas = null!;
        private Label lblKpiEmpresasEvaluadasSub = null!;
        private Label lblKpiEmpresasEvaluadasDeX = null!;   // "de 3"
        private Panel _progressEmpEval = null!;             // barra rellena
        private Panel _trackEmpEval = null!;                // track (fondo)
        private Label lblKpiEvalReciente = null!;
        private Label lblKpiEvalRecienteSub = null!;
        private Panel _pillNivelReciente = null!;
        private Label _lblPillNivelReciente = null!;
        private Label lblKpiNivelFrecuente = null!;
        private Label lblKpiNivelFrecuenteSub = null!;
        private Label lblKpiNivelFrecuenteNombre = null!;   // "Gestionado" al lado del número
        private Panel[] _miniBarrasNivelFrec = new Panel[5];

        // Gráfico distribución CMMI (5 filas, una por nivel)
        private Panel[] _barrasCmmi = new Panel[5];      // barras rellenas (para animar width)
        private Panel[] _tracksCmmi = new Panel[5];      // tracks de fondo
        private Panel[] _fondosCmmi = new Panel[5];      // container por fila (destacar el frec.)
        private Panel[] _chipsNumeroCmmi = new Panel[5]; // chip cuadrado con el número
        private Label[] _lblNumChipCmmi = new Label[5];  // label del número dentro del chip
        private Label[] _lblNombresNivel = new Label[5]; // "Inicial", "Gestionado"...
        private Panel[] _badgesFrec = new Panel[5];      // badges "MÁS FRECUENTE"
        private Label[] _valoresCmmi = new Label[5];     // cantidad (2, 1, 0...)
        private Label[] _porcentajesCmmi = new Label[5]; // "50%"
        private Label _lblBadgeCmmi = null!;             // "4 EMPRESAS" del badge del header
        private Label _lblInsightLinea1 = null!;
        private Label _lblInsightLinea2 = null!;

        // Gráfico de líneas: evaluaciones por día (últimos 7 días)
        private Panel _panelLineas = null!;
        private int[] _evaluacionesPorSemana = new int[7];
        private string[] _nombresDias = new string[7];
        private DateTime[] _fechasDias = new DateTime[7];   // fecha real de cada punto
        private PointF[] _puntosLineas = new PointF[7];     // posición calculada de cada punto

        // Áreas críticas: top 5 debilidades más frecuentes
        private Panel[] _barrasCriticas = new Panel[5];    // barra rellena (width animable)
        private Panel[] _tracksCriticas = new Panel[5];    // track de fondo
        private Panel[] _fondosCriticas = new Panel[5];    // container de la fila
        private Panel[] _chipsRankCriticas = new Panel[5]; // chip cuadrado con el ranking
        private Label[] _lblRankCriticas = new Label[5];   // número del ranking
        private Label[] _nombresCriticas = new Label[5];   // nombre de la debilidad
        private Label[] _valoresCriticas = new Label[5];   // cantidad
        private Label[] _subCriticas = new Label[5];       // "menciones" / % debajo del valor
        private Panel[] _badgesCriticas = new Panel[5];    // badge "MÁS FRECUENTE"
        private Label _lblBadgeCriticas = null!;
        private Label _lblInsightCriticas = null!;

        // Repositorios para consultar datos
        private Datos.RepositorioEmpresa _repoEmpresa = null!;
        private Datos.RepositorioConversacion _repoConversacion = null!;
        private Datos.RepositorioDiagnostico _repoDiagnostico = null!;

        // Lista de últimas evaluaciones
        private FlowLayoutPanel _flowUltimasEvaluaciones = null!;

        // Gráfico dona: sectores
        private List<SectorDato> _sectoresData = new();
        private Panel _panelDona = null!;
        private int _totalDona = 0;   // total dibujado en el centro de la dona (vía Paint)
        private FlowLayoutPanel _flowLegendaSectores = null!;
        private Label _lblBadgeSectores = null!;
        private Label _lblInsightSectores = null!;

        // Gráfico de líneas: extras
        private Label _lblBadgeActividad = null!;
        private Label _lblInsightActividad = null!;
        private int _diaPicoIdx = -1;   // índice del día con más actividad (para destacar)

        // Contenedor scrollable del dashboard
        private Panel panelDashboard = null!;

        public VistaResultados()
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

            ConfigurarControl();
            CrearPanelContenido();
            CrearHeader();

            // Cargar datos cuando la vista termina de inicializarse
            this.Load += (s, e) =>
            {
                // BeginInvoke garantiza que se ejecute DESPUÉS de que todos
                // los controles tengan su tamaño final
                this.BeginInvoke(new Action(() => CargarDatosReales()));
            };

            // Recargar datos si la vista pasa de oculta a visible (por si se reutiliza)
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible) CargarDatosReales();
            };
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

            // Avatar circular con icono
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
                Text = "📊",
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
                Text = "Resultados",
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
                Text = "Dashboard estadístico de evaluaciones de madurez tecnológica",
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

            // === BOTÓN "DESCARGAR PDF" ===
            // Panel personalizado con fondo morado, esquinas redondeadas y hover.
            // Se ubica a la izquierda del IndicadorModoConexion (que ya va a la derecha).
            _btnDescargarPdf = new Panel
            {
                Size = new Size(170, 36),
                BackColor = Paleta.MoradoOscuro,
                Cursor = Cursors.Hand
            };
            _btnDescargarPdf.Resize += (s, e) =>
                Paleta.AplicarBordeRedondeadoSuave(_btnDescargarPdf, 18);
            Paleta.AplicarBordeRedondeadoSuave(_btnDescargarPdf, 18);

            var lblBtnPdf = new Label
            {
                Text = "⬇  Descargar PDF",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _btnDescargarPdf.Controls.Add(lblBtnPdf);
            panelHeader.Controls.Add(_btnDescargarPdf);

            // Hover: aclara el fondo cuando el mouse entra
            _btnDescargarPdf.MouseEnter += (s, e) => _btnDescargarPdf.BackColor = Paleta.MoradoOscuroHover;
            _btnDescargarPdf.MouseLeave += (s, e) => _btnDescargarPdf.BackColor = Paleta.MoradoOscuro;
            lblBtnPdf.MouseEnter += (s, e) => _btnDescargarPdf.BackColor = Paleta.MoradoOscuroHover;
            lblBtnPdf.MouseLeave += (s, e) => _btnDescargarPdf.BackColor = Paleta.MoradoOscuro;

            // Click → generar PDF
            EventHandler descargarClick = (s, e) => OnDescargarPdfClick();
            _btnDescargarPdf.Click += descargarClick;
            lblBtnPdf.Click += descargarClick;

            // Recolocación al redimensionar: indicador a la derecha, botón PDF a la
            // izquierda del indicador (con 10 px de gap).
            void RecolocarBotonesHeader()
            {
                if (_indicadorConexion == null || _btnDescargarPdf == null) return;
                _indicadorConexion.Location = new Point(
                    panelHeader.Width - _indicadorConexion.Width - 20, 25);
                _btnDescargarPdf.Location = new Point(
                    _indicadorConexion.Left - _btnDescargarPdf.Width - 10, 25);
            }
            panelHeader.Resize += (s, e) => RecolocarBotonesHeader();
            RecolocarBotonesHeader();
        }

        // Handler del botón "Descargar PDF": pide ruta con SaveFileDialog y llama al
        // generador. Muestra feedback (mensaje de éxito con opción de abrir el PDF,
        // o error si algo falla). Deshabilita el botón durante la generación para
        // evitar clics dobles que abrirían dos diálogos.
        private void OnDescargarPdfClick()
        {
            if (!_btnDescargarPdf.Enabled) return;

            // Nombre sugerido con fecha para no pisar archivos previos
            string nombreDefault = $"Resultados_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            using var dlg = new SaveFileDialog
            {
                Title = "Guardar reporte de resultados como PDF",
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                FileName = nombreDefault,
                DefaultExt = "pdf",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (dlg.ShowDialog(this.FindForm()) != DialogResult.OK) return;

            _btnDescargarPdf.Enabled = false;
            Cursor previoCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // Se captura el panelDashboard (el que tiene AutoScroll y contiene TODAS
                // las secciones del dashboard: KPIs, gráficos, sectores, actividad, áreas
                // críticas, etc.). Al pasar el panel con scroll, el generador toma la
                // captura completa (contenido total, no solo lo visible en el viewport).
                Logica.GeneradorPdfResultados.Generar(
                    panelDashboard,
                    dlg.FileName,
                    "Dashboard de Resultados de Madurez Tecnológica");

                var abrir = MessageBox.Show(
                    this.FindForm(),
                    $"PDF guardado en:\n{dlg.FileName}\n\n¿Deseas abrirlo ahora?",
                    "Descarga completada",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (abrir == DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dlg.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // Si no hay app asociada a .pdf, no rompemos — el archivo ya
                        // está guardado y el usuario puede abrirlo manualmente.
                    }
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
            finally
            {
                this.Cursor = previoCursor;
                _btnDescargarPdf.Enabled = true;
            }
        }

        // ===================================================
        // PANEL CENTRAL (blanco, redondeado, scrollable)
        // ===================================================
        private void CrearPanelContenido()
        {
            panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(28, 22, 28, 22)
            };
            Controls.Add(panelContenido);
            panelContenido.Resize += (s, e) => AplicarBordeRedondeado(panelContenido, 25);

            // Panel interno scrollable donde irán todos los componentes
            panelDashboard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true
            };
            panelContenido.Controls.Add(panelDashboard);

            // Crear todas las secciones del dashboard
            CrearSeccionKpisPrincipales();
            CrearSeccionKpisSecundarios();
            CrearSeccionGraficoCmmi();
            CrearSeccionGraficoSectores();
            CrearSeccionGraficoLineas();
            CrearSeccionAreasCriticas();
            CrearSeccionUltimasEvaluaciones();
        }

        // ===================================================
        // UTILIDADES
        // ===================================================
        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            Paleta.AplicarBordeRedondeadoSuave(panel, radio);
        }

        // ===================================================
        // SECCIÓN DE KPIs PRINCIPALES (4 tarjetas en una fila)
        // ===================================================
        private void CrearSeccionKpisPrincipales()
        {
            // Panel contenedor de los 4 KPIs (FlowLayoutPanel para distribución automática)
            var filaKpis = new FlowLayoutPanel
            {
                Location = new Point(0, 10),
                Size = new Size(panelDashboard.ClientSize.Width, 130),
                BackColor = Color.White,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(filaKpis);

            // Re-centrar/redimensionar las tarjetas cuando el panel cambie de tamaño
            panelDashboard.Resize += (s, e) =>
            {
                filaKpis.Width = panelDashboard.ClientSize.Width;
                AjustarTamanoKpis(filaKpis);
            };

            // Crear las 4 tarjetas KPI
            Label subTemp;

            var kpi1 = CrearTarjetaKpi(
                etiqueta: "EMPRESAS\nREGISTRADAS",
                icono: "🏢",
                numero: "0",
                subtitulo: "Total en el sistema",
                colorAcento: Paleta.MoradoOscuro,
                out lblKpiEmpresas,
                out subTemp);
            TooltipEstilizado.AplicarACascada(
                "Cantidad total de empresas que se han registrado en el sistema. " +
                "Puedes ver el detalle completo en la sección 'Empresas'.",
                kpi1, lblKpiEmpresas);

            var kpi2 = CrearTarjetaKpi(
                etiqueta: "EVALUACIONES\nTOTALES",
                icono: "📋",
                numero: "0",
                subtitulo: "Análisis realizados",
                colorAcento: Paleta.VerdeGrisaceo,
                out lblKpiEvaluaciones,
                out subTemp);
            TooltipEstilizado.AplicarACascada(
                "Número total de análisis generados por la IA (incluye intermedios y finales). " +
                "Una misma empresa puede tener varias evaluaciones a lo largo del tiempo.",
                kpi2, lblKpiEvaluaciones);

            var kpi3 = CrearTarjetaKpi(
                etiqueta: "NIVEL CMMI\nPROMEDIO",
                icono: "⚡",
                numero: "—",
                subtitulo: "Sin datos aún",
                colorAcento: Paleta.MoradoClaro,
                out lblKpiNivelPromedio,
                out lblKpiNivelPromedioSub,
                sufijoNumero: "/ 5");
            TooltipEstilizado.AplicarACascada(
                "Promedio del nivel de madurez CMMI de todas las evaluaciones registradas. " +
                "Escala del 1 (Inicial) al 5 (Optimizado). " +
                "Ayuda a estimar el estado general del portafolio.",
                kpi3, lblKpiNivelPromedio);

            var kpi4 = CrearTarjetaKpi(
                etiqueta: "ESTE\nMES",
                icono: "📅",
                numero: "0",
                subtitulo: $"{DateTime.Now:MMMM yyyy}",
                colorAcento: Paleta.VerdeGrisaceoOscuro,
                out lblKpiEsteMes,
                out subTemp);
            TooltipEstilizado.AplicarACascada(
                $"Evaluaciones generadas durante el mes actual ({DateTime.Now:MMMM yyyy}). " +
                "Se reinicia el primer día de cada mes.",
                kpi4, lblKpiEsteMes);

            filaKpis.Controls.Add(kpi1);
            filaKpis.Controls.Add(kpi2);
            filaKpis.Controls.Add(kpi3);
            filaKpis.Controls.Add(kpi4);

            // Ajustar tamaños después de que el panel esté visible
            filaKpis.HandleCreated += (s, e) =>
            {
                filaKpis.BeginInvoke(new Action(() => AjustarTamanoKpis(filaKpis)));
            };
        }

        // ===================================================
        // AJUSTAR TAMAÑO DE LAS 4 TARJETAS KPI PARA QUE
        // LLENEN HORIZONTALMENTE EL ANCHO DISPONIBLE
        // ===================================================
        private void AjustarTamanoKpis(FlowLayoutPanel filaKpis)
        {
            if (filaKpis.Controls.Count != 4) return;
            int anchoTotal = filaKpis.ClientSize.Width;
            int gap = 14;
            int anchoTarjeta = (anchoTotal - (gap * 3) - 10) / 4;
            int alturaTarjeta = 132;

            foreach (Control c in filaKpis.Controls)
            {
                c.Size = new Size(anchoTarjeta, alturaTarjeta);
                c.Margin = new Padding(0, 0, gap, 0);
            }
        }

        // ===================================================
        // CREAR UNA TARJETA KPI INDIVIDUAL
        // Layout: chip circular con icono a la izq · label en 2 líneas al lado
        //         · número grande abajo · subtítulo debajo · barra de acento sutil
        // ===================================================
        private Panel CrearTarjetaKpi(
            string etiqueta,
            string icono,
            string numero,
            string subtitulo,
            Color colorAcento,
            out Label labelNumero,
            out Label labelSubtitulo,
            float tamanoFuenteNumero = 22f,
            string? sufijoNumero = null)
        {
            var tarjeta = new Panel
            {
                BackColor = Color.White
            };

            // Flag de hover que lee el Paint para iluminar la tarjeta.
            var hover = new bool[1];

            // === Fondo blanco con borde redondeado (todo en Paint) ===
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int radio = 14;

                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();

                Color fondo = hover[0] ? VersionPastel(colorAcento, 0.90f) : Color.White;
                using (var brushFondo = new SolidBrush(fondo))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(hover[0] ? colorAcento : Color.FromArgb(232, 229, 227), hover[0] ? 1.6f : 1f))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();
            tarjeta.Tag = hover;   // el flag queda accesible para enganchar el hover al final

            // === Chip circular con icono (izquierda-arriba) ===
            var chipIcono = new Panel
            {
                Size = new Size(34, 34),
                Location = new Point(14, 14),
                BackColor = VersionPastel(colorAcento)
            };
            var pathChip = new System.Drawing.Drawing2D.GraphicsPath();
            pathChip.AddEllipse(0, 0, chipIcono.Width, chipIcono.Height);
            chipIcono.Region = new Region(pathChip);

            var lblIconoChip = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI Emoji", 13),
                ForeColor = colorAcento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            chipIcono.Controls.Add(lblIconoChip);
            tarjeta.Controls.Add(chipIcono);

            // === Etiqueta en 2 líneas al lado del chip ===
            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(56, 15),
                Size = new Size(180, 32),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblEtiqueta);

            // === Número grande abajo ===
            // Capturamos la referencia en una variable local porque las lambdas
            // no pueden usar directamente el parámetro `out labelNumero`.
            var lblNumeroLocal = new Label
            {
                Text = numero,
                Font = new Font("Segoe UI", tamanoFuenteNumero, FontStyle.Bold),
                ForeColor = colorAcento,
                Location = new Point(14, 55),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblNumeroLocal);
            labelNumero = lblNumeroLocal;

            // === Sufijo del número (opcional, ej. "/ 5") ===
            if (!string.IsNullOrEmpty(sufijoNumero))
            {
                var lblSufijo = new Label
                {
                    Text = sufijoNumero,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(160, 155, 152),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                tarjeta.Controls.Add(lblSufijo);
                lblNumeroLocal.TextChanged += (s, e) =>
                {
                    lblSufijo.Location = new Point(
                        lblNumeroLocal.Right + 2,
                        lblNumeroLocal.Bottom - lblSufijo.Height - 4);
                };
                lblSufijo.Location = new Point(
                    lblNumeroLocal.Right + 2,
                    lblNumeroLocal.Bottom - lblSufijo.Height - 4);
            }

            // === Subtítulo ===
            var lblSubtituloLocal = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(16, 96),
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubtituloLocal);
            labelSubtitulo = lblSubtituloLocal;

            // === Barra de acento sutil bajo el subtítulo ===
            var barraAcento = new Panel
            {
                Size = new Size(28, 3),
                Location = new Point(16, 116),
                BackColor = colorAcento
            };
            tarjeta.Controls.Add(barraAcento);

            // Reposicionar/redimensionar hijos al cambiar tamaño de la tarjeta
            tarjeta.Resize += (s, e) =>
            {
                lblEtiqueta.Size = new Size(tarjeta.ClientSize.Width - 70, 32);
                lblSubtituloLocal.Size = new Size(tarjeta.ClientSize.Width - 32, 16);
            };

            EngancharHoverPaint(tarjeta, hover);
            return tarjeta;
        }

        // Engancha hover a una tarjeta cuyo Paint lee un flag bool[]. Aplica MouseEnter/
        // Leave a la tarjeta y a TODOS sus descendientes (para no parpadear al mover el
        // cursor entre los hijos), invalidando la tarjeta para que se repinte iluminada.
        private void EngancharHoverPaint(Panel tarjeta, bool[] hover)
        {
            void On() { hover[0] = true; tarjeta.Invalidate(); }
            void Off()
            {
                if (!tarjeta.ClientRectangle.Contains(tarjeta.PointToClient(Cursor.Position)))
                {
                    hover[0] = false;
                    tarjeta.Invalidate();
                }
            }
            void Enganchar(Control c)
            {
                c.MouseEnter += (s, e) => On();
                c.MouseLeave += (s, e) => Off();
                foreach (Control hijo in c.Controls) Enganchar(hijo);
            }
            Enganchar(tarjeta);
        }

        // Devuelve una versión muy clara (pastel) del color pasado.
        // mezcla=0.87 significa "87% blanco + 13% color".
        private Color VersionPastel(Color color, float mezcla = 0.87f)
        {
            int r = (int)(color.R * (1 - mezcla) + 255 * mezcla);
            int g = (int)(color.G * (1 - mezcla) + 255 * mezcla);
            int b = (int)(color.B * (1 - mezcla) + 255 * mezcla);
            return Color.FromArgb(r, g, b);
        }

        // Construye un GraphicsPath con forma de rectángulo redondeado.
        // Útil para pintar barras, píldoras, etc. sin usar Region.
        private System.Drawing.Drawing2D.GraphicsPath RectRedondeado(Rectangle rect, int radio)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radio * 2;
            if (rect.Width < d || rect.Height < d)
            {
                // Muy pequeño para redondear; devolver rectángulo simple
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ===================================================
        // SECCIÓN DE KPIs SECUNDARIOS (3 tarjetas en una fila)
        // Cada tarjeta tiene estructura específica para su tipo de dato.
        // ===================================================
        private void CrearSeccionKpisSecundarios()
        {
            var filaKpis = new FlowLayoutPanel
            {
                Location = new Point(0, 160),  // debajo de KPIs principales (10 + 132 + 18)
                Size = new Size(panelDashboard.ClientSize.Width, 145),
                BackColor = Color.White,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(filaKpis);

            panelDashboard.Resize += (s, e) =>
            {
                filaKpis.Width = panelDashboard.ClientSize.Width;
                AjustarTamanoKpisSecundarios(filaKpis);
            };

            var kpi1 = CrearTarjetaEmpresasEvaluadas();
            var kpi2 = CrearTarjetaEvalReciente();
            var kpi3 = CrearTarjetaNivelFrecuente();

            filaKpis.Controls.Add(kpi1);
            filaKpis.Controls.Add(kpi2);
            filaKpis.Controls.Add(kpi3);

            filaKpis.HandleCreated += (s, e) =>
            {
                filaKpis.BeginInvoke(new Action(() => AjustarTamanoKpisSecundarios(filaKpis)));
            };
        }

        // Base común: crea la tarjeta blanca redondeada con el chip de icono
        // y el label de etiqueta. Devuelve la tarjeta lista para agregar contenido.
        private Panel CrearBaseTarjetaSec(string etiqueta, string icono, Color colorAcento)
        {
            var tarjeta = new Panel { BackColor = Color.White };
            var hover = new bool[1];
            tarjeta.Tag = hover;   // el flag queda accesible para enganchar el hover al final

            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radio = 14;
                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();
                Color fondo = hover[0] ? VersionPastel(colorAcento, 0.90f) : Color.White;
                using (var brushFondo = new SolidBrush(fondo))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(hover[0] ? colorAcento : Color.FromArgb(232, 229, 227), hover[0] ? 1.6f : 1f))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // Chip icono
            var chip = new Panel
            {
                Size = new Size(34, 34),
                Location = new Point(14, 14),
                BackColor = VersionPastel(colorAcento)
            };
            var pathChip = new System.Drawing.Drawing2D.GraphicsPath();
            pathChip.AddEllipse(0, 0, chip.Width, chip.Height);
            chip.Region = new Region(pathChip);

            var lblIcono = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI Emoji", 13),
                ForeColor = colorAcento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            chip.Controls.Add(lblIcono);
            tarjeta.Controls.Add(chip);

            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(56, 15),
                Size = new Size(180, 32),
                BackColor = Color.Transparent,
                Tag = "etiqueta"   // para localizar y ajustar ancho en Resize
            };
            tarjeta.Controls.Add(lblEtiqueta);

            tarjeta.Resize += (s, e) =>
            {
                lblEtiqueta.Size = new Size(tarjeta.ClientSize.Width - 70, 32);
            };

            return tarjeta;
        }

        // TARJETA 1: EMPRESAS EVALUADAS
        // Layout: número grande + "de X" en gris + progress bar debajo + subtítulo
        private Panel CrearTarjetaEmpresasEvaluadas()
        {
            var tarjeta = CrearBaseTarjetaSec("EMPRESAS\nEVALUADAS", "✓", Paleta.MoradoOscuro);

            lblKpiEmpresasEvaluadas = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(14, 54),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiEmpresasEvaluadas);

            lblKpiEmpresasEvaluadasDeX = new Label
            {
                Text = "de 0",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 155, 152),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiEmpresasEvaluadasDeX);
            // Reposicionar "de X" al lado del número cuando cambie
            lblKpiEmpresasEvaluadas.TextChanged += (s, e) =>
            {
                lblKpiEmpresasEvaluadasDeX.Location = new Point(
                    lblKpiEmpresasEvaluadas.Right + 4,
                    lblKpiEmpresasEvaluadas.Bottom - lblKpiEmpresasEvaluadasDeX.Height - 6);
            };
            lblKpiEmpresasEvaluadasDeX.Location = new Point(
                lblKpiEmpresasEvaluadas.Right + 4,
                lblKpiEmpresasEvaluadas.Bottom - lblKpiEmpresasEvaluadasDeX.Height - 6);

            // Progress bar (track fondo + relleno)
            _trackEmpEval = new Panel
            {
                Location = new Point(16, 98),
                Size = new Size(160, 6),
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _trackEmpEval.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(_trackEmpEval, 3);
            tarjeta.Controls.Add(_trackEmpEval);

            _progressEmpEval = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 6),
                BackColor = Paleta.MoradoOscuro
            };
            _progressEmpEval.Resize += (s, e) =>
            {
                if (_progressEmpEval.Width > 6)
                    Paleta.AplicarBordeRedondeadoSuave(_progressEmpEval, 3);
            };
            _trackEmpEval.Controls.Add(_progressEmpEval);

            lblKpiEmpresasEvaluadasSub = new Label
            {
                Text = "0% del total registradas",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(16, 112),
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiEmpresasEvaluadasSub);

            tarjeta.Resize += (s, e) =>
            {
                _trackEmpEval.Size = new Size(tarjeta.ClientSize.Width - 32, 6);
                lblKpiEmpresasEvaluadasSub.Size = new Size(tarjeta.ClientSize.Width - 32, 16);
            };

            TooltipEstilizado.AplicarACascada(
                "De todas las empresas registradas, cuántas ya tienen al menos una evaluación completada. " +
                "La barra muestra la proporción visual del avance.",
                tarjeta, lblKpiEmpresasEvaluadas, lblKpiEmpresasEvaluadasSub, _trackEmpEval);

            EngancharHoverPaint(tarjeta, (bool[])tarjeta.Tag!);
            return tarjeta;
        }

        // TARJETA 2: EVALUACIÓN MÁS RECIENTE
        // Layout: nombre empresa (grande) + pill "NIVEL X" + fecha exacta
        private Panel CrearTarjetaEvalReciente()
        {
            var tarjeta = CrearBaseTarjetaSec("EVALUACIÓN\nMÁS RECIENTE", "🕐", Paleta.VerdeGrisaceo);

            lblKpiEvalReciente = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(16, 58),
                Size = new Size(220, 22),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            tarjeta.Controls.Add(lblKpiEvalReciente);

            // Pill "NIVEL X" (color verde grisáceo)
            _pillNivelReciente = new Panel
            {
                Size = new Size(60, 20),
                Location = new Point(16, 85),
                BackColor = Paleta.VerdeGrisaceo,
                Visible = false
            };
            _pillNivelReciente.Resize += (s, e) =>
                Paleta.AplicarBordeRedondeadoSuave(_pillNivelReciente, 10);

            _lblPillNivelReciente = new Label
            {
                Text = "NIVEL —",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _pillNivelReciente.Controls.Add(_lblPillNivelReciente);
            tarjeta.Controls.Add(_pillNivelReciente);

            lblKpiEvalRecienteSub = new Label
            {
                Text = "Sin evaluaciones aún",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(82, 88),   // al lado del pill
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiEvalRecienteSub);

            tarjeta.Resize += (s, e) =>
            {
                lblKpiEvalReciente.Size = new Size(tarjeta.ClientSize.Width - 32, 22);
                lblKpiEvalRecienteSub.Size = new Size(tarjeta.ClientSize.Width - 98, 16);
            };

            TooltipEstilizado.AplicarACascada(
                "La evaluación generada más recientemente en el sistema. " +
                "Muestra el nombre de la empresa evaluada, el nivel CMMI obtenido " +
                "y cuánto tiempo ha pasado desde que se generó.",
                tarjeta, lblKpiEvalReciente, lblKpiEvalRecienteSub, _pillNivelReciente);

            EngancharHoverPaint(tarjeta, (bool[])tarjeta.Tag!);
            return tarjeta;
        }

        // TARJETA 3: NIVEL MÁS FRECUENTE
        // Layout: número grande + nombre del nivel + subtítulo + 5 minibarras
        private Panel CrearTarjetaNivelFrecuente()
        {
            var tarjeta = CrearBaseTarjetaSec("NIVEL MÁS\nFRECUENTE", "🎯", Paleta.MoradoClaro);

            lblKpiNivelFrecuente = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Paleta.MoradoClaro,
                Location = new Point(14, 54),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiNivelFrecuente);

            lblKpiNivelFrecuenteNombre = new Label
            {
                Text = "Sin datos",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiNivelFrecuenteNombre);
            lblKpiNivelFrecuente.TextChanged += (s, e) =>
            {
                lblKpiNivelFrecuenteNombre.Location = new Point(
                    lblKpiNivelFrecuente.Right + 6,
                    lblKpiNivelFrecuente.Bottom - lblKpiNivelFrecuenteNombre.Height - 6);
            };
            lblKpiNivelFrecuenteNombre.Location = new Point(
                lblKpiNivelFrecuente.Right + 6,
                lblKpiNivelFrecuente.Bottom - lblKpiNivelFrecuenteNombre.Height - 6);

            lblKpiNivelFrecuenteSub = new Label
            {
                Text = "0 de 0 evaluaciones",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(16, 95),
                Size = new Size(200, 14),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblKpiNivelFrecuenteSub);

            // 5 mini barritas verticales que representan la distribución de niveles
            int miniY = 112;
            int miniBaseX = 16;
            int miniAncho = 12;
            int miniGap = 3;
            for (int i = 0; i < 5; i++)
            {
                var mini = new Panel
                {
                    Location = new Point(miniBaseX + i * (miniAncho + miniGap), miniY),
                    Size = new Size(miniAncho, 6),
                    BackColor = ColorTranslator.FromHtml("#EEE9F0")
                };
                mini.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(mini, 2);
                tarjeta.Controls.Add(mini);
                _miniBarrasNivelFrec[i] = mini;
            }

            tarjeta.Resize += (s, e) =>
            {
                lblKpiNivelFrecuenteSub.Size = new Size(tarjeta.ClientSize.Width - 32, 14);
            };

            TooltipEstilizado.AplicarACascada(
                "Nivel CMMI que aparece con más frecuencia en las evaluaciones registradas. " +
                "Las 5 barritas de abajo muestran cuántas evaluaciones hay en cada nivel " +
                "(de izquierda a derecha: Nivel 1 al 5). La barra más alta es la más frecuente.",
                tarjeta, lblKpiNivelFrecuente, lblKpiNivelFrecuenteSub, lblKpiNivelFrecuenteNombre);

            EngancharHoverPaint(tarjeta, (bool[])tarjeta.Tag!);
            return tarjeta;
        }

        // ===================================================
        // AJUSTAR TAMAÑO DE LAS 3 TARJETAS KPI SECUNDARIAS
        // ===================================================
        private void AjustarTamanoKpisSecundarios(FlowLayoutPanel filaKpis)
        {
            if (filaKpis.Controls.Count != 3) return;
            int anchoTotal = filaKpis.ClientSize.Width;
            int gap = 14;
            int anchoTarjeta = (anchoTotal - (gap * 2) - 10) / 3;
            int alturaTarjeta = 135;

            foreach (Control c in filaKpis.Controls)
            {
                c.Size = new Size(anchoTarjeta, alturaTarjeta);
                c.Margin = new Padding(0, 0, gap, 0);
            }
        }

        // ===================================================
        // GRÁFICO DE BARRAS: DISTRIBUCIÓN POR NIVEL CMMI
        // Rediseño: chip numerado + nombre + barra fina + cantidad + %
        // El nivel más frecuente se destaca con fondo tenue y badge.
        // ===================================================
        private void CrearSeccionGraficoCmmi()
        {
            // Contenedor de la tarjeta del gráfico
            var tarjeta = new Panel
            {
                Location = new Point(0, 315),
                Size = new Size(panelDashboard.ClientSize.Width - 10, 350),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(tarjeta);

            panelDashboard.Resize += (s, e) =>
            {
                tarjeta.Width = panelDashboard.ClientSize.Width - 10;
            };

            // Borde redondeado
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radio = 14;
                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();
                using (var brushFondo = new SolidBrush(Color.White))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // === Título (sin emoji al inicio, más limpio) ===
            var lblTituloChart = new Label
            {
                Text = "Distribución por Nivel CMMI",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Empresas evaluadas en cada nivel de madurez",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            // === Badge con punto de color + "N empresas" ===
            var badge = CrearBadgeConPunto("0 EMPRESAS", out _lblBadgeCmmi);
            badge.Location = new Point(tarjeta.Width - badge.Width - 24, 20);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            string[] nombres = { "Inicial", "Gestionado", "Definido", "Gestionado cuantitativo", "Optimizado" };
            string[] descripciones = {
                "Nivel 1 · Inicial: Los procesos son ad-hoc, caóticos y dependen del esfuerzo individual. El éxito depende de personas heroicas, no de procesos establecidos.",
                "Nivel 2 · Gestionado: Los procesos se planifican y ejecutan según políticas. Se administran los requisitos, hay control del proyecto y las prácticas se repiten con consistencia.",
                "Nivel 3 · Definido: Los procesos están documentados y estandarizados a nivel de organización. Existe un enfoque preventivo y las mejoras se comparten entre proyectos.",
                "Nivel 4 · Gestionado cuantitativamente: Se recopilan métricas detalladas y se gestiona el rendimiento con análisis estadístico. Las decisiones se basan en datos objetivos.",
                "Nivel 5 · Optimizado: La mejora continua es el foco. Se innovan procesos y tecnologías proactivamente basándose en el análisis cuantitativo del rendimiento."
            };

            int yInicio = 78;
            int alturaFila = 40;

            for (int i = 0; i < 5; i++)
            {
                int y = yInicio + i * alturaFila;
                int nivel = i + 1;
                Color colorNivel = ColorDelNivel(nivel);

                // === Container de la fila (para destacar el más frecuente con fondo) ===
                var fondoFila = new Panel
                {
                    Location = new Point(16, y),
                    Size = new Size(tarjeta.Width - 32, alturaFila - 4),
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Tag = nivel
                };
                fondoFila.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(fondoFila, 8);
                tarjeta.Controls.Add(fondoFila);
                _fondosCmmi[i] = fondoFila;

                // === Chip cuadrado con número ===
                var chipNum = new Panel
                {
                    Location = new Point(8, 6),
                    Size = new Size(24, 24),
                    BackColor = VersionPastel(colorNivel)
                };
                chipNum.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(chipNum, 6);
                fondoFila.Controls.Add(chipNum);
                _chipsNumeroCmmi[i] = chipNum;

                var lblNum = new Label
                {
                    Text = nivel.ToString(),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = colorNivel,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                chipNum.Controls.Add(lblNum);
                _lblNumChipCmmi[i] = lblNum;

                // === Label nombre del nivel ===
                var lblNombre = new Label
                {
                    Text = nombres[i],
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(40, 10),
                    Size = new Size(180, 18),
                    BackColor = Color.Transparent
                };
                fondoFila.Controls.Add(lblNombre);
                _lblNombresNivel[i] = lblNombre;

                // === Badge "MÁS FRECUENTE" (oculto por defecto) ===
                var badgeFrec = new Panel
                {
                    Location = new Point(220, 10),
                    Size = new Size(88, 18),
                    BackColor = colorNivel,
                    Visible = false
                };
                badgeFrec.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(badgeFrec, 9);
                var lblBadgeFrec = new Label
                {
                    Text = "MÁS FRECUENTE",
                    Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                badgeFrec.Controls.Add(lblBadgeFrec);
                fondoFila.Controls.Add(badgeFrec);
                _badgesFrec[i] = badgeFrec;

                // === Track (barra fina de fondo) ===
                var track = new Panel
                {
                    Location = new Point(40, 32),
                    Size = new Size(fondoFila.Width - 40 - 90, 4),
                    BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                track.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(track, 2);
                fondoFila.Controls.Add(track);
                _tracksCmmi[i] = track;

                // === Fill (barra rellena) ===
                var barra = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(0, 4),
                    BackColor = colorNivel
                };
                barra.Resize += (s, e) =>
                {
                    if (barra.Width > 4)
                        Paleta.AplicarBordeRedondeadoSuave(barra, 2);
                };
                track.Controls.Add(barra);
                _barrasCmmi[i] = barra;

                // === Label valor (cantidad) ===
                var lblValor = new Label
                {
                    Text = "0",
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Size = new Size(30, 18),
                    Location = new Point(fondoFila.Width - 90, 10),
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                fondoFila.Controls.Add(lblValor);
                _valoresCmmi[i] = lblValor;

                // === Label porcentaje ===
                var lblPct = new Label
                {
                    Text = "—",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(140, 135, 132),
                    Size = new Size(46, 18),
                    Location = new Point(fondoFila.Width - 54, 10),
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                fondoFila.Controls.Add(lblPct);
                _porcentajesCmmi[i] = lblPct;

                // Reposicionar valor y porcentaje al cambiar tamaño
                fondoFila.Resize += (s, e) =>
                {
                    lblValor.Location = new Point(fondoFila.Width - 90, 10);
                    lblPct.Location = new Point(fondoFila.Width - 54, 10);
                    track.Size = new Size(fondoFila.Width - 40 - 90, 4);
                };

                // Tooltip con la descripción CMMI del nivel
                TooltipEstilizado.AplicarACascada(descripciones[i],
                    fondoFila, chipNum, lblNum, lblNombre, track, lblValor, lblPct);
            }

            // === Footer con INSIGHT ===
            var separador = new Panel
            {
                Location = new Point(24, yInicio + 5 * alturaFila + 6),
                Size = new Size(tarjeta.Width - 48, 1),
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(separador);
            tarjeta.Resize += (s, e) =>
            {
                separador.Size = new Size(tarjeta.Width - 48, 1);
            };

            var lblInsightTitulo = new Label
            {
                Text = "📊  INSIGHT",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, yInicio + 5 * alturaFila + 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblInsightTitulo);

            _lblInsightLinea1 = new Label
            {
                Text = "Aún no hay evaluaciones registradas.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(74, 70, 80),
                Location = new Point(24, yInicio + 5 * alturaFila + 38),
                Size = new Size(tarjeta.Width - 48, 18),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_lblInsightLinea1);

            _lblInsightLinea2 = new Label
            {
                Text = "Genera tu primera evaluación desde 'Cargar Informe'.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(74, 70, 80),
                Location = new Point(24, yInicio + 5 * alturaFila + 58),
                Size = new Size(tarjeta.Width - 48, 18),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_lblInsightLinea2);

            tarjeta.Resize += (s, e) =>
            {
                _lblInsightLinea1.Size = new Size(tarjeta.Width - 48, 18);
                _lblInsightLinea2.Size = new Size(tarjeta.Width - 48, 18);
            };

            // Tooltip sobre el badge del header explicando el total
            TooltipEstilizado.Aplicar(badge,
                "Total de empresas que aparecen en la distribución. " +
                "Solo cuenta empresas que ya tienen al menos una evaluación completada.");
        }

        // Badge estilo píldora con un punto de color al inicio + texto
        private Panel CrearBadgeConPunto(string texto, out Label lblTexto)
        {
            var badge = new Panel
            {
                Size = new Size(115, 24),
                BackColor = ColorTranslator.FromHtml("#F0EDF5")
            };
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, 24, 24, 90, 180);
            path.AddArc(badge.Width - 24, 0, 24, 24, 270, 180);
            path.CloseFigure();
            badge.Region = new Region(path);

            var punto = new Panel
            {
                Size = new Size(6, 6),
                Location = new Point(12, 9),
                BackColor = Paleta.MoradoOscuro
            };
            var pathPunto = new System.Drawing.Drawing2D.GraphicsPath();
            pathPunto.AddEllipse(0, 0, 6, 6);
            punto.Region = new Region(pathPunto);
            badge.Controls.Add(punto);

            lblTexto = new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(22, 4),
                Size = new Size(85, 16),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            badge.Controls.Add(lblTexto);

            return badge;
        }

        // ===================================================
        // CREAR UN BADGE PEQUEÑO (estilo píldora)
        // ===================================================
        private Panel CrearBadge(string texto)
        {
            var badge = new Panel
            {
                Size = new Size(110, 24),
                BackColor = ColorTranslator.FromHtml("#F0EDF5")
            };
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, 24, 24, 90, 180);
            path.AddArc(badge.Width - 24, 0, 24, 24, 270, 180);
            path.CloseFigure();
            badge.Region = new Region(path);

            var lbl = new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            badge.Controls.Add(lbl);

            return badge;
        }

        // ===================================================
        // GRÁFICO DE DONA: EMPRESAS POR SECTOR
        // ===================================================
        private void CrearSeccionGraficoSectores()
        {
            // Inicializar con datos de placeholder (todos 0)
            _sectoresData = new List<SectorDato>
    {
        new() { Nombre = "Software a medida", Cantidad = 0, Color = Paleta.MoradoOscuro },
        new() { Nombre = "SaaS / Web", Cantidad = 0, Color = Paleta.MoradoClaro },
        new() { Nombre = "Móvil", Cantidad = 0, Color = Paleta.VerdeGrisaceo },
        new() { Nombre = "Integraciones", Cantidad = 0, Color = Paleta.VerdeGrisaceoOscuro },
        new() { Nombre = "Otros", Cantidad = 0, Color = Color.FromArgb(178, 172, 169) }
    };

            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 685),
                Size = new Size(panelDashboard.ClientSize.Width - 10, 320),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(tarjeta);

            panelDashboard.Resize += (s, e) =>
            {
                tarjeta.Width = panelDashboard.ClientSize.Width - 10;
            };

            // Borde redondeado (mismo patrón que las otras tarjetas)
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radio = 14;

                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();

                using (var brushFondo = new SolidBrush(Color.White))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // === Título (sin emoji, más limpio) ===
            var lblTituloChart = new Label
            {
                Text = "Empresas por Sector",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Distribución de las empresas registradas según su rubro",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            // Badge con punto de color + total dinámico
            var badge = CrearBadgeConPunto("0 EMPRESAS", out _lblBadgeSectores);
            badge.Location = new Point(tarjeta.Width - badge.Width - 24, 20);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === Panel de la dona (izquierda) ===
            // El número central "N EMPRESAS" se dibuja DENTRO del Paint de la dona
            // (no como labels encima), para que no tapen el anillo con su fondo.
            _panelDona = new Panel
            {
                Location = new Point(48, 74),
                Size = new Size(168, 168),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(_panelDona);
            _panelDona.Paint += DibujarDona;

            // === Leyenda (derecha) — con más aire ===
            _flowLegendaSectores = new FlowLayoutPanel
            {
                Location = new Point(238, 76),
                Size = new Size(tarjeta.Width - 262, 190),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_flowLegendaSectores);

            foreach (var sector in _sectoresData)
            {
                _flowLegendaSectores.Controls.Add(CrearItemLeyenda(sector));
            }

            // === Footer con INSIGHT ===
            var separador = new Panel
            {
                Location = new Point(24, 262),
                Size = new Size(tarjeta.Width - 48, 1),
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(separador);

            var lblInsightTitulo = new Label
            {
                Text = "🏭  INSIGHT",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 268),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblInsightTitulo);

            _lblInsightSectores = new Label
            {
                Text = "Aún no hay empresas registradas.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(74, 70, 80),
                Location = new Point(24, 285),
                Size = new Size(tarjeta.Width - 48, 18),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_lblInsightSectores);

            tarjeta.Resize += (s, e) =>
            {
                _flowLegendaSectores.Width = tarjeta.Width - 262;
                separador.Size = new Size(tarjeta.Width - 48, 1);
                _lblInsightSectores.Size = new Size(tarjeta.Width - 48, 18);
            };

            TooltipEstilizado.Aplicar(badge,
                "Total de empresas registradas en el sistema, agrupadas por su rubro o sector.");
            TooltipEstilizado.Aplicar(_panelDona,
                "Distribución visual de las empresas según su rubro. " +
                "Cada segmento de la dona representa un sector; " +
                "los sectores sin empresas no aparecen en la dona.");
        }

        // ===================================================
        // DIBUJAR LA DONA (evento Paint)
        // El texto central "N / EMPRESAS" se dibuja aquí mismo con DrawString,
        // así queda dentro del hueco y NUNCA tapa el anillo.
        // ===================================================
        private void DibujarDona(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int total = 0;
            int sectoresConDatos = 0;
            foreach (var s in _sectoresData)
            {
                total += s.Cantidad;
                if (s.Cantidad > 0) sectoresConDatos++;
            }

            int tamano = Math.Min(_panelDona.Width, _panelDona.Height);
            int grosorAnillo = 20;
            var rectExterno = new Rectangle(0, 0, tamano - 1, tamano - 1);
            var rectInterno = new Rectangle(
                grosorAnillo, grosorAnillo,
                tamano - grosorAnillo * 2, tamano - grosorAnillo * 2);

            // === 1) Dibujar el anillo ===
            if (total == 0)
            {
                // Vacío: anillo gris muy claro
                using var brushVacio = new SolidBrush(ColorTranslator.FromHtml("#EDE9F0"));
                g.FillEllipse(brushVacio, rectExterno);
                using var brushBlanco = new SolidBrush(Color.White);
                g.FillEllipse(brushBlanco, rectInterno);
            }
            else if (sectoresConDatos == 1)
            {
                // Un solo sector con datos → anillo completo de su color
                var unico = _sectoresData.FirstOrDefault(s => s.Cantidad > 0)!;
                using (var brushUnico = new SolidBrush(unico.Color))
                    g.FillEllipse(brushUnico, rectExterno);
                using var brushBlanco = new SolidBrush(Color.White);
                g.FillEllipse(brushBlanco, rectInterno);
            }
            else
            {
                // Múltiples sectores: segmentos con gap sutil
                const float gapGrados = 3f;
                float anguloInicial = -90f;
                foreach (var sector in _sectoresData)
                {
                    if (sector.Cantidad <= 0) continue;
                    float porcentaje = (float)sector.Cantidad / total;
                    float grados = porcentaje * 360f - gapGrados;
                    if (grados < 1f) grados = 1f;
                    using var brushSector = new SolidBrush(sector.Color);
                    g.FillPie(brushSector, rectExterno, anguloInicial, grados);
                    anguloInicial += grados + gapGrados;
                }
                using var brushCentro = new SolidBrush(Color.White);
                g.FillEllipse(brushCentro, rectInterno);
            }

            // === 2) Dibujar el texto central (número + "EMPRESAS") ===
            // Se dibuja centrado dentro del hueco; nada lo tapa porque es parte del Paint.
            string numeroTxt = _totalDona.ToString();
            using var fontNumero = new Font("Segoe UI", 22, FontStyle.Bold);
            using var fontLabel = new Font("Segoe UI", 7, FontStyle.Bold);
            using var brushNumero = new SolidBrush(Paleta.TextoOscuro);
            using var brushLabel = new SolidBrush(Color.FromArgb(150, 145, 152));

            var sizeNum = g.MeasureString(numeroTxt, fontNumero);
            var sizeLbl = g.MeasureString("EMPRESAS", fontLabel);

            float centroX = tamano / 2f;
            // Bloque de texto (número arriba, label debajo) centrado verticalmente
            float alturaBloque = sizeNum.Height + sizeLbl.Height - 4;
            float yNum = tamano / 2f - alturaBloque / 2f;

            g.DrawString(numeroTxt, fontNumero, brushNumero,
                centroX - sizeNum.Width / 2f, yNum);
            g.DrawString("EMPRESAS", fontLabel, brushLabel,
                centroX - sizeLbl.Width / 2f, yNum + sizeNum.Height - 4);
        }

        // ===================================================
        // CREAR UN ÍTEM DE LEYENDA (rediseñado)
        // Layout: chip color 18x18 · nombre + mini progress bar · valor+% derecha
        // El porcentaje se guarda en el Tag del fill y se aplica en el Resize
        // del track (que dispara cuando el layout se estabiliza).
        // ===================================================
        private Panel CrearItemLeyenda(SectorDato sector, int porcentaje = 0)
        {
            bool vacio = sector.Cantidad == 0;

            var item = new Panel
            {
                Size = new Size(400, 36),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6),
                Tag = sector
            };

            // Chip cuadrado redondeado con color del sector
            var chipColor = new Panel
            {
                Size = new Size(18, 18),
                Location = new Point(0, 5),
                BackColor = vacio
                    ? ColorTranslator.FromHtml("#EAE6EC")
                    : sector.Color
            };
            var pathChip = new System.Drawing.Drawing2D.GraphicsPath();
            int rC = 4;
            pathChip.AddArc(0, 0, rC * 2, rC * 2, 180, 90);
            pathChip.AddArc(chipColor.Width - rC * 2, 0, rC * 2, rC * 2, 270, 90);
            pathChip.AddArc(chipColor.Width - rC * 2, chipColor.Height - rC * 2, rC * 2, rC * 2, 0, 90);
            pathChip.AddArc(0, chipColor.Height - rC * 2, rC * 2, rC * 2, 90, 90);
            pathChip.CloseFigure();
            chipColor.Region = new Region(pathChip);
            item.Controls.Add(chipColor);

            // Nombre del sector
            var lblNombre = new Label
            {
                Text = sector.Nombre,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = vacio
                    ? Color.FromArgb(180, 175, 185)
                    : Paleta.TextoOscuro,
                Location = new Point(28, 2),
                Size = new Size(220, 16),
                BackColor = Color.Transparent
            };
            item.Controls.Add(lblNombre);

            // Track+fill dibujados con Paint (más robusto que usar Panel hijo con Anchor).
            // Todo el rendering es autónomo: no depende del sistema de layout ni Resize.
            Color colorTrack = ColorTranslator.FromHtml("#F2EFF6");   // muy claro para contraste
            Color colorFill = vacio ? colorTrack : sector.Color;

            var trackBar = new Panel
            {
                Location = new Point(28, 22),
                Size = new Size(200, 5),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            trackBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // 1) Track: rectángulo redondeado en color gris muy suave
                var rectTrack = new Rectangle(0, 0, trackBar.Width - 1, trackBar.Height - 1);
                using var pathTrack = RectRedondeado(rectTrack, 2);
                using (var brushTr = new SolidBrush(colorTrack))
                    g.FillPath(brushTr, pathTrack);

                // 2) Fill: encima del track, ancho proporcional al porcentaje
                if (porcentaje > 0 && !vacio)
                {
                    int anchoFill = trackBar.Width * porcentaje / 100;
                    if (anchoFill < 5) anchoFill = 5;  // mínimo visible
                    var rectFill = new Rectangle(0, 0, anchoFill - 1, trackBar.Height - 1);
                    using var pathFill = RectRedondeado(rectFill, 2);
                    using var brushFi = new SolidBrush(colorFill);
                    g.FillPath(brushFi, pathFill);
                }
            };
            trackBar.Resize += (s, e) => trackBar.Invalidate();  // repintar al cambiar ancho
            item.Controls.Add(trackBar);
            var track = trackBar;  // alias para el item.Resize de abajo

            // Valor (cantidad · %) — en gris muted si está vacío
            var lblValor = new Label
            {
                Text = vacio ? "—" : $"{sector.Cantidad} · {porcentaje}%",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = vacio
                    ? Color.FromArgb(180, 175, 185)
                    : Paleta.TextoOscuro,
                Location = new Point(250, 4),
                Size = new Size(130, 18),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            item.Controls.Add(lblValor);

            item.Resize += (s, e) =>
            {
                track.Size = new Size(item.Width - 28 - 145, 5);
            };

            return item;
        }

        // ===================================================
        // GRÁFICO DE LÍNEAS: ACTIVIDAD DE LOS ÚLTIMOS 7 DÍAS
        // ===================================================
        private void CrearSeccionGraficoLineas()
        {
            // Inicializar nombres y fechas de los últimos 7 días
            var hoy = DateTime.Now;
            for (int i = 0; i < 7; i++)
            {
                var fecha = hoy.AddDays(-(6 - i));
                _fechasDias[i] = fecha.Date;
                _nombresDias[i] = fecha.ToString("ddd").ToUpper();  // LUN, MAR, MIE...
                _evaluacionesPorSemana[i] = 0;
            }

            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 1025),  // desplazado por altura +30 de sectores
                Size = new Size(panelDashboard.ClientSize.Width - 10, 320),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(tarjeta);

            panelDashboard.Resize += (s, e) =>
            {
                tarjeta.Width = panelDashboard.ClientSize.Width - 10;
            };

            // Borde redondeado
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radio = 14;

                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();

                using (var brushFondo = new SolidBrush(Color.White))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // === Título limpio ===
            var lblTituloChart = new Label
            {
                Text = "Actividad de los Últimos 7 Días",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Evaluaciones generadas día a día durante esta semana",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            // Badge con rango de fechas (dinámico)
            var hoyInicio = DateTime.Now.Date;
            var rangoIni = hoyInicio.AddDays(-6);
            string textoBadge = $"{rangoIni:dd MMM}  →  {hoyInicio:dd MMM}".ToUpperInvariant();
            var badge = CrearBadgeConPunto(textoBadge, out _lblBadgeActividad);
            badge.Size = new Size(150, 24);
            var pathBadge = new System.Drawing.Drawing2D.GraphicsPath();
            pathBadge.AddArc(0, 0, 24, 24, 90, 180);
            pathBadge.AddArc(badge.Width - 24, 0, 24, 24, 270, 180);
            pathBadge.CloseFigure();
            badge.Region = new Region(pathBadge);
            _lblBadgeActividad.Size = new Size(120, 16);
            badge.Location = new Point(tarjeta.Width - badge.Width - 24, 20);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === Panel del gráfico ===
            _panelLineas = new Panel
            {
                Location = new Point(40, 76),
                Size = new Size(tarjeta.Width - 60, 170),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_panelLineas);
            _panelLineas.Paint += DibujarGraficoLineas;

            // === Footer con INSIGHT ===
            var separador = new Panel
            {
                Location = new Point(24, 258),
                Size = new Size(tarjeta.Width - 48, 1),
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(separador);

            var lblInsightTitulo = new Label
            {
                Text = "📈  INSIGHT",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 268),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblInsightTitulo);

            _lblInsightActividad = new Label
            {
                Text = "Sin actividad registrada en los últimos 7 días.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(74, 70, 80),
                Location = new Point(24, 286),
                Size = new Size(tarjeta.Width - 48, 18),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_lblInsightActividad);

            tarjeta.Resize += (s, e) =>
            {
                _panelLineas.Width = tarjeta.Width - 60;
                _panelLineas.Invalidate();
                separador.Size = new Size(tarjeta.Width - 48, 1);
                _lblInsightActividad.Size = new Size(tarjeta.Width - 48, 18);
            };

            TooltipEstilizado.Aplicar(badge,
                "Rango de fechas cubierto por el gráfico: los últimos 7 días. " +
                "El extremo derecho es hoy.");
            TooltipEstilizado.AplicarACascada(
                "Cada punto representa el número de evaluaciones generadas ese día. " +
                "El punto de mayor actividad de la semana se destaca con un anillo verde. " +
                "HOY aparece resaltado en morado.",
                _panelLineas);
        }

        // Calcula la escala del eje Y y las posiciones (X,Y) de cada punto.
        // Guarda el resultado en _puntosLineas para reuso (Paint + hotspots).
        private void CalcularGeometriaLineas(
            out int maxEje, out int numPasos,
            out int padIzq, out int padArr, out int areaAncho, out int areaAlto)
        {
            padIzq = 38;
            int padDer = 12;
            padArr = 12;
            int padAbj = 34;
            areaAncho = Math.Max(1, _panelLineas.Width - padIzq - padDer);
            areaAlto = Math.Max(1, _panelLineas.Height - padArr - padAbj);

            // Escala con pasos ENTEROS (nunca valores duplicados en el eje)
            int maxReal = _evaluacionesPorSemana.Max();
            if (maxReal == 0) { maxEje = 3; numPasos = 3; }
            else if (maxReal <= 2) { maxEje = 2; numPasos = 2; }
            else if (maxReal <= 4) { maxEje = 4; numPasos = 4; }
            else if (maxReal <= 5) { maxEje = 5; numPasos = 5; }
            else if (maxReal <= 8) { maxEje = 8; numPasos = 4; }
            else if (maxReal <= 10) { maxEje = 10; numPasos = 5; }
            else { maxEje = ((maxReal / 5) + 1) * 5; numPasos = 5; }

            for (int i = 0; i < 7; i++)
            {
                float x = padIzq + (areaAncho * i / 6f);
                float y = padArr + areaAlto - (_evaluacionesPorSemana[i] * areaAlto / (float)maxEje);
                _puntosLineas[i] = new PointF(x, y);
            }
        }

        // ===================================================
        // DIBUJAR EL GRÁFICO DE LÍNEAS (evento Paint)
        // Toques pro: burbujas de valor sobre los puntos, fechas bajo los días,
        // línea guía punteada en el pico, gradiente de área refinado.
        // ===================================================
        private void DibujarGraficoLineas(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            CalcularGeometriaLineas(out int maxEje, out int numPasos,
                out int padIzq, out int padArr, out int areaAncho, out int areaAlto);
            var puntos = _puntosLineas;
            int baseY = padArr + areaAlto;
            int diaHoyIdx = 6;
            bool hayDatos = _evaluacionesPorSemana.Sum() > 0;

            // === Grid horizontal + labels eje Y ===
            using var penGrid = new Pen(ColorTranslator.FromHtml("#F4F1F7"), 1);
            using var brushTextoEje = new SolidBrush(Color.FromArgb(165, 160, 168));
            using var fontEje = new Font("Segoe UI", 7.5f);
            for (int i = 0; i <= numPasos; i++)
            {
                int y = padArr + (areaAlto * i / numPasos);
                g.DrawLine(penGrid, padIzq, y, padIzq + areaAncho, y);
                int valor = maxEje - (maxEje * i / numPasos);
                var size = g.MeasureString(valor.ToString(), fontEje);
                g.DrawString(valor.ToString(), fontEje, brushTextoEje,
                    padIzq - size.Width - 6, y - size.Height / 2);
            }

            // === Línea guía punteada vertical en el día pico ===
            if (hayDatos && _diaPicoIdx >= 0 && _evaluacionesPorSemana[_diaPicoIdx] > 0)
            {
                using var penGuia = new Pen(Color.FromArgb(90, Paleta.MoradoClaro), 1)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                };
                g.DrawLine(penGuia, puntos[_diaPicoIdx].X, puntos[_diaPicoIdx].Y + 6,
                    puntos[_diaPicoIdx].X, baseY);
            }

            // === Área bajo la línea con gradiente vertical ===
            if (hayDatos)
            {
                var puntosArea = new List<PointF>(puntos);
                puntosArea.Add(new PointF(puntos[^1].X, baseY));
                puntosArea.Add(new PointF(puntos[0].X, baseY));

                var rectArea = new Rectangle(padIzq, padArr, areaAncho, areaAlto + 1);
                using var brushArea = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rectArea,
                    Color.FromArgb(95, Paleta.MoradoClaro),
                    Color.FromArgb(6, Paleta.MoradoClaro),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical);
                g.FillPolygon(brushArea, puntosArea.ToArray());
            }

            // === Línea conectora ===
            using var penLinea = new Pen(Paleta.MoradoOscuro, 2.8f)
            {
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            g.DrawLines(penLinea, puntos);

            // === Puntos (halo pico + borde blanco) ===
            for (int i = 0; i < puntos.Length; i++)
            {
                bool esPico = (i == _diaPicoIdx && _evaluacionesPorSemana[i] > 0);
                bool esHoy = (i == diaHoyIdx);
                int radio = (esPico || esHoy) ? 5 : 4;

                if (esPico)
                {
                    using var brushHalo = new SolidBrush(Color.FromArgb(130, Paleta.VerdeBrillante));
                    g.FillEllipse(brushHalo,
                        puntos[i].X - radio - 4, puntos[i].Y - radio - 4,
                        (radio + 4) * 2, (radio + 4) * 2);
                }

                using var brushBordeBlanco = new SolidBrush(Color.White);
                g.FillEllipse(brushBordeBlanco,
                    puntos[i].X - radio - 1.5f, puntos[i].Y - radio - 1.5f,
                    (radio + 1.5f) * 2, (radio + 1.5f) * 2);
                using var brushPunto = new SolidBrush(Paleta.MoradoOscuro);
                g.FillEllipse(brushPunto,
                    puntos[i].X - radio, puntos[i].Y - radio, radio * 2, radio * 2);
            }

            // === Burbujas de valor sobre los puntos con dato > 0 ===
            using var fontBurbuja = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            for (int i = 0; i < puntos.Length; i++)
            {
                if (_evaluacionesPorSemana[i] <= 0) continue;

                string valTxt = _evaluacionesPorSemana[i].ToString();
                var sz = g.MeasureString(valTxt, fontBurbuja);
                int bw = (int)sz.Width + 12;
                int bh = 17;
                float bx = puntos[i].X - bw / 2f;
                float by = puntos[i].Y - radioPuntoConHalo(i) - bh - 4;
                if (by < 0) by = puntos[i].Y + 8;  // si no cabe arriba, ponerla abajo

                var rectBurbuja = new Rectangle((int)bx, (int)by, bw, bh);
                using var pathBurbuja = RectRedondeado(rectBurbuja, bh / 2);
                using (var brushB = new SolidBrush(Paleta.MoradoOscuro))
                    g.FillPath(brushB, pathBurbuja);
                using (var brushT = new SolidBrush(Color.White))
                    g.DrawString(valTxt, fontBurbuja, brushT,
                        puntos[i].X - sz.Width / 2f, by + 2);
            }

            // === Labels eje X: día + fecha; HOY en negrita morada ===
            using var fontDiaNormal = new Font("Segoe UI", 8, FontStyle.Bold);
            using var fontDiaHoy = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var fontFecha = new Font("Segoe UI", 6.5f);
            using var brushDiaNormal = new SolidBrush(Color.FromArgb(140, 135, 132));
            using var brushDiaHoy = new SolidBrush(Paleta.MoradoOscuro);
            using var brushFecha = new SolidBrush(Color.FromArgb(180, 175, 182));

            for (int i = 0; i < _nombresDias.Length; i++)
            {
                bool esHoy = (i == diaHoyIdx);
                var font = esHoy ? fontDiaHoy : fontDiaNormal;
                var brush = esHoy ? brushDiaHoy : brushDiaNormal;
                string texto = esHoy ? "HOY" : _nombresDias[i];

                var size = g.MeasureString(texto, font);
                g.DrawString(texto, font, brush,
                    puntos[i].X - size.Width / 2, baseY + 8);

                // Fecha pequeña debajo (día del mes)
                string fechaTxt = _fechasDias[i].ToString("dd MMM");
                var sizeF = g.MeasureString(fechaTxt, fontFecha);
                g.DrawString(fechaTxt, fontFecha, brushFecha,
                    puntos[i].X - sizeF.Width / 2, baseY + 22);
            }
        }

        // Radio efectivo del punto (incluye halo si es el pico) para posicionar burbujas
        private float radioPuntoConHalo(int i)
        {
            bool esPico = (i == _diaPicoIdx && _evaluacionesPorSemana[i] > 0);
            return esPico ? 9f : 5f;
        }

        // ===================================================
        // ÁREAS CRÍTICAS DETECTADAS (top 5 debilidades)
        // ===================================================
        private void CrearSeccionAreasCriticas()
        {
            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 1365),  // debajo del gráfico de líneas (1025 + 320 + 20)
                Size = new Size(panelDashboard.ClientSize.Width - 10, 350),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(tarjeta);

            panelDashboard.Resize += (s, e) =>
            {
                tarjeta.Width = panelDashboard.ClientSize.Width - 10;
            };

            // Borde redondeado (mismo patrón)
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radio = 14;

                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();

                using (var brushFondo = new SolidBrush(Color.White))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // === Título (sin trofeo: son debilidades, no logros) ===
            var lblTituloChart = new Label
            {
                Text = "Áreas Críticas Detectadas",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Debilidades más frecuentes encontradas en las evaluaciones",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            // Badge ámbar con punto (semántica de "alerta")
            var badge = CrearBadgeConPunto("TOP 5", out _lblBadgeCriticas);
            badge.Location = new Point(tarjeta.Width - badge.Width - 24, 20);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            int yInicio = 78;
            int alturaFila = 40;

            for (int i = 0; i < 5; i++)
            {
                int y = yInicio + i * alturaFila;
                int rank = i + 1;
                Color colorRank = ColorCriticidad(rank);

                // Container de la fila (para destacar el #1 con fondo tenue)
                var fondoFila = new Panel
                {
                    Location = new Point(16, y),
                    Size = new Size(tarjeta.Width - 32, alturaFila - 4),
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Tag = rank
                };
                fondoFila.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(fondoFila, 8);
                tarjeta.Controls.Add(fondoFila);
                _fondosCriticas[i] = fondoFila;

                // Chip cuadrado con el ranking
                var chipRank = new Panel
                {
                    Location = new Point(8, 6),
                    Size = new Size(24, 24),
                    BackColor = VersionPastel(colorRank)
                };
                chipRank.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(chipRank, 6);
                fondoFila.Controls.Add(chipRank);
                _chipsRankCriticas[i] = chipRank;

                var lblRank = new Label
                {
                    Text = rank.ToString(),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = colorRank,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                chipRank.Controls.Add(lblRank);
                _lblRankCriticas[i] = lblRank;

                // Nombre de la debilidad (ancho acotado para no tapar el badge)
                var lblNombre = new Label
                {
                    Text = "—",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(40, 6),
                    Size = new Size(168, 18),
                    BackColor = Color.Transparent,
                    AutoEllipsis = true
                };
                fondoFila.Controls.Add(lblNombre);
                _nombresCriticas[i] = lblNombre;

                // Badge "MÁS FRECUENTE" (solo el #1, oculto por defecto)
                var badgeFrec = new Panel
                {
                    Location = new Point(214, 7),
                    Size = new Size(96, 17),
                    BackColor = colorRank,
                    Visible = false
                };
                badgeFrec.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(badgeFrec, 9);
                var lblBadgeFrec = new Label
                {
                    Text = "MÁS FRECUENTE",
                    Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                badgeFrec.Controls.Add(lblBadgeFrec);
                fondoFila.Controls.Add(badgeFrec);
                _badgesCriticas[i] = badgeFrec;

                // Track de la barra (fina)
                var track = new Panel
                {
                    Location = new Point(40, 30),
                    Size = new Size(fondoFila.Width - 40 - 90, 6),
                    BackColor = ColorTranslator.FromHtml("#F2EFF6"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                track.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(track, 3);
                fondoFila.Controls.Add(track);
                _tracksCriticas[i] = track;

                // Barra rellena con gradiente del color de criticidad
                var barra = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(0, 6),
                    BackColor = colorRank,
                    Tag = colorRank
                };
                barra.Paint += (s, e) =>
                {
                    if (barra.Width < 4) return;
                    var g2 = e.Graphics;
                    g2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    var rect = new Rectangle(0, 0, barra.Width, barra.Height);
                    var cBase = (Color)(barra.Tag ?? Paleta.MoradoOscuro);
                    using var brushGrad = new System.Drawing.Drawing2D.LinearGradientBrush(
                        rect, MezclarConNegro(cBase, 0.12f), cBase,
                        System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                    using var pathB = RectRedondeado(rect, 3);
                    g2.FillPath(brushGrad, pathB);
                };
                barra.Resize += (s, e) => barra.Invalidate();
                track.Controls.Add(barra);
                _barrasCriticas[i] = barra;

                // Valor (cantidad) a la derecha
                var lblValor = new Label
                {
                    Text = "0",
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Size = new Size(40, 18),
                    Location = new Point(fondoFila.Width - 84, 4),
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                fondoFila.Controls.Add(lblValor);
                _valoresCriticas[i] = lblValor;

                // Subtítulo pequeño "menciones" debajo del valor
                var lblSub = new Label
                {
                    Text = "menciones",
                    Font = new Font("Segoe UI", 6.5f),
                    ForeColor = Color.FromArgb(160, 155, 162),
                    Size = new Size(70, 12),
                    Location = new Point(fondoFila.Width - 84, 22),
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                fondoFila.Controls.Add(lblSub);
                _subCriticas[i] = lblSub;

                // Reposicionar valor/sub/track al cambiar tamaño
                fondoFila.Resize += (s, e) =>
                {
                    lblValor.Location = new Point(fondoFila.Width - 84, 4);
                    lblSub.Location = new Point(fondoFila.Width - 84, 22);
                    track.Size = new Size(fondoFila.Width - 40 - 90, 6);
                };
            }

            // === Footer con INSIGHT ===
            var separador = new Panel
            {
                Location = new Point(24, yInicio + 5 * alturaFila + 6),
                Size = new Size(tarjeta.Width - 48, 1),
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(separador);

            var lblInsightTitulo = new Label
            {
                Text = "⚠️  INSIGHT",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, yInicio + 5 * alturaFila + 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblInsightTitulo);

            _lblInsightCriticas = new Label
            {
                Text = "Aún no hay debilidades detectadas.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(74, 70, 80),
                Location = new Point(24, yInicio + 5 * alturaFila + 38),
                Size = new Size(tarjeta.Width - 48, 18),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_lblInsightCriticas);

            tarjeta.Resize += (s, e) =>
            {
                separador.Size = new Size(tarjeta.Width - 48, 1);
                _lblInsightCriticas.Size = new Size(tarjeta.Width - 48, 18);
            };

            TooltipEstilizado.Aplicar(badge,
                "Las 5 debilidades mencionadas con más frecuencia en las evaluaciones. " +
                "Son las áreas donde más empresas necesitan mejorar.");
        }

        // Color de criticidad según el ranking (heat ramp: rojo el peor → ámbar el menos grave)
        private Color ColorCriticidad(int rank) => rank switch
        {
            1 => ColorTranslator.FromHtml("#C13F3F"),  // rojo — más crítica
            2 => ColorTranslator.FromHtml("#D4841C"),  // naranja
            3 => ColorTranslator.FromHtml("#D99A2B"),  // ámbar
            4 => ColorTranslator.FromHtml("#C9A24B"),  // ámbar apagado
            5 => ColorTranslator.FromHtml("#B0A66A"),  // oliva suave
            _ => ColorTranslator.FromHtml("#B0A66A")
        };

        // Mezcla un color con negro (factor 0..1) para el extremo oscuro del gradiente
        private Color MezclarConNegro(Color c, float factor)
        {
            int r = (int)(c.R * (1 - factor));
            int g = (int)(c.G * (1 - factor));
            int b = (int)(c.B * (1 - factor));
            return Color.FromArgb(r, g, b);
        }

        // ===================================================
        // LISTA DE ÚLTIMAS EVALUACIONES
        // ===================================================
        private void CrearSeccionUltimasEvaluaciones()
        {
            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 1735),  // debajo de áreas críticas (1365 + 350 + 20)
                Size = new Size(panelDashboard.ClientSize.Width - 10, 380),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(tarjeta);

            panelDashboard.Resize += (s, e) =>
            {
                tarjeta.Width = panelDashboard.ClientSize.Width - 10;
            };

            // Borde redondeado
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radio = 14;

                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();

                using (var brushFondo = new SolidBrush(Color.White))
                    g.FillPath(brushFondo, pathFondo);
                using (var penBorde = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(penBorde, pathFondo);
            };
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // === Título ===
            var lblTituloChart = new Label
            {
                Text = "🕐  Últimas Evaluaciones",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Análisis más recientes realizados en el sistema",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(20, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            var badge = CrearBadge("Top 5 recientes");
            badge.Location = new Point(tarjeta.Width - badge.Width - 20, 22);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === FlowLayout para las filas de evaluaciones ===
            _flowUltimasEvaluaciones = new FlowLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(tarjeta.Width - 40, 280),
                BackColor = Color.White,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_flowUltimasEvaluaciones);

            // Re-ajustar ancho de la lista
            tarjeta.Resize += (s, e) =>
            {
                _flowUltimasEvaluaciones.Width = tarjeta.Width - 40;
            };

            // Mensaje placeholder cuando no hay datos
            var lblVacio = new Label
            {
                Text = "Aún no hay evaluaciones registradas.\nGenera tu primera evaluación desde 'Cargar Informe'.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 135, 132),
                Size = new Size(tarjeta.Width - 80, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 60, 0, 0)
            };
            _flowUltimasEvaluaciones.Controls.Add(lblVacio);
        }

        // ===================================================
        // CREAR UNA FILA DE EVALUACIÓN (placeholder, se usará en Tarea 9)
        // ===================================================
        private Panel CrearFilaEvaluacion(string inicial, string nombre, string detalle, int nivel, string tiempoRelativo)
        {
            Color filaNormal = ColorTranslator.FromHtml("#F9F5FF");
            Color filaHover = ColorTranslator.FromHtml("#F0E9FB");

            var fila = new Panel
            {
                Size = new Size(_flowUltimasEvaluaciones.Width - 20, 56),
                BackColor = filaNormal,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12, 10, 12, 10)
            };
            fila.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(fila, 12);

            // === Avatar circular con inicial ===
            var avatar = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(8, 10),
                BackColor = Paleta.MoradoOscuro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, 36, 36);
            avatar.Region = new Region(pathAv);

            var lblInicial = new Label
            {
                Text = inicial,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);
            fila.Controls.Add(avatar);

            // === Nombre de la empresa ===
            var lblNombre = new Label
            {
                Text = nombre,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(56, 8),
                Size = new Size(fila.Width - 280, 18),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            fila.Controls.Add(lblNombre);

            // === Detalles (sector · RIF) ===
            var lblDetalle = new Label
            {
                Text = detalle,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(56, 28),
                Size = new Size(fila.Width - 280, 16),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            fila.Controls.Add(lblDetalle);

            // === Badge de nivel (color según el nivel) ===
            Color colorBadge;
            if (nivel <= 1) colorBadge = Color.FromArgb(178, 172, 169);  // gris
            else if (nivel == 2) colorBadge = Paleta.VerdeGrisaceoOscuro;
            else if (nivel == 3) colorBadge = Paleta.VerdeGrisaceo;
            else if (nivel == 4) colorBadge = Paleta.MoradoClaro;
            else colorBadge = Paleta.MoradoOscuro;

            var badgeNivel = new Panel
            {
                Size = new Size(80, 24),
                BackColor = colorBadge,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            badgeNivel.Location = new Point(fila.Width - 200, 16);
            var pathBadge = new System.Drawing.Drawing2D.GraphicsPath();
            pathBadge.AddArc(0, 0, 24, 24, 90, 180);
            pathBadge.AddArc(badgeNivel.Width - 24, 0, 24, 24, 270, 180);
            pathBadge.CloseFigure();
            badgeNivel.Region = new Region(pathBadge);

            var lblNivel = new Label
            {
                Text = $"Nivel {nivel}",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            badgeNivel.Controls.Add(lblNivel);
            fila.Controls.Add(badgeNivel);

            // === Tiempo relativo ===
            var lblTiempo = new Label
            {
                Text = tiempoRelativo,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblTiempo.Location = new Point(fila.Width - 110, 18);
            fila.Controls.Add(lblTiempo);

            // Reposicionar el badge y tiempo al redimensionar
            fila.Resize += (s, e) =>
            {
                badgeNivel.Location = new Point(fila.Width - 200, 16);
                lblTiempo.Location = new Point(fila.Width - 110, 18);
            };

            // Hover: la fila se ilumina al pasar el mouse. Aplicado a la fila y a sus
            // hijos para evitar parpadeo al mover el cursor entre labels.
            void AplicarHover() => fila.BackColor = filaHover;
            void QuitarHover()
            {
                if (!fila.ClientRectangle.Contains(fila.PointToClient(Cursor.Position)))
                    fila.BackColor = filaNormal;
            }
            foreach (Control ctrl in new Control[] { fila, lblNombre, lblDetalle, lblTiempo, avatar, lblInicial })
            {
                ctrl.MouseEnter += (s, e) => AplicarHover();
                ctrl.MouseLeave += (s, e) => QuitarHover();
            }

            return fila;
        }

        // ===================================================
        // CARGAR DATOS REALES DE LA BD Y ACTUALIZAR EL DASHBOARD
        // ===================================================
        private void CargarDatosReales()
        {
            try
            {
                // Cargar datos desde la BD
                var empresas = _repoEmpresa.ObtenerTodas();
                var conversaciones = _repoConversacion.ObtenerTodas();
                var diagnosticos = new List<Modelos.Diagnostico>();
                foreach (var conv in conversaciones)
                {
                    var diagsConv = _repoDiagnostico.ObtenerHistorialPorConversacion(conv.Id);
                    diagnosticos.AddRange(diagsConv);
                }

                // Actualizar cada sección
                ActualizarKpisPrincipales(empresas, diagnosticos);
                ActualizarKpisSecundarios(empresas, diagnosticos);
                ActualizarGraficoCmmi(diagnosticos);
                ActualizarGraficoSectores(empresas);
                ActualizarGraficoLineas(diagnosticos);
                ActualizarAreasCriticas(diagnosticos);
                ActualizarUltimasEvaluaciones(empresas, conversaciones, diagnosticos);
            }
            catch (Exception ex)
            {
                Estilos.MensajeApp.Error($"Error al cargar datos del dashboard:\n{ex.Message}",
                    "Error", this.FindForm());
            }
        }

        // ===================================================
        // ACTUALIZAR KPIs PRINCIPALES (4 tarjetas)
        // ===================================================
        private void ActualizarKpisPrincipales(
            List<Modelos.Empresa> empresas,
            List<Modelos.Diagnostico> diagnosticos)
        {
            // KPI 1: Empresas registradas
            lblKpiEmpresas.Text = empresas.Count.ToString();

            // KPI 2: Evaluaciones (total de diagnósticos)
            lblKpiEvaluaciones.Text = diagnosticos.Count.ToString();

            // KPI 3: Nivel CMMI promedio + nombre del nivel más cercano
            if (diagnosticos.Count > 0)
            {
                double promedio = diagnosticos.Average(d => d.NivelMadurez);
                lblKpiNivelPromedio.Text = promedio.ToString("F1");
                int nivelRedondeado = (int)Math.Round(promedio);
                if (nivelRedondeado < 1) nivelRedondeado = 1;
                if (nivelRedondeado > 5) nivelRedondeado = 5;
                lblKpiNivelPromedioSub.Text = NombreDelNivel(nivelRedondeado);
            }
            else
            {
                lblKpiNivelPromedio.Text = "—";
                lblKpiNivelPromedioSub.Text = "Sin datos aún";
            }

            // KPI 4: Este mes
            var hoy = DateTime.Now;
            int delMes = diagnosticos.Count(d =>
                d.FechaGeneracion.Month == hoy.Month &&
                d.FechaGeneracion.Year == hoy.Year);
            lblKpiEsteMes.Text = delMes.ToString();
        }

        // ===================================================
        // ACTUALIZAR KPIs SECUNDARIOS (3 tarjetas)
        // ===================================================
        private void ActualizarKpisSecundarios(
            List<Modelos.Empresa> empresas,
            List<Modelos.Diagnostico> diagnosticos)
        {
            int totalEmpresas = empresas.Count;
            var empresaIdsConDiag = diagnosticos
                .Select(d => d.ConversacionId)
                .Distinct()
                .ToList();
            var empresaIdsEvaluadas = new HashSet<int>();
            foreach (var convId in empresaIdsConDiag)
            {
                var conv = _repoConversacion.ObtenerPorId(convId);
                if (conv != null) empresaIdsEvaluadas.Add(conv.EmpresaId);
            }

            // KPI 1: Empresas evaluadas + progress bar + "de X"
            int evaluadas = empresaIdsEvaluadas.Count;
            double porcentaje = totalEmpresas > 0 ? (evaluadas * 100.0 / totalEmpresas) : 0;
            lblKpiEmpresasEvaluadas.Text = $"{evaluadas}";
            lblKpiEmpresasEvaluadasDeX.Text = $"de {totalEmpresas}";
            lblKpiEmpresasEvaluadasSub.Text = totalEmpresas > 0
                ? $"{porcentaje:F0}% del total registradas"
                : "Sin empresas registradas aún";
            if (_trackEmpEval.Width > 0)
            {
                _progressEmpEval.Width = (int)(_trackEmpEval.Width * porcentaje / 100.0);
            }

            // KPI 2: Evaluación más reciente + pill nivel + fecha exacta
            if (diagnosticos.Count > 0)
            {
                var masReciente = diagnosticos.OrderByDescending(d => d.FechaGeneracion).First();
                var conv = _repoConversacion.ObtenerPorId(masReciente.ConversacionId);
                var empresa = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;

                lblKpiEvalReciente.Text = empresa?.Nombre ?? "—";
                _lblPillNivelReciente.Text = $"NIVEL {masReciente.NivelMadurez}";
                _pillNivelReciente.BackColor = ColorDelNivel(masReciente.NivelMadurez);
                _pillNivelReciente.Visible = true;
                lblKpiEvalRecienteSub.Text = $"{ObtenerTiempoRelativo(masReciente.FechaGeneracion)} · {masReciente.FechaGeneracion:dd/MM/yyyy}";
            }
            else
            {
                lblKpiEvalReciente.Text = "—";
                _pillNivelReciente.Visible = false;
                lblKpiEvalRecienteSub.Text = "Sin evaluaciones aún";
            }

            // KPI 3: Nivel más frecuente + mini barritas + nombre
            if (diagnosticos.Count > 0)
            {
                // Contar por nivel
                int[] cantidadPorNivel = new int[5];
                foreach (var d in diagnosticos)
                {
                    int idx = d.NivelMadurez - 1;
                    if (idx >= 0 && idx < 5) cantidadPorNivel[idx]++;
                }

                int maxNivel = cantidadPorNivel.Max();
                int nivelFrecuente = Array.IndexOf(cantidadPorNivel, maxNivel) + 1;

                lblKpiNivelFrecuente.Text = nivelFrecuente.ToString();
                lblKpiNivelFrecuenteNombre.Text = NombreDelNivel(nivelFrecuente);
                lblKpiNivelFrecuenteSub.Text = $"{maxNivel} de {diagnosticos.Count} evaluaciones";

                // Actualizar mini barritas (altura proporcional, 22px max)
                int alturaMax = 22;
                for (int i = 0; i < 5; i++)
                {
                    int alto = maxNivel > 0
                        ? Math.Max(4, (int)(cantidadPorNivel[i] * alturaMax / (double)maxNivel))
                        : 4;
                    // Reposicionar para que crezcan hacia arriba desde su base (y+22)
                    int baseY = 112 + alturaMax - alto;
                    _miniBarrasNivelFrec[i].Location = new Point(_miniBarrasNivelFrec[i].Location.X, baseY);
                    _miniBarrasNivelFrec[i].Height = alto;
                    _miniBarrasNivelFrec[i].BackColor = (i + 1 == nivelFrecuente)
                        ? Paleta.MoradoClaro
                        : ColorTranslator.FromHtml("#DDD5E0");
                }
            }
            else
            {
                lblKpiNivelFrecuente.Text = "—";
                lblKpiNivelFrecuenteNombre.Text = "Sin datos";
                lblKpiNivelFrecuenteSub.Text = "0 de 0 evaluaciones";
                for (int i = 0; i < 5; i++)
                {
                    _miniBarrasNivelFrec[i].Height = 4;
                    _miniBarrasNivelFrec[i].Location = new Point(_miniBarrasNivelFrec[i].Location.X, 130);
                    _miniBarrasNivelFrec[i].BackColor = ColorTranslator.FromHtml("#EEE9F0");
                }
            }
        }

        // Devuelve el color asociado al nivel CMMI para pills, chips, etc.
        private Color ColorDelNivel(int nivel) => nivel switch
        {
            1 => Color.FromArgb(178, 172, 169),
            2 => Paleta.VerdeGrisaceoOscuro,
            3 => Paleta.VerdeGrisaceo,
            4 => Paleta.MoradoClaro,
            5 => Paleta.MoradoOscuro,
            _ => Color.FromArgb(178, 172, 169)
        };

        // ===================================================
        // HELPER: convertir nivel CMMI a su nombre
        // ===================================================
        private string NombreDelNivel(int nivel)
        {
            return nivel switch
            {
                1 => "Inicial",
                2 => "Gestionado",
                3 => "Definido",
                4 => "Gest. cuant.",
                5 => "Optimizado",
                _ => "Desconocido"
            };
        }

        // ===================================================
        // HELPER: tiempo relativo ("Hace 5 min", "Hace 2 h", etc.)
        // ===================================================
        private string ObtenerTiempoRelativo(DateTime fecha)
        {
            var diff = DateTime.Now - fecha;
            if (diff.TotalMinutes < 1) return "Hace instantes";
            if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 7) return $"Hace {(int)diff.TotalDays} días";
            return fecha.ToString("dd/MM/yyyy");
        }

        // ===================================================
        // ACTUALIZAR GRÁFICO DE BARRAS CMMI
        // ===================================================
        private void ActualizarGraficoCmmi(List<Modelos.Diagnostico> diagnosticos)
        {
            // Contar empresas en cada nivel (1 a 5)
            int[] cantidades = new int[5];
            foreach (var diag in diagnosticos)
            {
                int idx = diag.NivelMadurez - 1;
                if (idx >= 0 && idx < 5) cantidades[idx]++;
            }

            int total = cantidades.Sum();
            int max = cantidades.Max();
            int nivelFrecuente = max > 0 ? Array.IndexOf(cantidades, max) + 1 : 0;

            // Actualizar el badge del header con el total
            _lblBadgeCmmi.Text = total == 1 ? "1 EMPRESA" : $"{total} EMPRESAS";

            for (int i = 0; i < 5; i++)
            {
                int nivel = i + 1;
                int cantidad = cantidades[i];
                Color colorNivel = ColorDelNivel(nivel);
                bool vacio = cantidad == 0;
                bool esFrecuente = (nivel == nivelFrecuente && cantidad > 0);

                // Valor
                _valoresCmmi[i].Text = cantidad.ToString();
                _valoresCmmi[i].ForeColor = vacio
                    ? Color.FromArgb(192, 189, 197)
                    : Paleta.TextoOscuro;

                // Porcentaje
                if (vacio)
                {
                    _porcentajesCmmi[i].Text = "—";
                    _porcentajesCmmi[i].ForeColor = Color.FromArgb(192, 189, 197);
                }
                else
                {
                    int pct = (int)Math.Round(cantidad * 100.0 / total);
                    _porcentajesCmmi[i].Text = $"{pct}%";
                    _porcentajesCmmi[i].ForeColor = Color.FromArgb(107, 104, 112);
                }

                // Nombre del nivel: gris muted si vacío
                _lblNombresNivel[i].ForeColor = vacio
                    ? Color.FromArgb(192, 189, 197)
                    : Paleta.TextoOscuro;

                // Chip: si es el más frecuente, invertir colores (fondo sólido con número blanco)
                if (esFrecuente)
                {
                    _chipsNumeroCmmi[i].BackColor = colorNivel;
                    _lblNumChipCmmi[i].ForeColor = Color.White;
                }
                else if (vacio)
                {
                    _chipsNumeroCmmi[i].BackColor = ColorTranslator.FromHtml("#F5F2F8");
                    _lblNumChipCmmi[i].ForeColor = Color.FromArgb(192, 189, 197);
                }
                else
                {
                    _chipsNumeroCmmi[i].BackColor = VersionPastel(colorNivel);
                    _lblNumChipCmmi[i].ForeColor = colorNivel;
                }

                // Fondo destacado si es el más frecuente
                _fondosCmmi[i].BackColor = esFrecuente
                    ? ColorTranslator.FromHtml("#F7F4FA")
                    : Color.Transparent;

                // Badge "MÁS FRECUENTE"
                _badgesFrec[i].Visible = esFrecuente;
                _badgesFrec[i].BackColor = colorNivel;

                // Barra rellena (width proporcional al máximo)
                if (_tracksCmmi[i].Width > 0)
                {
                    int nuevoAncho = max > 0
                        ? (cantidad * _tracksCmmi[i].Width) / max
                        : 0;
                    _barrasCmmi[i].Width = nuevoAncho;
                }
            }

            // === Actualizar el INSIGHT ===
            if (total == 0)
            {
                _lblInsightLinea1.Text = "Aún no hay evaluaciones registradas.";
                _lblInsightLinea2.Text = "Genera tu primera evaluación desde 'Cargar Informe'.";
            }
            else
            {
                int pctFrecuente = (int)Math.Round(max * 100.0 / total);
                string nombreFrec = NombreDelNivel(nivelFrecuente);
                _lblInsightLinea1.Text = $"El {pctFrecuente}% de las empresas evaluadas está en Nivel {nivelFrecuente} ({nombreFrec}).";

                // Segunda línea contextual: niveles avanzados
                int cantNivelesAlto = cantidades[3] + cantidades[4];  // niveles 4 y 5
                if (cantNivelesAlto == 0)
                {
                    _lblInsightLinea2.Text = "Ninguna empresa ha alcanzado los niveles avanzados (4 y 5) todavía.";
                }
                else
                {
                    int pctAlto = (int)Math.Round(cantNivelesAlto * 100.0 / total);
                    _lblInsightLinea2.Text = $"{cantNivelesAlto} empresa(s) han llegado a niveles avanzados (4-5): un {pctAlto}% del total.";
                }
            }
        }

        // ===================================================
        // ACTUALIZAR GRÁFICO DE DONA (SECTORES)
        // ===================================================
        private void ActualizarGraficoSectores(List<Modelos.Empresa> empresas)
        {
            // Clasificar empresas por sector
            // Como el campo Sector es texto libre, lo agrupamos por palabras clave
            var contadores = new Dictionary<string, int>
            {
                ["Software a medida"] = 0,
                ["SaaS / Web"] = 0,
                ["Móvil"] = 0,
                ["Integraciones"] = 0,
                ["Otros"] = 0
            };

            foreach (var emp in empresas)
            {
                string sector = (emp.Sector ?? "").ToLower();
                if (sector.Contains("medida") || sector.Contains("custom"))
                    contadores["Software a medida"]++;
                else if (sector.Contains("saas") || sector.Contains("web"))
                    contadores["SaaS / Web"]++;
                else if (sector.Contains("móvil") || sector.Contains("movil") || sector.Contains("mobile"))
                    contadores["Móvil"]++;
                else if (sector.Contains("integ"))
                    contadores["Integraciones"]++;
                else
                    contadores["Otros"]++;
            }

            // Actualizar los datos
            foreach (var sectorDato in _sectoresData)
            {
                if (contadores.TryGetValue(sectorDato.Nombre, out int cant))
                    sectorDato.Cantidad = cant;
            }

            // Actualizar el total (se dibuja en el centro vía Paint)
            int total = _sectoresData.Sum(s => s.Cantidad);
            _totalDona = total;

            // Badge del header con el total
            _lblBadgeSectores.Text = total == 1 ? "1 EMPRESA" : $"{total} EMPRESAS";

            // Actualizar leyenda: cada item se recrea con el porcentaje ya calculado
            _flowLegendaSectores.Controls.Clear();
            foreach (var sector in _sectoresData)
            {
                int porc = total > 0
                    ? (int)Math.Round(sector.Cantidad * 100.0 / total)
                    : 0;

                var item = CrearItemLeyenda(sector, porc);
                _flowLegendaSectores.Controls.Add(item);
            }

            // === INSIGHT ===
            if (total == 0)
            {
                _lblInsightSectores.Text = "Aún no hay empresas registradas en el sistema.";
            }
            else
            {
                var sectorMayor = _sectoresData
                    .Where(x => x.Cantidad > 0)
                    .OrderByDescending(x => x.Cantidad)
                    .FirstOrDefault();

                int sectoresConDatos = _sectoresData.Count(x => x.Cantidad > 0);

                if (sectorMayor != null && sectoresConDatos == 1)
                {
                    _lblInsightSectores.Text =
                        $"Las {total} empresas están concentradas en un solo sector: {sectorMayor.Nombre}.";
                }
                else if (sectorMayor != null)
                {
                    int pctMayor = (int)Math.Round(sectorMayor.Cantidad * 100.0 / total);
                    _lblInsightSectores.Text =
                        $"El sector predominante es {sectorMayor.Nombre} con el {pctMayor}% de las empresas.";
                }
            }

            _panelDona.Invalidate();
        }

        // ===================================================
        // ACTUALIZAR GRÁFICO DE LÍNEAS (ACTIVIDAD SEMANAL)
        // ===================================================
        private void ActualizarGraficoLineas(List<Modelos.Diagnostico> diagnosticos)
        {
            var hoy = DateTime.Now.Date;

            for (int i = 0; i < 7; i++)
            {
                var dia = hoy.AddDays(-(6 - i));
                _fechasDias[i] = dia;
                _evaluacionesPorSemana[i] = diagnosticos.Count(d =>
                    d.FechaGeneracion.Date == dia);
            }


            // Calcular índice del día pico (mayor cantidad).
            // Si empatan, escoge el más reciente para dar la impresión de "momentum".
            int maxCant = _evaluacionesPorSemana.Max();
            _diaPicoIdx = -1;
            if (maxCant > 0)
            {
                for (int i = 6; i >= 0; i--)
                {
                    if (_evaluacionesPorSemana[i] == maxCant)
                    {
                        _diaPicoIdx = i;
                        break;
                    }
                }
            }

            // Actualizar badge con rango real de fechas
            var rangoIni = hoy.AddDays(-6);
            _lblBadgeActividad.Text = $"{rangoIni:dd MMM}  →  {hoy:dd MMM}".ToUpperInvariant();

            // === INSIGHT ===
            int totalSemana = _evaluacionesPorSemana.Sum();
            if (totalSemana == 0)
            {
                _lblInsightActividad.Text = "Sin actividad registrada en los últimos 7 días.";
            }
            else
            {
                string nombreDiaPico = _diaPicoIdx >= 0
                    ? (_diaPicoIdx == 6 ? "hoy" : _nombresDias[_diaPicoIdx].ToLowerInvariant())
                    : "algún día";
                string plural = maxCant == 1 ? "evaluación" : "evaluaciones";
                string totalTxt = totalSemana == 1
                    ? "1 evaluación esta semana"
                    : $"{totalSemana} evaluaciones esta semana";

                _lblInsightActividad.Text =
                    $"{totalTxt}. Tu mayor actividad fue el {nombreDiaPico} con {maxCant} {plural}.";
            }

            _panelLineas.Invalidate();
        }

        // ===================================================
        // ACTUALIZAR ÁREAS CRÍTICAS DETECTADAS
        // ===================================================
        private void ActualizarAreasCriticas(List<Modelos.Diagnostico> diagnosticos)
        {
            // Palabras clave a buscar y su nombre amigable
            var palabrasClave = new Dictionary<string, string>
    {
        { "prueba", "Pruebas automatizadas" },
        { "ci/cd", "CI/CD" },
        { "documentación", "Documentación" },
        { "documentacion", "Documentación" },
        { "seguridad", "Seguridad" },
        { "proyecto", "Gestión de proyectos" },
        { "capacita", "Capacitación" },
        { "respaldo", "Respaldos" },
        { "código", "Calidad de código" },
        { "codigo", "Calidad de código" },
        { "infraestructura", "Infraestructura" },
        { "comunicación", "Comunicación" }
    };

            // Contar ocurrencias
            var conteo = new Dictionary<string, int>();
            foreach (var diag in diagnosticos)
            {
                string debilidades = (diag.Debilidades ?? "").ToLower();
                foreach (var par in palabrasClave)
                {
                    if (debilidades.Contains(par.Key))
                    {
                        if (conteo.ContainsKey(par.Value))
                            conteo[par.Value]++;
                        else
                            conteo[par.Value] = 1;
                    }
                }
            }

            // Top 5
            var top5 = conteo
                .OrderByDescending(p => p.Value)
                .Take(5)
                .ToList();

            int max = top5.Count > 0 ? top5.Max(p => p.Value) : 1;
            int totalEval = diagnosticos.Count;

            // Actualizar las 5 filas
            for (int i = 0; i < 5; i++)
            {
                bool tieneDato = i < top5.Count;
                Color colorRank = ColorCriticidad(i + 1);

                if (tieneDato)
                {
                    int cant = top5[i].Value;
                    _nombresCriticas[i].Text = top5[i].Key;
                    _nombresCriticas[i].ForeColor = Paleta.TextoOscuro;
                    _valoresCriticas[i].Text = cant.ToString();
                    _valoresCriticas[i].ForeColor = Paleta.TextoOscuro;

                    // "menciones" o "en N de M eval."
                    _subCriticas[i].Text = cant == 1 ? "mención" : "menciones";

                    // Chip: #1 con fondo sólido y número blanco; resto pastel
                    bool esTop = (i == 0);
                    _chipsRankCriticas[i].BackColor = esTop ? colorRank : VersionPastel(colorRank);
                    _lblRankCriticas[i].ForeColor = esTop ? Color.White : colorRank;

                    // Fondo destacado + badge para el #1
                    _fondosCriticas[i].BackColor = esTop
                        ? ColorTranslator.FromHtml("#FBF3EE")
                        : Color.Transparent;
                    _badgesCriticas[i].Visible = esTop;
                    _badgesCriticas[i].BackColor = colorRank;

                    // Barra proporcional al máximo
                    var track = _tracksCriticas[i];
                    if (track != null && track.Width > 0)
                    {
                        int nuevoAncho = (cant * track.Width) / max;
                        if (nuevoAncho < 6) nuevoAncho = 6;  // mínimo visible
                        _barrasCriticas[i].Width = nuevoAncho;
                    }

                    // Tooltip con contexto de la debilidad
                    int pct = totalEval > 0 ? (int)Math.Round(cant * 100.0 / totalEval) : 0;
                    TooltipEstilizado.AplicarACascada(
                        $"'{top5[i].Key}' apareció en {cant} de {totalEval} evaluaciones ({pct}%). " +
                        "Es una de las áreas donde las empresas evaluadas presentan más debilidades.",
                        _fondosCriticas[i], _nombresCriticas[i], _valoresCriticas[i], _chipsRankCriticas[i]);
                }
                else
                {
                    // Fila vacía en gris muted
                    _nombresCriticas[i].Text = "—";
                    _nombresCriticas[i].ForeColor = Color.FromArgb(192, 189, 197);
                    _valoresCriticas[i].Text = "0";
                    _valoresCriticas[i].ForeColor = Color.FromArgb(192, 189, 197);
                    _subCriticas[i].Text = "";
                    _chipsRankCriticas[i].BackColor = ColorTranslator.FromHtml("#F5F2F8");
                    _lblRankCriticas[i].ForeColor = Color.FromArgb(192, 189, 197);
                    _fondosCriticas[i].BackColor = Color.Transparent;
                    _badgesCriticas[i].Visible = false;
                    _barrasCriticas[i].Width = 0;
                }
            }

            // === INSIGHT ===
            if (top5.Count == 0)
            {
                _lblInsightCriticas.Text = totalEval == 0
                    ? "Aún no hay evaluaciones para detectar áreas críticas."
                    : "No se detectaron debilidades recurrentes en las evaluaciones.";
            }
            else
            {
                int cantTop = top5[0].Value;
                int pctTop = totalEval > 0 ? (int)Math.Round(cantTop * 100.0 / totalEval) : 0;
                _lblInsightCriticas.Text =
                    $"'{top5[0].Key}' es la debilidad más común: presente en el {pctTop}% de las evaluaciones.";
            }
        }

        // ===================================================
        // ACTUALIZAR LISTA DE ÚLTIMAS EVALUACIONES
        // ===================================================
        private void ActualizarUltimasEvaluaciones(
            List<Modelos.Empresa> empresas,
            List<Modelos.Conversacion> conversaciones,
            List<Modelos.Diagnostico> diagnosticos)
        {
            _flowUltimasEvaluaciones.Controls.Clear();

            // Si no hay datos, mostrar placeholder
            if (diagnosticos.Count == 0)
            {
                var lblVacio = new Label
                {
                    Text = "Aún no hay evaluaciones registradas.\nGenera tu primera evaluación desde 'Cargar Informe'.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = Color.FromArgb(140, 135, 132),
                    Size = new Size(_flowUltimasEvaluaciones.Width - 20, 80),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 60, 0, 0)
                };
                _flowUltimasEvaluaciones.Controls.Add(lblVacio);
                return;
            }

            // Tomar top 5 más recientes
            var top5 = diagnosticos
                .OrderByDescending(d => d.FechaGeneracion)
                .Take(5)
                .ToList();

            foreach (var diag in top5)
            {
                var conv = _repoConversacion.ObtenerPorId(diag.ConversacionId);
                var empresa = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;

                if (empresa == null) continue;

                string inicial = empresa.Nombre.Length > 0
                    ? empresa.Nombre[0].ToString().ToUpper()
                    : "?";
                string detalle = $"{empresa.Sector} · {empresa.Rif}";
                string tiempo = ObtenerTiempoRelativo(diag.FechaGeneracion);

                var fila = CrearFilaEvaluacion(inicial, empresa.Nombre, detalle, diag.NivelMadurez, tiempo);
                _flowUltimasEvaluaciones.Controls.Add(fila);
            }
        }

    }
}