# Technical Design: Carpeta de Trabajo

## 1. Technical Approach

This design follows the approach outlined in the proposal: extending the existing enum + launcher class pattern. The implementation adds a new access type that integrates with Windows Terminal using pre-configured tabs.

**Reference:** Proposal sections 2-5, PRD sections 5-6.

---

## 2. Architecture Decisions

### Decision 1: New Enum Value

| Aspect | Detail |
|--------|--------|
| **Choice** | Add `CarpetaDeTrabajo` to existing `ProyectoAccesoTipo` enum |
| **Alternatives considered** | Create separate enum; store as string constant |
| **Rationale** | Existing code already uses string comparison (`acceso.AccesoTipo == "Url"`) so enum value works seamlessly. Existing pattern in codebase. |

### Decision 2: Launcher Class Location

| Aspect | Detail |
|--------|--------|
| **Choice** | Create `AccesosLauncher/Core/WindowsTerminalLauncher.cs` as static class |
| **Alternatives considered** | Instance class; add to existing helper class |
| **Rationale** | Static class matches existing codebase patterns (e.g., `DatabaseHelper`). No state needed. New `Core` folder for business logic. |

### Decision 3: HerramientaConfig Placement

| Aspect | Detail |
|--------|--------|
| **Choice** | Create `HerramientaConfig.cs` in same namespace as launcher |
| **Alternatives considered** | Nested class inside WindowsTerminalLauncher |
| **Rationale** | Cleaner separation; easier unit testing; matches PRD specification. |

### Decision 4: Form Integration

| Aspect | Detail |
|--------|--------|
| **Choice** | Add new ComboBox item at index 3; show folder browser dialog for all non-URL types |
| **Alternatives considered** | Separate dialog for new type; separate form field |
| **Rationale** | Minimal code change; existing form structure supports this pattern (lines 2924-3078). Validation happens on save. |

### Decision 5: Click Handler Integration

| Aspect | Detail |
|--------|--------|
| **Choice** | Add `CarpetaDeTrabajo` case to existing `ProyectoAcceso_Click` handler (line 2547) |
| **Alternatives considered** | Separate handler method; event-based |
| **Rationale** | Minimal change; existing handler pattern already handles `Url` specially. |

---

## 3. Data Flow

```
User clicks "Carpeta de Trabajo" access
         │
         ▼
ProyectoAcceso_Click(sender, e)
         │
         ├─► acceso.AccesoTipo == "CarpetaDeTrabajo" ?
         │
         ▼ YES
WindowsTerminalLauncher.OpenInWindowsTerminal(acceso.AccesoFullPath)
         │
         ├─► ConvertFileUrlToPath(fileUrl) → localPath
         │
         ├─► Directory.Exists(localPath) ? → throw if not
         │
         ├─► LoadHerramientasConfig() → List<HerramientaConfig>
         │         │
         │         ├─► Read CarpetaDeTrabajoHerramientas.json
         │         ├─► If fails → GetDefaultHerramientas()
         │         └─► OrderBy(h => h.Orden)
         │
         ├─► BuildWtArguments(localPath, herramientas) → wt args string
         │
         ▼
Process.Start("wt.exe", wtArgs)
```

---

## 4. File Changes

| File | Action | Description |
|------|--------|-------------|
| `Enums/ProyectoAccesoTipo.cs` | Modify | Add `CarpetaDeTrabajo = 3` |
| `Core/HerramientaConfig.cs` | Create | JSON model: Orden, Title, TabColor, Parametro |
| `Core/WindowsTerminalLauncher.cs` | Create | Static class with OpenInWindowsTerminal() |
| `CarpetaDeTrabajoHerramientas.json` | Create | Default tab config in output directory |
| `MainWindow.xaml.cs` | Modify | Add combo item + handler case for new type |

---

## 5. Interfaces / Contracts

### HerramientaConfig

```csharp
namespace AccesosLauncher.Core
{
    public class HerramientaConfig
    {
        public int Orden { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TabColor { get; set; } = string.Empty;
        public string Parametro { get; set; } = string.Empty;
    }
}
```

### WindowsTerminalLauncher

```csharp
namespace AccesosLauncher.Core
{
    public static class WindowsTerminalLauncher
    {
        public static void OpenInWindowsTerminal(string fileUrl);
    }
}
```

---

## 6. Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `ConvertFileUrlToPath()` | Various URL formats: file:///, file://, UNC paths |
| Unit | `LoadHerramientasConfig()` | Valid JSON, invalid JSON, missing file |
| Unit | `BuildWtArguments()` | 1 tab, 3 tabs, empty Parametro |
| Unit | `GetDefaultHerramientas()` | Verify default values match PRD |

---

## 7. Migration / Rollout

- **Database:** No migration needed. `tipo` stored as text, existing reads handle new value.
- **Feature flags:** None required.
- **Dependencies:** None new beyond .NET 8 built-ins.

---

## 8. Risk Mitigation

| Risk | Mitigation |
|------|------------|
| WT not installed | Catch exception, show MessageBox with generic error |
| Invalid JSON | Fall back to default config (PRD RF-010) |
| Directory missing at runtime | Validate in handler before calling launcher |
| Spaces in path | Wrap in quotes in BuildWtArguments |

---

## 9. Non-Functional Notes

- **Performance:** `Process.Start()` is async, UI not blocked.
- **Security:** URL validated to be `file://` protocol only.
- **Compatibility:** Windows Terminal 1.0+ required on target machine.
