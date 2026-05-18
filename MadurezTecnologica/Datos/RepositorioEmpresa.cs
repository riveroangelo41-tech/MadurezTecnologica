using MadurezTecnologica.Datos;
using MadurezTecnologica.Modelos;
using Microsoft.Data.Sqlite;

namespace MadurezTecnologica.Datos
{
    public class RepositorioEmpresa
    {
        public int Guardar(Empresa empresa)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Empresas (Nombre, Rif, Sector, CantidadEmpleados, Direccion, Telefono, FechaRegistro)
                VALUES ($nombre, $rif, $sector, $empleados, $direccion, $telefono, $fecha);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("$nombre", empresa.Nombre);
            cmd.Parameters.AddWithValue("$rif", empresa.Rif);
            cmd.Parameters.AddWithValue("$sector", empresa.Sector ?? "");
            cmd.Parameters.AddWithValue("$empleados", empresa.CantidadEmpleados);
            cmd.Parameters.AddWithValue("$direccion", empresa.Direccion);
            cmd.Parameters.AddWithValue("$telefono", empresa.Telefono ?? "");
            cmd.Parameters.AddWithValue("$fecha", empresa.FechaRegistro.ToString("o"));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Empresa> ObtenerTodas()
        {
            var lista = new List<Empresa>();

            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT * FROM Empresas ORDER BY FechaRegistro DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Empresa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Rif = reader.GetString(2),
                    Sector = reader.GetString(3),
                    CantidadEmpleados = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    Direccion = reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    FechaRegistro = DateTime.Parse(reader.GetString(7))
                });
            }

            return lista;
        }

        public Empresa? ObtenerPorId(int id)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT * FROM Empresas WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Empresa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Rif = reader.GetString(2),
                    Sector = reader.GetString(3),
                    CantidadEmpleados = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    Direccion = reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    FechaRegistro = DateTime.Parse(reader.GetString(7))
                };
            }

            return null;
        }
        // Método para obtener una empresa por su RIF, que es un identificador único
        public Empresa? ObtenerPorRif(string rif)
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();

            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT * FROM Empresas WHERE Rif = $rif";
            cmd.Parameters.AddWithValue("$rif", rif);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Empresa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Rif = reader.GetString(2),
                    Sector = reader.GetString(3),
                    CantidadEmpleados = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    Direccion = reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    FechaRegistro = DateTime.Parse(reader.GetString(7))
                };
            }

            return null;
        }


    }
}