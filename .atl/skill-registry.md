# Skill Registry - AccesosLauncher

## Project-Level Skills
No project-level skills detected.

## User-Level Skills

| Skill | Location | Purpose |
|-------|----------|---------|
| sdd-init | `~/.config/opencode/skills/sdd-init/SKILL.md` | Initialize SDD context in any project |
| sdd-explore | `~/.config/opencode/skills/sdd-explore/SKILL.md` | Explore and investigate ideas before committing to a change |
| sdd-propose | `~/.config/opencode/skills/sdd-propose/SKILL.md` | Create a change proposal with intent, scope, and approach |
| sdd-spec | `~/.config/opencode/skills/sdd-spec/SKILL.md` | Write specifications with requirements and scenarios |
| sdd-design | `~/.config/opencode/skills/sdd-design/SKILL.md` | Create technical design document with architecture decisions |
| sdd-tasks | `~/.config/opencode/skills/sdd-tasks/SKILL.md` | Break down a change into an implementation task checklist |
| sdd-apply | `~/.config/opencode/skills/sdd-apply/SKILL.md` | Implement tasks from the change, writing actual code |
| sdd-verify | `~/.config/opencode/skills/sdd-verify/SKILL.md` | Validate that implementation matches specs, design, and tasks |
| sdd-archive | `~/.config/opencode/skills/sdd-archive/SKILL.md` | Sync delta specs to main specs and archive a completed change |
| skill-creator | `~/.config/opencode/skills/skill-creator/SKILL.md` | Create new AI agent skills following the Agent Skills spec |
| find-skills | `~/.agents/skills/find-skills/SKILL.md` | Discover and install agent skills |

## Project Conventions

- **AGENTS.md**: Found in project root — defines project overview, tech stack, and build instructions
- **Architecture**: WPF with code-behind (MainWindow.xaml.cs is 3130 lines, monolithic), DatabaseHelper for data access
- **Configuration**: Microsoft.Extensions.Configuration with appsettings.json
- **Domain language**: Spanish (Proyecto, Acceso, Carpeta, TipoCarpeta)
- **Database**: SQLite via Microsoft.Data.Sqlite, soft deletes (fecha_eliminacion)
- **Testing**: xUnit with coverlet, project AccesosLauncher.Tests
- **No linter/formatter config**: No .editorconfig, no CI/CD pipelines detected

## Notes

- This is a C# WPF application targeting .NET 10 (`net10.0-windows7.0`), NOT a Go project. The `go-testing` skill is NOT applicable.
- Mode: `engram` — SDD artifacts persisted to Engram persistent memory.
- Models implement INotifyPropertyChanged for WPF data binding.
- MainWindow has significant complexity (3130 lines) — a prime candidate for refactoring into smaller components.
