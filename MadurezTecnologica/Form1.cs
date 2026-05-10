using MadurezTecnologica.Datos;
using MadurezTecnologica.Modelos;
using System.Text;

namespace MadurezTecnologica
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void btnProbar_Click(object sender, EventArgs e)
        {
            var resultado = new StringBuilder();

            try
            {
                // === 1. GUARDAR UNA EMPRESA ===
                var repoEmpresa = new RepositorioEmpresa();
                var empresa = new Empresa
                {
                    Nombre = "Software Solutions C.A.",
                    Rif = $"J-{DateTime.Now:HHmmssfff}-0",
                    Sector = "Desarrollo de software a medida",
                    CantidadEmpleados = 25,
                    Direccion = "Av. 5 de Julio, Maracaibo, Zulia",
                    Telefono = "+58 261-7900000",
                    FechaRegistro = DateTime.Now
                };
                int empresaId = repoEmpresa.Guardar(empresa);
                resultado.AppendLine($"✓ Empresa guardada con ID: {empresaId}");

                // === 2. GUARDAR UNA CONVERSACIÓN ===
                var repoConv = new RepositorioConversacion();
                var conversacion = new Conversacion
                {
                    EmpresaId = empresaId,
                    FechaInicio = DateTime.Now,
                    Estado = "activa",
                    RutaInforme = "C:/informes/prueba.pdf"
                };
                int convId = repoConv.Guardar(conversacion);
                resultado.AppendLine($"✓ Conversación guardada con ID: {convId}");

                // === 3. GUARDAR 3 MENSAJES ===
                var repoMsg = new RepositorioMensaje();

                repoMsg.Guardar(new Mensaje
                {
                    ConversacionId = convId,
                    Remitente = "Usuario",
                    Contenido = "Hola, quiero evaluar mi empresa de software.",
                    Timestamp = DateTime.Now,
                    Orden = 1
                });

                repoMsg.Guardar(new Mensaje
                {
                    ConversacionId = convId,
                    Remitente = "IA",
                    Contenido = "Perfecto, comencemos con el análisis de su informe.",
                    Timestamp = DateTime.Now,
                    Orden = 2
                });

                repoMsg.Guardar(new Mensaje
                {
                    ConversacionId = convId,
                    Remitente = "Usuario",
                    Contenido = "Usamos Git y Jenkins para CI/CD.",
                    Timestamp = DateTime.Now,
                    Orden = 3
                });

                resultado.AppendLine($"✓ 3 mensajes guardados");

                // === 4. GUARDAR UN DIAGNÓSTICO INICIAL ===
                var repoDiag = new RepositorioDiagnostico();
                var diagnostico = new Diagnostico
                {
                    ConversacionId = convId,
                    NivelMadurez = 3,
                    Fortalezas = "Uso de control de versiones y CI/CD",
                    Debilidades = "Falta documentación técnica",
                    Riesgos = "Dependencia de pocos desarrolladores senior",
                    Recomendaciones = "Implementar revisiones de código sistemáticas",
                    FechaGeneracion = DateTime.Now,
                    EsFinal = false
                };
                int diagId = repoDiag.Guardar(diagnostico);
                resultado.AppendLine($"✓ Diagnóstico guardado con ID: {diagId}");

                resultado.AppendLine();
                resultado.AppendLine("===== LECTURA DE DATOS =====");
                resultado.AppendLine();

                // === 5. LEER TODAS LAS EMPRESAS ===
                var empresas = repoEmpresa.ObtenerTodas();
                resultado.AppendLine($"Empresas en BD: {empresas.Count}");
                foreach (var emp in empresas)
                {
                    resultado.AppendLine($"  - [{emp.Id}] {emp.Nombre}");
                    resultado.AppendLine($"    RIF: {emp.Rif} | Sector: {emp.Sector}");
                    resultado.AppendLine($"    Empleados: {emp.CantidadEmpleados} | Tel: {emp.Telefono}");
                    resultado.AppendLine($"    Dirección: {emp.Direccion}");
                    resultado.AppendLine();
                }

                // === 6. LEER MENSAJES DE LA CONVERSACIÓN ===
                resultado.AppendLine();
                int totalMensajes = repoMsg.ContarPorConversacion(convId);
                resultado.AppendLine($"Mensajes de la conversación {convId}: {totalMensajes}");

                var mensajes = repoMsg.ObtenerPorConversacion(convId);
                foreach (var msg in mensajes)
                {
                    resultado.AppendLine($"  [{msg.Orden}] {msg.Remitente}: {msg.Contenido}");
                }

                // === 7. LEER ÚLTIMO DIAGNÓSTICO ===
                resultado.AppendLine();
                var ultimoDiag = repoDiag.ObtenerUltimoPorConversacion(convId);
                if (ultimoDiag != null)
                {
                    resultado.AppendLine($"Último diagnóstico:");
                    resultado.AppendLine($"  Nivel madurez: {ultimoDiag.NivelMadurez}");
                    resultado.AppendLine($"  Es final: {ultimoDiag.EsFinal}");
                    resultado.AppendLine($"  Fortalezas: {ultimoDiag.Fortalezas}");
                }

                resultado.AppendLine();

                // === 8. DETECTAR MODO DE OPERACIÓN ===
                resultado.AppendLine();
                resultado.AppendLine("===== DETECTOR DE CONEXIÓN =====");
                try
                {
                    var detector = new MadurezTecnologica.Inteligencia.DetectorConexion();

                    // Probar el modo actual
                    var modo = await detector.DetectarModo();
                    resultado.AppendLine($"Modo actual: {modo}");
                    resultado.AppendLine($"Descripción: {detector.DescribirModo(modo)}");

                    // Probar verificación directa de internet
                    bool internet = await detector.HayInternet();
                    resultado.AppendLine($"¿Hay internet?: {internet}");

                    // Probar el modo forzado
                    resultado.AppendLine();
                    resultado.AppendLine("--- Activando modo offline forzado ---");
                    MadurezTecnologica.Inteligencia.DetectorConexion.ActivarModoOfflineForzado();
                    var modoForzado = await detector.DetectarModo();
                    resultado.AppendLine($"Modo tras forzar offline: {modoForzado}");
                    resultado.AppendLine($"Descripción: {detector.DescribirModo(modoForzado)}");

                    // Restaurar modo normal
                    MadurezTecnologica.Inteligencia.DetectorConexion.DesactivarModoOfflineForzado();
                    var modoFinal = await detector.DetectarModo();
                    resultado.AppendLine();
                    resultado.AppendLine($"--- Modo restaurado ---");
                    resultado.AppendLine($"Modo final: {modoFinal}");
                }
                catch (Exception ex)
                {
                    resultado.AppendLine($"✗ Error en detector: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                resultado.AppendLine();
                resultado.AppendLine($"✗ ERROR: {ex.Message}");
                resultado.AppendLine($"Tipo: {ex.GetType().Name}");
            }

            txtResultado.Text = resultado.ToString();
        }
    }
}


