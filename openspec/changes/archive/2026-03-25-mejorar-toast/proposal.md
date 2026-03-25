# Proposal: Mejorar Toast de Notificaciones

## Intent

Mejorar el componente toast de notificaciones en la pestaña "Proyectos" para resolver problemas de visibilidad, posicionamiento y experiencia de usuario. El toast actual es demasiado pequeño, está mal posicionado, dura solo 3 segundos, carece de animaciones y pasa desapercibido.

**Fuente de requisitos:** PRD `20260325_1810_PRD_MejorarToast.md` (validado por exploración)

## Scope

### In Scope
- Modificar `toastPanel` en MainWindow.xaml (línea 231): RowSpan, MinWidth 250px, MaxWidth 400px, DropShadowEffect, StackPanel con icono
- Modificar `ShowToast()` en MainWindow.xaml.cs (línea 2662): Timer como campo de clase, animaciones fade-in (200ms) / fade-out (300ms), parámetro ToastType
- Agregar `enum ToastType { Success, Warning, Error }` con iconos y colores diferenciados
- Agregar override `OnClosing()` para cleanup del timer
- Duración: 5 segundos (7s si mensaje > 50 caracteres)

### Out of Scope
- Mover toast fuera del TabItem "Proyectos" (scope limitado a Projects)
- Cola de notificaciones múltiples (reemplazo de mensaje, no encolamiento)
- Botón "Deshacer" en el toast (futuro)

## Approach

**Enfoque 1: PRD Completo** (recomendado por exploración)

Implementar todas las mejoras del PRD:
- Posicionamiento: `Grid.RowSpan="5"` + `HorizontalAlignment="Right"` → esquina superior derecha
- Tamaño: `MinWidth="250"`, `MaxWidth="400"`, `TextWrapping="Wrap"`
- Timer: `_toastTimer` como campo de clase con cleanup en `OnClosing()`
- Animaciones: `DoubleAnimation` sobre Opacity (200ms in, 300ms out)
- Iconos: `ToastType` enum con ✓ (verde), ⚠ (amarillo), ✕ (rojo)

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `MainWindow.xaml:231` | Modified | toastPanel con RowSpan, Min/MaxWidth, DropShadow, StackPanel con icono |
| `MainWindow.xaml.cs:2662` | Modified | ShowToast con timer, animaciones, ToastType |
| `MainWindow.xaml.cs` | New | `enum ToastType`, `_toastTimer` field, `OnClosing()` override |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Excepción Dispatcher al cerrar ventana | Media | Cleanup en OnClosing() |
| Animaciones fallan en GPUs antiguas | Baja | Fallback a Visibility.Collapsed |
| Toast tapa botones interactivos | Baja | IsHitTestVisible="False" |

## Rollback Plan

1. Revertir cambios en MainWindow.xaml (línea 231) — restaurar Border original
2. Revertir ShowToast() en MainWindow.xaml.cs — restaurar implementación con Timer local
3. Eliminar `_toastTimer` field si fue agregado
4. Eliminar `OnClosing()` override si fue agregado
5. Compilar y verificar que construye correctamente

## Dependencies

- .NET 10 SDK (ya disponible)
- WPF standard (sin librerías externas)

## Success Criteria

- [ ] Toast aparece en esquina superior derecha con margen 15px
- [ ] Ancho mínimo 250px, máximo 400px con text wrapping
- [ ] Duración 5s (7s si mensaje > 50 caracteres)
- [ ] Fade-in 200ms al aparecer, fade-out 300ms al desaparecer
- [ ] Icono variable por ToastType (✓ verde, ⚠ amarillo, ✕ rojo)
- [ ] No hay excepción al cerrar ventana antes de los 5 segundos
- [ ] Mensaje "Acceso(s) agregado(s): 3 (2 ya existían)" visible completo
