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
        }

        private async void btnProbar_Click(object sender, EventArgs e)
        {
            var resultado = new StringBuilder();
            resultado.AppendLine("===== PRUEBA: CONVERSACIÓN CON MEMORIA =====");
            txtResultado.Text = resultado.ToString();
            txtResultado.Refresh();

            try
            {
                var gestorConv = new MadurezTecnologica.Logica.GestorConversacion();

                // Usa el ID de una conversación que exista en tu BD (ajusta si es necesario)
                int conversacionId = 4;

                // Mostrar el historial antes de la pregunta
                resultado.AppendLine();
                resultado.AppendLine("--- Historial ANTES de la pregunta ---");
                resultado.AppendLine(gestorConv.ResumirHistorial(conversacionId));
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                // La pregunta del usuario
                string pregunta = "¿Cuál sería el primer paso más importante y económico que debería tomar mi empresa para mejorar su nivel de madurez?";

                resultado.AppendLine("--- Pregunta del usuario ---");
                resultado.AppendLine(pregunta);
                resultado.AppendLine();
                resultado.AppendLine("⏳ Enviando a Claude con todo el contexto...");
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                // Enviar y recibir respuesta
                string respuesta = await gestorConv.EnviarMensajeUsuario(conversacionId, pregunta);

                resultado.AppendLine();
                resultado.AppendLine("--- Respuesta de Claude ---");
                resultado.AppendLine(respuesta);
                resultado.AppendLine();

                // Mostrar el historial después
                resultado.AppendLine("--- Historial DESPUÉS de la conversación ---");
                resultado.AppendLine(gestorConv.ResumirHistorial(conversacionId));
            }
            catch (Exception ex)
            {
                resultado.AppendLine($"✗ Error: {ex.Message}");
            }

            txtResultado.Text = resultado.ToString();
        }

        // Método auxiliar para truncar strings en la tabla de resumen
        private string Truncar(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Length > max ? texto.Substring(0, max) : texto;
        }
        private string RecortarTexto(string texto, int maxCaracteres)
        {
            if (string.IsNullOrEmpty(texto)) return "(sin contenido)";
            if (texto.Length <= maxCaracteres) return texto;
            return texto.Substring(0, maxCaracteres) + "...";
        }

        private void btnDescargarPlantilla_Click(object sender, EventArgs e)
        {
           
            // Configurar el diálogo de guardado
            using var dialogo = new SaveFileDialog();
            dialogo.Title = "Guardar plantilla de evaluación"; // Título del diálogo
            dialogo.Filter = "Documento Word (*.docx)|*.docx"; // Solo permitir archivos .docx
            dialogo.DefaultExt = "docx"; // Extensión por defecto
            dialogo.FileName = $"plantilla_madurez_{DateTime.Now:yyyyMMdd_HHmmss}.docx"; // Nombre sugerido
            dialogo.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);// Abrir el diálogo en el escritorio

            // Mostrar el diálogo
            if (dialogo.ShowDialog() != DialogResult.OK)
            {
                // El usuario canceló
                return;
            }

            // Intentar generar la plantilla
            try
            {
                // Deshabilitar el botón mientras se genera
                btnDescargarPlantilla.Enabled = false;
                btnDescargarPlantilla.Text = "Generando...";
                btnDescargarPlantilla.Refresh();

                var generador = new MadurezTecnologica.Logica.GeneradorPlantilla();
                string archivoGenerado = generador.GenerarPlantilla(dialogo.FileName);

                // Confirmación al usuario
                var resultado = MessageBox.Show(
                    $"Plantilla generada exitosamente en:\n{archivoGenerado}\n\n¿Desea abrirla ahora?",
                    "Descarga completa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                // Si dice que sí, abrir el archivo
                if (resultado == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = archivoGenerado,
                        UseShellExecute = true
                    });
                }
            }
            catch (UnauthorizedAccessException) // Sin permisos para escribir en la ubicación seleccionada
            {
                MessageBox.Show(
                    "No tiene permisos para guardar en esa carpeta. Intente con otra ubicación (por ejemplo, su Escritorio).",
                    "Sin permisos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (System.IO.IOException ex) when (ex.Message.Contains("being used")) // Archivo bloqueado por otro proceso
            {
                MessageBox.Show(
                    "El archivo ya está abierto en otro programa. Ciérrelo e intente nuevamente.",
                    "Archivo bloqueado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (System.IO.DirectoryNotFoundException) // Carpeta no encontrada (aunque SaveFileDialog debería evitar esto)
            {
                MessageBox.Show(
                    "La carpeta seleccionada no existe. Por favor seleccione una carpeta válida.",
                    "Carpeta no encontrada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (Exception ex) // Cualquier otro error inesperado
            {
                MessageBox.Show(
                    $"Ocurrió un error inesperado al generar la plantilla:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                // Restaurar el botón
                btnDescargarPlantilla.Enabled = true;
                btnDescargarPlantilla.Text = "Descargar plantilla";
            }
        }
    }

 }




