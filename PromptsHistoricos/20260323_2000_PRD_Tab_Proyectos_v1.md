# PRD: Tab "Proyectos"

## Overview

Agregar un nuevo tab llamado **"Proyectos"** a la aplicación AccesosLauncher que permita organizar accesos en grupos temáticos llamados "proyectos". Cada proyecto contendrá su propia colección de accesos (archivos, carpetas, URLs) configurable e independiente.

---

## Objetivos

1. Permitir al usuario crear y gestionar múltiples proyectos con accesos relacionados
2. Mantener historial de uso de proyectos ordenado por última vez accedido
3. Proveer una interfaz visual similar al tab "Accesos" pero aislada por proyecto
4. Persistir toda la información en la base de datos SQLite existente

---

## Requerimientos Funcionales

### RF-001: Visualización del Tab Proyectos

**Descripción:** El tab "Proyectos" debe aparecer como un nuevo TabItem en el MainTabControl, ubicado inmediatamente después del tab "Accesos" (índice 1).

**Criterios de Aceptación:**
- [ ] El tab debe tener el header "Proyectos"
- [ ] Debe estar posicionado entre "Accesos" y "Más Usados"
- [ ] Debe ser accesible vía navegación con teclado (Tab key)

---

### RF-002: Búsqueda de Proyectos

**Descripción:** El tab debe contener un InputBox/TextBox en la parte superior para buscar proyectos por nombre o descripción, acompañado de un checkbox para filtrar por estado activo.

**Criterios de Aceptación:**
- [ ] TextBox con placeholder "Buscar proyecto..."
- [ ] Checkbox "Solo activos" ubicado al lado del TextBox (derecha o izquierda)
- [ ] Checkbox marcado por default
- [ ] Búsqueda en tiempo real (UpdateSourceTrigger=PropertyChanged)
- [ ] Filtra por nombre (match parcial, case-insensitive)
- [ ] Filtra por descripción corta (match parcial, case-insensitive)
- [ ] Cuando "Solo activos" está marcado, filtra proyectos con `Activo = 'S'`
- [ ] Al limpiar el search, muestra todos los proyectos (respetando filtro de activos)
- [ ] Al desmarcar "Solo activos", muestra proyectos activos e inactivos

---

### RF-003: Grilla de Proyectos

**Descripción:** Una grilla/DataGrid que muestre los proyectos disponibles con columnas específicas.

**Columnas Requeridas:**

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Activo | Texto | Estado del proyecto: "S" (Sí) o "N" (No) |
| Nombre | Texto | Nombre del proyecto |
| Descripción | Texto | Descripción corta (máx 50 caracteres visibles) |
| Último Acceso | DateTime | Fecha y hora de última vez que se seleccionó el proyecto |

**Criterios de Aceptación:**
- [ ] DataGrid con las 4 columnas especificadas
- [ ] Columna "Activo" muestra ícono check (✓) para "S", cruz (✗) o vacío para "N"
- [ ] Ordenamiento default por "Último Acceso" descendente (más reciente primero)
- [ ] AllowSorting=true en todas las columnas
- [ ] SelectionMode=Single
- [ ] AutoGenerateColumns=false
- [ ] CanUserAddRows=false
- [ ] IsReadOnly=true (la grilla es solo visualización)
- [ ] Click en una fila selecciona el proyecto y actualiza el panel de detalles

---

### RF-004: Panel de Descripción Larga

**Descripción:** La mitad izquierda (50%) del área principal del tab debe mostrar un RichTextBox con la descripción detallada del proyecto seleccionado.

**Criterios de Aceptación:**
- [ ] RichTextBox editable solo en modo edición
- [ ] Muestra descripción larga completa del proyecto
- [ ] Soporta formato RTF básico (negrita, listas, etc.)
- [ ] Scroll vertical automático si el contenido excede el espacio
- [ ] Cuando no hay proyecto seleccionado, mostrar mensaje: "Seleccione un proyecto para ver su descripción"

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

**Descripción:** La mitad derecha (50%) del área principal debe mostrar los accesos del proyecto seleccionado, organizados como íconos similares al tab "Accesos".

**Criterios de Aceptación:**
- [ ] Layout similar al tab "Accesos" (WrapPanel o ItemsControl)
- [ ] Cada ícono muestra:
  - Ícono del archivo/carpeta/URL
  - Nombre debajo (máx 2 líneas, text-wrapping)
- [ ] Click en ícono ejecuta el acceso (mismo comportamiento que tab Accesos)
- [ ] Doble click permite renombrar el acceso dentro del proyecto
- [ ] Right-click muestra contexto menu con:
  - Renombrar
  - Eliminar (solo del proyecto, no el archivo original)
  - Abrir ubicación del archivo
- [ ] Cuando no hay proyecto seleccionado, mostrar mensaje: "Seleccione un proyecto"

---

### RF-007: Reordenamiento de Accesos

**Descripción:** Los íconos de accesos dentro de un proyecto deben poder arrastrarse y reordenarse.

**Criterios de Aceptación:**
- [ ] Drag & Drop entre íconos del mismo proyecto
- [ ] El orden se persiste en la tabla `ProyectoAcceso` (columna `orden`)
- [ ] Animación visual durante el drag
- [ ] Drop indicator muestra dónde se insertará el ítem
- [ ] El orden se mantiene al recargar la aplicación

---

### RF-008: Agregar Nuevos Accesos

**Descripción:** Un ícono flotante de "+" debe permitir agregar nuevos accesos al proyecto seleccionado.

**Criterios de Aceptación:**
- [ ] Ícono "+" visible en la esquina superior derecha del panel de accesos
- [ ] Al hacer click, abre OpenFileDialog multiselección
- [ ] Permite seleccionar:
  - Archivos ejecutables (.exe, .bat, .cmd, .ps1)
  - Accesos directos (.lnk, .url)
  - Carpetas
- [ ] Al confirmar, agrega los accesos al proyecto seleccionado
- [ ] Asigna el siguiente orden disponible
- [ ] Muestra toast de confirmación con cantidad de ítems agregados
- [ ] Si no hay proyecto seleccionado, mostrar mensaje: "Primero seleccione un proyecto"

---

### RF-009: Persistencia en Base de Datos

**Descripción:** Toda la información de proyectos y sus accesos debe guardarse en la base de datos SQLite `accesos_launcher.db`.

**Esquema de Tablas:**

#### Tabla: `Proyecto`
```sql
CREATE TABLE IF NOT EXISTS Proyecto (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL UNIQUE,
    descripcion_corta TEXT NOT NULL,
    descripcion_larga TEXT,
    activo TEXT NOT NULL DEFAULT 'S' CHECK(activo IN ('S', 'N')),
    fecha_ultimo_acceso DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

#### Tabla: `ProyectoAcceso`
```sql
CREATE TABLE IF NOT EXISTS ProyectoAcceso (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    proyecto_id INTEGER NOT NULL,
    orden INTEGER NOT NULL DEFAULT 0,
    acceso_full_path TEXT NOT NULL,
    acceso_nombre TEXT NOT NULL,
    acceso_tipo TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url')),
    FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id) ON DELETE CASCADE,
    UNIQUE(proyecto_id, acceso_full_path)
);
```

**Índices:**
```sql
CREATE INDEX IF NOT EXISTS IX_ProyectoAcceso_orden ON ProyectoAcceso(proyecto_id, orden);
CREATE INDEX IF NOT EXISTS IX_Proyecto_fecha_ultimo_acceso ON Proyecto(fecha_ultimo_acceso DESC);
```

**Criterios de Aceptación:**
- [ ] Las tablas se crean automáticamente al iniciar la aplicación
- [ ] CRUD completo para Proyecto (Create, Read, Update, Delete)
- [ ] CRUD completo para ProyectoAcceso
- [ ] Cascade delete: al eliminar proyecto, se eliminan sus accesos
- [ ] Actualiza `fecha_ultimo_acceso` al seleccionar un proyecto

---

### RF-010: Creación de Proyectos

**Descripción:** Debe existir un mecanismo para crear nuevos proyectos desde la UI.

**Criterios de Aceptación:**
- [ ] Botón "Nuevo Proyecto" visible en la parte superior del tab
- [ ] Al hacer click, abre modal/dialog con:
  - TextBox: Nombre (requerido, único)
  - TextBox: Descripción corta (requerido)
  - RichTextBox: Descripción larga (opcional)
- [ ] Validaciones:
  - Nombre no vacío
  - Nombre único (case-insensitive)
  - Descripción corta no vacía
- [ ] Al guardar:
  - Inserta en tabla `Proyecto`
  - Cierra el modal
  - Selecciona el nuevo proyecto automáticamente
  - Muestra toast de confirmación

---

### RF-011: Eliminación de Proyectos

**Descripción:** Debe poder eliminarse un proyecto existente.

**Criterios de Aceptación:**
- [ ] Botón "Eliminar" visible cuando hay proyecto seleccionado
- [ ] Al hacer click, muestra confirmación: "¿Está seguro que desea eliminar el proyecto '{nombre}'? Esta acción eliminará también todos sus accesos."
- [ ] Al confirmar:
  - Elimina de la base de datos (cascade delete)
  - Limpia la selección
  - Refresca la grilla
  - Muestra toast de confirmación

---

## Requerimientos No Funcionales

### RNF-001: Performance

- [ ] La grilla debe cargar en < 500ms con hasta 100 proyectos
- [ ] La búsqueda debe filtrar en < 100ms
- [ ] Los íconos deben cargarse de forma asíncrona para no bloquear la UI

### RNF-002: Consistencia Visual

- [ ] Mantener el mismo estilo visual que el resto de la aplicación
- [ ] Usar los mismos colores, fuentes y espaciados
- [ ] Íconos consistentes con el design system existente

### RNF-003: Manejo de Errores

- [ ] Todos los errores de base de datos deben loguearse y mostrarse al usuario
- [ ] Operaciones de I/O deben ser asíncronas
- [ ] La UI no debe congelarse durante operaciones de DB

### RNF-004: Compatibilidad

- [ ] Mantener compatibilidad con Windows 10/11
- [ ] No romper funcionalidad existente del tab "Accesos"
- [ ] La base de datos debe ser backward compatible

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
    public string Activo { get; set; } // "S" o "N"
    public DateTime FechaUltimoAcceso { get; set; }

    // Propiedad calculada para la grilla
    public string DescripcionCortaTruncada =>
        DescripcionCorta?.Length > 50
            ? DescripcionCorta.Substring(0, 47) + "..."
            : DescripcionCorta;
    
    // Propiedad calculada para mostrar ícono en grilla
    public string ActivoIcono => Activo == "S" ? "✓" : "✗";
}

public class ProyectoAcceso : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int Orden { get; set; }
    public string AccesoFullPath { get; set; }
    public string AccesoNombre { get; set; }
    public string AccesoTipo { get; set; } // "File", "Folder", "Url"
    public ImageSource Icon { get; set; } // Calculado vía IconHelper
}
```

---

## Arquitectura de Implementación

### Archivos a Modificar

| Archivo | Cambios |
|---------|---------|
| `MainWindow.xaml` | Agregar TabItem "Proyectos", definir UI del nuevo tab |
| `MainWindow.xaml.cs` | Agregar lógica del tab, propiedades, comandos |
| `DatabaseHelper.cs` | Extender con métodos para Proyecto y ProyectoAcceso |
| `AppItem.cs` | Posiblemente refactorizar para compartir lógica con ProyectoAcceso |

### Nuevos Archivos

| Archivo | Propósito |
|---------|-----------|
| `Proyecto.cs` | Modelo de datos para Proyecto |
| `ProyectoAcceso.cs` | Modelo de datos para ProyectoAcceso |
| `ProyectoDatabaseHelper.cs` (opcional) | Si se prefiere separar la lógica de DB |

### Patrón de Implementación

Seguir el patrón **Code-Behind** existente en la aplicación:

1. **DataContext = this** (MainWindow)
2. **ObservableCollection** para binding
3. **INotifyPropertyChanged** en modelos
4. **Event handlers** directos en MainWindow
5. **IconHelper** existente para íconos
6. **DatabaseHelper** extendido para nuevas tablas

---

## Historias de Usuario

### HU-001: Ver Lista de Proyectos

**Como** usuario  
**Quiero** ver una lista de todos mis proyectos ordenados por último uso  
**Para** acceder rápidamente a mis proyectos más recientes

**Criterios:**
- Grilla muestra todos los proyectos
- Orden default: fecha último acceso descendente
- Búsqueda filtra en tiempo real

---

### HU-002: Seleccionar y Ver Detalles

**Como** usuario  
**Quiero** seleccionar un proyecto y ver su descripción completa  
**Para** recordar de qué se trata el proyecto antes de usar sus accesos

**Criterios:**
- Click en fila muestra descripción larga
- Panel dividido 50/50 con accesos
- Ícono de lápiz para editar

---

### HU-003: Gestionar Accesos del Proyecto

**Como** usuario  
**Quiero** agregar, reordenar y eliminar accesos de un proyecto  
**Para** mantener organizada mi colección de accesos por contexto

**Criterios:**
- Botón "+" para agregar
- Drag & drop para reordenar
- Context menu para eliminar/renombrar
- Persistencia automática

---

### HU-004: Crear Nuevo Proyecto

**Como** usuario  
**Quiero** crear un nuevo proyecto con nombre y descripción  
**Para** organizar un nuevo conjunto de accesos relacionados

**Criterios:**
- Modal con formulario
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

## Criterios de Aceptación Generales

- [ ] El tab "Proyectos" no debe afectar el funcionamiento del tab "Accesos"
- [ ] Todas las operaciones de DB deben ser transaccionales
- [ ] La UI debe responder en < 100ms para interacciones básicas
- [ ] Los íconos deben cargarse asíncronamente sin bloquear la UI
- [ ] El orden de los accesos debe persistir entre sesiones
- [ ] La búsqueda debe soportar caracteres especiales y acentos
- [ ] Debe haber feedback visual (toasts) para operaciones exitosas/fallidas

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
| Performance con muchos proyectos | Alto | Paginación virtual, carga asíncrona |
| Conflictos con nombres duplicados | Medio | Validación unique constraint en DB |
| Pérdida de datos durante migración | Alto | Backup automático de DB antes de actualizar |
| Íconos no cargan para ciertos tipos | Bajo | Fallback a ícono genérico |

---

## Métricas de Éxito

1. **Adopción:** 80% de usuarios usan el tab Proyectos dentro de las primeras 2 semanas
2. **Performance:** < 500ms de carga inicial del tab
3. **Satisfacción:** No hay reportes de bugs críticos relacionados
4. **Consistencia:** Mismo look & feel que el resto de la aplicación

---

## Notas de Implementación

1. **Reutilizar código existente:** Aprovechar la lógica de drag-drop, icon loading y file handling del tab "Accesos"
2. **Mantener consistencia:** Usar los mismos estilos, colores y patrones de UI
3. **Testing manual:** Probar con al menos 50 proyectos y 500 accesos totales
4. **Documentación:** Actualizar el README.md con la nueva funcionalidad

---

## Appendix: SQL Migration Script

```sql
-- Migración para agregar tablas de Proyectos
-- Ejecutar al iniciar la aplicación si no existen

CREATE TABLE IF NOT EXISTS Proyecto (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL UNIQUE,
    descripcion_corta TEXT NOT NULL,
    descripcion_larga TEXT,
    activo TEXT NOT NULL DEFAULT 'S' CHECK(activo IN ('S', 'N')),
    fecha_ultimo_acceso DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ProyectoAcceso (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    proyecto_id INTEGER NOT NULL,
    orden INTEGER NOT NULL DEFAULT 0,
    acceso_full_path TEXT NOT NULL,
    acceso_nombre TEXT NOT NULL,
    acceso_tipo TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url')),
    FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id) ON DELETE CASCADE,
    UNIQUE(proyecto_id, acceso_full_path)
);

CREATE INDEX IF NOT EXISTS IX_ProyectoAcceso_orden ON ProyectoAcceso(proyecto_id, orden);
CREATE INDEX IF NOT EXISTS IX_Proyecto_fecha_ultimo_acceso ON Proyecto(fecha_ultimo_acceso DESC);
```

---

**Versión:** 1.0  
**Fecha:** 2026-03-23  
**Autor:** AccesosLauncher Team
