# Proposal: Carpeta de Trabajo

## Intent

Agregar un nuevo tipo de acceso "Carpeta de Trabajo" que permita abrir Windows Terminal con pestañas preconfiguradas (lazygit, qwen, shell) en un directorio de proyecto específico. El objetivo es reducir la fricción de configuración repetitiva: de ~15 clicks a 1 click.

## Scope

### In Scope
- Nuevo tipo de acceso "Carpeta de Trabajo" en el combo de tipos
- Campo de ruta con FolderBrowserDialog y validación de directorio existente
- Clase `WindowsTerminalLauncher` con lógica de wt.exe
- Modelo `HerramientaConfig` para deserialización JSON
- Archivo `CarpetaDeTrabajoHerramientas.json` en directorio de la app
- Manejo de errores: directorio no existe, JSON inválido, error genérico
- Valores por defecto cuando JSON no existe

### Out of Scope
- Tests unitarios (pendientes para fase de verify)
- Documentación en README (pendiente)
- Otras plataformas (solo Windows)

## Approach

Seguir la **Opción 1** de la exploración: extender el enum `ProyectoAccesoTipo` + nueva clase lanzadora.

**Flujo de implementación:**
1. Agregar `CarpetaDeTrabajo` al enum `ProyectoAccesoTipo.cs`
2. Crear clase `HerramientaConfig.cs` con propiedades: Orden, Title, TabColor, Parametro
3. Crear clase `WindowsTerminalLauncher.cs` con los métodos del PRD (ConvertFileUrlToPath, LoadHerramientasConfig, BuildWtArguments, GetDefaultHerramientas)
4. Crear archivo JSON `CarpetaDeTrabajoHerramientas.json` con configuración inicial
5. Modificar `MainWindow.xaml.cs:2924` (BtnAgregarAcceso_Click) para nuevo tipo + campo ruta
6. Modificar `MainWindow.xaml.cs:2547` (ProyectoAcceso_Click) para detectar tipo y llamar launcher
7. Agregar try-catch con MessageBox para errores (RF-007, RF-008, RF-009)

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Enums/ProyectoAccesoTipo.cs` | Modified | Agregar valor `CarpetaDeTrabajo` |
| `HerramientaConfig.cs` | New | Modelo JSON con propiedades: Orden, Title, TabColor, Parametro |
| `WindowsTerminalLauncher.cs` | New | Clase estática: ConvertFileUrlToPath, LoadHerramientasConfig, BuildWtArguments, GetDefaultHerramientas |
| `CarpetaDeTrabajoHerramientas.json` | New | Archivo de configuración de pestañas |
| `MainWindow.xaml.cs:2924` | Modified | Agregar opción combo + campo ruta con FolderBrowserDialog |
| `MainWindow.xaml.cs:2547` | Modified | Detectar CarpetaDeTrabajo → llamar WindowsTerminalLauncher |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Windows Terminal no instalado | Medium | MessageBox con error genérico indicando que WT no está disponible |
| PowerShell 7+ requerido | Medium | Documentar requisito; usar valores por defecto si falla |
| Comandos externos (lazygit, qwen) no en PATH | Low | Son opcionales; la pestaña abre shell si falla |
| Espacios en paths | Medium | Envolver en comillas en BuildWtArguments |
| JSON inválido | Low | RF-010: usar configuración por defecto |

## Rollback Plan

1. Revertir cambios en `ProyectoAccesoTipo.cs` (eliminar CarpetaDeTrabajo)
2. Eliminar `HerramientaConfig.cs` y `WindowsTerminalLauncher.cs`
3. Revertir cambios en `MainWindow.xaml.cs` (eliminar case y handler)
4. Eliminar `CarpetaDeTrabajoHerramientas.json` si se copió a bin/

## Dependencies

- Windows Terminal 1.0+ instalado en el sistema
- .NET 8 (ya disponible en proyecto)
- System.Text.Json (incluido en .NET 8)
- PowerShell 7+ (recomendado para comandos en pestañas)

## Success Criteria

- [ ] Usuario puede crear acceso tipo "Carpeta de Trabajo" desde el formulario
- [ ] Campo ruta valida que el directorio existe antes de guardar
- [ ] Click en acceso abre Windows Terminal con 3 pestañas configuradas
- [ ] Las pestañas se abren en el directorio correcto
- [ ] Si JSON no existe, usa configuración por defecto (lazygit, qwen, shell)
- [ ] Si directorio no existe al ejecutar, muestra MessageBox de advertencia
- [ ] Errores genéricos muestran MessageBox con icono de error
