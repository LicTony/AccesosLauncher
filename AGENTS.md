# Project: AccesosLauncher

## Project Overview

AccesosLauncher is a C# WPF desktop application for Windows that manages quick-access shortcuts to applications, folders, URLs, and work environments. It uses a code-behind architecture (no MVVM framework) with SQLite for persistence and a dynamic UI built programmatically.

**Key Technologies:**

*   .NET 10 (`net10.0-windows7.0`)
*   C# 12
*   WPF (Windows Presentation Foundation)
*   Windows Forms (for FolderBrowserDialog)
*   SQLite via `Microsoft.Data.Sqlite` 10.0.5
*   Configuration via `Microsoft.Extensions.Configuration.Json` 10.0.5
*   xUnit 2.9.3 (testing)

## Solution Structure

*   `AccesosLauncher/` — Main WPF application
*   `AccesosLauncher.Tests/` — xUnit test project
*   `openspec/` — SDD (Spec-Driven Development) artifacts

## Building and Running

**Requirements:** Visual Studio 2022+ or .NET 10 SDK with `net10.0-windows7.0` workload.

**Build:**
```
dotnet build AccesosLauncher.sln
```

**Run:**
```
dotnet run --project AccesosLauncher
```

**Test:**
```
dotnet test
```

## Architecture Notes

*   `MainWindow.xaml.cs` is a **3130-line monolith** containing all form logic, event handlers, and business rules. Exercise caution when modifying — changes here are high-risk.
*   Core logic classes (e.g., `WindowsTerminalLauncher`, `DatabaseHelper`) are static.
*   Internal methods are exposed to tests via `[InternalsVisibleTo("AccesosLauncher.Tests")]`.
*   Access types are stored as strings in the database (`"File"`, `"Folder"`, `"Url"`, `"CarpetaDeTrabajo"`).
*   Domain language is Spanish (Proyecto, Acceso, Carpeta, Tipo).

## Development Conventions

*   **Nullable reference types:** enabled
*   **Implicit usings:** enabled
*   **Soft deletes:** use `fecha_eliminacion` column pattern
*   **Config files:** `appsettings.json` + `CarpetaDeTrabajoHerramientas.json` (copied to output)
*   **SDD artifacts:** `openspec/changes/` for active changes, `openspec/changes/archive/` for completed ones
*   **Testing:** xUnit, test project at `AccesosLauncher.Tests/`
