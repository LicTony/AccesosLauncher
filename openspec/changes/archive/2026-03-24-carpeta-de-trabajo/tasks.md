# Tasks: Carpeta de Trabajo

Implementación del nuevo tipo de acceso "Carpeta de Trabajo" que abre Windows Terminal con pestañas preconfiguradas.

## Phase 1: Foundation

- [x] 1.1 Create `AccesosLauncher/Core/HerramientaConfig.cs` — JSON model class with: `int Orden`, `string Title`, `string TabColor`, `string Parametro`
- [x] 1.2 Create `CarpetaDeTrabajoHerramientas.json` in project root — default config with 3 tabs (lazygit, qwen, shell)
- [x] 1.3 Update `AccesosLauncher.csproj` — add `<Content Include="..\CarpetaDeTrabajoHerramientas.json" CopyToOutputDirectory="PreserveNewest" />`

## Phase 2: Core Implementation

- [x] 2.1 Create `AccesosLauncher/Core/WindowsTerminalLauncher.cs` — static class with `OpenInWindowsTerminal(string fileUrl)` public method
- [x] 2.2 Implement `ConvertFileUrlToPath(string fileUrl)` — validate file:// URL, extract LocalPath using System.Uri
- [x] 2.3 Implement `LoadHerramientasConfig()` — read JSON from AppDomain.CurrentDomain.BaseDirectory, deserialize with System.Text.Json, sort by Orden
- [x] 2.4 Implement `GetDefaultHerramientas()` — return hardcoded 3-tab default config
- [x] 2.5 Implement `BuildWtArguments(string dir, List<HerramientaConfig>)` — construct wt.exe args with -d, new-tab, --title, --tabColor, -- Parametro

## Phase 3: Integration

- [x] 3.1 Read `AccesosLauncher/Enums/ProyectoAccesoTipo.cs` — add `CarpetaDeTrabajo` enum value if not exists
- [x] 3.2 Read `AccesosLauncher/MainWindow.xaml.cs` — locate ProyectoAcceso_Click handler around line 2547
- [x] 3.3 Add switch case in ProyectoAcceso_Click for "CarpetaDeTrabajo" — call WindowsTerminalLauncher.OpenInWindowsTerminal(acceso.Ruta)
- [x] 3.4 Wrap call in try-catch: DirectoryNotFoundException → MessageBox.Warning, Exception → MessageBox.Error
- [x] 3.5 Locate BtnAgregarAcceso_Click or form creation (line ~2924) — add "Carpeta de Trabajo" option to access type ComboBox
- [x] 3.6 Add FolderBrowserDialog or TextBox for directory path when type is CarpetaDeTrabajo

## Phase 4: Testing

- [x] 4.1 Test: `ConvertFileUrlToPath()` with valid file:/// URL returns correct path
- [x] 4.2 Test: `ConvertFileUrlToPath()` with null/empty throws ArgumentException
- [x] 4.3 Test: `ConvertFileUrlToPath()` with non-file:// URL throws ArgumentException
- [x] 4.4 Test: `LoadHerramientasConfig()` with valid JSON returns sorted list
- [x] 4.5 Test: `LoadHerramientasConfig()` with missing file returns defaults
- [x] 4.6 Test: `LoadHerramientasConfig()` with malformed JSON returns defaults
- [x] 4.7 Test: `BuildWtArguments()` produces correct wt.exe command format
- [x] 4.8 Test: `BuildWtArguments()` with empty Parametro omits `-- command`
- [x] 4.9 Test: `GetDefaultHerramientas()` returns 3 items with correct values

## Phase 5: Cleanup

- [x] 5.1 Verify error messages match PRD RF-008 (directory not found) and RF-009 (generic error)
- [x] 5.2 Verify default config triggers when JSON missing (RF-010)

## Implementation Order

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5

Dependencies:
- 1.1 (HerramientaConfig) must come before 2.3 (LoadHerramientasConfig) and 2.5 (BuildWtArguments)
- 1.2 (JSON file) must come before 2.3 (LoadHerramientasConfig)
- 2.1 (WindowsTerminalLauncher) must come before 3.3 (integration)
- 3.1 (enum) must come before 3.3 (handler case)

## Relevant Files

- `AccesosLauncher/Core/HerramientaConfig.cs` — NEW: JSON model
- `AccesosLauncher/Core/WindowsTerminalLauncher.cs` — NEW: Launcher logic
- `CarpetaDeTrabajoHerramientas.json` — NEW: Tab configuration
- `AccesosLauncher/Enums/ProyectoAccesoTipo.cs` — MODIFY: Add enum value
- `AccesosLauncher/MainWindow.xaml.cs` — MODIFY: Add handler + form option
- `AccesosLauncher/AccesosLauncher.csproj` — MODIFY: Include JSON file
