using MadurezTecnologica.Estilos;

namespace MadurezTecnologica.Presentacion
{
    public partial class FormMain : Form
    {
        // Controles del menú lateral
        private Panel panelMenu = null!;
        private Panel panelCentral = null!;
        private Panel panelLogo = null!;
        private Label lblTitulo = null!;
        private Label lblSubtitulo = null!;

        // Botones del menú
        private Button btnInicio = null!;
        private Button btnEmpresas = null!;
        private Button btnCargarInforme = null!;
        private Button btnChat = null!;
        private Button btnResultados = null!;
        private Button btnHistorial = null!;

        // Botón actualmente activo
        private Button? botonActivo = null;

        public FormMain()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearMenuLateral();
            CrearPanelCentral();
            // Arrancamos directamente en la vista de Inicio (no en el placeholder).
            // CambiarVista se encarga también de marcar el botón como activo.
            CambiarVista(btnInicio, "Inicio");
        }

        private void ConfigurarFormulario()
        {
            Text = "Sistema de Evaluación de Madurez Tecnológica";

            // === Tamaño inicial adaptado a la pantalla del usuario ===
            // Si la pantalla es pequeña se abre en 1100x650; si es más grande se
            // abre proporcional al área de trabajo (85% del ancho, 85% del alto)
            // sin pasarse de 1600x1000 para que en 4K no se vea enorme.
            // Igualmente WindowState=Maximized manda: el tamaño solo importa cuando
            // el usuario desmaximiza la ventana.
            var areaTrabajo = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 760);
            int anchoIni = Math.Clamp((int)(areaTrabajo.Width  * 0.85), 1100, 1600);
            int altoIni  = Math.Clamp((int)(areaTrabajo.Height * 0.85),  650, 1000);
            Size = new Size(anchoIni, altoIni);

            // Mínimo: nunca por debajo de lo que la interfaz soporta sin cortar.
            MinimumSize = new Size(1100, 650);

            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Paleta.GrisClaro;
            WindowState = FormWindowState.Maximized;

            // Icono de la ventana (aparece en la barra de título y en la de tareas).
            // Cae con gracia al icono por defecto si no se puede extraer del .exe.
            var icoApp = CargadorIconos.ObtenerIconoApp();
            if (icoApp != null) this.Icon = icoApp;
        }

        private void CrearMenuLateral()
        {
            panelMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Paleta.MoradoOscuro
            };
            Controls.Add(panelMenu);

            // Aplicar esquinas redondeadas y mantenerlas al redimensionar la ventana
            panelMenu.Resize += (s, e) => AplicarBordeRedondeado(panelMenu, 30);

            panelLogo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Paleta.MoradoOscuro
            };
            panelMenu.Controls.Add(panelLogo);

            // Logo del sistema: el cubo con diamante morado (PNG embebido).
            // Si el recurso no está disponible, mostramos el círculo morado como fallback
            // para que la ventana no rompa.
            var imgLogo = CargadorIconos.ObtenerRedimensionado(CargadorIconos.App, 44, 44);
            if (imgLogo != null)
            {
                var picLogo = new PictureBox
                {
                    Image = imgLogo,
                    Size = new Size(44, 44),
                    Location = new Point(18, 23),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                panelLogo.Controls.Add(picLogo);
            }
            else
            {
                var picLogo = new Panel
                {
                    Size = new Size(40, 40),
                    Location = new Point(20, 25),
                    BackColor = Paleta.MoradoClaro
                };
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(0, 0, picLogo.Width, picLogo.Height);
                picLogo.Region = new Region(path);
                panelLogo.Controls.Add(picLogo);
            }

            lblTitulo = new Label
            {
                Text = "Sistema de Madurez",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(75, 28),
                Size = new Size(170, 22),
                BackColor = Color.Transparent
            };
            panelLogo.Controls.Add(lblTitulo);

            lblSubtitulo = new Label
            {
                Text = "Tecnológica para PYMES",
                Font = new Font("Segoe UI", 9),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(75, 50),
                Size = new Size(170, 18),
                BackColor = Color.Transparent
            };
            panelLogo.Controls.Add(lblSubtitulo);

            // Iconos a la DERECHA del texto (según preferencia del usuario)
            btnInicio        = CrearBotonMenu("Inicio",                 150, CargadorIconos.Inicio);
            btnEmpresas      = CrearBotonMenu("Empresas",               205, CargadorIconos.Empresas);
            btnCargarInforme = CrearBotonMenu("Cargar Informe",         260, CargadorIconos.Cargar);
            btnChat          = CrearBotonMenu("Análisis con IA (Chat)", 315, CargadorIconos.Chat);
            btnResultados    = CrearBotonMenu("Resultados",             370, CargadorIconos.Resultados);
            btnHistorial     = CrearBotonMenu("Historial",              425, CargadorIconos.Historial);

            btnInicio.Click += (s, e) => CambiarVista(btnInicio, "Inicio");
            btnEmpresas.Click += (s, e) => CambiarVista(btnEmpresas, "Empresas");
            btnCargarInforme.Click += (s, e) => CambiarVista(btnCargarInforme, "Cargar Informe");
            btnChat.Click += (s, e) => CambiarVista(btnChat, "Análisis con IA (Chat)");
            btnResultados.Click += (s, e) => CambiarVista(btnResultados, "Resultados");
            btnHistorial.Click += (s, e) => CambiarVista(btnHistorial, "Historial");
        }

        private Button CrearBotonMenu(string texto, int top, string? nombreIcono = null)
        {
            var btn = new Button
            {
                Text = "  " + texto,     // padding izquierdo para el texto
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(230, 45),
                Location = new Point(10, top),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            btn.FlatAppearance.MouseDownBackColor = Paleta.MoradoOscuroHover;

            // Icono a la DERECHA del texto (según preferencia del usuario).
            // 20x20 px se ve nítido para un botón de 45 px de alto.
            if (!string.IsNullOrEmpty(nombreIcono))
            {
                var img = CargadorIconos.ObtenerRedimensionado(nombreIcono, 20, 20);
                if (img != null)
                {
                    btn.Image = img;
                    btn.ImageAlign = ContentAlignment.MiddleRight;
                    btn.TextImageRelation = TextImageRelation.TextBeforeImage;
                    btn.Padding = new Padding(0, 0, 14, 0);  // margen derecho para el icono
                }
            }

            panelMenu.Controls.Add(btn);
            return btn;
        }

        private void CrearPanelCentral()
        {
            panelCentral = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Paleta.GrisClaro,
                Padding = new Padding(20)
            };
            Controls.Add(panelCentral);

            panelCentral.BringToFront();
            panelMenu.SendToBack();

            var lblPlaceholder = new Label
            {
                Text = "Bienvenido al Sistema de Evaluación de Madurez Tecnológica",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelCentral.Controls.Add(lblPlaceholder);
        }

        private bool _animandoVista = false;
        private Button? _botonActivoActual = null;

        // Cache de vistas: se crean UNA sola vez y se reutilizan.
        // Esto permite que procesos en segundo plano (como el streaming del chat con la IA)
        // sigan funcionando aunque el usuario cambie de pestaña.
        private readonly Dictionary<Button, Control> _vistasCache = new();

        private void CambiarVista(Button botonClickeado, string nombreVista)
        {
            if (_animandoVista) return;

            // Si ya estoy en esa vista, no hago nada
            if (_botonActivoActual == botonClickeado) return;

            Button? botonAnterior = _botonActivoActual;
            EstablecerBotonActivo(botonClickeado);
            _botonActivoActual = botonClickeado;

            // Obtener la vista del cache o crearla si es la primera vez
            if (!_vistasCache.TryGetValue(botonClickeado, out var nuevaVista))
            {
                nuevaVista = CrearVistaParaBoton(botonClickeado, nombreVista);
                _vistasCache[botonClickeado] = nuevaVista;
                nuevaVista.Visible = false;
                panelCentral.Controls.Add(nuevaVista);
            }

            // Primera vista mostrada (o no hay vista anterior cacheada): mostrar sin animación
            if (botonAnterior == null || !_vistasCache.TryGetValue(botonAnterior, out var vistaAnterior))
            {
                // Ocultar cualquier vista placeholder que pueda existir
                foreach (Control c in panelCentral.Controls)
                {
                    if (c != nuevaVista) c.Visible = false;
                }
                nuevaVista.Dock = DockStyle.Fill;
                nuevaVista.Visible = true;
                nuevaVista.BringToFront();
                return;
            }

            AnimarTransicionVista(vistaAnterior, nuevaVista);
        }

        private Control CrearVistaParaBoton(Button botonClickeado, string nombreVista)
        {
            if (botonClickeado == btnInicio)
                return new MadurezTecnologica.Vistas.VistaInicio();

            if (botonClickeado == btnChat)
                return new MadurezTecnologica.Vistas.VistaChat();

            if (botonClickeado == btnEmpresas)
                return new MadurezTecnologica.Vistas.VistaEmpresas();

            if (botonClickeado == btnCargarInforme)
                return new MadurezTecnologica.Vistas.VistaCargarInforme();

            if (botonClickeado == btnResultados)
                return new MadurezTecnologica.Vistas.VistaResultados();

            if (botonClickeado == btnHistorial)
                return new MadurezTecnologica.Vistas.VistaHistorial();

            return new Label
            {
                Text = $"Vista: {nombreVista}\n\n(Se construirá próximamente)",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        // === Animación de transición entre vistas ===
        // Estrategia "snapshot bitmap": en lugar de deslizar las dos vistas reales
        // (con todos sus controles vivos), tomo una foto de cada una y desplazo esas
        // 2 imágenes en un único panel doblemente buffereado. Así:
        //   1. Ningún control se repinta durante la animación → cero micro-desfases.
        //   2. Solo se dibujan 2 bitmaps por frame → 60 FPS sostenidos, incluso en
        //      vistas con decenas de controles.
        //   3. El resultado se percibe como un bloque sólido moviéndose, no elemento
        //      por elemento.
        private void AnimarTransicionVista(Control vistaAnterior, Control nuevaVista)
        {
            _animandoVista = true;

            int anchoTotal = panelCentral.ClientSize.Width;
            int altoTotal  = panelCentral.ClientSize.Height;
            if (anchoTotal <= 0 || altoTotal <= 0)
            {
                // Panel aún sin dimensiones (arranque muy temprano) — sin animación.
                vistaAnterior.Visible = false;
                nuevaVista.Dock = DockStyle.Fill;
                nuevaVista.Visible = true;
                nuevaVista.BringToFront();
                _animandoVista = false;
                return;
            }

            // ---- 1. Preparar la vista nueva FUERA de la pantalla para poder fotografiarla ----
            // Debe tener el tamaño final y sus controles ya layouteados; si no, la foto sale
            // vacía o descolocada.
            nuevaVista.Dock = DockStyle.None;
            nuevaVista.Size = new Size(anchoTotal, altoTotal);
            nuevaVista.Location = new Point(anchoTotal + 10, 0);   // fuera de vista
            nuevaVista.Visible = true;
            nuevaVista.PerformLayout();

            vistaAnterior.Dock = DockStyle.None;
            vistaAnterior.Size = new Size(anchoTotal, altoTotal);
            vistaAnterior.Location = new Point(0, 0);
            vistaAnterior.Visible = true;
            vistaAnterior.PerformLayout();

            // Forzar el REPINTADO COMPLETO de las dos vistas antes de fotografiarlas.
            // Sin esto, hay controles (imágenes, listas dinámicas, tarjetas con Paint
            // custom) que aún no se han dibujado; la foto sale incompleta y el usuario
            // ve "aparecer" lo faltante al terminar la animación → sensación de glitch.
            // Refresh() + Update() garantiza que el estado visual actual está en pantalla
            // ANTES de que DrawToBitmap lo capture.
            vistaAnterior.Refresh();
            nuevaVista.Refresh();
            vistaAnterior.Update();
            nuevaVista.Update();

            // ---- 2. Tomar el snapshot de cada vista (bitmap del render actual) ----
            var bmpAnterior = new Bitmap(anchoTotal, altoTotal);
            vistaAnterior.DrawToBitmap(bmpAnterior, new Rectangle(0, 0, anchoTotal, altoTotal));
            var bmpNueva = new Bitmap(anchoTotal, altoTotal);
            nuevaVista.DrawToBitmap(bmpNueva, new Rectangle(0, 0, anchoTotal, altoTotal));

            // Ocultar ambas vistas reales — durante la animación solo se ve el snapshot.
            vistaAnterior.Visible = false;
            nuevaVista.Visible = false;

            // ---- 3. Panel de animación doblemente buffereado ----
            var panelAnim = new BufferedPanel
            {
                Location = new Point(0, 0),
                Size = new Size(anchoTotal, altoTotal),
                BackColor = panelCentral.BackColor
            };
            panelCentral.Controls.Add(panelAnim);
            panelAnim.BringToFront();

            int offset = 0;
            panelAnim.Paint += (sender, ev) =>
            {
                var g = ev.Graphics;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                // Vista anterior: se desplaza hacia la IZQUIERDA
                g.DrawImage(bmpAnterior, -offset, 0);
                // Vista nueva: entra por la DERECHA (queda pegada a la anterior)
                g.DrawImage(bmpNueva, anchoTotal - offset, 0);
            };

            // ---- 4. Timer que solo actualiza el offset ----
            var cronometro = System.Diagnostics.Stopwatch.StartNew();
            int duracionMs = 280;
            var timer = new System.Windows.Forms.Timer { Interval = 16 };   // ~60 FPS

            timer.Tick += (s, e) =>
            {
                double t = Math.Min(1.0, cronometro.ElapsedMilliseconds / (double)duracionMs);
                // easeOutCubic: arranca rápido y frena con suavidad al final
                double eased = 1 - Math.Pow(1 - t, 3);
                offset = (int)(anchoTotal * eased);
                panelAnim.Invalidate();

                if (t >= 1.0)
                {
                    timer.Stop();
                    timer.Dispose();
                    cronometro.Stop();

                    // ---- 5. Cierre ATÓMICO: la vista real debe estar completa y visible
                    //         DEBAJO del panel de animación ANTES de quitarlo. Así el
                    //         usuario nunca ve un frame vacío o los controles moviéndose
                    //         a su sitio → el swap es invisible.
                    panelCentral.SuspendLayout();

                    // (a) Colocar la vista nueva en su posición final, DEBAJO del panelAnim
                    vistaAnterior.Dock = DockStyle.None;
                    vistaAnterior.Visible = false;

                    nuevaVista.Location = new Point(0, 0);
                    nuevaVista.Size = new Size(anchoTotal, altoTotal);
                    nuevaVista.Dock = DockStyle.Fill;
                    nuevaVista.Visible = true;
                    nuevaVista.SendToBack();       // queda detrás del panelAnim
                    panelAnim.BringToFront();      // asegurar que la foto tapa la vista

                    panelCentral.ResumeLayout(false);

                    // (b) Forzar el repintado completo de la vista real ANTES de quitar
                    //     el panel de animación. Refresh recursivo + Update procesa toda
                    //     la cola de WM_PAINT ya mismo, no cuando llegue el hilo de UI.
                    nuevaVista.Refresh();
                    nuevaVista.Update();

                    // (c) Ahora sí: quitar el panel de animación. Como la vista real
                    //     ya está pintada idéntica a la foto, el usuario no percibe cambio.
                    panelCentral.Controls.Remove(panelAnim);
                    panelAnim.Dispose();
                    bmpAnterior.Dispose();
                    bmpNueva.Dispose();

                    _animandoVista = false;
                }
            };
            timer.Start();
        }

        private void EstablecerBotonActivo(Button botonNuevo)
        {
            if (botonActivo != null)
            {
                botonActivo.BackColor = Paleta.MoradoOscuro;
                botonActivo.FlatAppearance.MouseOverBackColor = Paleta.MoradoOscuroHover;
            }

            botonNuevo.BackColor = Paleta.MoradoClaro;
            botonNuevo.FlatAppearance.MouseOverBackColor = Paleta.MoradoClaro;
            botonActivo = botonNuevo;
        }
        private void AplicarBordeRedondeado(Panel panel, int radio)
        {
            if (panel.Width <= 0 || panel.Height <= 0) return;

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radio, radio, 180, 90);
            path.AddArc(panel.Width - radio, 0, radio, radio, 270, 90);
            path.AddArc(panel.Width - radio, panel.Height - radio, radio, radio, 0, 90);
            path.AddArc(0, panel.Height - radio, radio, radio, 90, 90);
            path.CloseFigure();
            panel.Region = new Region(path);
        }
        public void NavegarAVistaChat()
        {
            // Simulamos un click en el botón del menú lateral
            CambiarVista(btnChat, "Análisis con IA (Chat)");
        }

        // Métodos públicos de navegación usados por VistaInicio para las acciones rápidas
        public void NavegarAEmpresas()      => CambiarVista(btnEmpresas, "Empresas");
        public void NavegarACargarInforme() => CambiarVista(btnCargarInforme, "Cargar Informe");
        public void NavegarAResultados()    => CambiarVista(btnResultados, "Resultados");
        public void NavegarAHistorial()     => CambiarVista(btnHistorial, "Historial");
    }

    // Panel con doble buffer activado para animaciones sin parpadeo
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }
    }
}