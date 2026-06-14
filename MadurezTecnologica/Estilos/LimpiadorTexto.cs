using System.Text.RegularExpressions;

using System.Text.RegularExpressions;

namespace MadurezTecnologica.Estilos
{
    public static class LimpiadorTexto
    {
        /// <summary>
        /// Limpia el markdown común que produce Claude para mostrarlo como texto plano.
        /// Mantiene el contenido legible eliminando símbolos de formato.
        /// </summary>
        public static string LimpiarMarkdown(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;

            // Encabezados ## y ### → quitar los #
            texto = Regex.Replace(texto, @"^#{1,6}\s+", "", RegexOptions.Multiline);

            // Negritas **texto** o __texto__ → quitar marcadores
            texto = Regex.Replace(texto, @"\*\*(.+?)\*\*", "$1");
            texto = Regex.Replace(texto, @"__(.+?)__", "$1");

            // Cursivas *texto* o _texto_ → quitar marcadores
            texto = Regex.Replace(texto, @"(?<!\*)\*(?!\*)([^\*\n]+?)\*(?!\*)", "$1");
            texto = Regex.Replace(texto, @"(?<!_)_(?!_)([^_\n]+?)_(?!_)", "$1");

            // Separadores horizontales --- y ___ → quitar
            texto = Regex.Replace(texto, @"^[-_]{3,}\s*$", "", RegexOptions.Multiline);

            // Código en línea `texto` → quitar backticks
            texto = Regex.Replace(texto, @"`([^`]+)`", "$1");

            // Código en bloque ```...``` → quitar marcadores pero mantener contenido
            texto = Regex.Replace(texto, @"```[a-z]*\n?", "", RegexOptions.IgnoreCase);
            texto = Regex.Replace(texto, @"```", "");

            // Enlaces [texto](url) → dejar solo el texto
            texto = Regex.Replace(texto, @"\[([^\]]+)\]\([^\)]+\)", "$1");

            // Tablas markdown | col | col | → simplificar
            texto = Regex.Replace(texto, @"^\|[\s\-:]+\|[\s\-:|]*\|?\s*$", "", RegexOptions.Multiline);
            texto = Regex.Replace(texto, @"\s*\|\s*", "   ");

            // Limpiar múltiples saltos de línea consecutivos
            texto = Regex.Replace(texto, @"\n{3,}", "\n\n");

            return texto.Trim();
        }
    }
}