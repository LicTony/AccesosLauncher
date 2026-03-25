# Delta Spec: Mejorar Toast de Notificaciones

## Domain: UI/Toast Notifications

## ADDED Requirements

### Requirement: RF-01 - Posicionamiento en Esquina Superior Derecha

El toast DEBE mostrarse en la esquina superior derecha del área de contenido de la pestaña Proyectos, superpuesto sobre otros controles con margen consistente de 15px desde los bordes.

#### Scenario: Toast visible en posición correcta

- GIVEN El usuario está en la pestaña "Proyectos"
- WHEN Se invoca `ShowToast("Mensaje de prueba")`
- THEN El toast aparece en la esquina superior derecha con `Grid.RowSpan="5"`, `HorizontalAlignment="Right"`, `VerticalAlignment="Top"`, `Margin="15"`
- AND `IsHitTestVisible="False"` para evitar bloquear interacciones

#### Scenario: Toast no tapa controles críticos

- GIVEN El toast está visible en posición de esquina
- WHEN El usuario intenta hacer clic en botones cercanos
- THEN Los controles permanecen interactuables porque `IsHitTestVisible="False"`

---

### Requirement: RF-02 - Tamaño Mejorado

El toast DEBE tener un ancho mínimo de 250px y máximo de 400px con ajuste automático del texto. El texto DEBE hacer wrap si excede el ancho máximo.

#### Scenario: Toast con mensaje corto

- GIVEN Un mensaje de menos de 50 caracteres
- WHEN Se muestra el toast
- THEN El toast tiene `MinWidth="250"`, `MaxWidth="400"`, y `TextWrapping="Wrap"`

#### Scenario: Toast con mensaje largo hace wrap

- GIVEN Un mensaje mayor a 50 caracteres: "Acceso(s) agregado(s): 3 (2 ya existían)"
- WHEN Se renderiza el toast
- THEN El texto hace wrap automáticamente sin exceder 400px de ancho

---

### Requirement: RF-03 - Tiempo de Visualización

El toast DEBE permanecer visible durante 5 segundos por defecto, o 7 segundos si el mensaje excede 50 caracteres. El timer DEBE ser preciso y confiable.

#### Scenario: Mensaje corto visible 5 segundos

- GIVEN Un mensaje de 30 caracteres
- WHEN Se invoca `ShowToast(mensaje)`
- THEN El toast se oculta automáticamente después de 5 segundos

#### Scenario: Mensaje largo visible 7 segundos

- GIVEN Un mensaje de 60 caracteres
- WHEN Se invoca `ShowToast(mensaje)`
- THEN El toast se oculta automáticamente después de 7 segundos (duración extendida)

---

### Requirement: RF-04 - Animaciones de Fade

El toast DEBE tener una animación de fade-in de 200ms al aparecer y una animación de fade-out de 300ms al desaparecer. Las animaciones DEBEN ser suaves sin saltos ni parpadeos.

#### Scenario: Fade-in al aparecer

- GIVEN El toast está oculto (`Visibility.Collapsed`, `Opacity=0`)
- WHEN Se invoca `ShowToast(mensaje)`
- THEN La opacidad anima de 0 a 1 en 200ms usando `DoubleAnimation`

#### Scenario: Fade-out al desaparecer

- GIVEN El toast está visible con `Opacity=1`
- WHEN El timer expira después de 5-7 segundos
- THEN La opacidad anima de 1 a 0 en 300ms y luego se colapsa el visibility

---

### Requirement: RF-05 - Estilo Visual Mejorado

El toast DEBE tener un estilo visual mejorado con background semitransparente (#DD333333), corner radius de 8px, y DropShadowEffect con blur radius 10 y opacidad 0.5.

#### Scenario: Toast con estilo aplicado

- GIVEN El toastPanel está en el XAML
- WHEN Se renderiza el toast
- THEN Tiene `Background="#DD333333"`, `CornerRadius="8"`, y `DropShadowEffect(BlurRadius=10, Opacity=0.5, ShadowDepth=2)`

---

### Requirement: RF-06 - Reemplazo de Mensajes (No Cola)

CUANDO se invoca `ShowToast()` mientras un toast está visible, DEBE reemplazar el mensaje actual y resetear el timer. NO DEBE encolar múltiples mensajes.

#### Scenario: Nuevo toast reemplaza anterior

- GIVEN Un toast está visible con mensaje "Mensaje 1" y timer corriendo
- WHEN Se invoca `ShowToast("Mensaje 2")`
- THEN El mensaje cambia a "Mensaje 2", el timer anterior se cancela, y nuevo timer de 5s comienza

---

### Requirement: RF-07 - Icono Variable por Tipo de Mensaje

El toast DEBE mostrar un icono diferente según el tipo de mensaje: ✓ (verde #4CAF50) para éxito, ⚠ (amarillo #FFC107) para warning, ✕ (rojo #F44336) para error.

#### Scenario: Toast de éxito

- GIVEN Se invoca `ShowToast("Operación exitosa", ToastType.Success)`
- THEN El icono muestra ✓ en color verde #4CAF50

#### Scenario: Toast de warning

- GIVEN Se invoca `ShowToast("2 ya existían", ToastType.Warning)`
- THEN El icono muestra ⚠ en color amarillo #FFC107

#### Scenario: Toast de error

- GIVEN Se invoca `ShowToast("Operación fallida", ToastType.Error)`
- THEN El icono muestra ✕ en color rojo #F44336

---

### Requirement: RF-08 - Timer Cleanup en OnClosing

El sistema DEBE limpiar el timer del toast en el método `OnClosing()` de la ventana para evitar excepciones de Dispatcher cuando se cierra la ventana antes de que expire el timer.

#### Scenario: Ventana cerrada antes de que expire el timer

- GIVEN El toast está visible y el timer está corriendo
- WHEN El usuario cierra la ventana
- THEN El método `OnClosing()` cancela y dispone `_toastTimer` antes de cerrar

---

### Requirement: RF-09 - Enum ToastType Definición

El sistema DEBE definir un enum `ToastType` con los valores: `Success`, `Warning`, `Error`.

#### Scenario: ToastType enum disponible

- GIVEN El código compila
- WHEN Se referencia `ToastType.Success`, `ToastType.Warning`, o `ToastType.Error`
- THEN El enum está definido y accesible con los tres valores

---

## Acceptance Criteria Coverage

| Criteria | Covered | Scenario(s) |
|----------|---------|-------------|
| AC-01: Posición esquina superior derecha | ✅ | RF-01 |
| AC-02: Visibilidad mejorada | ✅ | RF-02, RF-05 |
| AC-03: Animaciones suaves | ✅ | RF-04 |
| AC-04: Timing 5-7s con cleanup | ✅ | RF-03, RF-08 |
| AC-05: Mensajes de prueba visibles | ✅ | RF-02 |

## Notes

- Es un dominio nuevo (no existen specs previas de toast)
- El scope se limita a la pestaña "Proyectos" (dentro del TabItem)
- La implementación debe seguir el diseño propuesto en el PRD para XAML y C#
