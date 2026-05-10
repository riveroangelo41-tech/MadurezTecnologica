using System.Text;
using UglyToad.PdfPig;

namespace MadurezTecnologica.Logica
{
    public class GestorInforme // clase para gestionar la extracción de texto de archivos PDF
    {
        public string ExtraerTexto(string rutaPdf)
        {
            if (!File.Exists(rutaPdf)) // Verificar si el archivo existe
            {
                throw new FileNotFoundException($"El archivo PDF no se encontro; {rutaPdf}");
            }

            if (!rutaPdf.ToLower().EndsWith(".pdf")) // Verificar si el archivo tiene la extensión .pdf
            { 
            
                throw new ArgumentException("El archivo proporcionado no es un PDF válido.");


            }

            var textoCompleto = new StringBuilder();

            try
            {
                using var documento = PdfDocument.Open(rutaPdf); // Abrir el documento PDF

                foreach (var pagina in documento.GetPages())
                {
                    textoCompleto.AppendLine(pagina.Text); // Agregar el texto de cada página al StringBuilder
                    textoCompleto.AppendLine(); // Agregar una línea en blanco entre páginas

                }    

            }
            catch (Exception ex)// Capturar cualquier excepción que ocurra durante la lectura del PDF
            {

                throw new Exception($"Error al leer el archivo PDF: {ex.Message}"); // Manejar cualquier excepción que ocurra durante la lectura del PDF


            }

            return textoCompleto.ToString(); // Devolver el texto completo extraído del PDF

        }


        public int ContarPaginas(string rutaPdf)// Método para contar el número de páginas de un archivo PDF
        {
            if (!File.Exists(rutaPdf)) // Verificar si el archivo existe
            {
             
               throw new FileNotFoundException($"El archivo PDF no se encontro; {rutaPdf}");

            }

            using var documento = PdfDocument.Open(rutaPdf); // Abrir el documento PDF
            return documento.NumberOfPages; // Devolver el número de páginas del PDF



        }

        public bool EsPdfValido(string rutaPdf) // Método para validar si un archivo es un PDF válido
        {
            if (!File.Exists(rutaPdf))
            {
                return false; // El archivo no existe, por lo tanto no es un PDF válido

            }

            if (!rutaPdf.ToLower().EndsWith(".pdf"))
            {
                return false; // El archivo no tiene la extensión .pdf, por lo tanto no es un PDF válido
            }

             try
            {
                using var documento = PdfDocument.Open(rutaPdf); // Intentar abrir el documento PDF
                return documento.NumberOfPages > 0; // Si el PDF se abre correctamente y tiene páginas, es un PDF válido


            }

             catch
             {

                return false; // Si ocurre una excepción al intentar abrir el PDF, no es un PDF válido

             }



        }

        public string ObtenerResumen(string rutaPdf)// Método para obtener un resumen del contenido de un archivo PDF
        {
            if(!EsPdfValido(rutaPdf))
            {
                return "PDF no valido o ilegible";

            }

            int paginas = ContarPaginas(rutaPdf); // Contar el número de páginas del PDF
            string texto = ExtraerTexto(rutaPdf); // Extraer el texto completo del PDF
            int palabras = texto.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length; // Contar el número de palabras en el texto
            
            return $"El PDF tiene {paginas} páginas y contiene aproximadamente {palabras} palabras."; // Devolver un resumen con el número de páginas y palabras del PDF



        }


    }





}
