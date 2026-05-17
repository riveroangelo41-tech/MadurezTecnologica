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

                // === 8. BATERÍA DE PRUEBAS DEL ORQUESTADOR ===
                resultado.AppendLine();
                resultado.AppendLine("===== BATERÍA DE PRUEBAS - SEMANA 2 =====");
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                // Ruta del PDF de prueba (declarada una sola vez)
                string rutaPdfPrueba = @"C:\Users\Home\Desktop\informe_codeminka_corto.pdf";

                var orquestador = new MadurezTecnologica.Logica.OrquestadorAnalisis();

                // ----- PRUEBA 1: Caso normal con PDF válido -----
                resultado.AppendLine();
                resultado.AppendLine("--- PRUEBA 1: Análisis normal ---");
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                try
                {
                    var empresa1 = new MadurezTecnologica.Modelos.Empresa
                    {
                        Nombre = "CodeMinka, C.A.",
                        Sector = "Desarrollo de aplicaciones móviles"
                    };

                    var analisis1 = await orquestador.AnalizarInformePdf(rutaPdfPrueba, empresa1);

                    resultado.AppendLine($"Modo: {analisis1.ModoUsado}");
                    resultado.AppendLine($"Método validación: {analisis1.MetodoValidacion}");
                    resultado.AppendLine($"Exitoso: {analisis1.Exitoso}");
                    resultado.AppendLine($"Caracteres procesados: {analisis1.CaracteresProcesados}");

                    if (analisis1.Exitoso && analisis1.Diagnostico != null)
                    {
                        var diag = analisis1.Diagnostico;
                        resultado.AppendLine();
                        resultado.AppendLine("--- DIAGNÓSTICO PARSEADO ---");
                        resultado.AppendLine($"Nivel de madurez: {diag.NivelMadurez}");
                        resultado.AppendLine();
                        resultado.AppendLine("FORTALEZAS:");
                        resultado.AppendLine(RecortarTexto(diag.Fortalezas, 250));
                        resultado.AppendLine();
                        resultado.AppendLine("DEBILIDADES:");
                        resultado.AppendLine(RecortarTexto(diag.Debilidades, 250));
                        resultado.AppendLine();
                        resultado.AppendLine("RIESGOS:");
                        resultado.AppendLine(RecortarTexto(diag.Riesgos, 250));
                        resultado.AppendLine();
                        resultado.AppendLine("RECOMENDACIONES:");
                        resultado.AppendLine(RecortarTexto(diag.Recomendaciones, 250));
                    }
                    else
                    {
                        resultado.AppendLine($"Mensaje: {analisis1.Mensaje}");
                    }
                }
                catch (Exception ex)
                {
                    resultado.AppendLine($"Error inesperado: {ex.Message}");
                }

                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                // ----- PRUEBA 2: PDF inexistente -----
                resultado.AppendLine();
                resultado.AppendLine("--- PRUEBA 2: PDF con ruta inválida ---");
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                try
                {
                    var empresa2 = new MadurezTecnologica.Modelos.Empresa
                    {
                        Nombre = "Empresa Fantasma",
                        Sector = "Test"
                    };

                    var analisis2 = await orquestador.AnalizarInformePdf(
                        @"C:\Ruta\Que\No\Existe\fantasma.pdf",
                        empresa2
                    );

                    resultado.AppendLine($"Modo: {analisis2.ModoUsado}");
                    resultado.AppendLine($"Exitoso: {analisis2.Exitoso}");
                    resultado.AppendLine($"Mensaje: {analisis2.Mensaje}");
                }
                catch (Exception ex)
                {
                    resultado.AppendLine($"Error inesperado: {ex.Message}");
                }

                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                // ----- PRUEBA 3: Modo offline forzado -----
                resultado.AppendLine();
                resultado.AppendLine("--- PRUEBA 3: Modo offline forzado ---");
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                try
                {
                    // Forzar offline
                    MadurezTecnologica.Inteligencia.DetectorConexion.ActivarModoOfflineForzado();

                    var empresa3 = new MadurezTecnologica.Modelos.Empresa
                    {
                        Nombre = "CodeMinka, C.A.",
                        Sector = "Desarrollo de aplicaciones móviles"
                    };

                    var analisis3 = await orquestador.AnalizarInformePdf(rutaPdfPrueba, empresa3);

                    resultado.AppendLine($"Modo: {analisis3.ModoUsado}");
                    resultado.AppendLine($"Exitoso: {analisis3.Exitoso}");
                    resultado.AppendLine($"Mensaje: {analisis3.Mensaje}");

                    // Restaurar modo normal
                    MadurezTecnologica.Inteligencia.DetectorConexion.DesactivarModoOfflineForzado();
                }
                catch (Exception ex)
                {
                    resultado.AppendLine($"Error inesperado: {ex.Message}");
                }
                // ----- PRUEBA 4: PDF que NO corresponde a la empresa registrada -----
                resultado.AppendLine();
                resultado.AppendLine("--- PRUEBA 4: Coherencia PDF-Empresa ---");
                txtResultado.Text = resultado.ToString();
                txtResultado.Refresh();

                try
                {
                    // Registramos una empresa DIFERENTE a la del PDF
                    var empresaIncorrecta = new MadurezTecnologica.Modelos.Empresa
                    {
                        Nombre = "OtraEmpresa Distinta, C.A.",
                        Sector = "Otro sector cualquiera"
                    };

                    // Subimos el PDF de CodeMinka pero registramos OtraEmpresa
                    var analisis4 = await orquestador.AnalizarInformePdf(rutaPdfPrueba, empresaIncorrecta);

                    resultado.AppendLine($"Modo: {analisis4.ModoUsado}");
                    resultado.AppendLine($"Método validación: {analisis4.MetodoValidacion}");
                    resultado.AppendLine($"Exitoso: {analisis4.Exitoso}");
                    resultado.AppendLine($"Mensaje: {analisis4.Mensaje}");
                }
                catch (Exception ex)
                {
                    resultado.AppendLine($"Error inesperado: {ex.Message}");
                }


                // ----- RESUMEN FINAL -----
                resultado.AppendLine();
                resultado.AppendLine("===== FIN DE PRUEBAS =====");
                txtResultado.Text = resultado.ToString();
            }
            catch (Exception ex)
            {
                // Manejo general para el try principal
                resultado.AppendLine($"Error inesperado: {ex.Message}");
                txtResultado.Text = resultado.ToString();
            }
        }
        private string RecortarTexto(string texto, int maxCaracteres)
        {
            if (string.IsNullOrEmpty(texto)) return "(sin contenido)";
            if (texto.Length <= maxCaracteres) return texto;
            return texto.Substring(0, maxCaracteres) + "...";
        }

    }
}



