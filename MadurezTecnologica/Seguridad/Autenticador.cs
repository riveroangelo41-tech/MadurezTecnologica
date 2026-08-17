using System.Security.Cryptography;
using System.Text;
using MadurezTecnologica.Inteligencia;

namespace MadurezTecnologica.Seguridad
{
    // Módulo de autenticación (RF-33).
    // Valida usuario y contraseña contra las credenciales de appconfi.json.
    // La contraseña se compara mediante su hash SHA-256 — nunca se maneja ni
    // almacena en texto plano.
    public static class Autenticador
    {
        // Calcula el hash SHA-256 de un texto y lo devuelve en hexadecimal (minúsculas).
        public static string HashearSHA256(string texto)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto ?? ""));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // Valida las credenciales ingresadas contra las del archivo de configuración.
        // - El usuario se compara sin distinguir mayúsculas/minúsculas.
        // - La contraseña se hashea y se compara contra el hash almacenado.
        public static bool Validar(string usuario, string password)
        {
            string usuarioEsperado = Configuracion.UsuarioAutenticacion;
            string hashEsperado = Configuracion.PasswordHashAutenticacion;

            // Si el config no tiene credenciales, se niega el acceso por seguridad.
            if (string.IsNullOrWhiteSpace(usuarioEsperado) || string.IsNullOrWhiteSpace(hashEsperado))
                return false;

            bool usuarioOk = string.Equals(
                (usuario ?? "").Trim(),
                usuarioEsperado.Trim(),
                StringComparison.OrdinalIgnoreCase);

            string hashIngresado = HashearSHA256(password ?? "");
            bool passwordOk = string.Equals(
                hashIngresado,
                hashEsperado.Trim(),
                StringComparison.OrdinalIgnoreCase);

            return usuarioOk && passwordOk;
        }
    }
}
