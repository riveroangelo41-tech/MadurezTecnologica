using MadurezTecnologica.Estilos;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Presentacion
{
    public partial class FormDetalleEmpresa : Form
    {
        private readonly Empresa _empresa;

        public FormDetalleEmpresa(Empresa empresa)
        {
            InitializeComponent();
            _empresa = empresa;
            ConfigurarForm();
            CrearContenido();
        }

        private void ConfigurarForm()
        {
            Text = $"Detalles — {_empresa.Nombre}";
            Size = new Size(560, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void CrearContenido()
        {
        
            // CABECERA (morado, con nombre y RIF)
           
            var panelCabecera = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Paleta.MoradoOscuro
            };
            Controls.Add(panelCabecera);

            // Avatar circular con inicial de la empresa
            var avatar = new Panel
            {
                Size = new Size(60, 60),
                Location = new Point(25, 25),
                BackColor = Paleta.MoradoClaro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAv);

            var lblInicial = new Label
            {
                Text = _empresa.Nombre.Length > 0 ? _empresa.Nombre[0].ToString().ToUpper() : "?",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblInicial);
            panelCabecera.Controls.Add(avatar);

            var lblNombre = new Label
            {
                Text = _empresa.Nombre,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(100, 28),
                Size = new Size(420, 30),
                BackColor = Color.Transparent
            };
            panelCabecera.Controls.Add(lblNombre);

            var lblRif = new Label
            {
                Text = $"RIF: {_empresa.Rif}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                Location = new Point(102, 62),
                Size = new Size(300, 20),
                BackColor = Color.Transparent
            };
            panelCabecera.Controls.Add(lblRif);


            // BOTÓN CERRAR (abajo)

            // ===============================================
            // BOTONES DE ACCIÓN (abajo)
            // ===============================================
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };
            Controls.Add(panelBotones);

            // === Botón EDITAR (Panel + Label centrado) ===
            var btnEditar = new Panel
            {
                BackColor = Paleta.VerdeGrisaceoOscuro,
                Size = new Size(130, 38),
                Location = new Point(20, 14),
                Cursor = Cursors.Hand
            };

            var lblBtnEditar = new Label
            {
                Text = "✎  Editar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnEditar.Controls.Add(lblBtnEditar);

            var pathEditar = new System.Drawing.Drawing2D.GraphicsPath();
            pathEditar.AddArc(0, 0, 38, 38, 90, 180);
            pathEditar.AddArc(btnEditar.Width - 38, 0, 38, 38, 270, 180);
            pathEditar.CloseFigure();
            btnEditar.Region = new Region(pathEditar);

            // Hover
            Color editarNormal = btnEditar.BackColor;
            Color editarHover = Paleta.VerdeGrisaceo;
            btnEditar.MouseEnter += (s, e) => btnEditar.BackColor = editarHover;
            btnEditar.MouseLeave += (s, e) => btnEditar.BackColor = editarNormal;
            lblBtnEditar.MouseEnter += (s, e) => btnEditar.BackColor = editarHover;
            lblBtnEditar.MouseLeave += (s, e) => btnEditar.BackColor = editarNormal;

            // Click
            EventHandler editarClick = (s, e) => OnEditarClick();
            btnEditar.Click += editarClick;
            lblBtnEditar.Click += editarClick;

            panelBotones.Controls.Add(btnEditar);

            //  Botón CERRAR (Panel + Label centrado) 
            var btnCerrar = new Panel
            {
                BackColor = Paleta.MoradoOscuro,
                Size = new Size(130, 38),
                Location = new Point(panelBotones.Width - 130 - 20, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };

            var lblBtnCerrar = new Label
            {
                Text = "Cerrar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCerrar.Controls.Add(lblBtnCerrar);

            var pathCerrar = new System.Drawing.Drawing2D.GraphicsPath();
            pathCerrar.AddArc(0, 0, 38, 38, 90, 180);
            pathCerrar.AddArc(btnCerrar.Width - 38, 0, 38, 38, 270, 180);
            pathCerrar.CloseFigure();
            btnCerrar.Region = new Region(pathCerrar);

            // Hover
            Color cerrarNormal = btnCerrar.BackColor;
            Color cerrarHover = Paleta.MoradoOscuroHover;
            btnCerrar.MouseEnter += (s, e) => btnCerrar.BackColor = cerrarHover;
            btnCerrar.MouseLeave += (s, e) => btnCerrar.BackColor = cerrarNormal;
            lblBtnCerrar.MouseEnter += (s, e) => btnCerrar.BackColor = cerrarHover;
            lblBtnCerrar.MouseLeave += (s, e) => btnCerrar.BackColor = cerrarNormal;

            // Click
            EventHandler cerrarClick = (s, e) => Close();
            btnCerrar.Click += cerrarClick;
            lblBtnCerrar.Click += cerrarClick;

            panelBotones.Controls.Add(btnCerrar);

            // CONTENIDO CENTRAL (scrollable)

            var panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(25, 20, 25, 10)
            };
            Controls.Add(panelContenido);
            panelContenido.BringToFront();

            int yActual = 15;

            // --- Sección 1: Información general ---
            yActual = CrearTituloSeccion(panelContenido, "Información general", yActual);
            yActual = CrearFilaDato(panelContenido, "Sector", _empresa.Sector, yActual);
            yActual = CrearFilaDato(panelContenido, "Empleados", _empresa.CantidadEmpleados.ToString(), yActual);
            yActual = CrearFilaDato(panelContenido, "Teléfono",
                string.IsNullOrEmpty(_empresa.Telefono) ? "—" : _empresa.Telefono, yActual);
            yActual = CrearFilaDato(panelContenido, "Dirección",
                string.IsNullOrEmpty(_empresa.Direccion) ? "—" : _empresa.Direccion, yActual);
            yActual = CrearFilaDato(panelContenido, "Fecha de registro",
                _empresa.FechaRegistro.ToString("dd 'de' MMMM 'de' yyyy"), yActual);

            yActual += 15;

            // Sección 2: Estado de evaluación (datos calculados) ---
            yActual = CrearTituloSeccion(panelContenido, "Estado de evaluación", yActual);

            // Obtener datos de la conversación y diagnósticos de esta empresa
            var repoConv = new Datos.RepositorioConversacion();
            var repoDiag = new Datos.RepositorioDiagnostico();

            var conversacion = repoConv.ObtenerTodas()
                                       .FirstOrDefault(c => c.EmpresaId == _empresa.Id);

            if (conversacion == null)
            {
                yActual = CrearFilaDato(panelContenido, "Estado", "Sin evaluación iniciada", yActual);
            }
            else
            {
                var diagnosticos = repoDiag.ObtenerHistorialPorConversacion(conversacion.Id);

                yActual = CrearFilaDato(panelContenido, "Conversación iniciada",
                    conversacion.FechaInicio.ToString("dd/MM/yyyy"), yActual);
                yActual = CrearFilaDato(panelContenido, "Evaluaciones generadas",
                    diagnosticos.Count.ToString(), yActual);

                if (diagnosticos.Count > 0)
                {
                    var ultimo = diagnosticos.OrderByDescending(d => d.FechaGeneracion).First();
                    yActual = CrearFilaDato(panelContenido, "Último nivel CMMI",
                        ultimo.NivelMadurez > 0 ? $"Nivel {ultimo.NivelMadurez}" : "No determinado", yActual);
                    yActual = CrearFilaDato(panelContenido, "Última evaluación",
                        ultimo.FechaGeneracion.ToString("dd/MM/yyyy HH:mm"), yActual);
                }
            }
        }

        
        // HELPERS DE CONSTRUCCIÓN
     

        private int CrearTituloSeccion(Panel padre, string titulo, int y)
        {
            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Paleta.MoradoOscuro,
                Location = new Point(5, y),
                Size = new Size(480, 24),
                BackColor = Color.Transparent
            };
            padre.Controls.Add(lblTitulo);

            // Línea separadora debajo del título
            var linea = new Panel
            {
                BackColor = Color.FromArgb(40, 83, 55, 123),
                Location = new Point(5, y + 28),
                Size = new Size(480, 1)
            };
            padre.Controls.Add(linea);

            return y + 40;
        }

        private int CrearFilaDato(Panel padre, string etiqueta, string valor, int y)
        {
            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(130, 125, 122),
                Location = new Point(5, y),
                Size = new Size(180, 20),
                BackColor = Color.Transparent
            };
            padre.Controls.Add(lblEtiqueta);

            var lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(190, y),
                Size = new Size(295, 20),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            padre.Controls.Add(lblValor);

            return y + 28;
        }

        private void OnEditarClick()
        {
            using var modal = new FormNuevaEmpresa(_empresa);
            var resultado = modal.ShowDialog(this);

            if (resultado == DialogResult.OK && modal.EmpresaGuardada != null)
            {
                MessageBox.Show(
                    "Cambios guardados correctamente.\n\n" +
                    "Cierra y vuelve a abrir este modal para ver los cambios actualizados.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Cerrar el modal de detalles también, así el usuario ve los datos frescos al reabrirlo
                Close();
            }
        }
      
        // HELPERS DE RESTRICCIÓN DE TECLADO 
        

        // Solo permite caracteres válidos para teléfono
        private void RestringirATelefono(TextBox txt)
        {
            txt.KeyPress += (s, e) =>
            {
                char c = e.KeyChar;
                // Permitir: dígitos, espacio, +, -, (, ), y teclas de control (backspace, etc.)
                if (!char.IsControl(c) && !char.IsDigit(c)
                    && c != '+' && c != '-' && c != ' '
                    && c != '(' && c != ')')
                {
                    e.Handled = true;  // bloquear
                }
            };
        }

        // Solo permite letras (J/V/E/G/P), dígitos y guion. Convierte letras a mayúsculas.
        private void RestringirARif(TextBox txt)
        {
            txt.KeyPress += (s, e) =>
            {
                char c = e.KeyChar;

                // Convertir a mayúscula si es letra
                if (char.IsLetter(c))
                {
                    c = char.ToUpper(c);
                    e.KeyChar = c;  // reemplazar la tecla por su versión mayúscula
                }

                // Permitir solo: J/V/E/G/P, dígitos, guion, y teclas de control
                if (!char.IsControl(c) && !char.IsDigit(c)
                    && c != '-' && !"JVEGP".Contains(c))
                {
                    e.Handled = true;
                }
            };
        }

    }
}