# PRD: Tab "Proyectos"

## Overview

Agregar un nuevo tab llamado **"Proyectos"** a la aplicación AccesosLauncher que permita organizar accesos en grupos temáticos llamados "proyectos". Cada proyecto contendrá su propia colección de accesos (archivos, carpetas, URLs) configurable e independiente.

---

## Objetivos

1. Permitir al usuario crear, editar y gestionar múltiples proyectos con accesos relacionados
2. Mantener historial de uso de proyectos ordenado por última vez accedido
3. Proveer una interfaz visual similar al tab "Accesos" pero aislada por proyecto
4. Persistir toda la información en la base de datos SQLite existente con soporte de migraciones versionadas

---

## Requerimientos Funcionales

### RF-001: Visualización del Tab Proyectos

**Descripción:** El tab "Proyectos" debe aparecer como un nuevo TabItem en el MainTabControl, ubicado inmediatamente después del tab "Accesos" (índice 1).

**Criterios de Aceptación:**
- [ ] El tab debe tener el header "Proyectos"
- [ ] Debe estar posicionado entre "Accesos" y "Más Usados"
- [ ] Debe ser accesible vía navegación con teclado (Tab key)
- [ ] Al no haber proyectos creados, el área principal muestra un **empty state** con el mensaje "No tenés proyectos aún" y un botón CTA "Crear tu primer proyecto"

---

### RF-002: Búsqueda de Proyectos

**Descripción:** El tab debe contener un InputBox/TextBox en la parte superior para buscar proyectos por nombre o descripción, acompañado de un checkbox para filtrar por estado activo.

**Criterios de Aceptación:**
- [ ] TextBox con placeholder "Buscar proyecto..."
- [ ] Checkbox "Solo activos" ubicado al lado del TextBox (izquierda)
- [ ] Checkbox marcado por default
- [ ] Búsqueda en tiempo real (UpdateSourceTrigger=PropertyChanged)
- [ ] **Soporte de comodín `%`:** El carácter `%` actúa como separador de términos. La búsqueda divide el texto por `%` y verifica que **TODOS** los términos estén presentes (match parcial, case-insensitive).
  - Ejemplo: `cliente%facturación` encuentra proyectos que contengan ambas palabras ("Cliente XYZ - Facturación 2026")
  - Ejemplo: `auth%login` encuentra "Authentication Module - Login Screen"
- [ ] Filtra por nombre (match parcial, case-insensitive, con soporte de `%`)
- [ ] Filtra por descripción corta (match parcial, case-insensitive, con soporte de `%`)
- [ ] Cuando "Solo activos" está marcado, filtra proyectos con `Activo = 'S'`
- [ ] Al limpiar el search, muestra todos los proyectos (respetando filtro de activos)
- [ ] Al desmarcar "Solo activos", muestra proyectos activos e inactivos
- [ ] La búsqueda debe soportar caracteres especiales, acentos y ñ

---

### RF-003: Grilla de Proyectos

**Descripción:** Una grilla/DataGrid que muestre los proyectos disponibles con columnas específicas.

**Columnas Requeridas:**

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Nombre | Texto | Nombre del proyecto |
| Activo | Ícono | Estado del proyecto: ícono ✓ para "S", vacío para "N" |
| Descripción | Texto | Descripción corta (máx 50 caracteres visibles, truncado con "...") |
| Último Acceso | DateTime | Fecha y hora de última vez que se seleccionó el proyecto |

> **Decisión de diseño:** La columna "Activo" muestra ✓ para activo y **celda vacía** para inactivo (no cruz). Se descartó la cruz para no generar sensación de error en proyectos simplemente pausados.

**Criterios de Aceptación:**
- [ ] DataGrid con las 4 columnas especificadas
- [ ] Orden de columnas: Nombre (1°), Activo (2°), Descripción (3°), Último Acceso (4°)
- [ ] Columna "Activo": ícono ✓ cuando `Activo = 'S'`, celda vacía cuando `Activo = 'N'`
- [ ] Ordenamiento default por "Último Acceso" descendente (más reciente primero)
- [ ] AllowSorting=true en todas las columnas
- [ ] SelectionMode=Single
- [ ] AutoGenerateColumns=false
- [ ] CanUserAddRows=false
- [ ] IsReadOnly=true (la grilla es solo visualización)
- [ ] Click en una fila selecciona el proyecto y actualiza el panel de detalles

---

### RF-004: Panel de Descripción Larga

**Descripción:** La mitad izquierda del área principal del tab muestra un RichTextBox con la descripción detallada del proyecto seleccionado. El split es de **60% descripción / 40% accesos** por defecto, ajustable por el usuario mediante un GridSplitter.

> **Decisión de diseño:** Se reemplaza el split fijo 50/50 por un GridSplitter arrastrable. El valor inicial es 60/40 dado que la descripción larga suele ser el contenido más relevante. El usuario puede ajustarlo libremente y **la proporción se persiste** entre sesiones en la configuración del usuario.

**Criterios de Aceptación:**
- [ ] RichTextBox editable solo en modo edición
- [ ] Muestra descripción larga completa del proyecto
- [ ] Soporta formato RTF básico (negrita, listas, etc.)
- [ ] Scroll vertical automático si el contenido excede el espacio
- [ ] Cuando no hay proyecto seleccionado, mostrar mensaje: "Seleccione un proyecto para ver su descripción"
- [ ] GridSplitter visible y funcional entre ambos paneles
- [ ] Ancho mínimo de cada panel: 200px (para evitar colapso accidental)
- [ ] **Persistencia de proporción:** El valor del split (en pixels o porcentaje) se guarda en `usersettings.json` al cerrar la app o cuando el usuario deja de arrastrar el splitter
- [ ] Al iniciar la app, se restaura la última proporción guardada
- [ ] Si no hay configuración previa, usa el default 60/40

---

### RF-005: Edición de Descripción

**Descripción:** Un ícono flotante de lápiz debe permitir habilitar/deshabilitar el modo edición de la descripción larga.

**Criterios de Aceptación:**
- [ ] Ícono de lápiz visible en la esquina superior derecha del panel de descripción
- [ ] Al hacer click, toggle entre modo lectura/edición
- [ ] En modo edición:
  - RichTextBox se vuelve editable
  - Aparecen botones "Guardar" y "Cancelar"
  - Ícono cambia a "check" para guardar
- [ ] Al guardar:
  - Actualiza la base de datos
  - Vuelve a modo lectura
  - Muestra toast de confirmación
- [ ] Al cancelar:
  - Descarta cambios
  - Recupera texto original
  - Vuelve a modo lectura

---

### RF-006: Panel de Accesos del Proyecto

**Descripción:** La mitad derecha del área principal muestra los accesos del proyecto seleccionado, organizados como íconos similares al tab "Accesos".

**Criterios de Aceptación:**
- [ ] Layout similar al tab "Accesos" (WrapPanel o ItemsControl)
- [ ] Cada ícono muestra:
  - Ícono del archivo/carpeta/URL
  - Nombre debajo (máx 2 líneas, text-wrapping)
- [ ] Si el `acceso_full_path` no existe en disco al momento de renderizar, el ícono muestra una superposición de advertencia (⚠) y el tooltip indica "Archivo no encontrado: {path}"
- [ ] Click en ícono válido ejecuta el acceso (mismo comportamiento que tab Accesos)
- [ ] Click en ícono con path inválido muestra MessageBox: "El archivo '{nombre}' no se encontró en '{path}'. ¿Deseas eliminarlo del proyecto?"
- [ ] Right-click muestra context menu con:
  - Renombrar
  - Eliminar (solo del proyecto, no el archivo original)
  - Abrir ubicación del archivo (deshabilitado si el path no existe)
- [ ] Cuando no hay proyecto seleccionado, mostrar mensaje: "Seleccione un proyecto"
- [ ] Si el proyecto no tiene accesos, mostrar mensaje: "Este proyecto no tiene accesos. Usá el botón + para agregar."

---

### RF-007: Reordenamiento de Accesos

**Descripción:** Los íconos de accesos dentro de un proyecto deben poder arrastrarse y reordenarse.

**Criterios de Aceptación:**
- [ ] Drag & Drop entre íconos del mismo proyecto
- [ ] El reordenamiento es una **transacción atómica**: se actualizan todos los valores de `orden` del proyecto en una única transacción SQLite. Si falla, se revierte al orden anterior.
- [ ] Animación visual durante el drag
- [ ] Drop indicator muestra dónde se insertará el ítem
- [ ] El orden se mantiene al recargar la aplicación

---

### RF-008: Agregar Nuevos Accesos

**Descripción:** Un ícono flotante de "+" debe permitir agregar nuevos accesos al proyecto seleccionado. Soporta tanto archivos/carpetas como URLs.

**Criterios de Aceptación:**
- [ ] Ícono "+" visible en la esquina superior derecha del panel de accesos
- [ ] Al hacer click, muestra un menú con dos opciones:
  - **"Archivo / Carpeta"** → abre OpenFileDialog multiselección
  - **"URL"** → abre un dialog simple con un TextBox para ingresar la URL y un TextBox para el nombre
- [ ] OpenFileDialog permite seleccionar:
  - Archivos ejecutables (.exe, .bat, .cmd, .ps1)
  - Accesos directos (.lnk, .url)
  - Carpetas
- [ ] **Validación de URL robusta:** Se usa `Uri.TryCreate()` en C# para validar el formato de la URL. Además se verifica que el esquema sea `http://`, `https://` o `ftp://`. Si la validación falla, muestra error inline en el dialog (no MessageBox).
- [ ] Al confirmar (archivo o URL), agrega los accesos al proyecto seleccionado
- [ ] Asigna el siguiente `orden` disponible (MAX(orden) + 1)
- [ ] Muestra toast de confirmación con cantidad de ítems agregados
- [ ] Si no hay proyecto seleccionado, mostrar mensaje: "Primero seleccione un proyecto"
- [ ] Duplicados: si el `acceso_full_path` ya existe en el proyecto, ignorarlo silenciosamente e incluirlo en el conteo del toast como "(N ya existían)"

---

### RF-009: Persistencia en Base de Datos

**Descripción:** Toda la información de proyectos y sus accesos debe guardarse en la base de datos SQLite `accesos_launcher.db`, con un sistema de migraciones versionadas.

#### Sistema de Migraciones

```sql
CREATE TABLE IF NOT EXISTS SchemaVersion (
    version     INTEGER PRIMARY KEY,
    descripcion TEXT NOT NULL,
    aplicada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

Cada migration se identifica con un número incremental. Al iniciar la app, se ejecutan en orden todas las migrations cuya `version` no esté registrada en `SchemaVersion`. **Nunca se modifican migrations ya aplicadas.**

| Version | Descripción |
|---------|-------------|
| 1 | Creación inicial de tablas Proyecto y ProyectoAcceso |

#### Tabla: `Proyecto`
```sql
CREATE TABLE IF NOT EXISTS Proyecto (
    id                   INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre               TEXT NOT NULL UNIQUE,
    descripcion_corta    TEXT NOT NULL,
    descripcion_larga    TEXT,
    activo               TEXT NOT NULL DEFAULT 'S' CHECK(activo IN ('S', 'N')),
    fecha_creacion       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_ultimo_acceso  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_eliminacion    DATETIME
);
```

> **Nota:** El campo `activo` usa `TEXT 'S'/'N'` para mantener consistencia con el esquema existente de la aplicación (tabla `Accesos`). No usar BOOLEAN.
> 
> **Soft Delete:** El campo `fecha_eliminacion` permite recuperación de proyectos eliminados. Si es `NULL`, el proyecto está activo. Si tiene valor, el proyecto está eliminado (pero no borrado de la DB).

#### Tabla: `ProyectoAcceso`
```sql
CREATE TABLE IF NOT EXISTS ProyectoAcceso (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    proyecto_id      INTEGER NOT NULL,
    orden            INTEGER NOT NULL DEFAULT 0,
    acceso_full_path TEXT NOT NULL,
    acceso_nombre    TEXT NOT NULL,
    acceso_tipo      TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url')),
    fecha_eliminacion DATETIME,
    FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id),
    UNIQUE(proyecto_id, acceso_full_path)
);
```

> **Soft Delete:** El campo `fecha_eliminacion` permite recuperación de accesos eliminados. Si es `NULL`, el acceso está activo. Al eliminar un proyecto, se marcan todos sus accesos con `fecha_eliminacion` (no se borran en cascada).

**Índices:**
```sql
CREATE INDEX IF NOT EXISTS IX_ProyectoAcceso_orden
    ON ProyectoAcceso(proyecto_id, orden);

CREATE INDEX IF NOT EXISTS IX_Proyecto_fecha_ultimo_acceso
    ON Proyecto(fecha_ultimo_acceso DESC);

CREATE INDEX IF NOT EXISTS IX_Proyecto_activo_fecha_ultimo_acceso
    ON Proyecto(activo DESC, fecha_ultimo_acceso DESC);

-- Índices compuestos para optimizar búsquedas
CREATE INDEX IF NOT EXISTS IX_Proyecto_busqueda
    ON Proyecto(nombre, descripcion_corta);

CREATE INDEX IF NOT EXISTS IX_Proyecto_busqueda_activo
    ON Proyecto(activo, nombre, descripcion_corta);

-- Índice para soft delete (consultas de proyectos no eliminados)
CREATE INDEX IF NOT EXISTS IX_Proyecto_no_eliminado
    ON Proyecto(fecha_eliminacion) WHERE fecha_eliminacion IS NULL;

CREATE INDEX IF NOT EXISTS IX_ProyectoAcceso_no_eliminado
    ON ProyectoAcceso(fecha_eliminacion) WHERE fecha_eliminacion IS NULL;
```

**Criterios de Aceptación:**
- [ ] Las tablas se crean automáticamente al iniciar la aplicación via sistema de migrations
- [ ] La tabla `SchemaVersion` se crea antes que cualquier otra
- [ ] CRUD completo para Proyecto (Create, Read, Update, Delete)
- [ ] CRUD completo para ProyectoAcceso
- [ ] **Soft Delete:** Al eliminar un proyecto, se setea `fecha_eliminacion = CURRENT_TIMESTAMP` en el proyecto y todos sus accesos (no se borran físicamente)
- [ ] Las consultas por defecto filtran `WHERE fecha_eliminacion IS NULL` (solo registros activos)
- [ ] Actualiza `fecha_ultimo_acceso` al seleccionar un proyecto
- [ ] **Backup único:** Antes de ejecutar el plan de migraciones, se hace **un solo backup** del `.db` (copia a `accesos_launcher.db.bak`). Si una migration falla, se restaura el backup.

---

### RF-010: Creación de Proyectos

**Descripción:** Debe existir un mecanismo para crear nuevos proyectos desde la UI.

**Criterios de Aceptación:**
- [ ] Botón "Nuevo Proyecto" visible en la parte superior del tab
- [ ] Al hacer click, abre modal/dialog con:
  - TextBox: Nombre (requerido, único)
  - TextBox: Descripción corta (requerido, máx 200 caracteres, muestra contador)
  - RichTextBox: Descripción larga (opcional)
- [ ] Validaciones:
  - Nombre no vacío
  - Nombre único (case-insensitive, validado contra DB)
  - Descripción corta no vacía
- [ ] Al guardar:
  - Inserta en tabla `Proyecto` con `activo = 'S'` (todos los proyectos nacen activos)
  - Cierra el modal
  - Selecciona el nuevo proyecto automáticamente en la grilla
  - Muestra toast de confirmación

---

### RF-011: Eliminación de Proyectos

**Descripción:** Debe poder eliminarse un proyecto existente (soft delete).

**Criterios de Aceptación:**
- [ ] Botón "Eliminar" visible cuando hay proyecto seleccionado
- [ ] Al hacer click, muestra confirmación: "¿Está seguro que desea eliminar el proyecto '{nombre}'? Esta acción marcará el proyecto y todos sus accesos como eliminados (podrán recuperarse)."
- [ ] Al confirmar:
  - Setea `fecha_eliminacion = CURRENT_TIMESTAMP` en el proyecto y todos sus accesos (soft delete)
  - Limpia la selección
  - Refresca la grilla (el proyecto desaparece si el filtro "Solo activos" está marcado)
  - Muestra toast de confirmación
- [ ] La eliminación es reversible (se puede implementar recuperación en el futuro)

---

### RF-012: Renombrar Acceso del Proyecto

**Descripción:** El usuario puede cambiar el nombre visible de un acceso dentro de un proyecto, sin afectar el archivo original ni el `acceso_full_path`.

**Criterios de Aceptación:**
- [ ] Accesible vía "Renombrar" en el context menu (RF-006)
- [ ] Muestra un TextBox inline o un pequeño dialog con el nombre actual pre-cargado
- [ ] Validaciones:
  - Nombre no vacío
  - Máximo 100 caracteres
- [ ] Al confirmar, actualiza `acceso_nombre` en `ProyectoAcceso`
- [ ] Al cancelar (Escape o botón Cancelar), no modifica nada
- [ ] Muestra toast de confirmación al guardar

---

### RF-013: Editar Proyecto (Nombre y Descripción Corta)

**Descripción:** El usuario puede modificar el nombre y la descripción corta de un proyecto existente.

**Criterios de Aceptación:**
- [ ] Botón "Editar" visible cuando hay proyecto seleccionado (o accesible desde context menu en la grilla)
- [ ] Abre el mismo modal que RF-010 pero con los campos pre-cargados
- [ ] Validaciones idénticas a RF-010
- [ ] Si el nombre cambió, verificar unicidad excluyendo el proyecto actual
- [ ] Al guardar:
  - Actualiza `nombre` y `descripcion_corta` en `Proyecto`
  - Refresca la grilla
  - Mantiene el proyecto seleccionado
  - Muestra toast de confirmación

---

### RF-014: Activar / Desactivar Proyecto

**Descripción:** El usuario puede cambiar el estado `activo` de un proyecto sin eliminarlo.

**Criterios de Aceptación:**
- [ ] Botón "Desactivar" visible cuando el proyecto seleccionado está activo (`Activo = 'S'`)
- [ ] Botón "Activar" visible cuando el proyecto seleccionado está inactivo (`Activo = 'N'`)
- [ ] No requiere confirmación (la acción es reversible)
- [ ] Al ejecutar:
  - Actualiza `activo` en `Proyecto`
  - Refresca la grilla
  - Si el filtro "Solo activos" está marcado y se desactivó el proyecto, el proyecto desaparece de la grilla y se limpia la selección
  - Muestra toast de confirmación

---

## Requerimientos No Funcionales

### RNF-001: Performance

- [ ] Carga inicial del tab < 500ms (con hasta 200 proyectos)
- [ ] Respuesta de búsqueda < 100ms (debounce de 150ms sobre el TextBox para no saturar)
- [ ] El WrapPanel de accesos no debe paginar para hasta **100 accesos por proyecto**; por encima de 100, mostrar aviso "Este proyecto tiene muchos accesos. Considerar dividirlo."
- [ ] Los íconos deben cargarse asíncronamente sin bloquear la UI

### RNF-002: Usabilidad

- [ ] Feedback visual (toasts) para todas las operaciones exitosas y fallidas
- [ ] Todos los dialogs/modals deben cerrarse con Escape
- [ ] Operaciones destructivas (eliminar) requieren confirmación explícita

### RNF-003: Manejo de Errores

- [ ] Todos los errores de base de datos deben loguearse y mostrarse al usuario con mensaje comprensible (no stack trace)
- [ ] Operaciones de I/O deben ser asíncronas
- [ ] La UI no debe congelarse durante operaciones de DB
- [ ] Si falla el backup de DB antes de una migration, la migration **no se ejecuta** y se muestra error al usuario

### RNF-004: Compatibilidad

- [ ] Mantener compatibilidad con Windows 10/11
- [ ] No romper funcionalidad existente del tab "Accesos"
- [ ] La base de datos debe ser backward compatible (via sistema de migrations)

---

## Modelo de Datos

### Clases C# Propuestas

```csharp
public class Proyecto : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string DescripcionCorta { get; set; }
    public string DescripcionLarga { get; set; }

    // IMPORTANTE: Usar string "S"/"N" para consistencia con el resto de la DB.
    // No cambiar a bool aunque parezca más natural en C#.
    public string Activo { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaUltimoAcceso { get; set; }
    public DateTime? FechaEliminacion { get; set; }  // NULL = no eliminado

    // Propiedad calculada para la grilla
    public string DescripcionCortaTruncada =>
        DescripcionCorta?.Length > 50
            ? DescripcionCorta.Substring(0, 47) + "..."
            : DescripcionCorta;

    // Propiedad calculada para mostrar ícono en grilla
    public string ActivoIcono => Activo == "S" ? "✓" : string.Empty;
    
    // Propiedad calculada para soft delete
    public bool EstaEliminado => FechaEliminacion.HasValue;
}

public class ProyectoAcceso : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int Orden { get; set; }
    public string AccesoFullPath { get; set; }
    public string AccesoNombre { get; set; }
    public string AccesoTipo { get; set; } // "File", "Folder", "Url"
    public ImageSource Icon { get; set; }   // Calculado vía IconHelper
    public DateTime? FechaEliminacion { get; set; }  // NULL = no eliminado

    // Calculado al cargar, no persiste en DB
    public bool PathExiste =>
        AccesoTipo == "Url" || System.IO.File.Exists(AccesoFullPath) || System.IO.Directory.Exists(AccesoFullPath);
    
    // Propiedad calculada para soft delete
    public bool EstaEliminado => FechaEliminacion.HasValue;
}

// Configuración de usuario que se persiste en usersettings.json
public class UserSettings
{
    // Tipo de carpeta seleccionado en el tab Accesos (existente)
    public TipoCarpeta SelectedTipoCarpeta { get; set; } = TipoCarpeta.Laboral;
    
    // Proporción del GridSplitter en el tab Proyectos (nuevo en v2.0)
    // Valor entre 0.0 y 1.0. Default 0.6 = 60% descripción, 40% accesos
    public double ProyectosSplitProportion { get; set; } = 0.6;
}
```

---

## Arquitectura de Implementación

### Decisión Arquitectónica: Code-Behind

Se mantiene el patrón **Code-Behind** existente (DataContext = this en MainWindow) para consistencia con el resto de la aplicación. Esta es una **decisión consciente de consistencia**, no la recomendación arquitectónica óptima.

> **Trade-off documentado:** Un `ProyectosViewModel` separado mejoraría la separabilidad y testeabilidad, pero implicaría refactorizar el tab "Accesos" también para mantener consistencia, lo que está fuera del scope de esta feature. Si en el futuro se decide migrar a MVVM, los modelos (`Proyecto.cs`, `ProyectoAcceso.cs`) ya están separados y la migración sería incremental.

### Archivos a Modificar

| Archivo | Cambios |
|---------|---------|
| `MainWindow.xaml` | Agregar TabItem "Proyectos", definir UI del nuevo tab con GridSplitter |
| `MainWindow.xaml.cs` | Agregar lógica del tab, propiedades, comandos |
| `DatabaseHelper.cs` | Extender con métodos para Proyecto y ProyectoAcceso, agregar sistema de migrations |
| `AppItem.cs` | Posiblemente refactorizar para compartir lógica con ProyectoAcceso |

### Nuevos Archivos

| Archivo | Propósito |
|---------|-----------|
| `Proyecto.cs` | Modelo de datos para Proyecto |
| `ProyectoAcceso.cs` | Modelo de datos para ProyectoAcceso |
| `ProyectoDatabaseHelper.cs` (opcional) | Si se prefiere separar la lógica de DB de proyectos |
| `MigrationRunner.cs` | Clase responsable de ejecutar el sistema de migrations |

### Patrón de Implementación

1. **DataContext = this** (MainWindow)
2. **ObservableCollection** para binding
3. **INotifyPropertyChanged** en modelos
4. **Event handlers** directos en MainWindow
5. **IconHelper** existente para íconos
6. **DatabaseHelper** extendido para nuevas tablas
7. **MigrationRunner** para versionado del schema

---

## Historias de Usuario

### HU-001: Ver Lista de Proyectos

**Como** usuario  
**Quiero** ver una lista de todos mis proyectos ordenados por último uso  
**Para** acceder rápidamente a mis proyectos más recientes

**Criterios:**
- Grilla muestra todos los proyectos
- Orden default: fecha último acceso descendente
- Búsqueda filtra en tiempo real con debounce
- Estado vacío con CTA si no hay proyectos

---

### HU-002: Seleccionar y Ver Detalles

**Como** usuario
**Quiero** seleccionar un proyecto y ver su descripción completa
**Para** recordar de qué se trata el proyecto antes de usar sus accesos

**Criterios:**
- Click en fila muestra descripción larga
- Panel dividido con GridSplitter arrastrable (default 60/40)
- Ícono de lápiz para editar descripción larga

---

### HU-003: Gestionar Accesos del Proyecto

**Como** usuario  
**Quiero** agregar, reordenar y eliminar accesos de un proyecto  
**Para** mantener organizada mi colección de accesos por contexto

**Criterios:**
- Botón "+" con submenú (Archivo/Carpeta o URL)
- Drag & drop para reordenar (transacción atómica)
- Context menu para eliminar/renombrar
- Ícono de advertencia en accesos con path inválido

---

### HU-004: Crear Nuevo Proyecto

**Como** usuario  
**Quiero** crear un nuevo proyecto con nombre y descripción  
**Para** organizar un nuevo conjunto de accesos relacionados

**Criterios:**
- Modal con formulario y contador de caracteres en descripción corta
- Validaciones de campos
- Confirmación visual al crear

---

### HU-005: Eliminar Proyecto

**Como** usuario  
**Quiero** eliminar un proyecto que ya no uso  
**Para** mantener limpia mi lista de proyectos

**Criterios:**
- Confirmación antes de eliminar
- Cascade delete de accesos
- Feedback visual

---

### HU-006: Editar Proyecto

**Como** usuario  
**Quiero** poder cambiar el nombre y descripción corta de un proyecto  
**Para** corregir errores o actualizar la información del proyecto

**Criterios:**
- Botón "Editar" disponible con proyecto seleccionado
- Mismo modal que creación con datos pre-cargados
- Validación de nombre único excluyendo el proyecto actual

---

### HU-007: Activar / Desactivar Proyecto

**Como** usuario  
**Quiero** poder pausar un proyecto sin eliminarlo  
**Para** mantenerlo archivado pero recuperable cuando lo necesite

**Criterios:**
- Botón contextual Activar/Desactivar según estado actual
- Sin confirmación (acción reversible)
- El filtro "Solo activos" oculta automáticamente los desactivados

---

## Criterios de Aceptación Generales

- [ ] El tab "Proyectos" no debe afectar el funcionamiento del tab "Accesos"
- [ ] Todas las operaciones de DB deben ser transaccionales
- [ ] La UI debe responder en < 100ms para interacciones básicas
- [ ] Los íconos deben cargarse asíncronamente sin bloquear la UI
- [ ] El orden de los accesos debe persistir entre sesiones
- [ ] La búsqueda debe soportar caracteres especiales, acentos y ñ
- [ ] Debe haber feedback visual (toasts) para operaciones exitosas y fallidas
- [ ] Todos los dialogs deben cerrarse con Escape
- [ ] El sistema de migrations debe ejecutarse antes de cualquier operación sobre las tablas de Proyectos

---

## Dependencias Técnicas

- **Microsoft.Data.Sqlite** v8.0.0 (ya existente en el proyecto)
- **IconHelper** existente para extracción de íconos
- **WPF** .NET 8 (framework actual)
- **Windows Forms** para OpenFileDialog (ya referenciado)

---

## Riesgos y Mitigaciones

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| Performance con muchos proyectos | Alto | Debounce en búsqueda, carga asíncrona, índices compuestos |
| Performance con muchos accesos por proyecto | Medio | Aviso al usuario si supera 100 accesos |
| Conflictos con nombres duplicados | Medio | Validación unique constraint en DB + validación en UI |
| Pérdida de datos durante migración | Alto | **Backup único** del .db antes de ejecutar el plan de migraciones. Si falla, se restaura. |
| Íconos no cargan para ciertos tipos | Bajo | Fallback a ícono genérico |
| Archivos movidos/eliminados del disco | Medio | Detección al cargar, ícono de advertencia, opción de eliminar del proyecto |
| Schema desactualizado en versiones futuras | Alto | Sistema de migrations versionadas con SchemaVersion |
| Reordenamiento fallido a mitad de transacción | Medio | Transacción atómica; rollback automático en caso de error |
| Complejidad de consultas con soft delete | Bajo | Índices parciales (`WHERE fecha_eliminacion IS NULL`), views filtradas |

---

## Métricas de Éxito

1. **Performance:** Carga inicial del tab < 500ms con 200 proyectos
2. **Estabilidad:** Cero crashes relacionados al tab Proyectos en las primeras 2 semanas de uso
3. **Integridad de datos:** Cero pérdidas de datos tras migraciones (verificado con suite de tests manuales pre-release)
4. **Consistencia:** Mismo look & feel que el resto de la aplicación, validado por revisión visual

---

## Notas de Implementación

1. **Reutilizar código existente:** Aprovechar la lógica de drag-drop, icon loading y file handling del tab "Accesos"
2. **Mantener consistencia visual:** Usar los mismos estilos, colores y patrones de UI
3. **Testing manual:** Probar con al menos 50 proyectos, 500 accesos totales, y al menos 1 proyecto con path inválido
4. **Documentación:** Actualizar el README.md con la nueva funcionalidad y el sistema de migrations
5. **No construir después de cada cambio:** El developer decide cuándo compilar; no agregar pasos de build automático al workflow
6. **Persistencia de UI:** El split del GridSplitter (RF-004) se guarda en `usersettings.json` (mismo archivo que `SelectedTipoCarpeta`). Usar propiedad `ProyectosSplitProportion` (double, default 0.6)

---

## Appendix A: SQL Migration Script (versión 1)

```sql
-- Migration #1: Creación inicial de tablas Proyecto y ProyectoAcceso
-- Ejecutar solo si version 1 no está en SchemaVersion

-- Tabla de control de versiones (se crea primero, siempre)
CREATE TABLE IF NOT EXISTS SchemaVersion (
    version     INTEGER PRIMARY KEY,
    descripcion TEXT NOT NULL,
    aplicada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tablas de la feature Proyectos
CREATE TABLE IF NOT EXISTS Proyecto (
    id                   INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre               TEXT NOT NULL UNIQUE,
    descripcion_corta    TEXT NOT NULL,
    descripcion_larga    TEXT,
    activo               TEXT NOT NULL DEFAULT 'S' CHECK(activo IN ('S', 'N')),
    fecha_creacion       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_ultimo_acceso  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_eliminacion    DATETIME
);

CREATE TABLE IF NOT EXISTS ProyectoAcceso (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    proyecto_id      INTEGER NOT NULL,
    orden            INTEGER NOT NULL DEFAULT 0,
    acceso_full_path TEXT NOT NULL,
    acceso_nombre    TEXT NOT NULL,
    acceso_tipo      TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url')),
    fecha_eliminacion DATETIME,
    FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id),
    UNIQUE(proyecto_id, acceso_full_path)
);

-- Índices para performance y soft delete
CREATE INDEX IF NOT EXISTS IX_ProyectoAcceso_orden
    ON ProyectoAcceso(proyecto_id, orden);

CREATE INDEX IF NOT EXISTS IX_Proyecto_fecha_ultimo_acceso
    ON Proyecto(fecha_ultimo_acceso DESC);

CREATE INDEX IF NOT EXISTS IX_Proyecto_activo_fecha_ultimo_acceso
    ON Proyecto(activo DESC, fecha_ultimo_acceso DESC);

-- Índices compuestos para optimizar búsquedas
CREATE INDEX IF NOT EXISTS IX_Proyecto_busqueda
    ON Proyecto(nombre, descripcion_corta);

CREATE INDEX IF NOT EXISTS IX_Proyecto_busqueda_activo
    ON Proyecto(activo, nombre, descripcion_corta);

-- Índices parciales para soft delete (SQLite 3.8.0+)
CREATE INDEX IF NOT EXISTS IX_Proyecto_no_eliminado
    ON Proyecto(fecha_eliminacion) WHERE fecha_eliminacion IS NULL;

CREATE INDEX IF NOT EXISTS IX_ProyectoAcceso_no_eliminado
    ON ProyectoAcceso(fecha_eliminacion) WHERE fecha_eliminacion IS NULL;

-- Registrar migration como aplicada
INSERT INTO SchemaVersion (version, descripcion)
VALUES (1, 'Creación inicial de tablas Proyecto y ProyectoAcceso');
```

---

**Versión:** 2.0  
**Fecha:** 2026-03-23  
**Basado en:** v1.0 — gaps y mejoras identificados en revisión  
**Autor:** AccesosLauncher Team
