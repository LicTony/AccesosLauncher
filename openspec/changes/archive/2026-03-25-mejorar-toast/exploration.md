# Exploración: Mejorar Toast de Notificaciones

## Resumen Ejecutivo

El cambio propone mejorar el componente toast de notificaciones en la pestaña "Proyectos" de AccesosLauncher. Tras verificar el código actual, las afirmaciones del PRD son precisas. El toast actual tiene problemas reales de visibilidad, posicionamiento y experiencia de usuario.

---

## Estado Actual Verificado

### Código Existente (verificado)

| Aspecto | Línea/Archivo | Descripción |
|---------|---------------|-------------|
| `ShowToast()` | MainWindow.xaml.cs:2662 | Método que muestra el toast |
| `toastPanel` | MainWindow.xaml:231 | Border con el UI del toast |
| Timer | MainWindow.xaml.cs:2667 | `new Timer(3000)` - 3 segundos |
| Usings | MainWindow.xaml.cs:23 | `using Timer = System.Timers.Timer;` |

### Problemas Confirmados

1. **Tamaño mínimo**: No hay MinWidth/MaxWidth - solo ajusta al contenido
2. **Posición**: `Grid.Row="0"` sin RowSpan - compite con otros controles
3. **Timer no limpiado**: No hay OnClosing override - posible excepción
4. **Sin animaciones**: DoubleAnimation no se usa en ningún lado del proyecto
5. **Sin iconos**: Solo TextBlock con mensaje, sin soporte para tipos
6. **Scope limitado**: toastPanel está DENTRO del TabItem "Proyectos" - no visible en otras pestañas

---

## Áreas Afectadas

| Archivo | Cambio Propuesto |
|---------|------------------|
| `AccesosLauncher/MainWindow.xaml` | Modificar toastPanel (línea 231) - agregar RowSpan, MinWidth, MaxWidth, DropShadowEffect, StackPanel con icono |
| `AccesosLauncher/MainWindow.xaml.cs` | Modificar ShowToast (línea 2662) - agregar Timer como campo de clase, animaciones fade-in/fade-out, parámetro ToastType |
| `AccesosLauncher/MainWindow.xaml.cs` | Agregar override OnClosing para cleanup del timer |

---

## Enfoques Posibles

### Enfoque 1: PRD Completo (Recomendado)
Implementar todas las mejoras del PRD: posicionamiento, tamaño, animaciones, iconos por tipo, timer como campo.

- **Pros**: Cumple todos los RF, experiencia de usuario consistente
- **Cons**: Mayor implementación (~1.5 horas)
- **Esfuerzo**: Medio

### Enfoque 2: Solo Visibilidad
Mejorar tamaño y posición sin animaciones ni iconos.

- **Pros**: Más simple, menor riesgo
- **Cons**: No cumple RF-04 ni RF-07
- **Esfuerzo**: Bajo

### Enfoque 3: Popup en lugar de toast
Reemplazar el toast por un Popup o Window emergente más elaborado.

- **Pros**: Más flexible, mejor control
- **Cons**: Sobrediseño para el problema, más complejo
- **Esfuerzo**: Alto

---

## Preocupaciones Adicionales no cubiertas por el PRD

1. **Timer disposed**: El código actual crea un Timer sin guardarlo ni disposinglo. Si la ventana se cierra antes de los 3s, `Dispatcher.Invoke` puede lanzar excepción porque el objeto ya no existe.

2. **Scope del toast**: El PRD menciona que el toast solo es visible en "Proyectos" y no lo cambia. Esto está bien para el scope actual, pero si en el futuro se necesita feedback desde otras pestañas, hay que repensar la arquitectura.

3. **Toasts concurrentes**: Si el usuario hace clic rápido en varias operaciones, cada ShowToast crea un nuevo Timer. El propuesta del PRD con `_toastTimer` como campo resuelve esto.

4. **Accesibilidad**: El PRD menciona ratio 4.5:1 pero no especifica cómo validarlo. El color #DD333333 con texto blanco cumple (~13:1 ratio).

---

## Riesgos Identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Excepción en Dispatcher al cerrar ventana | Media | Medio | Agregar OnClosing override |
| Animaciones fallan en GPUs antiguas | Baja | Bajo | Fallback a Visibility simple |
|toast tapa botones interactivos | Baja | Bajo | IsHitTestVisible="False" |

---

## Listo para Propuesta

**Sí** - El PRD está completo y las afirmaciones técnicas fueron verificadas. El cambio es de scope controlado (solo UI de Proyectos) con riesgos manejosos.

La implementación puede proceder directo a SDD Spec basándose en este PRD validado.

---

## Próximo Paso Recomendado

El orchestrator debería invocar `sdd-propose` para crear la propuesta formal del cambio "mejorar-toast". El artifact exploration ya está disponible en engram para esa fase.
