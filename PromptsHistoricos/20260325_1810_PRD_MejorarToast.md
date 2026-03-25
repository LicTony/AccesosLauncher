# PRD: Mejorar visibilidad y posición del Toast en Tab Proyectos

## 1. Resumen Ejecutivo

**Problema:** El toast notification que muestra los mensajes de "Acceso(s) agregado(s)" y "(N ya existían)" tiene problemas de visibilidad:
- Se muestra **demasiado pequeño** (solo el tamaño del texto)
- Posición **arriba a la izquierda** cuando debería estar arriba a la derecha
- Poco tiempo de visualización (3 segundos)
- Sin animación de entrada/salida
- Estilo visual mínimo que pasa desapercibido

**Solución:** Rediseñar el componente toast para mejorar su visibilidad, posición y experiencia de usuario.

---

## 2. Contexto Actual

### 2.1 Ubicación del Código

**Archivo:** `AccesosLauncher\MainWindow.xaml.cs`

- **Línea 2662:** Método `ShowToast(string message)`
- **Línea 3075:** Construcción del mensaje "ya existían"
- **Línea 231 XAML:** Definición del `toastPanel` en `MainWindow.xaml`

### 2.2 Implementación Actual

**XAML:**
```xml
<Border x:Name="toastPanel" Grid.Row="0" HorizontalAlignment="Right" VerticalAlignment="Top"
        Margin="10" Padding="15,10" Background="#FF333333" CornerRadius="5"
        Visibility="Collapsed">
    <TextBlock x:Name="toastMessage" Foreground="White"/>
</Border>
```

**C#:**
```csharp
private void ShowToast(string message)
{
    if (toastPanel == null || toastMessage == null) return;
    toastMessage.Text = message;
    toastPanel.Visibility = Visibility.Visible;
    var timer = new Timer(3000) { AutoReset = false };
    timer.Elapsed += (_, __) => Dispatcher.Invoke(() => toastPanel.Visibility = Visibility.Collapsed);
    timer.Start();
}
```

> **Nota:** El código actual usa `System.Timers.Timer` (alias: `using Timer = System.Timers.Timer;`)

### 2.3 Notas sobre el Estado Actual

- El toast está **DENTRO del TabItem "Proyectos"** — solo es visible cuando esa pestaña está activa
- El toast usa `Grid.Row="0"` sin `RowSpan` — compite por espacio con los botones de acción
- No hay limpieza del timer al cerrar la ventana (posible excepción en `Dispatcher.Invoke`)
- `ShowToast` se llama desde **10 puntos distintos** en el codebase (no solo del mensaje "ya existían")
- **No existe ninguna animación** en todo el proyecto (es código completamente nuevo)

---

## 3. Requerimientos Funcionales

### RF-01: Posicionamiento Correcto
- El toast debe mostrarse en la **esquina superior derecha** del área de contenido de la Tab Proyectos
- Debe superponerse sobre otros controles (usar `Grid.RowSpan` si es necesario)
- Debe mantener un margen consistente de 10-15px desde los bordes

### RF-02: Tamaño Mejorado
- **Ancho mínimo:** 250px
- **Ancho máximo:** 400px (con text wrapping)
- **Padding interno:** 16px horizontal, 12px vertical
- El texto debe hacer wrap si excede el ancho máximo

### RF-03: Tiempo de Visualización
- **Duración base:** 5 segundos (incrementado desde 3s)
- **Duración extendida:** 7 segundos si el mensaje excede 50 caracteres
- El toast debe desaparecer automáticamente después del tiempo configurado

### RF-04: Animaciones
- **Fade-in:** 200ms al aparecer
- **Fade-out:** 300ms al desaparecer
- Animación suave usando `DoubleAnimation` sobre la propiedad `Opacity`

### RF-05: Estilo Visual Mejorado
- **Background:** `#DD333333` (más opaco que el actual `#FF333333`)
- **CornerRadius:** 8px
- **Shadow:** DropShadowEffect con blur radius 10, opacidad 0.5
- **Font size:** 14px (incrementado desde 13px)
- **Font weight:** Regular
- **Icono opcional:** Checkmark ✓ para éxitos, advertencia ⚠ para warnings

### RF-06: Soporte para Múltiples Mensajes
- Si se llama a `ShowToast()` mientras otro toast está visible:
  - Se reemplaza el mensaje actual y se resetea el timer (implementación actualizada cancela el timer anterior)
  - No se encolan mensajes — el toast es feedback transitorio, no una cola de notificaciones

### RF-07: Icono Variable por Tipo de Mensaje
- El icono del toast debe variar según el tipo de operación:
  - ✓ (verde `#4CAF50`) — éxito: agregado, guardado, actualizado, activado
  - ⚠ (amarillo `#FFC107`) — warning: "ya existían", desactivado
  - ✕ (rojo `#F44336`) — error: eliminación fallida, operación fallida
- Implementar mediante sobrecarga: `ShowToast(string message, ToastType type = ToastType.Success, int durationSeconds = 5)`

---

## 4. Requerimientos No Funcionales

### RNF-01: Performance
- Las animaciones deben usar aceleración por hardware
- No debe haber impacto perceptible en el rendimiento de la UI

### RNF-02: Accesibilidad
- El texto debe tener contraste suficiente (ratio mínimo 4.5:1)
- El toast no debe bloquear interacciones con otros controles

### RNF-03: Mantenibilidad
- El código debe estar documentado con XML comments
- Separar la lógica del toast en un método reutilizable
- El timer debe limpiarse en `OnClosing` para evitar excepciones al cerrar la ventana

---

## 5. Criterios de Aceptación

### AC-01: Posición
- ✅ El toast aparece en la esquina superior derecha
- ✅ No se superpone con botones o controles interactivos críticos
- ✅ El margen es consistente en diferentes resoluciones

### AC-02: Visibilidad
- ✅ El toast es claramente visible sobre el fondo oscuro de la aplicación
- ✅ El texto es legible sin esfuerzo
- ✅ El tamaño se ajusta al contenido pero con mínimos definidos

### AC-03: Animaciones
- ✅ Fade-in suave al aparecer
- ✅ Fade-out suave al desaparecer
- ✅ No hay saltos o parpadeos

### AC-04: Timing
- ✅ Permanece visible 5-7 segundos según longitud del mensaje
- ✅ El timer se limpia correctamente si se cierra la ventana

### AC-05: Mensajes de Prueba
- ✅ "Acceso(s) agregado(s): 3 (2 ya existían)" — se ve completo
- ✅ "(5 ya existían)" — centrado y legible
- ✅ Mensajes largos hacen wrap correctamente

---

## 6. Diseño Propuesto

### 6.1 XAML Modificado
```xml
<!-- Modificar el Border existente en línea 231 -->
<Border x:Name="toastPanel" 
        Grid.Row="0" 
        Grid.RowSpan="5"
        HorizontalAlignment="Right" 
        VerticalAlignment="Top"
        Margin="15" 
        Padding="16,12" 
        Background="#DD333333" 
        CornerRadius="8"
        Visibility="Collapsed"
        MinWidth="250" 
        MaxWidth="400"
        IsHitTestVisible="False">
    <Border.Effect>
        <DropShadowEffect BlurRadius="10" 
                          ShadowDepth="2" 
                          Opacity="0.5"/>
    </Border.Effect>
    <StackPanel Orientation="Horizontal">
        <TextBlock x:Name="toastIcon" 
                   Text="✓" 
                   Foreground="#4CAF50" 
                   FontSize="16" 
                   VerticalAlignment="Center"
                   Margin="0,0,10,0"/>
        <TextBlock x:Name="toastMessage" 
                   Foreground="White"
                   FontSize="14"
                   TextWrapping="Wrap"
                   VerticalAlignment="Center"/>
    </StackPanel>
</Border>
```

### 6.2 C# Modificado
```csharp
private Timer? _toastTimer;

private void ShowToast(string message, int durationSeconds = 5)
{
    if (toastPanel == null || toastMessage == null) return;
    
    // Cancelar toast anterior si existe
    _toastTimer?.Stop();
    _toastTimer?.Dispose();
    
    // Configurar mensaje
    toastMessage.Text = message;
    
    // Ajustar duración según longitud del mensaje
    if (message.Length > 50)
        durationSeconds = 7;
    
    // Mostrar con fade-in
    toastPanel.Opacity = 0;
    toastPanel.Visibility = Visibility.Visible;
    
    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
    toastPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    
    // Timer para ocultar
    _toastTimer = new Timer(durationSeconds * 1000) { AutoReset = false };
    _toastTimer.Elapsed += (_, __) => Dispatcher.Invoke(() =>
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        fadeOut.Completed += (s, e) => toastPanel.Visibility = Visibility.Collapsed;
        toastPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    });
    _toastTimer.Start();
}

// Agregar en el evento Window.Closing o OnClosing:
protected override void OnClosing(CancelEventArgs e)
{
    _toastTimer?.Stop();
    _toastTimer?.Dispose();
    base.OnClosing(e);
}
```

---

## 7. Riesgos y Dependencias

### Riesgos
- **Bajo:** El cambio es aislado al UI de la Tab Proyectos
- **Medio:** Las animaciones podrían tener problemas en GPUs antiguas (mitigado con fallback a Visibility)
- **Nota de diseño:** El toast actualmente solo es visible en la pestaña "Proyectos" (está dentro del TabItem). Si en el futuro se necesita feedback desde otras pestañas, el toast debería moverse a nivel Window. Este PRD mantiene el scope dentro de "Proyectos" por simplicidad.

### Dependencias
- .NET 10 (ya disponible en el proyecto)
- WPF standard (sin librerías externas adicionales)

---

## 8. Métricas de Éxito

- ✅ El 100% de los usuarios de testing notan el toast inmediatamente
- ✅ El mensaje se lee completo sin necesidad de "forzar" la vista
- ✅ No hay reportes de "no me llegó la confirmación" después del cambio

---

## 9. Timeline Estimado

- **Análisis:** 15 min (completado)
- **Implementación:** 60 min (incrementado por ToastType enum y limpieza de timer)
- **Testing manual:** 20 min
- **Total:** ~1.5 horas

---

## 10. Notas Adicionales

- Considerar extraer el toast a un control reusable si se planea usar en otras tabs
- El icono del toast podría cambiar según el tipo de mensaje (éxito, error, warning, info)
- Futuro: Agregar botón "Deshacer" en el toast para operaciones críticas
