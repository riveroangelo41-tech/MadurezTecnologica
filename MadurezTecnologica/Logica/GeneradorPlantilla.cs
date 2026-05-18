using NPOI.XWPF.UserModel; //  Librería para manipular documentos Word (.docx)
using NPOI.OpenXmlFormats.Wordprocessing; //  Formatos específicos para Word (tablas, párrafos, etc.)

namespace MadurezTecnologica.Logica
{
    public class GeneradorPlantilla
    {
        // Paleta de colores corporativos
        private const string COLOR_AZUL = "1F4E79";
        private const string COLOR_GRIS = "595959";
        private const string COLOR_AZUL_CLARO = "D5E8F0";
        private const string COLOR_GRIS_CLARO = "F2F2F2";

        // Ancho seguro de tablas (funciona en US Letter y A4 con márgenes estándar)
        private const int ANCHO_TABLA = 8500;

        // ==========================================================
        // MÉTODO PRINCIPAL
        // ==========================================================
        public string GenerarPlantilla(string rutaSalida)
        {
            var doc = new XWPFDocument();

            ConfigurarPagina(doc);

            AgregarPortada(doc);
            AgregarInstrucciones(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion1(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion2(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion3(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion4(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion5(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion6(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion7(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion8(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion9(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion10(doc);
            AgregarSaltoPagina(doc);

            AgregarSeccion11(doc);
            AgregarCierre(doc);

            using (var fs = new FileStream(rutaSalida, FileMode.Create))
            {
                doc.Write(fs);
            }

            return rutaSalida;
        }

        private void ConfigurarPagina(XWPFDocument doc)
        {
            var body = doc.Document.body;
            var sectPr = body.sectPr;
            if (sectPr == null)
            {
                sectPr = new NPOI.OpenXmlFormats.Wordprocessing.CT_SectPr();
                body.sectPr = sectPr;
            }

            // Tamaño A4
            if (sectPr.pgSz == null)
                sectPr.pgSz = new NPOI.OpenXmlFormats.Wordprocessing.CT_PageSz();
            sectPr.pgSz.w = 11906;
            sectPr.pgSz.h = 16838;

            // Márgenes de 1 pulgada (1440 DXA)
            if (sectPr.pgMar == null)
                sectPr.pgMar = new NPOI.OpenXmlFormats.Wordprocessing.CT_PageMar();
            sectPr.pgMar.top = 1440;
            sectPr.pgMar.bottom = 1440;
            sectPr.pgMar.left = 1440;
            sectPr.pgMar.right = 1440;
            sectPr.pgMar.header = 720;
            sectPr.pgMar.footer = 720;
        }

        // ==========================================================
        // MÉTODOS AUXILIARES DE FORMATO
        // ==========================================================

        private void AgregarTitulo(XWPFDocument doc, string texto)
        {
            var parrafo = doc.CreateParagraph();
            parrafo.Alignment = ParagraphAlignment.CENTER;
            var run = parrafo.CreateRun();
            run.SetText(texto);
            run.IsBold = true;
            run.FontSize = 18;
            run.FontFamily = "Calibri";
            run.SetColor(COLOR_AZUL);
        }

        private void AgregarSubtitulo(XWPFDocument doc, string texto)
        {
            var parrafo = doc.CreateParagraph();
            parrafo.Alignment = ParagraphAlignment.CENTER;
            var run = parrafo.CreateRun();
            run.SetText(texto);
            run.IsItalic = true;
            run.FontSize = 11;
            run.FontFamily = "Calibri";
            run.SetColor(COLOR_GRIS);
        }

        private void AgregarEncabezadoSeccion(XWPFDocument doc, string numero, string texto)
        {
            var tabla = doc.CreateTable(1, 1);
            ConfigurarTabla(tabla, ANCHO_TABLA);

            var celda = tabla.GetRow(0).GetCell(0);
            EstablecerAnchoCelda(celda, ANCHO_TABLA);
            celda.SetColor(COLOR_AZUL);

            var parrafo = celda.Paragraphs[0];
            var run = parrafo.CreateRun();
            run.SetText($"{numero}. {texto}");
            run.IsBold = true;
            run.FontSize = 13;
            run.FontFamily = "Calibri";
            run.SetColor("FFFFFF");

            // Espacio después de la sección
            doc.CreateParagraph();
        }

        private void AgregarDescripcion(XWPFDocument doc, string texto)
        {
            var parrafo = doc.CreateParagraph();
            var run = parrafo.CreateRun();
            run.SetText(texto);
            run.IsItalic = true;
            run.FontSize = 10;
            run.FontFamily = "Calibri";
            run.SetColor(COLOR_GRIS);
        }

        private void AgregarPregunta(XWPFDocument doc, string texto)
        {
            var parrafo = doc.CreateParagraph();
            parrafo.SpacingBefore = 200;
            parrafo.SpacingAfter = 60;
            var run = parrafo.CreateRun();
            run.SetText(texto);
            run.IsBold = true;
            run.FontSize = 11;
            run.FontFamily = "Calibri";
            run.SetColor(COLOR_AZUL);
        }

        private void AgregarHint(XWPFDocument doc, string texto)
        {
            var parrafo = doc.CreateParagraph();
            parrafo.SpacingAfter = 100;
            var run = parrafo.CreateRun();
            run.SetText(texto);
            run.IsItalic = true;
            run.FontSize = 9;
            run.FontFamily = "Calibri";
            run.SetColor(COLOR_GRIS);
        }

        private void AgregarBullet(XWPFDocument doc, string texto)
        {
            var parrafo = doc.CreateParagraph();
            parrafo.IndentationLeft = 360;
            var run = parrafo.CreateRun();
            run.SetText($"•  {texto}");
            run.FontSize = 10;
            run.FontFamily = "Calibri";
        }

        private void AgregarCajaRespuesta(XWPFDocument doc, int alturaLineas)
        {
            var tabla = doc.CreateTable(1, 1);
            ConfigurarTabla(tabla, ANCHO_TABLA);

            var celda = tabla.GetRow(0).GetCell(0);
            EstablecerAnchoCelda(celda, ANCHO_TABLA);

            // Llenar la celda con párrafos vacíos para darle altura
            for (int i = 0; i < alturaLineas; i++)
            {
                XWPFParagraph p;
                if (i == 0)
                {
                    // El primer párrafo ya existe en la celda recién creada
                    p = celda.Paragraphs[0];
                }
                else
                {
                    p = celda.AddParagraph();
                }

                var r = p.CreateRun();
                r.SetText(" ");
                r.FontSize = 11;
                r.FontFamily = "Calibri";
            }

            // Espacio después de la caja
            doc.CreateParagraph();
        }

        private void AgregarFilaDato(XWPFTable tabla, string etiqueta)
        {
            var fila = tabla.CreateRow();

            var celdaEtiqueta = fila.GetCell(0);
            EstablecerAnchoCelda(celdaEtiqueta, 2800);

            var celdaValor = fila.GetCell(1);
            EstablecerAnchoCelda(celdaValor, 5700);

            celdaEtiqueta.SetColor(COLOR_GRIS_CLARO);
            var pE = celdaEtiqueta.Paragraphs[0];
            var rE = pE.CreateRun();
            rE.SetText(etiqueta);
            rE.IsBold = true;
            rE.FontSize = 10;
            rE.FontFamily = "Calibri";

            // La segunda celda queda en blanco para que el usuario llene
            fila.GetCell(1);
        }

        private void ConfigurarTabla(XWPFTable tabla, ulong ancho)
        {
            var ctTbl = tabla.GetCTTbl();
            var tblPr = ctTbl.tblPr;
            if (tblPr == null) tblPr = ctTbl.AddNewTblPr();

            // Forzar el ancho de la tabla
            var w = tblPr.tblW;
            if (w == null)
            {
                w = new NPOI.OpenXmlFormats.Wordprocessing.CT_TblWidth();
                tblPr.tblW = w;
            }
            w.w = ancho.ToString();
            w.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;

            // CLAVE: Forzar layout FIXED para que no se auto-ajuste
            var layout = tblPr.tblLayout;
            if (layout == null)
            {
                layout = new NPOI.OpenXmlFormats.Wordprocessing.CT_TblLayoutType();
                tblPr.tblLayout = layout;
            }
            layout.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblLayoutType.@fixed;
        }

        private void EstablecerAnchoCelda(XWPFTableCell celda, ulong ancho)
        {
            var ctTc = celda.GetCTTc();
            var tcPr = ctTc.tcPr;
            if (tcPr == null) tcPr = ctTc.AddNewTcPr();

            var w = tcPr.tcW;
            if (w == null)
            {
                w = new NPOI.OpenXmlFormats.Wordprocessing.CT_TblWidth();
                tcPr.tcW = w;
            }
            w.w = ancho.ToString();
            w.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;
        }

        private void AgregarSaltoPagina(XWPFDocument doc)
        {
            var parrafo = doc.CreateParagraph();
            parrafo.IsPageBreak = true;
        }

        private void AgregarEspacio(XWPFDocument doc)
        {
            doc.CreateParagraph();
        }

        private void AgregarCajaInformativa(XWPFDocument doc, string titulo, string contenido)
        {
            var tabla = doc.CreateTable(1, 1);
            ConfigurarTabla(tabla, ANCHO_TABLA);

            var celda = tabla.GetRow(0).GetCell(0);
            EstablecerAnchoCelda(celda, ANCHO_TABLA);
            celda.SetColor(COLOR_AZUL_CLARO);

            var pTitulo = celda.Paragraphs[0];
            var rTitulo = pTitulo.CreateRun();
            rTitulo.SetText(titulo);
            rTitulo.IsBold = true;
            rTitulo.FontSize = 12;
            rTitulo.FontFamily = "Calibri";
            rTitulo.SetColor(COLOR_AZUL);

            var pContenido = celda.AddParagraph();
            var rContenido = pContenido.CreateRun();
            rContenido.SetText(contenido);
            rContenido.FontSize = 10;
            rContenido.FontFamily = "Calibri";

            doc.CreateParagraph();
        }

        // ==========================================================
        // SECCIONES DEL DOCUMENTO
        // ==========================================================

        private void AgregarPortada(XWPFDocument doc)
        {
            AgregarTitulo(doc, "PLANTILLA DE EVALUACIÓN");
            AgregarTitulo(doc, "DE MADUREZ TECNOLÓGICA");
            AgregarSubtitulo(doc, "Sistema de Evaluación de Madurez Tecnológica para PYMES");
            AgregarSubtitulo(doc, "Sector: Desarrollo de Software");
            AgregarEspacio(doc);

            AgregarCajaInformativa(doc,
                "¿QUÉ ES ESTE DOCUMENTO?",
                "Este documento es la plantilla oficial que su empresa debe completar para iniciar el proceso de evaluación de madurez tecnológica. La información que usted proporcione será analizada por nuestro sistema, el cual evaluará el nivel de madurez de su organización según los marcos de referencia CMMI, COBIT, ISO 25010 e ITIL.");
        }

        private void AgregarInstrucciones(XWPFDocument doc)
        {
            var titulo = doc.CreateParagraph();
            titulo.SpacingBefore = 200;
            var run = titulo.CreateRun();
            run.SetText("INSTRUCCIONES DE LLENADO");
            run.IsBold = true;
            run.FontSize = 12;
            run.FontFamily = "Calibri";
            run.SetColor(COLOR_AZUL);

            AgregarBullet(doc, "Complete todas las secciones con la información más precisa y honesta posible. La calidad del diagnóstico depende directamente de la calidad de los datos suministrados.");
            AgregarBullet(doc, "Si alguna sección no aplica a su empresa, indíquelo explícitamente con \"No aplica\" y explique brevemente por qué.");
            AgregarBullet(doc, "Use datos cuantitativos cuando sea posible (porcentajes, cantidades, fechas, métricas).");
            AgregarBullet(doc, "Una vez completado, guarde el documento como PDF (Archivo → Guardar como → Tipo PDF) y súbalo al sistema.");
            AgregarBullet(doc, "Toda la información será tratada confidencialmente y utilizada únicamente para fines del análisis.");
        }

        private void AgregarSeccion1(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "1", "INFORMACIÓN GENERAL DE LA EMPRESA");
            AgregarDescripcion(doc, "Datos básicos de identificación de la organización.");
            AgregarEspacio(doc);

            var tabla = doc.CreateTable(1, 2);
            ConfigurarTabla(tabla, ANCHO_TABLA);

            // Anchos específicos para las 2 columnas (suman ANCHO_TABLA)
            const ulong ANCHO_ETIQUETA = 2800;
            const ulong ANCHO_VALOR = 5700;

            // Primera fila (se crea automáticamente con CreateTable)
            var primeraFila = tabla.GetRow(0);
            var celdaInicial = primeraFila.GetCell(0);
            EstablecerAnchoCelda(celdaInicial, ANCHO_ETIQUETA);
            EstablecerAnchoCelda(primeraFila.GetCell(1), ANCHO_VALOR);
            celdaInicial.SetColor(COLOR_GRIS_CLARO);
            var pInicial = celdaInicial.Paragraphs[0];
            var rInicial = pInicial.CreateRun();
            rInicial.SetText("Nombre de la empresa:");
            rInicial.IsBold = true;
            rInicial.FontSize = 10;
            rInicial.FontFamily = "Calibri";

            // Resto de las filas
            AgregarFilaDato(tabla, "RIF:");
            AgregarFilaDato(tabla, "Año de fundación:");
            AgregarFilaDato(tabla, "Sede principal (ciudad, país):");
            AgregarFilaDato(tabla, "Sucursales o sedes adicionales:");
            AgregarFilaDato(tabla, "Cantidad total de empleados:");
            AgregarFilaDato(tabla, "Empleados en desarrollo de software:");
            AgregarFilaDato(tabla, "Facturación anual aproximada (USD):");
            AgregarFilaDato(tabla, "Sitio web:");
            AgregarFilaDato(tabla, "Correo de contacto:");
        }

        private void AgregarSeccion2(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "2", "NATURALEZA DEL NEGOCIO");
            AgregarDescripcion(doc, "Describa el tipo de trabajo principal que realiza la empresa y los productos o servicios que ofrece.");

            AgregarPregunta(doc, "2.1 Tipo de trabajo o servicios principales");
            AgregarHint(doc, "Describa a qué se dedica la empresa. Ejemplos: desarrollo de software a medida, mantenimiento de sistemas existentes, consultoría tecnológica, integración de plataformas, etc.");
            AgregarCajaRespuesta(doc, 4);

            AgregarPregunta(doc, "2.2 Sectores de clientes a los que atiende");
            AgregarHint(doc, "Indique los sectores en los que tiene clientes. Ejemplos: retail, salud, educación, finanzas, gobierno, manufactura, etc.");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "2.3 ¿La empresa desarrolla software propio o productos de software?");
            AgregarHint(doc, "Marque la opción que mejor describa la situación de su empresa:");
            AgregarBullet(doc, "(  ) Sí, desarrollamos productos propios");
            AgregarBullet(doc, "(  ) No, solo prestamos servicios a terceros");
            AgregarBullet(doc, "(  ) Ambos: tenemos productos propios y prestamos servicios");

            AgregarPregunta(doc, "2.4 Si desarrolla software, ¿qué tipo de software?");
            AgregarHint(doc, "Marque todas las que apliquen y describa brevemente cada caso:");
            AgregarBullet(doc, "(  ) Aplicaciones web (sitios, plataformas SaaS, portales)");
            AgregarBullet(doc, "(  ) Aplicaciones móviles (Android, iOS, multiplataforma)");
            AgregarBullet(doc, "(  ) Aplicaciones de escritorio (Windows, macOS, Linux)");
            AgregarBullet(doc, "(  ) Sistemas empresariales (ERP, CRM, gestión administrativa)");
            AgregarBullet(doc, "(  ) Software a medida según requerimientos del cliente");
            AgregarBullet(doc, "(  ) Integraciones, APIs o middleware");
            AgregarBullet(doc, "(  ) Otros (especifique):");
            AgregarCajaRespuesta(doc, 2);

            AgregarPregunta(doc, "2.5 Descripción detallada de los principales productos o servicios");
            AgregarHint(doc, "Describa con detalle los 2 o 3 productos o servicios más importantes. Incluya nombre, propósito, clientes objetivo y volumen aproximado.");
            AgregarCajaRespuesta(doc, 6);
        }

        private void AgregarSeccion3(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "3", "ESTRUCTURA ORGANIZACIONAL Y EQUIPOS");
            AgregarDescripcion(doc, "Cómo está organizado el equipo de trabajo y cómo se coordinan internamente.");

            AgregarPregunta(doc, "3.1 Organización del equipo de desarrollo");
            AgregarHint(doc, "Describa cómo está estructurado el equipo: ¿hay células, escuadras o equipos? ¿cómo se asignan los proyectos?");
            AgregarCajaRespuesta(doc, 4);

            AgregarPregunta(doc, "3.2 Roles formales presentes en la organización");
            AgregarHint(doc, "Indique qué roles existen formalmente en su empresa. Por ejemplo: Scrum Master, Product Owner, Tech Lead, QA, DevOps, Arquitecto, etc.");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "3.3 Herramientas de comunicación interna");
            AgregarHint(doc, "¿Qué herramientas usan para comunicarse entre el equipo? Ejemplos: Slack, Microsoft Teams, WhatsApp, correo, llamadas, presenciales.");
            AgregarCajaRespuesta(doc, 2);

            AgregarPregunta(doc, "3.4 Frecuencia y tipo de reuniones de coordinación");
            AgregarHint(doc, "¿Realizan reuniones diarias, semanales? ¿Hay retrospectivas? ¿Daily standups? Describa la dinámica.");
            AgregarCajaRespuesta(doc, 3);
        }

        private void AgregarSeccion4(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "4", "PROCESOS DE DESARROLLO DE SOFTWARE");
            AgregarDescripcion(doc, "Cómo se llevan a cabo los procesos técnicos de desarrollo.");

            AgregarPregunta(doc, "4.1 Sistema de control de versiones");
            AgregarHint(doc, "¿Usan Git u otro sistema? ¿Desde cuándo? ¿Dónde alojan los repositorios (GitHub, GitLab, Bitbucket, propio)?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "4.2 Metodología de desarrollo");
            AgregarHint(doc, "¿Usan Scrum, Kanban, cascada, híbrido, ad-hoc? ¿Está formalmente documentada o es informal?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "4.3 Convenciones de código y revisiones (code review)");
            AgregarHint(doc, "¿Hay estándares de codificación documentados? ¿Las revisiones de código son obligatorias, opcionales o no existen?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "4.4 Stack tecnológico principal");
            AgregarHint(doc, "Enumere los lenguajes, frameworks, bibliotecas y herramientas que más usa la empresa.");
            AgregarCajaRespuesta(doc, 4);
        }

        private void AgregarSeccion5(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "5", "INFRAESTRUCTURA TECNOLÓGICA");
            AgregarDescripcion(doc, "Cómo aloja y administra la infraestructura técnica.");

            AgregarPregunta(doc, "5.1 Ambientes de trabajo");
            AgregarHint(doc, "¿Tienen ambientes separados para desarrollo, pruebas (staging) y producción? Describa cada uno.");
            AgregarCajaRespuesta(doc, 4);

            AgregarPregunta(doc, "5.2 Alojamiento de servidores");
            AgregarHint(doc, "¿Dónde están alojados sus servidores? Ejemplos: AWS, Azure, Google Cloud, DigitalOcean, hosting compartido, máquinas propias en oficina.");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "5.3 Estrategia de respaldos");
            AgregarHint(doc, "¿Hacen respaldos? ¿Con qué frecuencia? ¿Son automáticos o manuales? ¿Se verifican periódicamente?");
            AgregarCajaRespuesta(doc, 3);
        }

        private void AgregarSeccion6(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "6", "GESTIÓN DE CALIDAD Y PRUEBAS");
            AgregarDescripcion(doc, "Cómo verifica la calidad del software que produce.");

            AgregarPregunta(doc, "6.1 Pruebas automatizadas");
            AgregarHint(doc, "¿Existen pruebas unitarias, de integración o end-to-end? ¿Cuál es el porcentaje aproximado de cobertura?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "6.2 Integración y despliegue continuo (CI/CD)");
            AgregarHint(doc, "¿Tienen pipelines automatizados? ¿Qué herramientas usan (Jenkins, GitHub Actions, GitLab CI)?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "6.3 Proceso de despliegues a producción");
            AgregarHint(doc, "¿Los despliegues son manuales o automáticos? ¿Quién los realiza? ¿Con qué frecuencia?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "6.4 Gestión de defectos e incidencias");
            AgregarHint(doc, "¿Cómo se registran los bugs reportados por clientes? ¿Usan alguna herramienta? ¿Hay un proceso definido?");
            AgregarCajaRespuesta(doc, 3);
        }

        private void AgregarSeccion7(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "7", "GESTIÓN DE PROYECTOS");
            AgregarDescripcion(doc, "Cómo planifica, ejecuta y entrega los proyectos.");

            AgregarPregunta(doc, "7.1 Levantamiento y documentación de requisitos");
            AgregarHint(doc, "¿Cómo se documentan los requisitos al inicio de un proyecto? ¿Hay especificaciones formales?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "7.2 Estimación de tiempos y esfuerzos");
            AgregarHint(doc, "¿Usan story points, function points, días-hombre? ¿O se estima por experiencia del líder?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "7.3 Cumplimiento de plazos");
            AgregarHint(doc, "¿Qué porcentaje aproximado de proyectos del último año se entregaron en el tiempo acordado?");
            AgregarCajaRespuesta(doc, 2);

            AgregarPregunta(doc, "7.4 Comunicación con clientes");
            AgregarHint(doc, "¿Cómo se mantiene informado al cliente? ¿Frecuencia, medios y formatos de reporte?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "7.5 Retrospectivas y lecciones aprendidas");
            AgregarHint(doc, "¿Al cerrar un proyecto se realizan retrospectivas formales? ¿Se documentan las lecciones aprendidas?");
            AgregarCajaRespuesta(doc, 3);
        }

        private void AgregarSeccion8(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "8", "SEGURIDAD INFORMÁTICA");
            AgregarDescripcion(doc, "Prácticas y políticas relacionadas con la seguridad de la información.");

            AgregarPregunta(doc, "8.1 Gestión de credenciales y secretos");
            AgregarHint(doc, "¿Dónde y cómo se almacenan claves, tokens y contraseñas? ¿Usan algún gestor centralizado?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "8.2 Política de accesos");
            AgregarHint(doc, "¿Cuántas personas tienen acceso a servidores de producción? ¿Hay control individual o cuentas compartidas?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "8.3 Auditorías de seguridad");
            AgregarHint(doc, "¿Se realizan auditorías o pruebas de penetración periódicas? ¿Cuentan con alguna certificación (ISO 27001, etc.)?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "8.4 Incidentes de seguridad recientes");
            AgregarHint(doc, "¿Han tenido algún incidente de seguridad en los últimos 2 años? Describa qué ocurrió y cómo se manejó.");
            AgregarCajaRespuesta(doc, 4);
        }

        private void AgregarSeccion9(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "9", "CAPACITACIÓN Y RECURSOS HUMANOS");
            AgregarDescripcion(doc, "Inversión en formación y gestión del talento humano.");

            AgregarPregunta(doc, "9.1 Programa de capacitación");
            AgregarHint(doc, "¿La empresa cuenta con un programa formal de capacitación? ¿Asigna presupuesto anual a formación?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "9.2 Plan de carrera");
            AgregarHint(doc, "¿Existe un plan de carrera definido? ¿Hay criterios claros para ascensos y promociones?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "9.3 Rotación de personal");
            AgregarHint(doc, "¿Cuál fue el porcentaje de rotación del último año? ¿Cuáles son las razones más comunes de salida?");
            AgregarCajaRespuesta(doc, 3);
        }

        private void AgregarSeccion10(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "10", "DOCUMENTACIÓN TÉCNICA");
            AgregarDescripcion(doc, "Cómo se documenta y se transfiere el conocimiento técnico.");

            AgregarPregunta(doc, "10.1 Documentación de arquitectura");
            AgregarHint(doc, "¿Qué porcentaje aproximado de los proyectos cuenta con documentación formal de arquitectura?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "10.2 Documentación de código");
            AgregarHint(doc, "¿Los archivos README de los repositorios están actualizados? ¿Existen estándares de documentación inline en el código?");
            AgregarCajaRespuesta(doc, 3);

            AgregarPregunta(doc, "10.3 Transferencia de conocimiento");
            AgregarHint(doc, "Cuando un desarrollador deja la empresa, ¿cómo se transfiere su conocimiento? ¿Hay un proceso estructurado?");
            AgregarCajaRespuesta(doc, 3);
        }

        private void AgregarSeccion11(XWPFDocument doc)
        {
            AgregarEncabezadoSeccion(doc, "11", "ÁREAS CRÍTICAS IDENTIFICADAS POR LA EMPRESA");
            AgregarDescripcion(doc, "Espacio abierto para que la empresa exponga las preocupaciones o áreas críticas que motivan esta evaluación.");

            AgregarPregunta(doc, "11.1 Principales preocupaciones operativas");
            AgregarHint(doc, "¿Qué situaciones o problemas le preocupan más en el día a día? Sea específico (ejemplo: retrasos en entregas, bugs frecuentes, dependencia de pocas personas).");
            AgregarCajaRespuesta(doc, 5);

            AgregarPregunta(doc, "11.2 Objetivos esperados de esta evaluación");
            AgregarHint(doc, "¿Qué espera obtener al final de este análisis de madurez? ¿Qué decisiones quiere tomar con base en el diagnóstico?");
            AgregarCajaRespuesta(doc, 5);

            AgregarPregunta(doc, "11.3 Información adicional relevante");
            AgregarHint(doc, "Cualquier información adicional que considere importante y que no haya quedado cubierta en las secciones anteriores.");
            AgregarCajaRespuesta(doc, 6);
        }

        private void AgregarCierre(XWPFDocument doc)
        {
            AgregarEspacio(doc);
            AgregarEspacio(doc);

            AgregarCajaInformativa(doc,
                "PRÓXIMO PASO",
                "Una vez completada esta plantilla, guarde el documento en formato PDF (Archivo → Guardar como → Tipo PDF) y súbalo a la plataforma para iniciar el análisis automatizado de madurez tecnológica.");
        }
    }
}
