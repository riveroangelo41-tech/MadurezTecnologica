
using Microsoft.Data.Sqlite;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Datos
{
    public class RepositorioConversacion
    {
        public int Guardar(Conversacion conversacion) // metodo para guardar una nueva conversación
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open(); //la cadena de conexion

            var cmd = conexion.CreateCommand();// crear los comandos par bd
            cmd.CommandText = @"
                INSERT INTO Conversaciones (EmpresaId, FechaInicio, Estado, RutaInforme)
                VALUES ($empresaId, $fecha, $estado, $ruta);
                SELECT last_insert_rowid();";

            // prametros de valor
            cmd.Parameters.AddWithValue("$empresaId", conversacion.EmpresaId);
            cmd.Parameters.AddWithValue("$fecha", conversacion.FechaInicio.ToString("o"));
            cmd.Parameters.AddWithValue("$estado", conversacion.Estado);
            cmd.Parameters.AddWithValue("$ruta", conversacion.RutaInforme ?? ""); 

            return Convert.ToInt32(cmd.ExecuteScalar());




        }

        public List<Conversacion> ObtenerTodas() //metodo para obtener los datos de la conversación
        {
            var lista = new List<Conversacion>(); //se crea la lista

            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();//la cadena de conexion

            //para obtener los datos de la conversación ordenados por la fecha
            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT * FROM Conversaciones ORDER BY FechaInicio DESC";

            using var reader = cmd.ExecuteReader();
            // se lee cada fila y se agrega a la lista de conversaciones
            while (reader.Read())
            {
                lista.Add(new Conversacion
                {
                    Id = reader.GetInt32(0),
                    EmpresaId = reader.GetInt32(1),
                    FechaInicio = DateTime.Parse(reader.GetString(2)),
                    Estado = reader.GetString(3),
                    RutaInforme = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });




            }



            return lista;



        }

        public Conversacion? ObtenerPorId(int id)// metodo para obtener una conversación por su id
       {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();//la cadena de conexion

            //crear los comandos de la bd
            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT * FROM Conversaciones WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            // se lee la fila y se devuelve la conversación si se encuentra, de lo contrario se devuelve null
            if (reader.Read())
            {
                return new Conversacion
                {
                    Id = reader.GetInt32(0),
                    EmpresaId = reader.GetInt32(1),
                    FechaInicio = DateTime.Parse(reader.GetString(2)),
                    Estado = reader.GetString(3),
                    RutaInforme = reader.IsDBNull(4) ? "" : reader.GetString(4)


               
                };



            }



            return null;



        }

        public void ActualizarEstado (int id, string nuevoEstado) // metodo para actualizar el estado de una conversación
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open();//la cadena de conexion

            //crear los comandos de la bd para actualizar el estado de la conversación con el id especificado
            var cmd = conexion.CreateCommand();
            cmd.CommandText = "UPDATE Conversaciones SET Estado = $estado WHERE Id = $id";
            cmd.Parameters.AddWithValue("$estado", nuevoEstado);
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();




        }



    }

}



