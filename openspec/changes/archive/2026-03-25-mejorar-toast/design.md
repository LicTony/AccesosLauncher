# Design: Mejorar Toast de Notificaciones

## Technical Approach

Implementar mejoras al componente toast existente según PRD: posicionamiento en esquina superior derecha mediante `Grid.RowSpan="5"`, dimensiones mínimas/máximas (250-400px), animaciones fade-in/out (200ms/300ms), y soporte para ToastType con iconos diferenciados.

## Architecture Decisions

### Decision: Timer como campo de clase (no local)

**Choice**: `_toastTimer` como campo privado nullable en la clase MainWindow
**Alternatives considered**: Timer local (implementación actual), Timer como variable estática
**Rationale**: Permite cancelar el timer anterior cuando se llama ShowToast() múltiples veces en rápida sucesión, y hacer cleanup en OnClosing() para evitar excepción Dispatcher

### Decision: ToastType como enum interno

**Choice**: Enum anidado `private enum ToastType { Success, Warning, Error }` en MainWindow.xaml.cs
**Alternatives considered**: Clase constante, struct, diccionario de configuración
**Rationale**: Tipado seguro, switch expression para comportamiento, simple de extender. No requiere exposición pública ya que solo se usa internamente

### Decision: Animaciones con DoubleAnimation sobre Opacity

**Choice**: DoubleAnimation desde/hacia 0 sobre UIElement.OpacityProperty
**Alternatives considered**: Storyboard, RenderTransform con Scale, Visibility toggle con delay
**Rationale**: API nativa WPF, hardware-accelerated, transición suave. El fade-out usa callback en Completed para setear Visibility.Collapsed

### Decision: IsHitTestVisible="False" en toastPanel

**Choice**: Agregar IsHitTestVisible="False" al Border del toast
**Alternatives considered**: Ninguno (siempre fue necesario)
**Rationale**: El toast no debe bloquear interacción con botones o controles subyacentes

## Data Flow

```
Llamada ShowToast(message, type)
         │
         ▼
┌─────────────────────────┐
│ 1. Cancelar timer       │
│    anterior si existe   │
└─────────────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 2. Configurar mensaje   │
│    + icono según type   │
└─────────────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 3. Fade-in 200ms        │
│    (Opacity 0→1)        │
└─────────────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 4. Iniciar timer       │
│    (5s o 7s según       │
│    longitud mensaje)    │
└─────────────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 5. En elapsed:          │
│    Fade-out 300ms       │
│    → Visibility.Collapsed│
└─────────────────────────┘
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `MainWindow.xaml:231-235` | Modify | toastPanel: agregar Grid.RowSpan="5", MinWidth="250", MaxWidth="400", DropShadowEffect, StackPanel con toastIcon + toastMessage |
| `MainWindow.xaml.cs:2662-2670` | Modify | ShowToast: cambiar firma a (string message, ToastType type = .Success, int duration = 5), agregar cancel previous timer, fade animations |
| `MainWindow.xaml.cs:2660` | Add | Declarar campo `_toastTimer` nullable antes de ShowToast |
| `MainWindow.xaml.cs:~3100` | Add | Override `OnClosing(CancelEventArgs e)` para cleanup del timer |
| `MainWindow.xaml.cs` | Add | Enum `ToastType { Success, Warning, Error }` como clase anidada privada |

## Call Site Migration (10 locations)

| Line | Current Message | ToastType建议 | Duration |
|------|-----------------|---------------|----------|
| 2299 | "Orden actualizado" | Success | 5s |
| 2600 | "Acceso renombrado" | Success | 5s |
| 2619 | "Acceso eliminado" | Success | 5s |
| 2736 | "Proyecto creado" | Success | 5s |
| 2817 | "Proyecto actualizado" | Success | 5s |
| 2847 | "Proyecto eliminado" | Success | 5s |
| 2868 | "Proyecto desactivado" | Warning | 5s |
| 2874 | "Proyecto activado" | Success | 5s |
| 2925 | "Descripcion guardada" | Success | 5s |
| 3082 | "Acceso(s) agregado(s): N (M ya existían)" | Warning (si hay existing) / Success | 7s (mensaje largo) |

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | ShowToast con timer cancel, fade callback execution | Test methods internal via InternalsVisibleTo |
| Integration | Timer cleanup en window close | Manual: abrir proyecto, mostrar toast, cerrar ventana antes de 5s |
| E2E | Visibilidad, posicionamiento, wrap de texto | Manual: verificar mensaje largo hace wrap a 400px |

## Migration / Rollout

No migration required (no database/schema changes). El cambio es backwards-compatible:
- ShowToast(string) existente sigue funcionando via overload con ToastType.Success por defecto
- Timer cleanup previene excepción que ya ocurre potencialmente

## Open Questions

- [ ] Ninguno — el diseño sigue exactamente el PRD validado

## Riscos

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Excepción Dispatcher al cerrar ventana durante toast | Media | Cleanup en OnClosing() — _toastTimer?.Stop()?.Dispose() |
| Animaciones fallan en GPUs antiguas (DirectX fallback) | Baja | Try-catch envuelve animation, fallback a Visibility directa |
| Multiple rapid calls: mensaje parpadea | Baja | Cancel previous timer antes de mostrar nuevo |

## Key Implementation Notes

1. **System.Timers namespace**: Con ImplicitUsings enabled en .NET 10, `System.Timers` está disponible. El alias `using Timer = System.Timers.Timer;` ya existe en línea 23.

2. **Iconos por ToastType**:
   - Success: ✓ (verde #4CAF50)
   - Warning: ⚠ (amarillo #FFC107)
   - Error: ✕ (rojo #F44336)

3. **Duración según longitud**:
   ```csharp
   if (message.Length > 50) duration = 7;
   ```

4. **Fade-out completion**:
   ```csharp
   fadeOut.Completed += (s, e) => toastPanel.Visibility = Visibility.Collapsed;
   ```
