namespace MadurezTecnologica.Estado
{
   
    /// Estado global compartido entre vistas.
  
    public static class EstadoApp
    {
       
        /// ID de la empresa actualmente seleccionada por el usuario.
        
        public static int? EmpresaActivaId { get; set; } = null;

        /// Evento que se dispara cuando cambia la empresa activa.
        
        public static event Action? EmpresaActivaCambio;

        
        /// Cambia la empresa activa y notifica a las vistas suscritas.
       
        public static void EstablecerEmpresaActiva(int empresaId)
        {
            EmpresaActivaId = empresaId;
            EmpresaActivaCambio?.Invoke();
        }
    }
}
