# Tasks: Mejorar Toast de Notificaciones

## Phase 1: Foundation (ToastType Enum)

- [x] 1.1 Agregar enum privado `ToastType { Success, Warning, Error }` en MainWindow.xaml.cs (cerca del campo _toastTimer)

## Phase 2: XAML ( toastPanel modifications )

- [x] 2.1 Modificar toastPanel Border (línea 231): agregar `Grid.RowSpan="5"`, `MinWidth="250"`, `MaxWidth="400"`, `IsHitTestVisible="False"`
- [x] 2.2 Agregar DropShadowEffect al Border: `BlurRadius="10"`, `ShadowDepth="2"`, `Opacity="0.5"`
- [x] 2.3 Cambiar Background de `#FF333333` a `#DD333333`, CornerRadius de 5 a 8
- [x] 2.4 Reemplazar TextBlock toastMessage con StackPanel: agregar TextBlock toastIcon (antes del mensaje) con símbolo ✓, verde, FontSize=16, Margin=0,0,10,0
- [x] 2.5 Agregar TextWrapping="Wrap" y FontSize="14" al toastMessage

## Phase 3: C# Core (ShowToast rewrite)

- [x] 3.1 Declarar campo `_toastTimer` nullable (tipo `Timer?`) como campo de clase
- [x] 3.2 Reescribir ShowToast: cambiar firma a `ShowToast(string message, ToastType type = ToastType.Success, int duration = 5)`
- [x] 3.3 En ShowToast: cancelar timer anterior (`_toastTimer?.Stop()`?.Dispose()`) antes de mostrar nuevo
- [x] 3.4 En ShowToast: configurar icono según ToastType (✓ verde/⚠ amarillo/✕ rojo) y setear toastMessage.Text
- [x] 3.5 Implementar fade-in 200ms: setear Opacity=0, Visibility=Visible, DoubleAnimation(0→1)
- [x] 3.6 Calcular duración: si message.Length > 50 → duration = 7s
- [x] 3.7 Configurar timer con duración calculada, AutoReset=false, crear elapsed handler con fade-out 300ms
- [x] 3.8 Implementar fade-out: DoubleAnimation(1→0), en Completed setear Visibility.Collapsed

## Phase 4: C# Cleanup (OnClosing)

- [x] 4.1 Agregar override `OnClosing(CancelEventArgs e)` a MainWindow
- [x] 4.2 En OnClosing: cleanup timer (`_toastTimer?.Stop()`?.Dispose()`) antes de llamar base.OnClosing(e)

## Phase 5: Call Site Migration (10 locations)

- [x] 5.1 Línea 2299: "Orden actualizado" → ToastType.Success (5s)
- [x] 5.2 Línea 2600: "Acceso renombrado" → ToastType.Success (5s)
- [x] 5.3 Línea 2619: "Acceso eliminado" → ToastType.Warning (5s) [decisión: eliminación es warning, no success]
- [x] 5.4 Línea 2736: "Proyecto creado" → ToastType.Success (5s)
- [x] 5.5 Línea 2817: "Proyecto actualizado" → ToastType.Success (5s)
- [x] 5.6 Línea 2847: "Proyecto eliminado" → ToastType.Warning (5s)
- [x] 5.7 Línea 2868: "Proyecto desactivado" → ToastType.Warning (5s)
- [x] 5.8 Línea 2874: "Proyecto activado" → ToastType.Success (5s)
- [x] 5.9 Línea 2925: "Descripcion guardada" → ToastType.Success (5s)
- [x] 5.10 Línea 3082: "Acceso(s) agregado(s): N (M ya existían)" → ToastType.Warning (7s, mensaje largo)

## Phase 6: Testing Manual

- [x] 6.1 AC-01: Verificar posición esquina superior derecha (Grid.RowSpan="5", Margin="15")
- [x] 6.2 AC-02: Verificar tamaño 250-400px con mensaje corto y largo haciendo wrap
- [x] 6.3 AC-03: Verificar fade-in 200ms y fade-out 300ms suaves
- [x] 6.4 AC-04: Verificar timer 5s mensaje corto, 7s mensaje largo, y cleanup al cerrar ventana antes de expire
- [x] 6.5 AC-05: Probar mensajes "(5 ya existían)", "Acceso(s) agregado(s): 3 (2 ya existían)" legibles y con icono correcto

## Implementation Order

1. Enum (1.1) → necesario para ShowToast
2. XAML (2.1-2.5) → necesario para que ShowToast tenga los elementos (toastIcon)
3. C# core (3.1-3.8) → dependencias resueltas
4. Cleanup (4.1-4.2) → dependencia: campo timer declarado en 3.1
5. Call sites (5.1-5.10) → dependencia: firma de ShowToast modificada en 3.2
6. Testing (6.1-6.5) → post-implementación

**Total: 22 tasks compactos, ejecutables en sesiones de 30-60 min cada uno.**
