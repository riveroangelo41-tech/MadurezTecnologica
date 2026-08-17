using MadurezTecnologica.Datos;
using MadurezTecnologica.Estilos;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Vistas
{
    // Vista de Inicio (opción C híbrida):
    //  - Si NO hay empresas registradas → renderiza onboarding de 3 pasos.
    //  - Si HAY datos → renderiza dashboard operativo (saludo + KPIs + acciones + actividad + panel destilación).
    // La vista se re-renderiza automáticamente cada vez que se vuelve visible.
    public partial class VistaInicio : UserControl
    {
        // Repositorios
        private readonly RepositorioEmpresa _repoEmpresa;
        private readonly RepositorioDiagnostico _repoDiag;
        private readonly RepositorioConversacion _repoConv;
        private readonly RepositorioPaqueteHeuristico _repoPkg;

        // Controles raíz
        private Panel _panelHeader = null!;
        private Label _lblHeaderTitulo = null!;   // saludo dinámico en el header
        private Label _lblHeaderSub = null!;       // fecha + stats en el header
        private Panel _panelContenido = null!;
        private Panel _panelInterno = null!;       // contenido scrollable
        private Estilos.IndicadorModoConexion _indicadorConexion = null!;

        public VistaInicio()
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            UpdateStyles();

            _repoEmpresa = new RepositorioEmpresa();
            _repoDiag = new RepositorioDiagnostico();
            _repoConv = new RepositorioConversacion();
            _repoPkg = new RepositorioPaqueteHeuristico();

            ConfigurarControl();
            // Orden igual que las otras vistas: contenido (Fill) PRIMERO, header (Top) DESPUÉS.
            // Así el Fill queda detrás del header sin solaparse (patrón de VistaResultados).
            CrearPanelContenido();
            CrearHeader();

            Load += (s, e) => BeginInvoke(new Action(RenderizarSegunEstado));
            VisibleChanged += (s, e) =>
            {
                if (Visible) RenderizarSegunEstado();
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
        // HEADER (título + indicador de modo)
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
            Controls.Add(_panelHeader);

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
                Text = "👋",
                Font = new Font("Segoe UI Emoji", 18),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            picAvatar.Controls.Add(lblIcono);
            _panelHeader.Controls.Add(picAvatar);

            // Título y subtítulo del header — se actualizan según el estado (saludo dinámico).
            _lblHeaderTitulo = new Label
            {
                Text = "Inicio",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 15),
                Size = new Size(600, 30),
                BackColor = Color.Transparent
            };
            _panelHeader.Controls.Add(_lblHeaderTitulo);

            _lblHeaderSub = new Label
            {
                Text = "Panel principal del sistema de evaluación de madurez tecnológica",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(70, 48),
                Size = new Size(800, 20),
                BackColor = Color.Transparent
            };
            _panelHeader.Controls.Add(_lblHeaderSub);

            _indicadorConexion = new Estilos.IndicadorModoConexion
            {
                Size = new Size(175, 36)
            };
            _panelHeader.Controls.Add(_indicadorConexion);

            _panelHeader.Resize += (s, e) =>
            {
                _indicadorConexion.Location = new Point(
                    _panelHeader.Width - _indicadorConexion.Width - 20, 25);
            };
            _indicadorConexion.Location = new Point(
                _panelHeader.Width - _indicadorConexion.Width - 20, 25);
        }

        // ===================================================
        // PANEL BLANCO REDONDEADO
        // ===================================================
        private void CrearPanelContenido()
        {
            _panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(28, 18, 28, 18)
            };
            Controls.Add(_panelContenido);
            _panelContenido.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(_panelContenido, 25);

            // Contenedor scrollable — mismo patrón exacto que VistaResultados (panelDashboard).
            _panelInterno = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true
            };
            _panelContenido.Controls.Add(_panelInterno);
        }

        // ===================================================
        // RENDERIZAR SEGÚN ESTADO (onboarding vs dashboard)
        // ===================================================
        private void RenderizarSegunEstado()
        {
            _panelInterno.SuspendLayout();
            _panelInterno.Controls.Clear();

            List<Empresa> empresas;
            try
            {
                empresas = _repoEmpresa.ObtenerTodas();
            }
            catch
            {
                empresas = new List<Empresa>();
            }

            if (empresas.Count == 0)
                RenderizarOnboarding();
            else
                RenderizarDashboard(empresas);

            _panelInterno.ResumeLayout(true);
        }

        // Actualiza el saludo dinámico en el header (título + subtítulo).
        private void SetHeaderSaludo(string titulo, string subtitulo)
        {
            _lblHeaderTitulo.Text = titulo;
            _lblHeaderSub.Text = subtitulo;
        }

        // ===================================================
        // ESTADO 1: ONBOARDING (BD VACÍA)
        // ===================================================
        private void RenderizarOnboarding()
        {
            // Saludo dinámico en el header
            SetHeaderSaludo(
                $"{SaludoDinamico()}, bienvenido",
                $"Sistema listo para tu primer análisis · {FechaBonita(DateTime.Now)}");

            // Tarjeta con el onboarding — posición absoluta + Anchor (patrón VistaResultados)
            var tarjeta = CrearTarjetaBase();
            tarjeta.Location = new Point(0, 0);
            tarjeta.Size = new Size(_panelInterno.ClientSize.Width - 4, 380);
            tarjeta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _panelInterno.Controls.Add(tarjeta);

            // === Título sección onboarding ===
            var lblTitSec = new Label
            {
                Text = "Comienza en 3 pasos",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitSec);

            var lblSubSec = new Label
            {
                Text = "Sigue el flujo para evaluar tu primera empresa",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(140, 135, 132),
                Location = new Point(24, 44),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblSubSec);

            // === 3 pasos ===
            var paso1 = CrearPaso(1, "Registra tu empresa", "Nombre, RIF, sector, empleados", "🏢");
            paso1.Location = new Point(24, 76);
            paso1.Size = new Size(tarjeta.Width - 48, 66);
            paso1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tarjeta.Controls.Add(paso1);

            var paso2 = CrearPaso(2, "Carga el informe técnico", "Archivo PDF con la evaluación de la empresa", "📄");
            paso2.Location = new Point(24, 152);
            paso2.Size = new Size(tarjeta.Width - 48, 66);
            paso2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tarjeta.Controls.Add(paso2);

            var paso3 = CrearPaso(3, "Recibe el diagnóstico", "Nivel CMMI, fortalezas, riesgos y recomendaciones", "⚡");
            paso3.Location = new Point(24, 228);
            paso3.Size = new Size(tarjeta.Width - 48, 66);
            paso3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tarjeta.Controls.Add(paso3);

            tarjeta.Resize += (s, e) =>
            {
                paso1.Width = tarjeta.Width - 48;
                paso2.Width = tarjeta.Width - 48;
                paso3.Width = tarjeta.Width - 48;
            };

            // === CTA grande ===
            var btnComenzar = new Button
            {
                Text = "Comenzar ahora  →",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(tarjeta.Width - 48, 48),
                Location = new Point(24, 312),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnComenzar.FlatAppearance.BorderSize = 0;
            btnComenzar.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            btnComenzar.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(btnComenzar, 24);
            btnComenzar.Click += (s, e) => NavegarAVista("Empresas");
            tarjeta.Controls.Add(btnComenzar);

            tarjeta.Resize += (s, e) =>
            {
                btnComenzar.Width = tarjeta.Width - 48;
            };
        }

        // ===================================================
        // ESTADO 2: DASHBOARD (CON DATOS)
        // ===================================================
        private void RenderizarDashboard(List<Empresa> empresas)
        {
            // Cargar todos los diagnósticos (para KPIs y actividad)
            var conversaciones = _repoConv.ObtenerTodas();
            var diagnosticos = new List<Diagnostico>();
            foreach (var c in conversaciones)
                diagnosticos.AddRange(_repoDiag.ObtenerHistorialPorConversacion(c.Id));

            // Saludo dinámico en el header
            string plural = empresas.Count == 1 ? "empresa" : "empresas";
            string pluralE = diagnosticos.Count == 1 ? "evaluación" : "evaluaciones";
            SetHeaderSaludo(
                $"{SaludoDinamico()}, Angelo",
                $"{FechaBonita(DateTime.Now)} · {empresas.Count} {plural} · {diagnosticos.Count} {pluralE}");

            // Tarjeta con el contenido — posición absoluta + Anchor (patrón VistaResultados,
            // que scrollea sin cortar el contenido). NO usar Dock=Top: rompe el AutoScroll.
            // El Anchor Left|Right ajusta el ancho solo al redimensionar.
            var tarjeta = CrearTarjetaBase();
            tarjeta.Location = new Point(0, 0);
            tarjeta.Size = new Size(_panelInterno.ClientSize.Width - 4, 620);
            tarjeta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _panelInterno.Controls.Add(tarjeta);

            // === 4 mini KPIs ===
            var filaKpis = CrearFilaKpis(empresas, diagnosticos);
            filaKpis.Location = new Point(24, 18);
            filaKpis.Width = tarjeta.Width - 48;
            filaKpis.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tarjeta.Controls.Add(filaKpis);

            // === Título "Acciones rápidas" ===
            var lblTitAcc = new Label
            {
                Text = "Acciones rápidas",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 110),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitAcc);

            // === 3 acciones rápidas ===
            var filaAcciones = CrearFilaAcciones();
            filaAcciones.Location = new Point(24, 136);
            filaAcciones.Width = tarjeta.Width - 48;
            filaAcciones.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tarjeta.Controls.Add(filaAcciones);

            // === Actividad reciente ===
            var lblTitAct = new Label
            {
                Text = "Actividad reciente",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(24, 244),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tarjeta.Controls.Add(lblTitAct);

            var lnkHistorial = new Label
            {
                Text = "Ver historial  →",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.MoradoClaro,
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lnkHistorial.Location = new Point(tarjeta.Width - lnkHistorial.PreferredWidth - 24, 246);
            lnkHistorial.Click += (s, e) => NavegarAVista("Historial");
            // Hover: el link se ilumina (morado oscuro + subrayado) al pasar el mouse
            lnkHistorial.MouseEnter += (s, e) =>
            {
                lnkHistorial.ForeColor = Paleta.MoradoOscuro;
                lnkHistorial.Font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Underline);
            };
            lnkHistorial.MouseLeave += (s, e) =>
            {
                lnkHistorial.ForeColor = Paleta.MoradoClaro;
                lnkHistorial.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            };
            tarjeta.Controls.Add(lnkHistorial);

            int yActividad = 268;
            var recientes = diagnosticos
                .OrderByDescending(d => d.FechaGeneracion)
                .Take(3)
                .ToList();

            if (recientes.Count == 0)
            {
                var lblVacio = new Label
                {
                    Text = "Aún no se han generado evaluaciones.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = Color.FromArgb(140, 135, 132),
                    Location = new Point(24, yActividad + 8),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                tarjeta.Controls.Add(lblVacio);
                yActividad += 40;
            }
            else
            {
                foreach (var diag in recientes)
                {
                    var conv = _repoConv.ObtenerPorId(diag.ConversacionId);
                    var emp = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;
                    if (emp == null) continue;

                    var fila = CrearFilaActividad(emp, diag);
                    fila.Location = new Point(24, yActividad);
                    fila.Size = new Size(tarjeta.Width - 48, 46);
                    fila.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    tarjeta.Controls.Add(fila);

                    yActividad += 52;
                }
            }

            // === Separador antes del panel destilación ===
            int ySepDest = yActividad + 8;
            var sepDest = new Panel
            {
                Location = new Point(24, ySepDest),
                Size = new Size(tarjeta.Width - 48, 1),
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tarjeta.Controls.Add(sepDest);

            // === Panel destilación (aporte tesis) ===
            var panelDest = CrearPanelDestilacion();
            panelDest.Location = new Point(24, ySepDest + 16);
            panelDest.Size = new Size(tarjeta.Width - 48, 130);
            panelDest.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tarjeta.Controls.Add(panelDest);

            // Altura final de la tarjeta
            int alturaTotal = ySepDest + 16 + 130 + 20;
            tarjeta.Height = alturaTotal;

            // Reposicionar el link al redimensionar (los separadores usan anchor)
            tarjeta.Resize += (s, e) =>
            {
                lnkHistorial.Location = new Point(tarjeta.Width - lnkHistorial.PreferredWidth - 24, 246);
            };
        }

        // ===================================================
        // COMPONENTES: fila de KPIs (4 mini cards)
        // ===================================================
        private Panel CrearFilaKpis(List<Empresa> empresas, List<Diagnostico> diagnosticos)
        {
            var fila = new Panel
            {
                Height = 76,
                BackColor = Color.Transparent
            };
            // El width se setea desde afuera
            fila.Size = new Size(_panelInterno.ClientSize.Width - 68, 76);

            // Datos
            int totalEmp = empresas.Count;
            int totalEval = diagnosticos.Count;
            double promedio = totalEval > 0 ? diagnosticos.Average(d => d.NivelMadurez) : 0;
            var ultima = diagnosticos.OrderByDescending(d => d.FechaGeneracion).FirstOrDefault();
            string ultimaTxt = ultima != null ? TiempoRelativo(ultima.FechaGeneracion) : "—";
            string ultimaSubTxt = "sin datos";
            if (ultima != null)
            {
                var conv = _repoConv.ObtenerPorId(ultima.ConversacionId);
                var emp = conv != null ? _repoEmpresa.ObtenerPorId(conv.EmpresaId) : null;
                if (emp != null) ultimaSubTxt = emp.Nombre.Length > 12 ? emp.Nombre.Substring(0, 12) + "…" : emp.Nombre;
            }

            var k1 = CrearKpiMini("EMPRESAS", totalEmp.ToString(), "registradas", Paleta.MoradoOscuro);
            var k2 = CrearKpiMini("EVAL.", totalEval.ToString(), "totales", Paleta.VerdeGrisaceo);
            var k3 = CrearKpiMini("NIVEL PROM.", totalEval > 0 ? promedio.ToString("F1") : "—", "/ 5", Paleta.MoradoClaro);
            var k4 = CrearKpiMiniTexto("ÚLTIMA", ultimaTxt, ultimaSubTxt, Paleta.VerdeGrisaceoOscuro);

            fila.Controls.Add(k1);
            fila.Controls.Add(k2);
            fila.Controls.Add(k3);
            fila.Controls.Add(k4);

            void Reposicionar()
            {
                int gap = 12;
                int ancho = (fila.Width - gap * 3) / 4;
                k1.Size = new Size(ancho, 76); k1.Location = new Point(0, 0);
                k2.Size = new Size(ancho, 76); k2.Location = new Point(ancho + gap, 0);
                k3.Size = new Size(ancho, 76); k3.Location = new Point((ancho + gap) * 2, 0);
                k4.Size = new Size(ancho, 76); k4.Location = new Point((ancho + gap) * 3, 0);
            }
            fila.Resize += (s, e) => Reposicionar();
            fila.HandleCreated += (s, e) => fila.BeginInvoke(new Action(Reposicionar));

            return fila;
        }

        private Panel CrearKpiMini(string etiqueta, string numero, string sub, Color acento)
        {
            var pastel = VersionPastel(acento);
            var panel = new Panel { BackColor = pastel };
            panel.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(panel, 10);

            var lblEt = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = acento,
                Location = new Point(12, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblEt);

            var lblNum = new Label
            {
                Text = numero,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = acento,
                Location = new Point(12, 26),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblNum);

            var lblSub = new Label
            {
                Text = sub,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 115, 115),
                Location = new Point(12, 58),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblSub);

            AgregarHoverCardSolida(panel, acento);
            return panel;
        }

        // Agrega hover a una card de fondo sólido (KPI/stat): oscurece un poco el pastel
        // al pasar el mouse. Se aplica a la card y a sus hijos para que no parpadee.
        private void AgregarHoverCardSolida(Panel card, Color acento)
        {
            Color normal = VersionPastel(acento);
            Color hover = VersionPastel(acento, 0.78f);   // pastel un poco más saturado
            void On() => card.BackColor = hover;
            void Off()
            {
                if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
                    card.BackColor = normal;
            }
            card.MouseEnter += (s, e) => On();
            card.MouseLeave += (s, e) => Off();
            foreach (Control child in card.Controls)
            {
                child.MouseEnter += (s, e) => On();
                child.MouseLeave += (s, e) => Off();
            }
        }

        private Panel CrearKpiMiniTexto(string etiqueta, string valorGrande, string valorPequeno, Color acento)
        {
            var pastel = VersionPastel(acento);
            var panel = new Panel { BackColor = pastel };
            panel.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(panel, 10);

            var lblEt = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = acento,
                Location = new Point(12, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblEt);

            var lblVal = new Label
            {
                Text = valorGrande,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = acento,
                Location = new Point(12, 32),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblVal);

            var lblSub = new Label
            {
                Text = valorPequeno,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 115, 115),
                Location = new Point(12, 55),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblSub);

            AgregarHoverCardSolida(panel, acento);
            return panel;
        }

        // ===================================================
        // COMPONENTES: fila de 3 acciones rápidas
        // ===================================================
        private Panel CrearFilaAcciones()
        {
            var fila = new Panel
            {
                Height = 96,
                BackColor = Color.Transparent
            };
            fila.Size = new Size(_panelInterno.ClientSize.Width - 68, 96);

            var a1 = CrearAccionRapida("Cargar informe", "Analizar PDF nuevo", "📄", Paleta.MoradoOscuro, "CargarInforme");
            var a2 = CrearAccionRapida("Chat con IA", "Refinar análisis", "💬", Paleta.VerdeGrisaceo, "Chat");
            var a3 = CrearAccionRapida("Ver resultados", "Dashboard completo", "📊", Paleta.MoradoClaro, "Resultados");

            fila.Controls.Add(a1);
            fila.Controls.Add(a2);
            fila.Controls.Add(a3);

            void Reposicionar()
            {
                int gap = 12;
                int ancho = (fila.Width - gap * 2) / 3;
                a1.Size = new Size(ancho, 96); a1.Location = new Point(0, 0);
                a2.Size = new Size(ancho, 96); a2.Location = new Point(ancho + gap, 0);
                a3.Size = new Size(ancho, 96); a3.Location = new Point((ancho + gap) * 2, 0);
            }
            fila.Resize += (s, e) => Reposicionar();
            fila.HandleCreated += (s, e) => fila.BeginInvoke(new Action(Reposicionar));

            return fila;
        }

        private Panel CrearAccionRapida(string titulo, string subtitulo, string icono, Color acento, string vistaDestino)
        {
            // Flag de hover que lee el Paint para iluminar la card (fondo + borde de acento).
            bool hover = false;

            var panel = new Panel
            {
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = 12;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(panel.Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(panel.Width - r * 2 - 1, panel.Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, panel.Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();

                // En hover: fondo pastel del color de acento + borde de acento (2px).
                Color fondo = hover ? VersionPastel(acento, 0.90f) : Color.White;
                using (var br = new SolidBrush(fondo))
                    g.FillPath(br, path);
                using (var pen = new Pen(hover ? acento : Color.FromArgb(232, 229, 227), hover ? 1.6f : 1f))
                    g.DrawPath(pen, path);
            };
            panel.Resize += (s, e) => panel.Invalidate();

            // Chip icono
            var chip = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(16, 14),
                BackColor = VersionPastel(acento),
                Cursor = Cursors.Hand
            };
            var pathChip = new System.Drawing.Drawing2D.GraphicsPath();
            pathChip.AddEllipse(0, 0, 36, 36);
            chip.Region = new Region(pathChip);
            var lblIco = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI Emoji", 14),
                ForeColor = acento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            chip.Controls.Add(lblIco);
            panel.Controls.Add(chip);

            var lblTit = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(16, 58),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(lblTit);

            var lblSub = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(130, 125, 125),
                Location = new Point(16, 76),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(lblSub);

            // Click y hover en cualquier parte de la card (panel + hijos) para que no
            // parpadee al mover el cursor entre el icono y los textos.
            EventHandler onClick = (s, e) => NavegarAVista(vistaDestino);
            void AplicarHover() { hover = true; panel.Invalidate(); }
            void QuitarHover()
            {
                if (!panel.ClientRectangle.Contains(panel.PointToClient(Cursor.Position)))
                {
                    hover = false;
                    panel.Invalidate();
                }
            }

            foreach (Control ctrl in new Control[] { panel, chip, lblIco, lblTit, lblSub })
            {
                ctrl.Cursor = Cursors.Hand;
                ctrl.Click += onClick;
                ctrl.MouseEnter += (s, e) => AplicarHover();
                ctrl.MouseLeave += (s, e) => QuitarHover();
            }

            return panel;
        }

        // ===================================================
        // COMPONENTES: fila de actividad reciente
        // ===================================================
        private Panel CrearFilaActividad(Empresa empresa, Diagnostico diag)
        {
            Color filaNormal = ColorTranslator.FromHtml("#F7F4FA");
            Color filaHover = ColorTranslator.FromHtml("#EFEAF6");

            var fila = new Panel
            {
                Height = 46,
                BackColor = filaNormal
            };
            fila.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(fila, 8);

            // Avatar circular con inicial
            var avatar = new Panel
            {
                Size = new Size(28, 28),
                Location = new Point(10, 9),
                BackColor = ColorDelNivel(diag.NivelMadurez)
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, 28, 28);
            avatar.Region = new Region(pathAv);
            var lblIni = new Label
            {
                Text = empresa.Nombre.Length > 0 ? empresa.Nombre[0].ToString().ToUpper() : "?",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblIni);
            fila.Controls.Add(avatar);

            var lblNom = new Label
            {
                Text = empresa.Nombre,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(48, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            fila.Controls.Add(lblNom);

            var lblDet = new Label
            {
                Text = $"Nivel {diag.NivelMadurez} · {NombreNivel(diag.NivelMadurez)} · {TiempoRelativo(diag.FechaGeneracion)}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(130, 125, 125),
                Location = new Point(48, 24),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            fila.Controls.Add(lblDet);

            // Pill nivel a la derecha
            var pill = new Panel
            {
                Size = new Size(58, 20),
                BackColor = ColorDelNivel(diag.NivelMadurez),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pill.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(pill, 10);
            var lblPill = new Label
            {
                Text = $"NIVEL {diag.NivelMadurez}",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            pill.Controls.Add(lblPill);
            fila.Controls.Add(pill);

            fila.Resize += (s, e) =>
            {
                pill.Location = new Point(fila.Width - pill.Width - 12, 13);
            };
            fila.HandleCreated += (s, e) => fila.BeginInvoke(new Action(() =>
            {
                pill.Location = new Point(fila.Width - pill.Width - 12, 13);
            }));

            // Hover: la fila se ilumina al pasar el mouse por encima. Aplicado a la fila
            // y a sus hijos para que no parpadee al mover el cursor entre los labels.
            void AplicarHover() => fila.BackColor = filaHover;
            void QuitarHover()
            {
                if (!fila.ClientRectangle.Contains(fila.PointToClient(Cursor.Position)))
                    fila.BackColor = filaNormal;
            }

            foreach (Control ctrl in new Control[] { fila, lblNom, lblDet, avatar, lblIni })
            {
                ctrl.MouseEnter += (s, e) => AplicarHover();
                ctrl.MouseLeave += (s, e) => QuitarHover();
            }

            return fila;
        }

        // ===================================================
        // COMPONENTES: panel destacado del destilador (aporte tesis)
        // ===================================================
        private Panel CrearPanelDestilacion()
        {
            var panel = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#FBF8FE")
            };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = 12;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(panel.Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(panel.Width - r * 2 - 1, panel.Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, panel.Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                using (var br = new SolidBrush(ColorTranslator.FromHtml("#FBF8FE")))
                    g.FillPath(br, path);
                using (var pen = new Pen(ColorTranslator.FromHtml("#E1D6EF"), 1))
                    g.DrawPath(pen, path);
            };
            panel.Resize += (s, e) => panel.Invalidate();

            // Consultar paquete activo (puede no existir aún)
            PaqueteHeuristico? activo = null;
            try { activo = _repoPkg.ObtenerActivo(); } catch { activo = null; }

            var lblTag = new Label
            {
                Text = "🧠  MOTOR OFFLINE INTELIGENTE",
                Font = new Font("Segoe UI Emoji", 7.5f, FontStyle.Bold),
                ForeColor = Paleta.MoradoClaro,
                Location = new Point(18, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblTag);

            if (activo == null)
            {
                var lblTit = new Label
                {
                    Text = "Aprendizaje progresivo aún no activado",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(18, 32),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(lblTit);

                var lblExp = new Label
                {
                    Text = "Genera más análisis online para que el motor offline aprenda de la IA.\n" +
                           "El destilador se ejecuta automáticamente al terminar cada análisis con Claude.",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(74, 70, 80),
                    Location = new Point(18, 56),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(lblExp);
            }
            else
            {
                var lblTit = new Label
                {
                    Text = "Aprendizaje progresivo activo",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(18, 32),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(lblTit);

                var lblLinea1 = new Label
                {
                    Text = $"Paquete heurístico v{activo.Version} · destilado de {activo.NumDictamenes} dictámenes IA",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(74, 70, 80),
                    Location = new Point(18, 54),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(lblLinea1);

                int pctBase = (int)Math.Round(activo.ExactitudBase * 100);
                int pctDest = (int)Math.Round(activo.ExactitudDestilada * 100);
                int mejora = pctDest - pctBase;
                string signo = mejora >= 0 ? "+" : "";

                var lblLinea2 = new Label
                {
                    Text = $"Exactitud del motor offline: {pctBase}% → {pctDest}% ({signo}{mejora}%)",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(74, 70, 80),
                    Location = new Point(18, 72),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(lblLinea2);

                // Mini progress bar visual
                var track = new Panel
                {
                    Location = new Point(18, 96),
                    Size = new Size(panel.Width - 36, 6),
                    BackColor = ColorTranslator.FromHtml("#EEE9F0"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                track.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(track, 3);
                panel.Controls.Add(track);

                var fill = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(0, 6),
                    BackColor = Paleta.MoradoClaro
                };
                fill.Resize += (s, e) =>
                {
                    if (fill.Width > 4)
                        Paleta.AplicarBordeRedondeadoSuave(fill, 3);
                };
                track.Controls.Add(fill);
                track.Resize += (s, e) => fill.Width = track.Width * pctDest / 100;
                track.HandleCreated += (s, e) => track.BeginInvoke(new Action(() =>
                {
                    fill.Width = track.Width * pctDest / 100;
                }));

                var lblFecha = new Label
                {
                    Text = $"Última destilación: {TiempoRelativo(activo.FechaGeneracion)} · Próxima: automática",
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = Color.FromArgb(140, 135, 132),
                    Location = new Point(18, 108),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(lblFecha);
            }

            return panel;
        }

        // ===================================================
        // TARJETA BASE (fondo blanco redondeado con borde)
        // ===================================================
        private Panel CrearTarjetaBase()
        {
            var t = new Panel { BackColor = Color.White };
            t.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = 14;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(t.Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(t.Width - r * 2 - 1, t.Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, t.Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                using (var br = new SolidBrush(Color.White))
                    g.FillPath(br, path);
                using (var pen = new Pen(Color.FromArgb(232, 229, 227), 1))
                    g.DrawPath(pen, path);
            };
            t.Resize += (s, e) => t.Invalidate();
            return t;
        }

        // ===================================================
        // ONBOARDING: crear una fila de paso numerado
        // ===================================================
        private Panel CrearPaso(int numero, string titulo, string sub, string icono)
        {
            var paso = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#F7F4FA")
            };
            paso.Resize += (s, e) => Paleta.AplicarBordeRedondeadoSuave(paso, 10);

            // Número circular
            var chip = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(14, 15),
                BackColor = Paleta.MoradoOscuro
            };
            var pathChip = new System.Drawing.Drawing2D.GraphicsPath();
            pathChip.AddEllipse(0, 0, 36, 36);
            chip.Region = new Region(pathChip);
            var lblNum = new Label
            {
                Text = numero.ToString(),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            chip.Controls.Add(lblNum);
            paso.Controls.Add(chip);

            var lblTit = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(62, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            paso.Controls.Add(lblTit);

            var lblSub = new Label
            {
                Text = sub,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(130, 125, 125),
                Location = new Point(62, 32),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            paso.Controls.Add(lblSub);

            // Icono derecho
            var lblIco = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI Emoji", 14),
                ForeColor = Paleta.MoradoClaro,
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            paso.Controls.Add(lblIco);
            paso.Resize += (s, e) =>
            {
                lblIco.Location = new Point(paso.Width - 44, 18);
            };
            paso.HandleCreated += (s, e) => paso.BeginInvoke(new Action(() =>
            {
                lblIco.Location = new Point(paso.Width - 44, 18);
            }));

            return paso;
        }

        // ===================================================
        // NAVEGACIÓN a otra vista (usa FormMain.NavegarA…)
        // ===================================================
        private void NavegarAVista(string clave)
        {
            var form = FindForm() as Presentacion.FormMain;
            if (form == null) return;

            switch (clave)
            {
                case "Empresas": form.NavegarAEmpresas(); break;
                case "CargarInforme": form.NavegarACargarInforme(); break;
                case "Chat": form.NavegarAVistaChat(); break;
                case "Resultados": form.NavegarAResultados(); break;
                case "Historial": form.NavegarAHistorial(); break;
            }
        }

        // ===================================================
        // HELPERS DE FORMATO Y COLORES
        // ===================================================
        private string SaludoDinamico()
        {
            int hora = DateTime.Now.Hour;
            if (hora < 12) return "Buenos días";
            if (hora < 19) return "Buenas tardes";
            return "Buenas noches";
        }

        private string FechaBonita(DateTime fecha)
        {
            string s = fecha.ToString("dddd d 'de' MMMM");
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        private string TiempoRelativo(DateTime fecha)
        {
            var diff = DateTime.Now - fecha;
            if (diff.TotalMinutes < 1) return "hace instantes";
            if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 7) return $"hace {(int)diff.TotalDays} días";
            return fecha.ToString("dd/MM/yyyy");
        }

        private string NombreNivel(int nivel) => nivel switch
        {
            1 => "Inicial",
            2 => "Gestionado",
            3 => "Definido",
            4 => "Gest. cuantitativo",
            5 => "Optimizado",
            _ => "—"
        };

        private Color ColorDelNivel(int nivel) => nivel switch
        {
            1 => Color.FromArgb(178, 172, 169),
            2 => Paleta.VerdeGrisaceoOscuro,
            3 => Paleta.VerdeGrisaceo,
            4 => Paleta.MoradoClaro,
            5 => Paleta.MoradoOscuro,
            _ => Color.FromArgb(178, 172, 169)
        };

        // Versión pastel del color (mezcla con blanco 87%)
        private Color VersionPastel(Color color, float mezcla = 0.87f)
        {
            int r = (int)(color.R * (1 - mezcla) + 255 * mezcla);
            int g = (int)(color.G * (1 - mezcla) + 255 * mezcla);
            int b = (int)(color.B * (1 - mezcla) + 255 * mezcla);
            return Color.FromArgb(r, g, b);
        }
    }
}
