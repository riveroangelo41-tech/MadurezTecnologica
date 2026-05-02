using Microsoft.Data.Sqlite;
using MadurezTecnologica.Modelos;

namespace MadurezTecnologica.Datos
{
    public class RepositorioMensaje
    {
       public int Guardar (Mensaje mensaje)// Devuelve el ID del mensaje guardado
        {
            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open ();// la cadena de conexion

            //creando comandos de la bd
            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Mensajes (ConversacionId, Remitente, Contenido, Timestamp, Orden)
                VALUES ($convId, $remitente, $contenido, $timestamp, $orden);
                SELECT last_insert_rowid();";

            // creando los parametros de valor
            cmd.Parameters.AddWithValue("$convId", mensaje.ConversacionId);
            cmd.Parameters.AddWithValue("$remitente", mensaje.Remitente);
            cmd.Parameters.AddWithValue("$contenido", mensaje.Contenido);
            cmd.Parameters.AddWithValue("$timestamp", mensaje.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$orden", mensaje.Orden);

            return Convert.ToInt32(cmd.ExecuteScalar()); // Ejecuta el comando y devuelve el ID generado



        }

        public List<Mensaje> ObtenerPorConversacion(int conversacionId)// Devuelve la lista de mensajes de una conversación
        {

            var lista = new List<Mensaje>(); // se crea la lista

            using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open(); // la cadena de conexion

            
            var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Mensajes
                WHERE ConversacionId = $convId
                ORDER BY Orden ASC"; // se ordena por orden de mensaje de manera ascendente
            cmd.Parameters.AddWithValue("$convId", conversacionId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Mensaje
                {
                    Id = reader.GetInt32(0),
                    ConversacionId = reader.GetInt32(1),
                    Remitente = reader.GetString(2),
                    Contenido = reader.GetString(3),
                    Timestamp = DateTime.Parse(reader.GetString(4)),
                    Orden = reader.GetInt32(5)
                });
            }
            return lista;

        }
        public int ContarPorConversacion(int conversacionId) // Devuelve el número de mensajes en una conversación
            {
          using var conexion = new SqliteConnection(BaseDatos.CadenaConexion);
            conexion.Open(); // la cadena de conexion
            
            var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT COUNT (*) FROM Mensajes WHERE ConversacionId = $convId";
            cmd.Parameters.AddWithValue("$convId", conversacionId);
            return Convert.ToInt32(cmd.ExecuteScalar());

            
        }


    }

}