-- ============================================================================
-- Diagrama Entidad-Relación · Sistema de Evaluación de Madurez Tecnológica
-- Autor: Angelo Rivero
-- Motor: SQLite (base de datos local del sistema)
--
-- Instrucciones para DrawSQL:
--   1. Abre https://drawsql.app/ y crea un nuevo diagrama (New Database Diagram).
--   2. Elige "Import from SQL".
--   3. Selecciona el dialecto "MySQL" (compatible con esta sintaxis).
--   4. Pega TODO este archivo y confirma la importación.
--   5. DrawSQL detectará las claves foráneas y dibujará las relaciones.
-- ============================================================================


-- ============================================================
-- ENTIDAD PRINCIPAL: Empresas
-- Cada empresa evaluada por el sistema.
-- ============================================================
CREATE TABLE `Empresas` (
    `Id`                 INT           NOT NULL AUTO_INCREMENT,
    `Nombre`             VARCHAR(200)  NOT NULL,
    `Rif`                VARCHAR(30)   NOT NULL UNIQUE,
    `Sector`             VARCHAR(200)  NOT NULL,
    `CantidadEmpleados`  INT           NOT NULL,
    `Direccion`          VARCHAR(300)  NOT NULL,
    `Telefono`           VARCHAR(30),
    `FechaRegistro`      DATETIME      NOT NULL,
    PRIMARY KEY (`Id`)
);


-- ============================================================
-- Conversaciones: cada análisis genera una conversación IA-usuario.
-- Relación N:1 con Empresas (una empresa tiene muchas conversaciones).
-- ============================================================
CREATE TABLE `Conversaciones` (
    `Id`            INT           NOT NULL AUTO_INCREMENT,
    `EmpresaId`     INT           NOT NULL,
    `FechaInicio`   DATETIME      NOT NULL,
    `Estado`        VARCHAR(20)   NOT NULL,
    `RutaInforme`   VARCHAR(500),
    PRIMARY KEY (`Id`),
    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas`(`Id`)
);


-- ============================================================
-- Mensajes: historial de la conversación (usuario ↔ IA).
-- Relación N:1 con Conversaciones.
-- ============================================================
CREATE TABLE `Mensajes` (
    `Id`               INT            NOT NULL AUTO_INCREMENT,
    `ConversacionId`   INT            NOT NULL,
    `Remitente`        VARCHAR(20)    NOT NULL,
    `Contenido`        TEXT           NOT NULL,
    `Timestamp`        DATETIME       NOT NULL,
    `Orden`            INT            NOT NULL,
    PRIMARY KEY (`Id`),
    FOREIGN KEY (`ConversacionId`) REFERENCES `Conversaciones`(`Id`)
);


-- ============================================================
-- Diagnosticos: dictamen estructurado (nivel CMMI + secciones).
-- Relación N:1 con Conversaciones.
-- La columna Origen distingue quién generó el dictamen:
--   'IA'      → Claude Sonnet (usado por el Destilador para aprender)
--   'OFFLINE' → MotorOffline (NO se usa para destilar, evita bucle degradante)
-- ============================================================
CREATE TABLE `Diagnosticos` (
    `Id`                INT            NOT NULL AUTO_INCREMENT,
    `ConversacionId`    INT            NOT NULL,
    `ResumenEmpresa`    TEXT,
    `NivelMadurez`      INT,
    `Fortalezas`        TEXT,
    `Debilidades`       TEXT,
    `Riesgos`           TEXT,
    `Recomendaciones`   TEXT,
    `FechaGeneracion`   DATETIME       NOT NULL,
    `EsFinal`           TINYINT(1)     NOT NULL DEFAULT 0,
    `Origen`            VARCHAR(10)    NOT NULL DEFAULT 'IA',
    PRIMARY KEY (`Id`),
    FOREIGN KEY (`ConversacionId`) REFERENCES `Conversaciones`(`Id`)
);


-- ============================================================
-- PaquetesHeuristicos: heurísticas destiladas por el componente Destilador
-- a partir del corpus de dictámenes de la IA.
-- Cada corrida del destilador genera una nueva versión con su métrica de
-- validación antes/después.
-- ============================================================
CREATE TABLE `PaquetesHeuristicos` (
    `Id`                    INT           NOT NULL AUTO_INCREMENT,
    `Version`               INT           NOT NULL,
    `FechaGeneracion`       DATETIME      NOT NULL,
    `NumDictamenes`         INT           NOT NULL,
    `HashCorpus`            VARCHAR(64)   NOT NULL,
    `Estado`                VARCHAR(20)   NOT NULL,
    `ContenidoJson`         TEXT          NOT NULL,
    `ExactitudBase`         DOUBLE,
    `ExactitudDestilada`    DOUBLE,
    `F1MacroBase`           DOUBLE,
    `F1MacroDestilada`      DOUBLE,
    `MetricasJson`          TEXT,
    PRIMARY KEY (`Id`)
);


-- ============================================================
-- PaqueteDictamen: tabla intermedia N:M que conecta cada paquete
-- heurístico con los dictámenes IA que lo formaron.
-- Cumple con el modelo relacional estricto: no hay tablas huérfanas.
-- Permite trazabilidad bidireccional para auditoría del proceso de destilación:
--   - ¿Qué dictámenes formaron el paquete v3?
--   - ¿En qué paquetes se usó el dictamen #42?
-- ============================================================
CREATE TABLE `PaqueteDictamen` (
    `PaqueteId`      INT NOT NULL,
    `DiagnosticoId`  INT NOT NULL,
    PRIMARY KEY (`PaqueteId`, `DiagnosticoId`),
    FOREIGN KEY (`PaqueteId`)     REFERENCES `PaquetesHeuristicos`(`Id`),
    FOREIGN KEY (`DiagnosticoId`) REFERENCES `Diagnosticos`(`Id`)
);
