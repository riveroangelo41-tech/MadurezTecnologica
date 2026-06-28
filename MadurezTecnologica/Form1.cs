using MadurezTecnologica.Datos;
using MadurezTecnologica.Modelos;
using System.Text;

namespace MadurezTecnologica
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // === Botón "Abrir chat" creado por código ===
            var btnAbrirChat = new Button
            {
                Name = "btnAbrirChat",
                Text = "Abrir chat",
                Size = new Size(150, 35),
                Location = new Point(750, 180)   // ajusta si se solapa con los otros botones
            };
            btnAbrirChat.Click += BtnAbrirChat_Click;
            Controls.Add(btnAbrirChat);
        }

        private string RecortarTexto(string texto, int maxCaracteres)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            if (texto.Length <= maxCaracteres) return texto;
            return texto.Substring(0, maxCaracteres) + "...";
        }

        private async void btnProbar_Click(object sender, EventArgs e)
        {
            var resultado = new StringBuilder();
            resultado.AppendLine("===== PRUEBA: MOTOR OFFLINE =====");
            resultado.AppendLine();
            txtResultado.Text = resultado.ToString();
            txtResultado.Refresh();

            try
            {
                // Leer el texto del PDF
                var gestorInforme = new MadurezTecnologica.Logica.GestorInforme();
                string rutaPdf = @"C:\Users\Home\Desktop\informe_codeminka_corto.pdf";

                if (!System.IO.File.Exists(rutaPdf))
                {
                    resultado.AppendLine($"✗ No se encontró el PDF en: {rutaPdf}");
                    txtResultado.Text = resultado.ToString();
                    return;
                }

                string textoPdf = gestorInforme.ExtraerTexto(rutaPdf);
                resultado.AppendLine($"PDF leído: {textoPdf.Length} caracteres");
                resultado.AppendLine();

                // Crear empresa de prueba
                var empresa = new MadurezTecnologica.Modelos.Empresa
                {
                    Nombre = "CodeMinka, C.A.",
                    Rif = "J-40128765-3"
                };

                // Ejecutar motor offline
                var motor = new MadurezTecnologica.Logica.MotorOffline();
                var diagnostico = motor.AnalizarTexto(textoPdf, empresa);

                resultado.AppendLine($"Nivel detectado: {diagnostico.NivelMadurez}");
                resultado.AppendLine($"(esperado para CodeMinka: 1)");
                resultado.AppendLine();
                resultado.AppendLine("--- RESUMEN ---");
                resultado.AppendLine(diagnostico.ResumenEmpresa);
                resultado.AppendLine();
                resultado.AppendLine("--- FORTALEZAS ---");
                resultado.AppendLine(diagnostico.Fortalezas);
                resultado.AppendLine();
                resultado.AppendLine("--- DEBILIDADES ---");
                resultado.AppendLine(diagnostico.Debilidades);
                resultado.AppendLine();
                resultado.AppendLine("--- RECOMENDACIONES ---");
                resultado.AppendLine(diagnostico.Recomendaciones);
            }
            catch (Exception ex)
            {
                resultado.AppendLine($"✗ Error: {ex.Message}");
            }

            txtResultado.Text = resultado.ToString();
        }

        private void btnDescargarPlantilla_Click(object sender, EventArgs e)
        {
            // Configurar el diálogo de guardado
            using var dialogo = new SaveFileDialog();
            dialogo.Title = "Guardar plantilla de evaluación";
            dialogo.Filter = "Documento Word (*.docx)|*.docx";
            dialogo.DefaultExt = "docx";
            dialogo.FileName = $"plantilla_madurez_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            dialogo.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (dialogo.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                btnDescargarPlantilla.Enabled = false;
                btnDescargarPlantilla.Text = "Generando...";
                btnDescargarPlantilla.Refresh();

                var generador = new MadurezTecnologica.Logica.GeneradorPlantilla();
                string archivoGenerado = generador.GenerarPlantilla(dialogo.FileName);

                bool abrir = Estilos.MensajeApp.Confirmar(
                    $"Plantilla generada exitosamente en:\n{archivoGenerado}\n\n¿Desea abrirla ahora?",
                    "Descarga completa",
                    this);

                if (abrir)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = archivoGenerado,
                        UseShellExecute = true
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                Estilos.MensajeApp.Advertencia(
                    "No tiene permisos para guardar en esa carpeta. Intente con otra ubicación (por ejemplo, su Escritorio).",
                    "Sin permisos",
                    this);
            }
            catch (System.IO.IOException ex) when (ex.Message.Contains("being used"))
            {
                Estilos.MensajeApp.Advertencia(
                    "El archivo ya está abierto en otro programa. Ciérrelo e intente nuevamente.",
                    "Archivo bloqueado",
                    this);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                Estilos.MensajeApp.Advertencia(
                    "La carpeta seleccionada no existe. Por favor seleccione una carpeta válida.",
                    "Carpeta no encontrada",
                    this);
            }
            catch (Exception ex)
            {
                Estilos.MensajeApp.Error(
                    $"Ocurrió un error inesperado al generar la plantilla:\n\n{ex.Message}",
                    "Error",
                    this);
            }
            finally
            {
                btnDescargarPlantilla.Enabled = true;
                btnDescargarPlantilla.Text = "Descargar plantilla";
            }
        }

        private void BtnAbrirChat_Click(object? sender, EventArgs e)
        {
            // Pedir el ID de conversación al usuario (versión simple)
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Ingresa el ID de la conversación que deseas abrir:",
                "Abrir chat",
                "4");

            if (string.IsNullOrWhiteSpace(input)) return;

            if (!int.TryParse(input, out int conversacionId))
            {
                Estilos.MensajeApp.Advertencia("Debes ingresar un número válido.",
                    "ID inválido", this);
                return;
            }

            var formChat = new MadurezTecnologica.Presentacion.FormChat(conversacionId);
            formChat.Show();
        }
    }
}