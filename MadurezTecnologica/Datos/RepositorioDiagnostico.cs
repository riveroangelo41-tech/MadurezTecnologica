using Microsoft.Data.Sqlite;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Datos
{
    public class RepositorioDiagnostico
    {
        public int Guardar(Diagnostico diagnostico)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Diagnosticos
                (ConversacionId, ResumenEmpresa, NivelMadurez, Fortalezas, Debilidades, Riesgos,
                 Recomendaciones, FechaGeneracion, EsFinal, Origen)
                VALUES
                ($convId,$resumen, $nivel, $fort, $deb, $ries, $rec, $fecha, $final, $origen);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("$convId", diagnostico.ConversacionId);
            cmd.Parameters.AddWithValue("$resumen", diagnostico.ResumenEmpresa ?? "");
            cmd.Parameters.AddWithValue("$nivel", diagnostico.NivelMadurez);
            cmd.Parameters.AddWithValue("$fort", diagnostico.Fortalezas ?? "");
            cmd.Parameters.AddWithValue("$deb", diagnostico.Debilidades ?? "");
            cmd.Parameters.AddWithValue("$ries", diagnostico.Riesgos ?? "");
            cmd.Parameters.AddWithValue("$rec", diagnostico.Recomendaciones ?? "");
            cmd.Parameters.AddWithValue("$fecha", diagnostico.FechaGeneracion.ToString("o"));
            cmd.Parameters.AddWithValue("$final", diagnostico.EsFinal ? 1 : 0);
            cmd.Parameters.AddWithValue("$origen", string.IsNullOrWhiteSpace(diagnostico.Origen) ? "IA" : diagnostico.Origen);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Devuelve todos los dictámenes generados por Claude (origen "IA").
        // Es el corpus de entrenamiento del Destilador; nunca lee dictámenes offline
        // para evitar retroalimentación degradante.
        public List<Diagnostico> ObtenerTodosPorOrigen(string origen)
        {
            var lista = new List<Diagnostico>();
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Diagnosticos
                WHERE Origen = $origen
                ORDER BY FechaGeneracion ASC";
            cmd.Parameters.AddWithValue("$origen", origen);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(LeerDiagnostico(reader));
            }
            return lista;
        }

        public Diagnostico? ObtenerUltimoPorConversacion(int conversacionId)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
            SELECT * FROM Diagnosticos
            WHERE ConversacionId = $convId
            ORDER BY FechaGeneracion DESC
            LIMIT 1;";

            cmd.Parameters.AddWithValue("$convId", conversacionId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
               return LeerDiagnostico(reader);

            }

            return null;

        }

        public Diagnostico? ObtenerFinalPorConversacion(int conversacionId)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Diagnosticos
                WHERE ConversacionId = $convId AND EsFinal = 1
                LIMIT 1";
            cmd.Parameters.AddWithValue("$convId", conversacionId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return LeerDiagnostico(reader);
            }

            return null;
        }
        public List<Diagnostico> ObtenerHistorialPorConversacion(int conversacionId)
        {
            var lista = new List<Diagnostico>();

            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Diagnosticos
                WHERE ConversacionId = $convId
                ORDER BY FechaGeneracion ASC";
            cmd.Parameters.AddWithValue("$convId", conversacionId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(LeerDiagnostico(reader));
            }

            return lista;
        }

        public void DesmarcarFinalesPorConversacion(int conversacionId)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                UPDATE Diagnosticos
                SET EsFinal = 0
                WHERE ConversacionId = $convId AND EsFinal = 1";
            cmd.Parameters.AddWithValue("$convId", conversacionId);
            cmd.ExecuteNonQuery();
        }

        public void EliminarPorId(int id)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "DELETE FROM Diagnosticos WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public void EliminarPorConversacion(int conversacionId)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "DELETE FROM Diagnosticos WHERE ConversacionId = $convId";
            cmd.Parameters.AddWithValue("$convId", conversacionId);
            cmd.ExecuteNonQuery();
        }

        private Diagnostico LeerDiagnostico(SqliteDataReader reader)
        {
            // Leo Origen por nombre por si el orden de columnas cambia en el futuro,
            // y para tolerar filas antiguas donde la columna pudiera no estar.
            string origen = "IA";
            try
            {
                int ord = reader.GetOrdinal("Origen");
                if (!reader.IsDBNull(ord)) origen = reader.GetString(ord);
            }
            catch { /* columna no existe en versiones antiguas → asumimos IA */ }

            return new Diagnostico
            {
                Id = reader.GetInt32(0),
                ConversacionId = reader.GetInt32(1),
                ResumenEmpresa = reader.IsDBNull(2) ? "" : reader.GetString(2),
                NivelMadurez = reader.GetInt32(3),
                Fortalezas = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Debilidades = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Riesgos = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Recomendaciones = reader.IsDBNull(7) ? "" : reader.GetString(7),
                FechaGeneracion = DateTime.Parse(reader.GetString(8)),
                EsFinal = reader.GetInt32(9) == 1,
                Origen = origen
            };
        }
    }
}


            