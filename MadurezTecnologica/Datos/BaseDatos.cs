using Microsoft.Data.Sqlite;

namespace MadurezTecnologica.Datos
{
    public class BaseDatos
    {
        // Ruta ABSOLUTA al lado del ejecutable (AppContext.BaseDirectory).
        // Antes era relativa ("madurez.db"), lo que hacía que la BD dependiera del
        // directorio de trabajo actual y generaba archivos huérfanos (ej. una BD vacía
        // en la raíz del proyecto). Con ruta absoluta, siempre se usa la misma BD:
        //  - En desarrollo (F5): bin\Debug\net8.0-windows\madurez.db
        //  - En el publish:      <carpeta del .exe>\madurez.db
        private static readonly string _rutaDB =
            System.IO.Path.Combine(AppContext.BaseDirectory, "madurez.db");

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
                  Rif TEXT NOT NULL UNIQUE,
                  Sector TEXT NOT NULL,
                  CantidadEmpleados INTEGER,
                  Direccion TEXT NOT NULL,
                  Telefono TEXT,
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
                    ResumenEmpresa TEXT,
                    NivelMadurez INTEGER,
                    Fortalezas TEXT,
                    Debilidades TEXT,
                    Riesgos TEXT,
                    Recomendaciones TEXT,
                    FechaGeneracion TEXT NOT NULL,
                    EsFinal INTEGER NOT NULL DEFAULT 0,
                    Origen TEXT NOT NULL DEFAULT 'IA',
                    FOREIGN KEY (ConversacionId) REFERENCES Conversaciones(Id)
                );

                CREATE TABLE IF NOT EXISTS PaquetesHeuristicos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Version INTEGER NOT NULL,
                    FechaGeneracion TEXT NOT NULL,
                    NumDictamenes INTEGER NOT NULL,
                    HashCorpus TEXT NOT NULL,
                    Estado TEXT NOT NULL,
                    ContenidoJson TEXT NOT NULL,
                    ExactitudBase REAL,
                    ExactitudDestilada REAL,
                    F1MacroBase REAL,
                    F1MacroDestilada REAL,
                    MetricasJson TEXT
                );

                -- Tabla intermedia N:M: qué dictámenes formaron cada paquete.
                -- Cumple con el modelo relacional estricto (toda tabla tiene relaciones)
                -- y aporta trazabilidad exhaustiva para auditoría del proceso de destilación.
                CREATE TABLE IF NOT EXISTS PaqueteDictamen (
                    PaqueteId INTEGER NOT NULL,
                    DiagnosticoId INTEGER NOT NULL,
                    PRIMARY KEY (PaqueteId, DiagnosticoId),
                    FOREIGN KEY (PaqueteId) REFERENCES PaquetesHeuristicos(Id),
                    FOREIGN KEY (DiagnosticoId) REFERENCES Diagnosticos(Id)
                );
               ";
            cmd.ExecuteNonQuery();

            // Migración: si la BD existe sin la columna Origen, la añadimos.
            // SQLite no soporta 'ADD COLUMN IF NOT EXISTS', así que consultamos primero.
            var cmdCheck = conexion.CreateCommand();
            cmdCheck.CommandText = "PRAGMA table_info(Diagnosticos);";
            bool tieneOrigen = false;
            using (var reader = cmdCheck.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(1).Equals("Origen", StringComparison.OrdinalIgnoreCase))
                    {
                        tieneOrigen = true;
                        break;
                    }
                }
            }
            if (!tieneOrigen)
            {
                var cmdMig = conexion.CreateCommand();
                cmdMig.CommandText = "ALTER TABLE Diagnosticos ADD COLUMN Origen TEXT NOT NULL DEFAULT 'IA';";
                cmdMig.ExecuteNonQuery();
            }
        }
    }
}