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

        // Header
        private Label lblTitulo = null!;
        private Label lblSubtitulo = null!;

        // KPIs principales (referencias para luego actualizarles los datos reales)
        private Label lblKpiEmpresas = null!;
        private Label lblKpiEvaluaciones = null!;
        private Label lblKpiNivelPromedio = null!;
        private Label lblKpiEsteMes = null!;

        // KPIs secundarios
        private Label lblKpiEmpresasEvaluadas = null!;
        private Label lblKpiEmpresasEvaluadasSub = null!;
        private Label lblKpiEvalReciente = null!;
        private Label lblKpiEvalRecienteSub = null!;
        private Label lblKpiNivelFrecuente = null!;
        private Label lblKpiNivelFrecuenteSub = null!;

        // Gráfico distribución CMMI (5 barras, una por nivel)
        private Panel[] _barrasCmmi = new Panel[5];
        private Label[] _valoresCmmi = new Label[5];

        // Gráfico de líneas: evaluaciones por día (últimos 7 días)
        private Panel _panelLineas = null!;
        private int[] _evaluacionesPorSemana = new int[7];
        private string[] _nombresDias = new string[7];

        // Áreas críticas: top 5 debilidades más frecuentes
        private Panel[] _barrasCriticas = new Panel[5];
        private Label[] _nombresCriticas = new Label[5];
        private Label[] _valoresCriticas = new Label[5];

        // Repositorios para consultar datos
        private Datos.RepositorioEmpresa _repoEmpresa = null!;
        private Datos.RepositorioConversacion _repoConversacion = null!;
        private Datos.RepositorioDiagnostico _repoDiagnostico = null!;

        // Lista de últimas evaluaciones
        private FlowLayoutPanel _flowUltimasEvaluaciones = null!;

        // Gráfico dona: sectores
        private List<SectorDato> _sectoresData = new();
        private Panel _panelDona = null!;
        private Label _lblDonaTotal = null!;
        private FlowLayoutPanel _flowLegendaSectores = null!;

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
            Label subTemp;  // descartable

            var kpi1 = CrearTarjetaKpi(
                etiqueta: "EMPRESAS REGISTRADAS",
                icono: "🏢",
                numero: "0",
                subtitulo: "Total en el sistema",
                colorBarra: Paleta.MoradoOscuro,
                colorNumero: Paleta.MoradoOscuro,
                out lblKpiEmpresas,
                out subTemp);

            var kpi2 = CrearTarjetaKpi(
                etiqueta: "EVALUACIONES",
                icono: "📋",
                numero: "0",
                subtitulo: "Análisis realizados",
                colorBarra: Paleta.VerdeGrisaceo,
                colorNumero: Paleta.VerdeGrisaceo,
                out lblKpiEvaluaciones,
                out subTemp);

            var kpi3 = CrearTarjetaKpi(
                etiqueta: "NIVEL CMMI PROMEDIO",
                icono: "⚡",
                numero: "—",
                subtitulo: "De 5 niveles posibles",
                colorBarra: Paleta.MoradoClaro,
                colorNumero: Paleta.MoradoClaro,
                out lblKpiNivelPromedio,
                out subTemp);

            var kpi4 = CrearTarjetaKpi(
                etiqueta: "ESTE MES",
                icono: "📅",
                numero: "0",
                subtitulo: $"{DateTime.Now:MMMM yyyy}",
                colorBarra: Paleta.VerdeGrisaceoOscuro,
                colorNumero: Paleta.VerdeGrisaceoOscuro,
                out lblKpiEsteMes,
                out subTemp);

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
            int alturaTarjeta = 115;

            foreach (Control c in filaKpis.Controls)
            {
                c.Size = new Size(anchoTarjeta, alturaTarjeta);
                c.Margin = new Padding(0, 0, gap, 0);
            }
        }

        // ===================================================
        // CREAR UNA TARJETA KPI INDIVIDUAL
        // ===================================================
        private Panel CrearTarjetaKpi(
            string etiqueta,
            string icono,
            string numero,
            string subtitulo,
            Color colorBarra,
            Color colorNumero,
            out Label labelNumero,
            out Label labelSubtitulo,
            float tamanoFuenteNumero = 26f)
        {
            var tarjeta = new Panel
            {
                BackColor = Color.White
            };

            // === Dibujar TODO con Paint (borde redondeado + barra de color superior) ===
            tarjeta.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int radio = 14;
                int alturaBarra = 4;

                // FONDO blanco con esquinas redondeadas
                using var pathFondo = new System.Drawing.Drawing2D.GraphicsPath();
                pathFondo.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathFondo.AddArc(tarjeta.Width - radio * 2 - 1, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 0, 90);
                pathFondo.AddArc(0, tarjeta.Height - radio * 2 - 1, radio * 2, radio * 2, 90, 90);
                pathFondo.CloseFigure();

                using (var brushFondo = new SolidBrush(Color.White))
                    g.FillPath(brushFondo, pathFondo);

                // BORDE gris sutil
                using (var penBorde = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(penBorde, pathFondo);

                // BARRA SUPERIOR de color (con esquinas superiores redondeadas)
                using var pathBarra = new System.Drawing.Drawing2D.GraphicsPath();
                pathBarra.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
                pathBarra.AddArc(tarjeta.Width - radio * 2 - 1, 0, radio * 2, radio * 2, 270, 90);
                pathBarra.AddLine(tarjeta.Width - 1, alturaBarra, 0, alturaBarra);
                pathBarra.CloseFigure();

                using (var brushBarra = new SolidBrush(colorBarra))
                    g.FillPath(brushBarra, pathBarra);
            };

            // Repintar cuando cambia el tamaño
            tarjeta.Resize += (s, e) => tarjeta.Invalidate();

            // === Etiqueta (esquina superior izquierda, bajo la barra) ===
            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(16, 16),
                Size = new Size(180, 14),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblEtiqueta);

            // === Icono (esquina superior derecha) ===
            var lblIcono = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(160, 155, 152),
                Size = new Size(22, 20),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblIcono);

            // === Número grande ===
            labelNumero = new Label
            {
                Text = numero,
                Font = new Font("Segoe UI", tamanoFuenteNumero, FontStyle.Bold),
                ForeColor = colorNumero,
                Location = new Point(14, 36),
                Size = new Size(200, 42),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(labelNumero);

            labelSubtitulo = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(16, 82),
                Size = new Size(200, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(labelSubtitulo);

            // Reposicionar el icono cuando la tarjeta tenga su ancho final
            tarjeta.Resize += (s, e) =>
            {
                lblIcono.Location = new Point(tarjeta.ClientSize.Width - lblIcono.Width - 14, 16);
                lblEtiqueta.Size = new Size(tarjeta.ClientSize.Width - 50, 14);
            };

            return tarjeta;
        }

        // ===================================================
        // SECCIÓN DE KPIs SECUNDARIOS (3 tarjetas en una fila)
        // ===================================================
        private void CrearSeccionKpisSecundarios()
        {
            var filaKpis = new FlowLayoutPanel
            {
                Location = new Point(0, 140),  // debajo de los KPIs principales
                Size = new Size(panelDashboard.ClientSize.Width, 130),
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

            // 1) Empresas evaluadas
            var kpi1 = CrearTarjetaKpi(
                etiqueta: "EMPRESAS EVALUADAS",
                icono: "✓",
                numero: "0",
                subtitulo: "0% del total registradas",
                colorBarra: Paleta.MoradoOscuro,
                colorNumero: Paleta.MoradoOscuro,
                out lblKpiEmpresasEvaluadas,
                out lblKpiEmpresasEvaluadasSub,
                tamanoFuenteNumero: 22f);

            // 2) Evaluación más reciente
            var kpi2 = CrearTarjetaKpi(
                etiqueta: "EVALUACIÓN MÁS RECIENTE",
                icono: "🕐",
                numero: "—",
                subtitulo: "Sin evaluaciones aún",
                colorBarra: Paleta.VerdeGrisaceo,
                colorNumero: Paleta.VerdeGrisaceo,
                out lblKpiEvalReciente,
                out lblKpiEvalRecienteSub,
                tamanoFuenteNumero: 14f);

            // 3) Nivel más frecuente
            var kpi3 = CrearTarjetaKpi(
                etiqueta: "NIVEL MÁS FRECUENTE",
                icono: "🎯",
                numero: "—",
                subtitulo: "Sin datos",
                colorBarra: Paleta.MoradoClaro,
                colorNumero: Paleta.MoradoClaro,
                out lblKpiNivelFrecuente,
                out lblKpiNivelFrecuenteSub,
                tamanoFuenteNumero: 18f);

            filaKpis.Controls.Add(kpi1);
            filaKpis.Controls.Add(kpi2);
            filaKpis.Controls.Add(kpi3);

            filaKpis.HandleCreated += (s, e) =>
            {
                filaKpis.BeginInvoke(new Action(() => AjustarTamanoKpisSecundarios(filaKpis)));
            };
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
            int alturaTarjeta = 115;

            foreach (Control c in filaKpis.Controls)
            {
                c.Size = new Size(anchoTarjeta, alturaTarjeta);
                c.Margin = new Padding(0, 0, gap, 0);
            }
        }

        // ===================================================
        // GRÁFICO DE BARRAS: DISTRIBUCIÓN POR NIVEL CMMI
        // ===================================================
        private void CrearSeccionGraficoCmmi()
        {
            // Contenedor de la tarjeta del gráfico
            var tarjeta = new Panel
            {
                Location = new Point(0, 280),  // debajo de los KPIs secundarios (10 + 130 + 140)
                Size = new Size(panelDashboard.ClientSize.Width - 10, 280),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelDashboard.Controls.Add(tarjeta);

            // Re-ajustar ancho cuando el panel cambie de tamaño
            panelDashboard.Resize += (s, e) =>
            {
                tarjeta.Width = panelDashboard.ClientSize.Width - 10;
            };

            // Dibujar borde redondeado con Paint (igual que las tarjetas KPI)
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

            // === Título del gráfico ===
            var lblTituloChart = new Label
            {
                Text = "📈  Distribución por Nivel CMMI",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            // === Subtítulo ===
            var lblSubChart = new Label
            {
                Text = "Cantidad de empresas evaluadas en cada nivel de madurez",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(20, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            // === Badge a la derecha ===
            var badge = CrearBadge("0 empresas");
            badge.Location = new Point(tarjeta.Width - badge.Width - 20, 22);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === Las 5 barras (una por nivel) ===
            string[] nombres = { "Inicial", "Gestionado", "Definido", "Gest. cuant.", "Optimizado" };
            Color[] colores = {
        Color.FromArgb(178, 172, 169),    // Gris (Nivel 1)
        Paleta.VerdeGrisaceoOscuro,        // Verde oscuro (Nivel 2)
        Paleta.VerdeGrisaceo,              // Verde grisáceo (Nivel 3)
        Paleta.MoradoClaro,                // Morado claro (Nivel 4)
        Paleta.MoradoOscuro                // Morado oscuro (Nivel 5)
    };

            int yInicio = 80;
            int alturaBarra = 28;
            int espacioEntre = 10;

            for (int i = 0; i < 5; i++)
            {
                int y = yInicio + i * (alturaBarra + espacioEntre);

                // Label "Nivel N"
                var lblNivel = new Label
                {
                    Text = $"Nivel {i + 1}",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(20, y + 6),
                    Size = new Size(60, 18),
                    BackColor = Color.Transparent
                };
                tarjeta.Controls.Add(lblNivel);

                // Track de fondo (la barra "vacía" gris claro)
                var track = new Panel
                {
                    Location = new Point(90, y),
                    Size = new Size(tarjeta.Width - 90 - 60, alturaBarra),
                    BackColor = ColorTranslator.FromHtml("#F5F2F8"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                // Redondear el track
                track.Resize += (s, e) =>
                    Paleta.AplicarBordeRedondeadoSuave(track, alturaBarra / 2);
                tarjeta.Controls.Add(track);

                // Barra de relleno (con valor proporcional)
                var barra = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(0, alturaBarra),  // ancho 0 inicial, se actualiza después
                    BackColor = colores[i]
                };
                barra.Resize += (s, e) =>
                {
                    if (barra.Width > 10)
                        Paleta.AplicarBordeRedondeadoSuave(barra, alturaBarra / 2);
                };
                track.Controls.Add(barra);
                _barrasCmmi[i] = barra;

                // Texto dentro de la barra (nombre del nivel)
                var lblNombre = new Label
                {
                    Text = nombres[i],
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(12, 0),
                    Size = new Size(120, alturaBarra),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                barra.Controls.Add(lblNombre);

                // Label del valor a la derecha
                var lblValor = new Label
                {
                    Text = "0",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Size = new Size(50, alturaBarra),
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                lblValor.Location = new Point(tarjeta.Width - 60, y + 4);
                tarjeta.Controls.Add(lblValor);
                _valoresCmmi[i] = lblValor;

                // Reposicionar el label del valor al cambiar de tamaño la tarjeta
                tarjeta.Resize += (s, e) =>
                {
                    lblValor.Location = new Point(tarjeta.Width - 60, lblValor.Location.Y);
                };
            }
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
                Location = new Point(0, 580),  // debajo del gráfico de barras
                Size = new Size(panelDashboard.ClientSize.Width - 10, 290),
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

            // === Título ===
            var lblTituloChart = new Label
            {
                Text = "🏭  Empresas por Sector",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Distribución de las empresas registradas según su rubro",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(20, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            var badge = CrearBadge($"{_sectoresData.Count} sectores");
            badge.Location = new Point(tarjeta.Width - badge.Width - 20, 22);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === Panel donde se dibuja la dona (izquierda) ===
            _panelDona = new Panel
            {
                Location = new Point(40, 80),
                Size = new Size(180, 180),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(_panelDona);

            // Evento Paint para dibujar la dona
            _panelDona.Paint += DibujarDona;

            // === Número total en el centro de la dona ===
            _lblDonaTotal = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(40, 130),
                Size = new Size(180, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(_lblDonaTotal);
            _lblDonaTotal.BringToFront();

            var lblDonaLabel = new Label
            {
                Text = "EMPRESAS",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(40, 165),
                Size = new Size(180, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblDonaLabel);
            lblDonaLabel.BringToFront();

            // === Leyenda (derecha) ===
            _flowLegendaSectores = new FlowLayoutPanel
            {
                Location = new Point(250, 80),
                Size = new Size(tarjeta.Width - 270, 180),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_flowLegendaSectores);

            // Crear los ítems de la leyenda
            foreach (var sector in _sectoresData)
            {
                _flowLegendaSectores.Controls.Add(CrearItemLeyenda(sector));
            }

            // Re-ajustar ancho de la leyenda
            tarjeta.Resize += (s, e) =>
            {
                _flowLegendaSectores.Width = tarjeta.Width - 270;
            };
        }

        // ===================================================
        // DIBUJAR LA DONA (evento Paint)
        // ===================================================
        private void DibujarDona(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int total = 0;
            foreach (var s in _sectoresData) total += s.Cantidad;

            int tamano = Math.Min(_panelDona.Width, _panelDona.Height);
            int grosorAnillo = 30;
            var rectExterno = new Rectangle(0, 0, tamano - 1, tamano - 1);

            // Si no hay datos, dibujar un círculo gris claro
            if (total == 0)
            {
                using var brushVacio = new SolidBrush(ColorTranslator.FromHtml("#F0EDF5"));
                g.FillEllipse(brushVacio, rectExterno);

                // Hueco central blanco
                var rectInterno = new Rectangle(grosorAnillo, grosorAnillo,
                    tamano - grosorAnillo * 2, tamano - grosorAnillo * 2);
                using var brushBlanco = new SolidBrush(Color.White);
                g.FillEllipse(brushBlanco, rectInterno);
                return;
            }

            // Dibujar las rebanadas de la dona
            float anguloInicial = -90f;  // Empezamos arriba (las 12 en punto)
            foreach (var sector in _sectoresData)
            {
                if (sector.Cantidad <= 0) continue;

                float porcentaje = (float)sector.Cantidad / total;
                float grados = porcentaje * 360f;

                // Dibujar el sector como un "pie slice" (rebanada de pastel)
                using var brushSector = new SolidBrush(sector.Color);
                g.FillPie(brushSector, rectExterno, anguloInicial, grados);

                anguloInicial += grados;
            }

            // Dibujar el hueco central blanco (esto convierte el pastel en dona)
            var rectInternoFinal = new Rectangle(grosorAnillo, grosorAnillo,
                tamano - grosorAnillo * 2, tamano - grosorAnillo * 2);
            using var brushBlancoFinal = new SolidBrush(Color.White);
            g.FillEllipse(brushBlancoFinal, rectInternoFinal);
        }

        // ===================================================
        // CREAR UN ÍTEM DE LEYENDA
        // ===================================================
        private Panel CrearItemLeyenda(SectorDato sector)
        {
            var item = new Panel
            {
                Size = new Size(400, 28),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6),
                Tag = sector  // guardamos referencia al sector
            };

            // Cuadrado de color
            var cuadradoColor = new Panel
            {
                Size = new Size(14, 14),
                Location = new Point(0, 7),
                BackColor = sector.Color
            };
            var pathCuadrado = new System.Drawing.Drawing2D.GraphicsPath();
            int rC = 3;
            pathCuadrado.AddArc(0, 0, rC * 2, rC * 2, 180, 90);
            pathCuadrado.AddArc(cuadradoColor.Width - rC * 2, 0, rC * 2, rC * 2, 270, 90);
            pathCuadrado.AddArc(cuadradoColor.Width - rC * 2, cuadradoColor.Height - rC * 2, rC * 2, rC * 2, 0, 90);
            pathCuadrado.AddArc(0, cuadradoColor.Height - rC * 2, rC * 2, rC * 2, 90, 90);
            pathCuadrado.CloseFigure();
            cuadradoColor.Region = new Region(pathCuadrado);
            item.Controls.Add(cuadradoColor);

            // Nombre del sector
            var lblNombre = new Label
            {
                Text = sector.Nombre,
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 5),
                Size = new Size(220, 18),
                BackColor = Color.Transparent
            };
            item.Controls.Add(lblNombre);

            // Valor (cantidad · %)
            var lblValor = new Label
            {
                Text = $"{sector.Cantidad} · 0%",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(250, 5),
                Size = new Size(130, 18),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            item.Controls.Add(lblValor);

            return item;
        }

        // ===================================================
        // GRÁFICO DE LÍNEAS: ACTIVIDAD DE LOS ÚLTIMOS 7 DÍAS
        // ===================================================
        private void CrearSeccionGraficoLineas()
        {
            // Inicializar nombres de los últimos 7 días
            var hoy = DateTime.Now;
            for (int i = 0; i < 7; i++)
            {
                var fecha = hoy.AddDays(-(6 - i));
                _nombresDias[i] = fecha.ToString("ddd").ToUpper();  // LUN, MAR, MIE...
                _evaluacionesPorSemana[i] = 0;
            }

            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 890),  // debajo de la dona (580 + 290 + 20)
                Size = new Size(panelDashboard.ClientSize.Width - 10, 290),
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
                Text = "📊  Actividad de los Últimos 7 Días",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Evaluaciones realizadas día a día durante esta semana",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(20, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            var badge = CrearBadge("Esta semana");
            badge.Location = new Point(tarjeta.Width - badge.Width - 20, 22);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === Panel donde dibujamos el gráfico ===
            _panelLineas = new Panel
            {
                Location = new Point(40, 80),
                Size = new Size(tarjeta.Width - 60, 180),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(_panelLineas);

            _panelLineas.Paint += DibujarGraficoLineas;

            // Re-ajustar el panel de líneas
            tarjeta.Resize += (s, e) =>
            {
                _panelLineas.Width = tarjeta.Width - 60;
                _panelLineas.Invalidate();
            };
        }

        // ===================================================
        // DIBUJAR EL GRÁFICO DE LÍNEAS (evento Paint)
        // ===================================================
        private void DibujarGraficoLineas(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // === Configuración del área de dibujo ===
            int paddingIzquierdo = 35;   // espacio para los números del eje Y
            int paddingDerecho = 10;
            int paddingArriba = 10;
            int paddingAbajo = 30;       // espacio para los nombres de los días

            int areaAncho = _panelLineas.Width - paddingIzquierdo - paddingDerecho;
            int areaAlto = _panelLineas.Height - paddingArriba - paddingAbajo;

            // === Calcular el máximo del eje Y ===
            int max = 1;  // mínimo 1 para evitar división por cero
            foreach (int v in _evaluacionesPorSemana)
                if (v > max) max = v;

            // Redondear el max al siguiente múltiplo "bonito" para escala semanal
            if (max < 3) max = 3;
            else if (max < 5) max = 5;
            else if (max < 10) max = 10;
            else max = ((max / 5) + 1) * 5;

            // === Dibujar las líneas horizontales del grid (5 líneas) ===
            using var penGrid = new Pen(ColorTranslator.FromHtml("#F0EDF5"), 1);
            using var brushTexto = new SolidBrush(Color.FromArgb(140, 135, 132));
            using var fontEje = new Font("Segoe UI", 7);

            for (int i = 0; i <= 4; i++)
            {
                int y = paddingArriba + (areaAlto * i / 4);
                g.DrawLine(penGrid, paddingIzquierdo, y, paddingIzquierdo + areaAncho, y);

                // Etiqueta del eje Y (valor)
                int valor = max - (max * i / 4);
                var size = g.MeasureString(valor.ToString(), fontEje);
                g.DrawString(valor.ToString(), fontEje, brushTexto,
                    paddingIzquierdo - size.Width - 4, y - size.Height / 2);
            }

            // === Calcular los puntos (X, Y) ===
            var puntos = new PointF[_evaluacionesPorSemana.Length];
            for (int i = 0; i < _evaluacionesPorSemana.Length; i++)
            {
                float x = paddingIzquierdo + (areaAncho * i / (float)(_evaluacionesPorSemana.Length - 1));
                float y = paddingArriba + areaAlto - (_evaluacionesPorSemana[i] * areaAlto / (float)max);
                puntos[i] = new PointF(x, y);
            }

            // === Dibujar el área (relleno bajo la línea) ===
            if (_evaluacionesPorSemana.Sum() > 0)  // solo si hay datos
            {
                var puntosArea = new List<PointF>(puntos);
                puntosArea.Add(new PointF(puntos[^1].X, paddingArriba + areaAlto));  // bajar al eje X
                puntosArea.Add(new PointF(puntos[0].X, paddingArriba + areaAlto));   // ir al inicio
                using var brushArea = new SolidBrush(Color.FromArgb(60, Paleta.MoradoClaro));
                g.FillPolygon(brushArea, puntosArea.ToArray());
            }

            // === Dibujar la línea conectando los puntos ===
            using var penLinea = new Pen(Paleta.MoradoOscuro, 2.5f)
            {
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            if (puntos.Length > 1)
                g.DrawLines(penLinea, puntos);

            // === Dibujar los puntos (círculos en cada valor) ===
            for (int i = 0; i < puntos.Length; i++)
            {
                bool esUltimo = i == puntos.Length - 1;
                int radio = esUltimo ? 6 : 4;

                // El último punto se destaca con borde verde brillante (HOY)
                if (esUltimo && _evaluacionesPorSemana[i] > 0)
                {
                    using var brushUlt = new SolidBrush(Paleta.VerdeBrillante);
                    g.FillEllipse(brushUlt, puntos[i].X - radio - 2, puntos[i].Y - radio - 2, (radio + 2) * 2, (radio + 2) * 2);
                }

                using var brushPunto = new SolidBrush(Paleta.MoradoOscuro);
                g.FillEllipse(brushPunto, puntos[i].X - radio, puntos[i].Y - radio, radio * 2, radio * 2);
            }

            // === Dibujar las etiquetas del eje X (nombres de los días) ===
            using var fontDia = new Font("Segoe UI", 8, FontStyle.Bold);
            using var brushDia = new SolidBrush(Color.FromArgb(140, 135, 132));
            for (int i = 0; i < _nombresDias.Length; i++)
            {
                var size = g.MeasureString(_nombresDias[i], fontDia);
                g.DrawString(_nombresDias[i], fontDia, brushDia,
                    puntos[i].X - size.Width / 2,
                    paddingArriba + areaAlto + 8);
            }
        }

        // ===================================================
        // ÁREAS CRÍTICAS DETECTADAS (top 5 debilidades)
        // ===================================================
        private void CrearSeccionAreasCriticas()
        {
            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 1200),  // debajo del gráfico de líneas (890 + 290 + 20)
                Size = new Size(panelDashboard.ClientSize.Width - 10, 290),
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

            // === Título ===
            var lblTituloChart = new Label
            {
                Text = "🏆  Áreas Críticas Detectadas",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(20, 18),
                Size = new Size(400, 22),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTituloChart);

            var lblSubChart = new Label
            {
                Text = "Frecuencia de debilidades comunes encontradas en las evaluaciones",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(20, 42),
                Size = new Size(500, 16),
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubChart);

            var badge = CrearBadge("Top 5");
            badge.Location = new Point(tarjeta.Width - badge.Width - 20, 22);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tarjeta.Controls.Add(badge);

            // === Crear las 5 filas (placeholder vacío hasta la Tarea 9) ===
            int yInicio = 80;
            int alturaBarra = 28;
            int espacioEntre = 10;

            for (int i = 0; i < 5; i++)
            {
                int y = yInicio + i * (alturaBarra + espacioEntre);

                // Label del nombre de la debilidad (a la izquierda)
                var lblNombre = new Label
                {
                    Text = $"Debilidad {i + 1}",  // placeholder
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(20, y + 6),
                    Size = new Size(180, 18),
                    BackColor = Color.Transparent
                };
                tarjeta.Controls.Add(lblNombre);
                _nombresCriticas[i] = lblNombre;

                // Track de fondo
                var track = new Panel
                {
                    Location = new Point(210, y),
                    Size = new Size(tarjeta.Width - 210 - 60, alturaBarra),
                    BackColor = ColorTranslator.FromHtml("#F5F2F8"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                track.Resize += (s, e) =>
                    Paleta.AplicarBordeRedondeadoSuave(track, alturaBarra / 2);
                tarjeta.Controls.Add(track);

                // Barra de relleno (con gradient morado degradado)
                var barra = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(0, alturaBarra),  // empieza vacía
                    BackColor = Paleta.MoradoOscuro
                };
                barra.Paint += (s, e) =>
                {
                    if (barra.Width <= 2) return;
                    // Dibujar gradient de morado oscuro a morado claro
                    var rect = new Rectangle(0, 0, barra.Width, barra.Height);
                    using var brushGradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                        rect,
                        Paleta.MoradoOscuro,
                        Paleta.MoradoClaro,
                        System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                    using var pathBarra = new System.Drawing.Drawing2D.GraphicsPath();
                    int r = alturaBarra / 2;
                    pathBarra.AddArc(0, 0, r * 2, r * 2, 180, 90);
                    pathBarra.AddArc(barra.Width - r * 2, 0, r * 2, r * 2, 270, 90);
                    pathBarra.AddArc(barra.Width - r * 2, barra.Height - r * 2, r * 2, r * 2, 0, 90);
                    pathBarra.AddArc(0, barra.Height - r * 2, r * 2, r * 2, 90, 90);
                    pathBarra.CloseFigure();
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brushGradient, pathBarra);
                };
                barra.Resize += (s, e) => barra.Invalidate();
                track.Controls.Add(barra);
                _barrasCriticas[i] = barra;

                // Label del valor (cantidad)
                var lblValor = new Label
                {
                    Text = "0",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Size = new Size(50, alturaBarra),
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                lblValor.Location = new Point(tarjeta.Width - 60, y + 4);
                tarjeta.Controls.Add(lblValor);
                _valoresCriticas[i] = lblValor;

                // Reposicionar el label del valor al cambiar de tamaño
                tarjeta.Resize += (s, e) =>
                {
                    lblValor.Location = new Point(tarjeta.Width - 60, lblValor.Location.Y);
                };
            }
        }

        // ===================================================
        // LISTA DE ÚLTIMAS EVALUACIONES
        // ===================================================
        private void CrearSeccionUltimasEvaluaciones()
        {
            // Contenedor de la tarjeta
            var tarjeta = new Panel
            {
                Location = new Point(0, 1510),  // debajo de áreas críticas (1200 + 290 + 20)
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
            var fila = new Panel
            {
                Size = new Size(_flowUltimasEvaluaciones.Width - 20, 56),
                BackColor = ColorTranslator.FromHtml("#F9F5FF"),
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

            // KPI 3: Nivel CMMI promedio
            if (diagnosticos.Count > 0)
            {
                double promedio = diagnosticos.Average(d => d.NivelMadurez);
                lblKpiNivelPromedio.Text = promedio.ToString("F1");
            }
            else
            {
                lblKpiNivelPromedio.Text = "—";
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

            int evaluadas = empresaIdsEvaluadas.Count;
            double porcentaje = totalEmpresas > 0 ? (evaluadas * 100.0 / totalEmpresas) : 0;
            lblKpiEmpresasEvaluadas.Text = $"{evaluadas}";
            lblKpiEmpresasEvaluadasSub.Text = $"{porcentaje:F0}% del total registradas";

            // KPI 2: Evaluación más reciente
            if (diagnosticos.Count > 0)
            {
                var masReciente = diagnosticos.OrderByDescending(d => d.FechaGeneracion).First();
                var conv = _repoConversacion.ObtenerPorId(masReciente.ConversacionId);
                var empresa = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;

                lblKpiEvalReciente.Text = empresa?.Nombre ?? "—";
                lblKpiEvalRecienteSub.Text = $"Nivel {masReciente.NivelMadurez} · {ObtenerTiempoRelativo(masReciente.FechaGeneracion)}";
            }
            else
            {
                lblKpiEvalReciente.Text = "—";
                lblKpiEvalRecienteSub.Text = "Sin evaluaciones aún";
            }

            // KPI 3: Nivel más frecuente
            if (diagnosticos.Count > 0)
            {
                var nivelMasFrec = diagnosticos
                    .GroupBy(d => d.NivelMadurez)
                    .OrderByDescending(g => g.Count())
                    .First();

                string nombreNivel = NombreDelNivel(nivelMasFrec.Key);
                lblKpiNivelFrecuente.Text = $"Nivel {nivelMasFrec.Key}";
                lblKpiNivelFrecuenteSub.Text = $"{nombreNivel} · {nivelMasFrec.Count()} empresas";
            }
            else
            {
                lblKpiNivelFrecuente.Text = "—";
                lblKpiNivelFrecuenteSub.Text = "Sin datos";
            }
        }

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

            // Encontrar el máximo para escalar las barras
            int max = cantidades.Max();
            if (max == 0) max = 1;

            // Actualizar cada barra
            for (int i = 0; i < 5; i++)
            {
                int cantidad = cantidades[i];
                _valoresCmmi[i].Text = cantidad.ToString();

                // Calcular el ancho proporcional
                var track = _barrasCmmi[i].Parent;
                if (track != null && track.Width > 0)
                {
                    int nuevoAncho = (cantidad * track.Width) / max;
                    _barrasCmmi[i].Width = nuevoAncho;
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

            // Actualizar el total en el centro
            int total = _sectoresData.Sum(s => s.Cantidad);
            _lblDonaTotal.Text = total.ToString();

            // Actualizar leyenda
            _flowLegendaSectores.Controls.Clear();
            foreach (var sector in _sectoresData)
            {
                var item = CrearItemLeyenda(sector);
                // Actualizar el valor con porcentaje real
                var lblValor = item.Controls.OfType<Label>().LastOrDefault();
                if (lblValor != null && total > 0)
                {
                    int porc = (int)Math.Round(sector.Cantidad * 100.0 / total);
                    lblValor.Text = $"{sector.Cantidad} · {porc}%";
                }
                else if (lblValor != null)
                {
                    lblValor.Text = $"{sector.Cantidad} · 0%";
                }
                _flowLegendaSectores.Controls.Add(item);
            }

            // Forzar redibujado de la dona
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
                _evaluacionesPorSemana[i] = diagnosticos.Count(d =>
                    d.FechaGeneracion.Date == dia);
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

            // Actualizar las 5 filas
            for (int i = 0; i < 5; i++)
            {
                if (i < top5.Count)
                {
                    _nombresCriticas[i].Text = top5[i].Key;
                    _valoresCriticas[i].Text = top5[i].Value.ToString();

                    var track = _barrasCriticas[i].Parent;
                    if (track != null && track.Width > 0)
                    {
                        int nuevoAncho = (top5[i].Value * track.Width) / max;
                        _barrasCriticas[i].Width = nuevoAncho;
                    }
                }
                else
                {
                    _nombresCriticas[i].Text = "—";
                    _valoresCriticas[i].Text = "0";
                    _barrasCriticas[i].Width = 0;
                }
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