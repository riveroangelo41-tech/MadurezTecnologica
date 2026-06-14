using MadurezTecnologica.Estilos;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Presentacion
{
    public partial class FormDiagnostico : Form
    {
        private readonly Diagnostico _diag;

        public FormDiagnostico(Diagnostico diag)
        {
            InitializeComponent();
            _diag = diag;
            ConfigurarForm();
            CrearContenido();
        }

        private void ConfigurarForm()
        {
            Text = $"Evaluación del {_diag.FechaGeneracion:dd/MM/yyyy HH:mm}";
            Size = new Size(620, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void CrearContenido()
        {
            // === Cabecera con nivel CMMI destacado ===
            var panelCabecera = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = _diag.EsFinal ? Paleta.MoradoOscuro : Paleta.VerdeGrisaceo
            };
            Controls.Add(panelCabecera);

            var lblNivel = new Label
            {
                Text = $"Nivel CMMI {_diag.NivelMadurez}",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(25, 18),
                Size = new Size(400, 40),
                BackColor = Color.Transparent
            };
            panelCabecera.Controls.Add(lblNivel);

            var lblTipo = new Label
            {
                Text = _diag.EsFinal ? "Evaluación final" : "Evaluación intermedia",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(28, 60),
                Size = new Size(400, 25),
                BackColor = Color.Transparent
            };
            panelCabecera.Controls.Add(lblTipo);

            // === Botón cerrar abajo ===
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(15)
            };
            Controls.Add(panelBotones);

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                BackColor = Paleta.MoradoOscuro,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 35),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            panelBotones.Controls.Add(btnCerrar);

            // === Contenido scrollable ===
            var panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(25, 20, 25, 20)
            };
            Controls.Add(panelContenido);
            panelContenido.BringToFront();

            // Helper local para crear cada sección (título + contenido)
            int yActual = 0;
            void CrearSeccion(string titulo, string contenido)
            {
                var lblTitulo = new Label
                {
                    Text = titulo,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = Paleta.MoradoOscuro,
                    Location = new Point(5, yActual),
                    Size = new Size(540, 22),
                    BackColor = Color.Transparent
                };
                panelContenido.Controls.Add(lblTitulo);
                yActual += 28;

                var lblContenido = new Label
                {
                    Text = string.IsNullOrWhiteSpace(contenido) ? "(Sin información)" : contenido,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Paleta.TextoOscuro,
                    Location = new Point(5, yActual),
                    MaximumSize = new Size(540, 0),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                panelContenido.Controls.Add(lblContenido);
                yActual += lblContenido.Height + 18;
            }

            // Crear todas las secciones
            CrearSeccion("Resumen de la empresa", _diag.ResumenEmpresa);
            CrearSeccion("Fortalezas", _diag.Fortalezas);
            CrearSeccion("Debilidades", _diag.Debilidades);
            CrearSeccion("Riesgos", _diag.Riesgos);
            CrearSeccion("Recomendaciones", _diag.Recomendaciones);
        }
    }
}