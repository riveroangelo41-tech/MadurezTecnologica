using Microsoft.Data.Sqlite;
using System.Text.Json;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Datos
{
    // Persistencia de paquetes heurísticos versionados.
    // El contenido (indicadores + recomendaciones) se serializa como JSON en
    // ContenidoJson; las métricas van en columnas propias para consultas rápidas.
    public class RepositorioPaqueteHeuristico
    {
        // Guarda un paquete y sus fuentes (los dictámenes que lo formaron) en UNA
        // transacción atómica. Si diagnosticoIdsUsados es null se guarda sin fuentes
        // (compatibilidad hacia atrás).
        public int Guardar(PaqueteHeuristico paquete, IEnumerable<int>? diagnosticoIdsUsados = null)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();
            using var tx = conexion.BeginTransaction();

            // 1) Insertar el paquete
            var cmd = conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO PaquetesHeuristicos
                (Version, FechaGeneracion, NumDictamenes, HashCorpus, Estado,
                 ContenidoJson, ExactitudBase, ExactitudDestilada,
                 F1MacroBase, F1MacroDestilada, MetricasJson)
                VALUES
                ($ver, $fecha, $n, $hash, $estado, $json, $eb, $ed, $fb, $fd, $mj);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("$ver", paquete.Version);
            cmd.Parameters.AddWithValue("$fecha", paquete.FechaGeneracion.ToString("o"));
            cmd.Parameters.AddWithValue("$n", paquete.NumDictamenes);
            cmd.Parameters.AddWithValue("$hash", paquete.HashCorpus ?? "");
            cmd.Parameters.AddWithValue("$estado", paquete.Estado);
            cmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(new
            {
                paquete.Indicadores,
                paquete.Recomendaciones
            }));
            cmd.Parameters.AddWithValue("$eb", paquete.ExactitudBase);
            cmd.Parameters.AddWithValue("$ed", paquete.ExactitudDestilada);
            cmd.Parameters.AddWithValue("$fb", paquete.F1MacroBase);
            cmd.Parameters.AddWithValue("$fd", paquete.F1MacroDestilada);
            cmd.Parameters.AddWithValue("$mj", "{}");

            int idPaquete = Convert.ToInt32(cmd.ExecuteScalar());

            // 2) Poblar la tabla intermedia con las fuentes usadas
            if (diagnosticoIdsUsados != null)
            {
                var cmdSrc = conexion.CreateCommand();
                cmdSrc.Transaction = tx;
                cmdSrc.CommandText = @"
                    INSERT OR IGNORE INTO PaqueteDictamen (PaqueteId, DiagnosticoId)
                    VALUES ($pid, $did);";
                var pPid = cmdSrc.Parameters.Add("$pid", Microsoft.Data.Sqlite.SqliteType.Integer);
                var pDid = cmdSrc.Parameters.Add("$did", Microsoft.Data.Sqlite.SqliteType.Integer);
                pPid.Value = idPaquete;

                foreach (int did in diagnosticoIdsUsados)
                {
                    pDid.Value = did;
                    cmdSrc.ExecuteNonQuery();
                }
            }

            tx.Commit();
            return idPaquete;
        }

        // Devuelve los IDs de los dictámenes que formaron un paquete específico.
        // Útil para auditar y para las queries de trazabilidad exigidas por diseño relacional.
        public List<int> ObtenerFuentes(int paqueteId)
        {
            var lista = new List<int>();
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                SELECT DiagnosticoId FROM PaqueteDictamen
                WHERE PaqueteId = $pid
                ORDER BY DiagnosticoId ASC";
            cmd.Parameters.AddWithValue("$pid", paqueteId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) lista.Add(reader.GetInt32(0));
            return lista;
        }

        // Devuelve el paquete activo (el único con Estado='activo'), o null si aún no hay ninguno.
        public PaqueteHeuristico? ObtenerActivo()
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM PaquetesHeuristicos
                WHERE Estado = 'activo'
                ORDER BY Version DESC LIMIT 1";

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Leer(reader) : null;
        }

        public int ObtenerUltimaVersion()
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM PaquetesHeuristicos";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Promueve un paquete a activo, retirando el anterior activo (si existía).
        // Se hace en una única transacción para no dejar dos activos simultáneos.
        public void PromoverAActivo(int idNuevoActivo)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();
            using var tx = conexion.BeginTransaction();

            var cmd1 = conexion.CreateCommand();
            cmd1.Transaction = tx;
            cmd1.CommandText = "UPDATE PaquetesHeuristicos SET Estado='retirado' WHERE Estado='activo'";
            cmd1.ExecuteNonQuery();

            var cmd2 = conexion.CreateCommand();
            cmd2.Transaction = tx;
            cmd2.CommandText = "UPDATE PaquetesHeuristicos SET Estado='activo' WHERE Id=$id";
            cmd2.Parameters.AddWithValue("$id", idNuevoActivo);
            cmd2.ExecuteNonQuery();

            tx.Commit();
        }

        public List<PaqueteHeuristico> ObtenerHistorial()
        {
            var lista = new List<PaqueteHeuristico>();
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT * FROM PaquetesHeuristicos ORDER BY Version DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) lista.Add(Leer(reader));
            return lista;
        }

        private PaqueteHeuristico Leer(SqliteDataReader r)
        {
            var pkg = new PaqueteHeuristico
            {
                Version = r.GetInt32(r.GetOrdinal("Version")),
                FechaGeneracion = DateTime.Parse(r.GetString(r.GetOrdinal("FechaGeneracion"))),
                NumDictamenes = r.GetInt32(r.GetOrdinal("NumDictamenes")),
                HashCorpus = r.GetString(r.GetOrdinal("HashCorpus")),
                Estado = r.GetString(r.GetOrdinal("Estado")),
                ExactitudBase = r.GetDouble(r.GetOrdinal("ExactitudBase")),
                ExactitudDestilada = r.GetDouble(r.GetOrdinal("ExactitudDestilada")),
                F1MacroBase = r.GetDouble(r.GetOrdinal("F1MacroBase")),
                F1MacroDestilada = r.GetDouble(r.GetOrdinal("F1MacroDestilada"))
            };
            string json = r.GetString(r.GetOrdinal("ContenidoJson"));
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Indicadores", out var ind))
                    pkg.Indicadores = JsonSerializer.Deserialize<List<IndicadorDestilado>>(ind.GetRawText()) ?? new();
                if (doc.RootElement.TryGetProperty("Recomendaciones", out var rec))
                    pkg.Recomendaciones = JsonSerializer.Deserialize<List<RecomendacionDestilada>>(rec.GetRawText()) ?? new();
            }
            catch { /* JSON corrupto → paquete vacío */ }
            return pkg;
        }
    }
}
