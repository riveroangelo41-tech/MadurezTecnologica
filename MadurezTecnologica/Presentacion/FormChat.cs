using System.Text;
using MadurezTecnologica.Logica;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Presentacion
{
    public class FormChat : Form
    {
        // Controles
        private Label lblTitulo = null!;
        private RichTextBox rtbMensajes = null!;
        private TextBox txtEntrada = null!;
        private Button btnEnviar = null!;
        private Button btnRefinar = null!;
        private Button btnCerrar = null!;
        private Label lblEstado = null!;

        // Lógica
        private readonly GestorConversacion _gestorConv;
        private readonly int _conversacionId;

        public FormChat(int conversacionId)
        {
            _gestorConv = new GestorConversacion();
            _conversacionId = conversacionId;

            ConfigurarFormulario();
            CrearControles();
            CargarHistorialInicial();
        }

      
        // CONFIGURACIÓN VISUAL
        

        private void ConfigurarFormulario() // Configuración general del formulario
        {
            Text = $"Chat con Claude — Conversación #{_conversacionId}";
            Width = 850;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 500);
            BackColor = Color.White;
        }

        private void CrearControles() // Creación y configuración de los controles 
        {
            // === Título ===
            lblTitulo = new Label
            {
                Text = $"Conversación de análisis #{_conversacionId}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 78, 121),
                Location = new Point(15, 10),
                Size = new Size(800, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(lblTitulo);

            // Mensaje de bienvenida si no hay historial
            rtbMensajes = new RichTextBox
            {
                Location = new Point(15, 45),
                Size = new Size(800, 400),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(rtbMensajes);

            // estado de procesamiento (debajo del chat, antes del input)
            lblEstado = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(15, 455),
                Size = new Size(800, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(lblEstado);

            // espacio para escribir el mensaje
            txtEntrada = new TextBox
            {
                Location = new Point(15, 480),
                Size = new Size(800, 60),
                Font = new Font("Segoe UI", 10),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            txtEntrada.KeyDown += TxtEntrada_KeyDown;
            Controls.Add(txtEntrada);

            // botón para enviar el mensaje
            btnEnviar = new Button
            {
                Text = "Enviar mensaje",
                Location = new Point(15, 555),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(31, 78, 121),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.Click += BtnEnviar_Click;
            Controls.Add(btnEnviar);

            // botón para refinar el diagnóstico final
            btnRefinar = new Button
            {
                Text = "Refinar diagnóstico",
                Location = new Point(165, 555),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnRefinar.FlatAppearance.BorderSize = 0;
            btnRefinar.Click += BtnRefinar_Click;
            Controls.Add(btnRefinar);

            // botón para cerrar el chat
            btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(740, 555),
                Size = new Size(75, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCerrar.Click += (s, e) => Close();
            Controls.Add(btnCerrar);
        }

        
        // CARGA INICIAL DEL HISTORIAL
      

        private void CargarHistorialInicial() // Carga el historial de mensajes al abrir el chat
        {
            try
            {
                var historial = _gestorConv.CargarHistorial(_conversacionId);

                if (historial.Count == 0)
                {
                    AgregarMensajeAlChat("Sistema", "Esta conversación no tiene mensajes previos.");
                    return;
                }

                foreach (var mensaje in historial)
                {
                    AgregarMensajeAlChat(mensaje.Remitente, mensaje.Contenido);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el historial: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // RENDERIZADO DE MENSAJES
        

        private void AgregarMensajeAlChat(string remitente, string contenido) // Agrega un mensaje al chat con formato según el remitente
        {
            // Color y etiqueta según remitente
            Color colorEtiqueta;
            string etiqueta;

            switch (remitente)
            {
                case "IA":
                    colorEtiqueta = Color.FromArgb(31, 78, 121);
                    etiqueta = "Claude";
                    break;
                case "Usuario":
                    colorEtiqueta = Color.FromArgb(46, 125, 50);
                    etiqueta = "Tú";
                    break;
                default:
                    colorEtiqueta = Color.Gray;
                    etiqueta = remitente;
                    break;
            }

            // Etiqueta del remitente (negrita, color)
            rtbMensajes.SelectionStart = rtbMensajes.TextLength;
            rtbMensajes.SelectionLength = 0;
            rtbMensajes.SelectionColor = colorEtiqueta;
            rtbMensajes.SelectionFont = new Font(rtbMensajes.Font, FontStyle.Bold);
            rtbMensajes.AppendText($"{etiqueta}: ");

            // Contenido del mensaje (color normal)
            rtbMensajes.SelectionColor = Color.Black;
            rtbMensajes.SelectionFont = new Font(rtbMensajes.Font, FontStyle.Regular);
            rtbMensajes.AppendText($"{contenido}\n\n");

            // Scroll automático al final
            rtbMensajes.SelectionStart = rtbMensajes.TextLength;
            rtbMensajes.ScrollToCaret();
        }


        // INTERACCIÓN DEL USUARIO


       
        private void TxtEntrada_KeyDown(object? sender, KeyEventArgs e) // Detecta Ctrl+Enter para enviar el mensaje sin hacer clic en el botón
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnEnviar_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnEnviar_Click(object? sender, EventArgs e) // Envía el mensaje del usuario a Claude, muestra el mensaje en el chat y luego muestra la respuesta de Claude
        {
            string mensaje = txtEntrada.Text.Trim();

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                MessageBox.Show("Escribe un mensaje antes de enviar.",
                    "Mensaje vacío", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Bloquear la UI mientras se envía
            BloquearControles(true);
            lblEstado.Text = "Claude está procesando tu mensaje...";

            // Mostrar inmediatamente el mensaje del usuario
            AgregarMensajeAlChat("Usuario", mensaje);
            txtEntrada.Clear();

            try
            {
                // Enviar el mensaje a Claude y esperar la respuesta
                string respuesta = await _gestorConv.EnviarMensajeUsuario(_conversacionId, mensaje);
                AgregarMensajeAlChat("IA", respuesta); 
            }
            catch (Exception ex)
            {
                // Mostrar error si falla el envío o la respuesta
                MessageBox.Show($"Error al enviar el mensaje:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Siempre desbloquear la UI al final
                BloquearControles(false);
                lblEstado.Text = "";
                txtEntrada.Focus();
            }
        }

        private async void BtnRefinar_Click(object? sender, EventArgs e) // Permite al usuario refinar el diagnóstico final consultando a Claude con todo el historial actual. Muestra un mensaje de confirmación antes de proceder.
        {
            // Confirmación antes de refinar
            var confirmacion = MessageBox.Show(
                "¿Deseas regenerar el diagnóstico final considerando toda la conversación?\n\n" +
                "Esto consultará a Claude usando todo el historial actual.",
                "Confirmar refinamiento",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes) return;

            BloquearControles(true);
            lblEstado.Text = "Refinando diagnóstico final con todo el contexto...";

            try
            {
                // Lógica para refinar el diagnóstico final usando todo el historial actual
                var diagFinal = await _gestorConv.RegenerarDiagnosticoFinal(_conversacionId);

                AgregarMensajeAlChat("Sistema",
                    $"Diagnóstico final regenerado correctamente.\n" +
                    $"Nivel CMMI: {diagFinal.NivelMadurez}\n" +
                    $"El diagnóstico se guardó en la base de datos marcado como FINAL.");
            }
            catch (InvalidOperationException ex)
            {
                // Este error se lanza si no hay suficiente información para generar un diagnóstico final. Se muestra como información al usuario.
                MessageBox.Show(ex.Message, "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Mostrar error si falla el refinamiento
                MessageBox.Show($"Error al refinar:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                BloquearControles(false);
                lblEstado.Text = "";
            }
        }

        // UTILIDADES 


        private void BloquearControles(bool bloquear)
        {
            btnEnviar.Enabled = !bloquear;
            btnRefinar.Enabled = !bloquear;
            txtEntrada.Enabled = !bloquear;
        }
    }
}