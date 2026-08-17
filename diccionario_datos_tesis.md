# DICCIONARIO DE DATOS — Sistema de Evaluación de Madurez Tecnológica

---

## INSTRUCCIONES PARA CLAUDE

Este archivo contiene la información en bruto de las 5 tablas de mi base de datos.
Por favor, **formatéalo como un diccionario de datos apto para una tesis de ingeniería**
usando el siguiente formato para cada tabla:

1. Un encabezado con el nombre de la tabla.
2. Una descripción semántica de 2–3 líneas explicando el propósito de la tabla.
3. Una tabla en Markdown con columnas: **Nº, Nombre del campo, Tipo de dato, Longitud, Nulo, Descripción**.
4. Una sección "Restricciones" que enumere PK, FK, UNIQUE, valores por defecto.
5. Una sección "Observaciones" con notas relevantes (formato de fechas, semántica de valores especiales, decisiones de diseño).

Estilo:
- Usa lenguaje formal académico en español.
- Redacta descripciones completas (no telegrámaticas) para cada campo — que un lector no técnico pueda entender qué representa.
- Cuando un campo tenga valores predefinidos (enum-like), enuméralos explícitamente en la descripción.
- Mantén consistencia en la nomenclatura entre tablas.

Al final, agrega una **sección de convenciones generales** que resuma:
- Motor de BD utilizado.
- Convención de fechas.
- Convención de booleanos.
- Convención de claves primarias.

---

## CONTEXTO DEL PROYECTO

- **Sistema**: Sistema de Evaluación de Madurez Tecnológica para PYMES.
- **Autor**: Angelo Rivero.
- **Modelo de referencia**: CMMI (5 niveles de madurez, del 1-Inicial al 5-Optimizado).
- **Motor de BD**: SQLite (base de datos local embebida).
- **Framework de acceso**: `Microsoft.Data.Sqlite` (ADO.NET) sobre .NET 8.
- **Función del sistema**: analiza informes técnicos en PDF de empresas y genera un dictamen estructurado con nivel CMMI, fortalezas, debilidades, riesgos y recomendaciones. Puede operar en modo online (usando Claude Sonnet como motor de IA) o modo offline (usando un motor de reglas propio que se alimenta progresivamente del historial IA mediante un componente destilador).

---

## CONVENCIONES TÉCNICAS COMUNES

- **SQLite** utiliza afinidad de tipos ("type affinity"), no tipos rígidos. Todos los campos aquí listados usan afinidades compatibles con el sistema.
- **Fechas**: se persisten como `TEXT` en formato ISO 8601 (ej. `2026-07-24T15:30:45.1234567+00:00`). Esta convención sigue la recomendación oficial de SQLite y preserva precisión de milisegundos y zona horaria.
- **Booleanos**: se persisten como `INTEGER` con valores `0` (falso) o `1` (verdadero), ya que SQLite carece de tipo booleano nativo.
- **Claves primarias**: todas las tablas usan una PK sintética entera con `AUTOINCREMENT` para desacoplar la identidad interna de los valores de negocio.

---

## TABLA 1 — Empresas

**Propósito**: registrar cada empresa evaluada por el sistema, con sus datos identificatorios y de contacto.

### Campos

| Campo | Tipo físico (SQLite) | Nulo | Default | Descripción |
|---|---|---|---|---|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | No | (auto) | Identificador único interno de la empresa. |
| Nombre | TEXT | No | — | Razón social o nombre comercial. |
| Rif | TEXT | No | — | Registro de Información Fiscal venezolano; identificador tributario único. |
| Sector | TEXT | No | — | Rubro económico de la empresa (ej. "Desarrollo de software", "SaaS", "Consultoría"). |
| CantidadEmpleados | INTEGER | Sí | — | Número total de empleados reportados. |
| Direccion | TEXT | No | — | Dirección física completa. |
| Telefono | TEXT | Sí | — | Número de contacto. |
| FechaRegistro | TEXT (ISO 8601) | No | — | Fecha y hora en la que la empresa fue registrada por primera vez en el sistema. |

### Restricciones
- PK: `Id`
- UNIQUE: `Rif` (no puede haber dos empresas con el mismo RIF)

### Observaciones
- El sistema utiliza el RIF como clave de negocio para deduplicación al importar nuevos informes.
- El campo `Sector` es texto libre; no está normalizado en una tabla catálogo porque el análisis lo procesa por coincidencia de palabras clave.

---

## TABLA 2 — Conversaciones

**Propósito**: representar cada sesión de análisis realizada sobre un informe. Agrupa el intercambio completo de mensajes entre el usuario y la IA, y sirve como contenedor de los diagnósticos generados en esa sesión.

### Campos

| Campo | Tipo físico (SQLite) | Nulo | Default | Descripción |
|---|---|---|---|---|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | No | (auto) | Identificador único de la conversación. |
| EmpresaId | INTEGER | No | — | Referencia a la empresa evaluada en esta conversación. |
| FechaInicio | TEXT (ISO 8601) | No | — | Fecha y hora de creación de la conversación. |
| Estado | TEXT | No | — | Estado del ciclo de vida. Valores posibles: "activa", "cerrada". |
| RutaInforme | TEXT | Sí | — | Ruta absoluta al archivo PDF del informe original que originó la conversación. |

### Restricciones
- PK: `Id`
- FK: `EmpresaId` → `Empresas(Id)`

### Observaciones
- Una empresa puede tener múltiples conversaciones a lo largo del tiempo (evaluaciones periódicas o análisis de distintos informes).
- El campo `RutaInforme` permite trazabilidad al documento original, aunque el sistema no lo utiliza para reprocesar (el texto extraído se persiste en la tabla `Mensajes`).

---

## TABLA 3 — Mensajes

**Propósito**: almacenar el historial completo de la interacción entre el usuario y la IA dentro de una conversación, en orden cronológico.

### Campos

| Campo | Tipo físico (SQLite) | Nulo | Default | Descripción |
|---|---|---|---|---|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | No | (auto) | Identificador único del mensaje. |
| ConversacionId | INTEGER | No | — | Referencia a la conversación a la que pertenece el mensaje. |
| Remitente | TEXT | No | — | Autor del mensaje. Valores posibles: "Usuario", "IA". |
| Contenido | TEXT | No | — | Cuerpo textual del mensaje. Para el primer mensaje de una conversación puede ser el texto completo extraído del PDF. |
| Timestamp | TEXT (ISO 8601) | No | — | Marca temporal exacta en la que se registró el mensaje. |
| Orden | INTEGER | No | — | Posición secuencial del mensaje dentro de la conversación (1, 2, 3…). Permite ordenar el historial sin depender de la marca temporal. |

### Restricciones
- PK: `Id`
- FK: `ConversacionId` → `Conversaciones(Id)`

### Observaciones
- El campo `Orden` se usa como criterio primario de ordenamiento y `Timestamp` como criterio secundario para desempate.
- El primer mensaje de una conversación (`Orden = 1`) es tradicionalmente el emitido por la IA con el texto completo del análisis o el texto crudo extraído del PDF.

---

## TABLA 4 — Diagnosticos

**Propósito**: registrar los dictámenes estructurados generados por el sistema para cada conversación. Cada dictamen incluye el nivel CMMI evaluado y las secciones analíticas (resumen, fortalezas, debilidades, riesgos, recomendaciones).

### Campos

| Campo | Tipo físico (SQLite) | Nulo | Default | Descripción |
|---|---|---|---|---|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | No | (auto) | Identificador único del diagnóstico. |
| ConversacionId | INTEGER | No | — | Referencia a la conversación de la cual proviene el dictamen. |
| ResumenEmpresa | TEXT | Sí | — | Resumen ejecutivo de la evaluación de la empresa. |
| NivelMadurez | INTEGER | Sí | — | Nivel CMMI asignado. Valores posibles: 1 (Inicial), 2 (Gestionado), 3 (Definido), 4 (Gestionado cuantitativamente), 5 (Optimizado). |
| Fortalezas | TEXT | Sí | — | Fortalezas identificadas, en formato de lista con viñetas. |
| Debilidades | TEXT | Sí | — | Debilidades detectadas, en formato de lista con viñetas. |
| Riesgos | TEXT | Sí | — | Riesgos asociados al nivel de madurez actual. |
| Recomendaciones | TEXT | Sí | — | Recomendaciones específicas para avanzar al siguiente nivel CMMI. |
| FechaGeneracion | TEXT (ISO 8601) | No | — | Fecha y hora en la que se generó el dictamen. |
| EsFinal | INTEGER (boolean) | No | 0 | Indica si es el dictamen final de la conversación (1) o un dictamen intermedio (0). Por diseño, solo puede haber un dictamen con `EsFinal=1` por conversación. |
| Origen | TEXT | No | 'IA' | Componente que generó el dictamen. Valores posibles: "IA" (Claude Sonnet en modo online), "OFFLINE" (motor de reglas local). Esta distinción es crítica para el componente Destilador. |

### Restricciones
- PK: `Id`
- FK: `ConversacionId` → `Conversaciones(Id)`
- DEFAULT: `EsFinal = 0`, `Origen = 'IA'`

### Observaciones
- El campo `Origen` fue introducido para permitir el aprendizaje seguro del componente Destilador: este solo procesa dictámenes con `Origen='IA'`, evitando que el motor offline se retroalimente de sus propios outputs y se degrade con el tiempo.
- Un mismo `ConversacionId` puede acumular múltiples dictámenes (intermedios + un final), representando refinamientos sucesivos del análisis.
- Los campos analíticos (`Fortalezas`, `Debilidades`, `Riesgos`, `Recomendaciones`) se almacenan como texto plano con viñetas para permitir presentación flexible en la interfaz.

---

## TABLA 5 — PaquetesHeuristicos

**Propósito**: almacenar las heurísticas destiladas versionadas producidas por el componente Destilador. Cada paquete es el resultado de procesar un corpus de dictámenes generados por la IA e incluye indicadores léxicos por nivel CMMI y recomendaciones recurrentes que enriquecen al motor offline.

### Campos

| Campo | Tipo físico (SQLite) | Nulo | Default | Descripción |
|---|---|---|---|---|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | No | (auto) | Identificador único del paquete. |
| Version | INTEGER | No | — | Número de versión del paquete, incremental (v1, v2, v3…). |
| FechaGeneracion | TEXT (ISO 8601) | No | — | Fecha y hora de la corrida del destilador que produjo este paquete. |
| NumDictamenes | INTEGER | No | — | Cantidad de dictámenes IA que formaron el corpus de esta destilación. |
| HashCorpus | TEXT | No | — | Firma SHA-256 abreviada (16 caracteres hex) del corpus utilizado. Permite detectar si dos corridas partieron del mismo conjunto de datos. |
| Estado | TEXT | No | — | Ciclo de vida del paquete. Valores posibles: "candidato" (recién generado, en evaluación), "activo" (en uso por el motor offline), "retirado" (reemplazado por una versión posterior). |
| ContenidoJson | TEXT | No | — | Serialización JSON del contenido del paquete: lista de indicadores destilados (término, nivel, peso, soporte) y lista de recomendaciones destiladas (nivel, texto, frecuencia). |
| ExactitudBase | REAL | Sí | — | Exactitud del motor offline sin las heurísticas del paquete, medida sobre el conjunto de validación. |
| ExactitudDestilada | REAL | Sí | — | Exactitud del motor offline con las heurísticas del paquete aplicadas. |
| F1MacroBase | REAL | Sí | — | Métrica F1 macro (multi-clase) del motor offline sin destilar. |
| F1MacroDestilada | REAL | Sí | — | Métrica F1 macro (multi-clase) del motor offline con las heurísticas aplicadas. |
| MetricasJson | TEXT | Sí | — | Campo reservado para métricas adicionales en formato JSON (matriz de confusión, precisión/recall por clase, etc.). |

### Restricciones
- PK: `Id`
- Relación N:M con `Diagnosticos` a través de la tabla intermedia `PaqueteDictamen` (ver Tabla 6).

### Observaciones
- **Ciclo de vida garantizado**: solo puede existir un paquete con `Estado='activo'` en cualquier momento; la promoción de un nuevo paquete a activo se realiza en una transacción atómica que retira automáticamente el anterior.
- **Criterio de promoción**: un paquete recién generado permanece como `candidato` y solo se promueve a `activo` si `ExactitudDestilada >= ExactitudBase` en el conjunto de validación, evitando degradaciones del motor offline.
- **Trazabilidad al corpus**: los dictámenes específicos usados en cada destilación se registran en la tabla intermedia `PaqueteDictamen`. Adicionalmente, el campo `HashCorpus` guarda una firma criptográfica del conjunto para verificaciones rápidas.
- El campo `ContenidoJson` deserializa a dos listas:
  - `Indicadores`: `[{ Termino, Nivel, Peso, Soporte }]`
  - `Recomendaciones`: `[{ Nivel, Texto, Frecuencia }]`

---

## TABLA 6 — PaqueteDictamen

**Propósito**: tabla intermedia que resuelve la relación muchos-a-muchos entre paquetes heurísticos y diagnósticos. Registra qué dictámenes específicos formaron parte del corpus de cada destilación, garantizando el cumplimiento estricto del modelo relacional (ninguna tabla huérfana) y aportando trazabilidad bidireccional del proceso de aprendizaje.

### Campos

| Campo | Tipo físico (SQLite) | Nulo | Default | Descripción |
|---|---|---|---|---|
| PaqueteId | INTEGER | No | — | Referencia al paquete heurístico. |
| DiagnosticoId | INTEGER | No | — | Referencia al dictamen que contribuyó a la destilación de dicho paquete. |

### Restricciones
- PK compuesta: (`PaqueteId`, `DiagnosticoId`) — impide duplicados en la relación.
- FK: `PaqueteId` → `PaquetesHeuristicos(Id)`
- FK: `DiagnosticoId` → `Diagnosticos(Id)`

### Observaciones
- **Justificación relacional**: dado que un paquete se destila de N dictámenes y un dictamen puede contribuir a M paquetes futuros, la relación es intrínsecamente N:M. El patrón estándar del modelo relacional para representar N:M es la tabla intermedia (o "asociativa"), evitando así grupos repetitivos y cumpliendo con la 1ª Forma Normal.
- **Poblado atómico**: al persistir un nuevo paquete, sus filas en `PaqueteDictamen` se insertan dentro de la misma transacción SQL que la inserción del paquete, garantizando consistencia referencial.
- **Consultas de trazabilidad habilitadas** (ejemplos):
  - *¿Qué dictámenes formaron el paquete v3?* — `SELECT d.* FROM Diagnosticos d JOIN PaqueteDictamen pd ON pd.DiagnosticoId = d.Id WHERE pd.PaqueteId = 3;`
  - *¿En qué paquetes contribuyó el dictamen #42?* — `SELECT p.Version FROM PaquetesHeuristicos p JOIN PaqueteDictamen pd ON pd.PaqueteId = p.Id WHERE pd.DiagnosticoId = 42;`

---

## RELACIONES ENTRE TABLAS

```
Empresas (1) ──────< (N) Conversaciones (1) ──────< (N) Mensajes
                                       │
                                       └──< (N) Diagnosticos
                                                     │
                                                     [Origen: 'IA' | 'OFFLINE']
                                                     │
                                                     │ (solo Origen='IA')
                                                     ▼
                                             PaqueteDictamen  (tabla intermedia N:M)
                                                     ▲
                                                     │
                                             PaquetesHeuristicos
```

### Detalle de cardinalidades

| Relación | Cardinalidad | Tipo |
|---|---|---|
| Empresas → Conversaciones | 1:N | FK física |
| Conversaciones → Mensajes | 1:N | FK física |
| Conversaciones → Diagnosticos | 1:N | FK física |
| Diagnosticos ↔ PaquetesHeuristicos | N:M | Vía `PaqueteDictamen` |
| PaqueteDictamen → PaquetesHeuristicos | N:1 | FK física |
| PaqueteDictamen → Diagnosticos | N:1 | FK física |

---

## FORMATO DE SALIDA SUGERIDO PARA LA TESIS

Por favor formatea el diccionario final aplicando este esquema para cada tabla:

1. **Encabezado**: `Tabla N.N — Nombre de la tabla` con numeración jerárquica (ej. "Tabla 4.1 — Empresas").
2. **Descripción textual** en un párrafo breve.
3. **Tabla de campos** con columnas: `Nº | Nombre del atributo | Tipo de dato | Longitud | Nulo (S/N) | Descripción`.
4. **Restricciones y relaciones** como lista enumerada.
5. **Observaciones adicionales** cuando existan decisiones de diseño relevantes.

Si el usuario lo pide, genera también:
- Un párrafo introductorio que sitúe el diccionario dentro del capítulo de diseño de base de datos.
- Un diagrama textual o descripción en prosa del modelo entidad-relación.
