using Microsoft.Data.Sqlite;

namespace MadurezTecnologica.Datos
{
    public class BaseDatos
    {
        private static string _rutaDB = "madurez.db";

        public static string CadenaConexion => $"Data Source={_rutaDB}";

        public static void Inicializar()
        {
            using var conexion = new SqliteConnection(CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Empresas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Sector TEXT NOT NULL,
                    CantidadEmpleados INTEGER,
                    FechaRegistro TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Conversaciones (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EmpresaId INTEGER NOT NULL,
                    FechaInicio TEXT NOT NULL,
                    Estado TEXT NOT NULL,
                    RutaInforme TEXT,
                    FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
                );

                CREATE TABLE IF NOT EXISTS Mensajes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConversacionId INTEGER NOT NULL,
                    Remitente TEXT NOT NULL,
                    Contenido TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Orden INTEGER NOT NULL,
                    FOREIGN KEY (ConversacionId) REFERENCES Conversaciones(Id)
                );

                CREATE TABLE IF NOT EXISTS Diagnosticos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConversacionId INTEGER NOT NULL,
                    NivelMadurez INTEGER NOT NULL,
                    Fortalezas TEXT,
                    Debilidades TEXT,
                    Riesgos TEXT,
                    Recomendaciones TEXT,
                    FechaGeneracion TEXT NOT NULL,
                    EsFinal INTEGER NOT NULL,
                    FOREIGN KEY (ConversacionId) REFERENCES Conversaciones(Id)
                );
            ";
            cmd.ExecuteNonQuery();
        }
    }
}