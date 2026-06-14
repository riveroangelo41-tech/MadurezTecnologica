using MadurezTecnologica.Estilos;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Presentacion
{
    public partial class FormNuevaEmpresa : Form
    {
        // null = registrar nueva | no null = editar existente
        private readonly Empresa? _empresaExistente;
        private readonly bool _esEdicion;

        // Repositorio
        private readonly Datos.RepositorioEmpresa _repoEmpresa;

        // Resultado del guardado (se expone al llamador)
        public Empresa? EmpresaGuardada { get; private set; }

        // Campos del formulario
        private TextBox txtNombre = null!;
        private TextBox txtRif = null!;
        private TextBox txtSector = null!;
        private NumericUpDown numEmpleados = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtDireccion = null!;

        // Labels de error (visibles solo si hay error)
        private Label lblErrorNombre = null!;
        private Label lblErrorRif = null!;
        private Label lblErrorSector = null!;
        private Label lblErrorEmpleados = null!;

        public FormNuevaEmpresa(Empresa? empresaParaEditar = null)
        {
            InitializeComponent();
            _repoEmpresa = new Datos.RepositorioEmpresa();
            _empresaExistente = empresaParaEditar;
            _esEdicion = empresaParaEditar != null;

            ConfigurarForm();
            CrearCabecera();
            CrearBotones();
            CrearCampos();

            if (_esEdicion)
            {
                CargarDatosExistentes();
            }
        }

        private void ConfigurarForm()
        {
            Text = _esEdicion
                ? $"Editar empresa — {_empresaExistente!.Nombre}"
                : "Registrar nueva empresa";
            Size = new Size(560, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        // =====================================================
        // CABECERA
        // =====================================================
        private void CrearCabecera()
        {
            var panelCabecera = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Paleta.MoradoOscuro
            };
            Controls.Add(panelCabecera);

            // Icono circular
            var avatar = new Panel
            {
                Size = new Size(60, 60),
                Location = new Point(25, 20),
                BackColor = Paleta.MoradoClaro
            };
            var pathAv = new System.Drawing.Drawing2D.GraphicsPath();
            pathAv.AddEllipse(0, 0, avatar.Width, avatar.Height);
            avatar.Region = new Region(pathAv);

            var lblIcono = new Label
            {
                Text = _esEdicion ? "✎" : "+",
                Font = new Font("Segoe UI", _esEdicion ? 22 : 28, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(lblIcono);
            panelCabecera.Controls.Add(avatar);

            var lblTitulo = new Label
            {
                Text = _esEdicion ? "Editar empresa" : "Registrar nueva empresa",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Location = new Point(100, 28),
                Size = new Size(420, 28),
                BackColor = Color.Transparent
            };
            panelCabecera.Controls.Add(lblTitulo);

            var lblSubtitulo = new Label
            {
                Text = _esEdicion
                    ? "Modifica la información de la empresa"
                    : "Completa los campos para registrar una nueva empresa",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                Location = new Point(102, 58),
                Size = new Size(420, 20),
                BackColor = Color.Transparent
            };
            panelCabecera.Controls.Add(lblSubtitulo);
        }

        // =====================================================
        // BOTONES (abajo)
        // =====================================================
        private void CrearBotones()
        {
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(25, 15, 25, 15)
            };
            Controls.Add(panelBotones);

            // === Botón GUARDAR (derecha) ===
            var btnGuardar = new Panel
            {
                BackColor = Paleta.MoradoOscuro,
                Size = new Size(140, 40),
                Location = new Point(panelBotones.Width - 140 - 25, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };

            var lblBtnGuardar = new Label
            {
                Text = _esEdicion ? "Guardar cambios" : "Guardar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnGuardar.Controls.Add(lblBtnGuardar);

            var pathGuardar = new System.Drawing.Drawing2D.GraphicsPath();
            pathGuardar.AddArc(0, 0, 40, 40, 90, 180);
            pathGuardar.AddArc(btnGuardar.Width - 40, 0, 40, 40, 270, 180);
            pathGuardar.CloseFigure();
            btnGuardar.Region = new Region(pathGuardar);

            Color guardarNormal = btnGuardar.BackColor;
            Color guardarHover = Paleta.MoradoOscuroHover;
            btnGuardar.MouseEnter += (s, e) => btnGuardar.BackColor = guardarHover;
            btnGuardar.MouseLeave += (s, e) => btnGuardar.BackColor = guardarNormal;
            lblBtnGuardar.MouseEnter += (s, e) => btnGuardar.BackColor = guardarHover;
            lblBtnGuardar.MouseLeave += (s, e) => btnGuardar.BackColor = guardarNormal;

            EventHandler guardarClick = (s, e) => OnGuardarClick();
            btnGuardar.Click += guardarClick;
            lblBtnGuardar.Click += guardarClick;

            panelBotones.Controls.Add(btnGuardar);

            // === Botón CANCELAR (izquierda) ===
            var btnCancelar = new Panel
            {
                BackColor = Color.FromArgb(180, 180, 180),
                Size = new Size(120, 40),
                Location = new Point(25, 15),
                Cursor = Cursors.Hand
            };

            var lblBtnCancelar = new Label
            {
                Text = "Cancelar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Paleta.TextoBlanco,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCancelar.Controls.Add(lblBtnCancelar);

            var pathCancelar = new System.Drawing.Drawing2D.GraphicsPath();
            pathCancelar.AddArc(0, 0, 40, 40, 90, 180);
            pathCancelar.AddArc(btnCancelar.Width - 40, 0, 40, 40, 270, 180);
            pathCancelar.CloseFigure();
            btnCancelar.Region = new Region(pathCancelar);

            Color cancelarNormal = btnCancelar.BackColor;
            Color cancelarHover = Color.FromArgb(140, 140, 140);
            btnCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = cancelarHover;
            btnCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = cancelarNormal;
            lblBtnCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = cancelarHover;
            lblBtnCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = cancelarNormal;

            EventHandler cancelarClick = (s, e) => Close();
            btnCancelar.Click += cancelarClick;
            lblBtnCancelar.Click += cancelarClick;

            panelBotones.Controls.Add(btnCancelar);
        }

        // =====================================================
        // CAMPOS DEL FORMULARIO (scrollable)
        // =====================================================
        private void CrearCampos()
        {
            var panelCampos = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(25, 20, 25, 10)
            };
            Controls.Add(panelCampos);
            panelCampos.BringToFront();

            int yActual = 5;

            // --- Nombre ---
            txtNombre = CrearTextBox();
            txtNombre.MaxLength = 100;
            lblErrorNombre = CrearLabelError();
            yActual = CrearCampo(panelCampos, "Nombre de la empresa", "*", txtNombre, lblErrorNombre, yActual);

            // --- RIF ---
            txtRif = CrearTextBox();
            txtRif.MaxLength = 15;
            RestringirARif(txtRif);
            lblErrorRif = CrearLabelError();
            yActual = CrearCampo(panelCampos, "RIF (ej. J-12345678-9)", "*", txtRif, lblErrorRif, yActual);

            // --- Sector ---
            txtSector = CrearTextBox();
            txtSector.MaxLength = 80;
            lblErrorSector = CrearLabelError();
            yActual = CrearCampo(panelCampos, "Sector / Rubro", "*", txtSector, lblErrorSector, yActual);

            // --- Empleados ---
            numEmpleados = new NumericUpDown
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoOscuro,
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(485, 32),
                Minimum = 1,
                Maximum = 100000,
                Value = 1
            };
            lblErrorEmpleados = CrearLabelError();
            yActual = CrearCampo(panelCampos, "Cantidad de empleados", "*", numEmpleados, lblErrorEmpleados, yActual);

            // --- Teléfono ---
            txtTelefono = CrearTextBox();
            txtTelefono.MaxLength = 20;
            RestringirATelefono(txtTelefono);
            yActual = CrearCampo(panelCampos, "Teléfono", "", txtTelefono, null, yActual);

            // --- Dirección (multiline) ---
            txtDireccion = CrearTextBox();
            txtDireccion.Multiline = true;
            txtDireccion.Height = 65;
            txtDireccion.MaxLength = 250;
            yActual = CrearCampo(panelCampos, "Dirección", "", txtDireccion, null, yActual);
        }

        // =====================================================
        // HELPERS DE CONSTRUCCIÓN DE CAMPOS
        // =====================================================
        private TextBox CrearTextBox()
        {
            return new TextBox
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Paleta.TextoOscuro,
                BackColor = ColorTranslator.FromHtml("#F0EDF5"),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(485, 32)
            };
        }

        private Label CrearLabelError()
        {
            return new Label
            {
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(200, 50, 50),
                Size = new Size(485, 14),
                BackColor = Color.Transparent,
                Visible = false
            };
        }

        private int CrearCampo(Panel padre, string etiqueta, string asterisco, Control input, Label? errorLabel, int y)
        {
            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Paleta.TextoOscuro,
                Location = new Point(5, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            padre.Controls.Add(lblEtiqueta);

            if (!string.IsNullOrEmpty(asterisco))
            {
                var lblAsterisco = new Label
                {
                    Text = " " + asterisco,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(200, 50, 50),
                    Location = new Point(5 + TextRenderer.MeasureText(etiqueta, lblEtiqueta.Font).Width, y),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                padre.Controls.Add(lblAsterisco);
            }

            input.Location = new Point(5, y + 22);
            padre.Controls.Add(input);

            int yFinal = y + 22 + input.Height + 5;

            if (errorLabel != null)
            {
                errorLabel.Location = new Point(5, yFinal);
                padre.Controls.Add(errorLabel);
                yFinal += 18;
            }

            return yFinal + 8;
        }

        // =====================================================
        // HELPERS DE RESTRICCIÓN DE TECLADO (validación en vivo)
        // =====================================================
        private void RestringirATelefono(TextBox txt)
        {
            txt.KeyPress += (s, e) =>
            {
                char c = e.KeyChar;
                if (!char.IsControl(c) && !char.IsDigit(c)
                    && c != '+' && c != '-' && c != ' '
                    && c != '(' && c != ')')
                {
                    e.Handled = true;
                }
            };
        }

        private void RestringirARif(TextBox txt)
        {
            txt.KeyPress += (s, e) =>
            {
                char c = e.KeyChar;

                if (char.IsLetter(c))
                {
                    c = char.ToUpper(c);
                    e.KeyChar = c;
                }

                if (!char.IsControl(c) && !char.IsDigit(c)
                    && c != '-' && !"JVEGP".Contains(c))
                {
                    e.Handled = true;
                }
            };
        }

        // =====================================================
        // CARGAR DATOS EN MODO EDICIÓN
        // =====================================================
        private void CargarDatosExistentes()
        {
            if (_empresaExistente == null) return;

            txtNombre.Text = _empresaExistente.Nombre;
            txtRif.Text = _empresaExistente.Rif;
            txtSector.Text = _empresaExistente.Sector;
            numEmpleados.Value = Math.Max(1, _empresaExistente.CantidadEmpleados);
            txtTelefono.Text = _empresaExistente.Telefono ?? "";
            txtDireccion.Text = _empresaExistente.Direccion ?? "";
        }

        // =====================================================
        // VALIDACIÓN Y GUARDADO
        // =====================================================
        private void OnGuardarClick()
        {
            if (!ValidarFormulario()) return;

            try
            {
                var empresa = _esEdicion ? _empresaExistente! : new Empresa();

                empresa.Nombre = txtNombre.Text.Trim();
                empresa.Rif = txtRif.Text.Trim();
                empresa.Sector = txtSector.Text.Trim();
                empresa.CantidadEmpleados = (int)numEmpleados.Value;
                empresa.Telefono = txtTelefono.Text.Trim();
                empresa.Direccion = txtDireccion.Text.Trim();

                if (_esEdicion)
                {
                    _repoEmpresa.Actualizar(empresa);
                }
                else
                {
                    empresa.FechaRegistro = DateTime.Now;
                    int nuevoId = _repoEmpresa.Guardar(empresa);
                    empresa.Id = nuevoId;
                }

                EmpresaGuardada = empresa;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la empresa:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidarFormulario()
        {
            // Reset de errores
            lblErrorNombre.Visible = false;
            lblErrorRif.Visible = false;
            lblErrorSector.Visible = false;
            lblErrorEmpleados.Visible = false;

            bool valido = true;

            // --- Nombre ---
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MostrarError(lblErrorNombre, "El nombre es obligatorio.");
                valido = false;
            }
            else if (nombre.Length < 3)
            {
                MostrarError(lblErrorNombre, "El nombre debe tener al menos 3 caracteres.");
                valido = false;
            }
            else if (nombre.Length > 100)
            {
                MostrarError(lblErrorNombre, "El nombre no puede superar los 100 caracteres.");
                valido = false;
            }

            // --- RIF ---
            string rif = txtRif.Text.Trim();
            if (string.IsNullOrEmpty(rif))
            {
                MostrarError(lblErrorRif, "El RIF es obligatorio.");
                valido = false;
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(rif, @"^[JVEGP]-\d{8,9}-\d$"))
            {
                MostrarError(lblErrorRif, "Formato inválido. Usa el formato J-12345678-9.");
                valido = false;
            }
            else
            {
                var existente = _repoEmpresa.ObtenerPorRif(rif);
                if (existente != null && (!_esEdicion || existente.Id != _empresaExistente!.Id))
                {
                    MostrarError(lblErrorRif, "Ya existe una empresa registrada con este RIF.");
                    valido = false;
                }
            }

            // --- Sector ---
            string sector = txtSector.Text.Trim();
            if (string.IsNullOrEmpty(sector))
            {
                MostrarError(lblErrorSector, "El sector es obligatorio.");
                valido = false;
            }
            else if (sector.Length < 3)
            {
                MostrarError(lblErrorSector, "El sector debe tener al menos 3 caracteres.");
                valido = false;
            }

            // --- Empleados ---
            if (numEmpleados.Value < 1)
            {
                MostrarError(lblErrorEmpleados, "Debe tener al menos 1 empleado.");
                valido = false;
            }

            return valido;
        }

        private void MostrarError(Label errorLabel, string mensaje)
        {
            errorLabel.Text = mensaje;
            errorLabel.Visible = true;
        }
    }
}