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
            resultado.AppendLine("===== PRUEBA: CARGA DE HISTORIAL DE CONVERSACIÓN =====");
            txtResultado.Text = resultado.ToString();
            txtResultado.Refresh();

            try
            {
                var gestorConv = new MadurezTecnologica.Logica.GestorConversacion();

                // Probar con las 3 conversaciones que tienes de las pruebas del sábado
                // Si los IDs son distintos en tu BD, ajústalos según lo que veas en DB Browser
                int[] conversacionesAProbra = { 1, 2, 3 };

                foreach (int convId in conversacionesAProbra)
                {
                    resultado.AppendLine();
                    resultado.AppendLine($"--- Conversación #{convId} ---");

                    // Cargar el historial
                    var mensajes = gestorConv.CargarHistorial(convId);

                    if (mensajes.Count == 0)
                    {
                        resultado.AppendLine($"Sin mensajes (esta conversación no existe o está vacía)");
                        continue;
                    }

                    // Mostrar el resumen
                    resultado.AppendLine(gestorConv.ResumirHistorial(convId));

                    // Convertir al formato Claude
                    var paraIA = gestorConv.ConstruirMensajesParaIA(mensajes);
                    resultado.AppendLine($"Mensajes convertidos al formato Claude: {paraIA.Count}");
                    resultado.AppendLine($"  - Mensajes 'user': {paraIA.Count(m => m.Role == "user")}");
                    resultado.AppendLine($"  - Mensajes 'assistant': {paraIA.Count(m => m.Role == "assistant")}");

                    // Calcular siguiente orden
                    int siguienteOrden = gestorConv.CalcularSiguienteOrden(convId);
                    resultado.AppendLine($"Siguiente Orden: {siguienteOrden}");
                }
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




