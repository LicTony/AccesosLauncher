# Exploración: Carpeta de Trabajo

## Resumen
Se investigó la implementación de un nuevo tipo de acceso "Carpeta de Trabajo" que abre Windows Terminal con 3 pestañas preconfiguradas en un directorio específico.

## Estado Actual

### Arquitectura del Sistema

**Accesos (Filesystem-based)**
- Lee archivos ejecutables de `BaseDir` (configurable en appsettings.json)
- Muestra archivos `.lnk`, `.url`, y cualquier ejecutable en PATHEXT
- Expone carpetas vacías como placeholders

**Proyectos (Database-based)**
- Almacena accesos en tabla `ProyectoAcceso` con campos: `acceso_full_path`, `acceso_nombre`, `acceso_tipo`
- El tipo se guarda como texto: "File", "Folder", "Url"
- Lanzamiento actual en `ProyectoAcceso_Click`:
  - **Url**: `Process.Start(url)` con UseShellExecute
  - **File/Folder**: `Process.Start(path)` con UseShellExecute + WorkingDirectory

### Archivos Clave

| Archivo | Propósito |
|---------|------------|
| `Enums/ProyectoAccesoTipo.cs` | Enum con File, Folder, Url |
| `Models/ProyectoAcceso.cs` | Modelo con validación PathExiste |
| `MainWindow.xaml.cs:2924` | `BtnAgregarAcceso_Click` - formulario dinámico |
| `MainWindow.xaml.cs:2547` | `ProyectoAcceso_Click` - lanzador de accesos |
| `DatabaseHelper.cs` | CRUD de ProyectoAcceso |

## Áreas Afectadas

- **`Enums/ProyectoAccesoTipo.cs`** — Añadir valor `CarpetaDeTrabajo` al enum
- **`MainWindow.xaml.cs`** — 
  - `BtnAgregarAcceso_Click`: agregar opción "Carpeta de Trabajo" al ComboBox con FolderBrowserDialog
  - `ProyectoAcceso_Click`: detectar nuevo tipo y usar WindowsTerminalLauncher
- **`AppItem.cs` / `ProyectoAcceso.cs`** — Extender `ItemType` para soportar nuevo tipo
- **Nuevo archivo** `WindowsTerminalLauncher.cs` — Clase que encapsula la lógica de wt.exe
- **Nuevo archivo** `HerramientaConfig.cs` — Modelo para deserializar JSON
- **Nuevo archivo** `CarpetaDeTrabajoHerramientas.json` — Configuración de pestañas

## Análisis de Opciones

### Opción 1: Extender Enum + Nueva Clase Lanzadora
**Descripción**: Agregar `CarpetaDeTrabajo` al enum existente y crear clase `WindowsTerminalLauncher`.

- **Pros**: 
  - Tipo seguro (type-safe)
  - Separación de responsabilidades clara
  - El PRD provee código de referencia completo
- **Contras**: 
  - Requiere modificar enum (migración de BD no necesaria, se guarda como string)
  - Agregar nuevo handler en MainWindow
- **Esfuerzo**: Medio

### Opción 2: Tipo String ("Carpeta de Trabajo" como texto)
**Descripción**: No modificar el enum, usar el string directamente.

- **Pros**: 
  - Sin cambios en enum
  - Compatible hacia atrás
- **Contras**: 
  - Menos type-safe
  - Requiere magic strings en el código
- **Esfuerzo**: Bajo

## Recomendación

**Opción 1** - Extender Enum + Nueva Clase Lanzadora.

**Justificación:**
- El código de referencia del PRD es completo y bien diseñado
- Mantiene consistencia con tipos existentes (File, Folder, Url)
- Facilita testing unitario de WindowsTerminalLauncher
- Permite扩展 futura (nuevos launchers)

## Integración con Windows Terminal

### Construcción de argumentos wt.exe
El PRD especifica un formato:
```
-d "{directorio}" new-tab --title "{title}" --tabColor "{color}" -- {comando}
```

**Desafío**: La primera pestaña usa `-d` como argumento global, las demás lo repiten.

### Validaciones requeridas
1. **Directorio existe**: `Directory.Exists(localPath)` antes de lanzar
2. **URL válida**: Solo `file://` - NO ejecutar otros protocolos
3. **Archivo JSON**: Si no existe o falla parseo, usar defaults (RF-010)
4. **Excepciones**: Capturar y mostrar MessageBox apropiado

## Riesgos

1. **Windows Terminal no instalado** — Lanzar wt.exe fallará silenciosamente o mostrará error del sistema. Debería mostrarse mensaje amigable.
2. **PowerShell 7+ requerido** — Los comandos `pwsh -NoExit -Command` asumen PS7. Si solo hay Windows PowerShell 5.1, los comandos fallarán.
3. **Comandos externos (lazygit, qwen)** — Estos son opcionales según el PRD, pero pueden no estar en PATH.
4. **Espacios en paths** — El argumento de wt.exe debe escapar correctamente.
5. **Versión de Windows Terminal** — La sintaxis de `new-tab` puede variar según versión.

## Listo para Propuesta

**Sí** — La investigación reveló:
1. La arquitectura permite añadir nuevos tipos de acceso
2. No hay cambios necessários en DatabaseHelper (tipo se guarda como texto)
3. El código de referencia del PRD es directamente implementable
4. Los puntos de integración están claros: enum + formulario + handler + nueva clase

**Próximo paso**: Crear Proposal que defina alcance y aproximación técnica.