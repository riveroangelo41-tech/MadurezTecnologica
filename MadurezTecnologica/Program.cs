using MadurezTecnologica.Datos;

namespace MadurezTecnologica
{ 
    internal static class Program {
    
       [STAThread]
       
       static void Main() {
            //inciar base de datos antes de mostrar la interfaz
            
            ApplicationConfiguration.Initialize();
           
            BaseDatos.Inicializar();

            Application.Run(new Form1());
        }



    }






 }