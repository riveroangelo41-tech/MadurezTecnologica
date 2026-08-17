namespace MadurezTecnologica.Estado
{
   
    /// Estado global compartido entre vistas.
  
    public static class EstadoApp
    {
       
        /// ID de la empresa actualmente seleccionada por el usuario.
        
        public static int? EmpresaActivaId { get; set; } = null;

        /// Evento que se dispara cuando cambia la empresa activa.

        public static event Action? EmpresaActivaCambio;

        /// Evento que se dispara cuando cambia el historial de diagnósticos/conversaciones
        /// (ej. tras eliminar diagnósticos o una conversación). Las vistas que muestran
        /// esos datos (Chat, Resultados) se suscriben para refrescarse al instante.
        public static event Action? HistorialCambio;


        /// Cambia la empresa activa y notifica a las vistas suscritas.

        public static void EstablecerEmpresaActiva(int empresaId)
        {
            EmpresaActivaId = empresaId;
            EmpresaActivaCambio?.Invoke();
        }

        /// Notifica a las vistas suscritas que el historial cambió (borrados, etc.).
        public static void NotificarHistorialCambio()
        {
            HistorialCambio?.Invoke();
        }
    }
}
