using Microsoft.Data.Sqlite;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Datos
{
    public class RepositorioEmpresa
    {
        public int Guardar(Empresa empresa) //metodo de guadar empresa
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open(); // la cadena de conexion

            var cmd = conexion.CreateCommand(); // se crean los comando de la bd
            cmd.CommandText = @"INSERT INTO Empresas (Nombre, Sector, CantidadEmpleados, FechaRegistro) 
                                VALUES ($nombre, $sector, $empleados, $fecha);
                                SELECT last_insert_rowid();";
            //asignar los valores a los parametros
            cmd.Parameters.AddWithValue("$nombre", empresa.Nombre);
            cmd.Parameters.AddWithValue("$sector", empresa.Sector ?? "");
            cmd.Parameters.AddWithValue("$empleados", empresa.CantidadEmpleados);
            cmd.Parameters.AddWithValue("$fecha", empresa.FechaRegistro.ToString("o"));

            return Convert.ToInt32(cmd.ExecuteScalar());

        }

        public List<Empresa> ObtenerTodas() // metodo para obtener los datos de las empresas registradas
        {

            var lista = new List<Empresa>(); // se crea la lista

            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open(); // la cadena de conexion

            var cmd = conexion.CreateCommand(); // se crean los comando de la bd
            cmd.CommandText = "SELECT * FROM Empresas ORDER BY FechaRegistro DESC";

            using var reader = cmd.ExecuteReader(); // para recurrer las lista una por una

            while(reader.Read()) //para avanzar al siguiente registro, si no hay mas devolvera un false y se detendra el ciclo
            {
              lista.Add (new Empresa
              {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Sector = reader.IsDBNull(2) ?"": reader.GetString (2),
                CantidadEmpleados = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                FechaRegistro = DateTime.Parse(reader.GetString(4))
              });

            }
            return lista;
          }

        public Empresa? ObtenerPorId(int id) // metodo para obtener una empresa por su id
        {
         
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open(); // la cadena de conexion

            var cmd = conexion.CreateCommand();// se crean los comando de la bd
            cmd.CommandText = "SELECT * FROM Empresas WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();// para recurrer las lista una por una
            if (reader.Read()) //si hay una empresa con el id se ejecute el if
            {
                return new Empresa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Sector = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    CantidadEmpleados = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    FechaRegistro = DateTime.Parse(reader.GetString(4))
                };


            }
            return null;
        }

    }

}
