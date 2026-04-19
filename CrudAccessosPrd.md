# PRD - CRUD de Tipos de Acceso (TipoCarpeta)

## Propósito
Permitir a los usuarios definir sus propios tipos de acceso (categorías) para las carpetas en la pestaña principal, rompiendo la limitación de tener solo "Personal", "Laboral" y "Ambos", pero manteniendo la lógica funcional de estos tres tipos base.

## Alcance
Este cambio afecta a la pestaña de **Accesos**, al filtrado de carpetas, al menú contextual de directorios y a la persistencia de configuraciones de usuario.

## Requerimientos Funcionales

### RF-01: Gestión Dinámica de Tipos
- El sistema debe permitir crear, editar y eliminar tipos de acceso personalizados.
- Los tipos **Personal**, **Laboral** y **Ambos** son protegidos: no se pueden editar ni eliminar.
- Interfaz de CRUD integrada en la pestaña de **Configuración**.

### RF-02: Persistencia de Tipos
- La lista de tipos personalizados se debe guardar en `usersettings.json`.
- La asignación de un tipo a una carpeta se persistirá mediante archivos marcadores dentro de la propia carpeta:
    - Tipos base: `.personal`, `.mixta` (se mantiene retrocompatibilidad).
    - Tipos personalizados: `.tipo_NombreDelTipo`.

### RF-03: Filtrado y UI
- El ComboBox `TipoCarpetaComboBox` debe cargar dinámicamente todos los tipos (Base + Personalizados).
- Al seleccionar un tipo personalizado, la lista solo mostrará las carpetas que contengan el archivo marcador correspondiente.

### RF-04: Atajo de Teclado (Ctrl + T)
- Al presionar `Ctrl + T`, el selector de tipo debe ciclar por **todos** los tipos disponibles en el orden: Base (Personal, Laboral, Ambos) -> Personalizados (en orden alfabético).

### RF-05: Menú Contextual de Carpetas
- El submenú "Tipo" en el clic derecho de una carpeta debe listar todas las opciones disponibles.
- Al seleccionar un tipo:
    - Si es un tipo base, se sigue la lógica actual (crear/borrar `.personal` y `.mixta`).
    - Si es un tipo personalizado, se borran otros marcadores de tipo y se crea `.tipo_NombreDelTipo`.
    - Debe mostrar un check (✓) al lado del tipo actual de la carpeta.

### RF-06: Contador Dinámico
- El texto "Mostrando X elemento(s)" debe reflejar siempre la cantidad de elementos visibles actualmente.
- El contador debe actualizarse automáticamente al cambiar el tipo de acceso (filtro de categoría), al realizar una búsqueda, o al modificar la lista de elementos.

## Requerimientos No Funcionales
- **Retrocompatibilidad**: Las carpetas marcadas actualmente como Personal/Laboral/Ambos deben seguir funcionando sin cambios.
- **Robustez**: Si se elimina un tipo personalizado que estaba siendo usado, la carpeta debe volver por defecto a ser tratada como "Laboral" (comportamiento estándar para carpetas sin marcador).
- **Rendimiento**: El filtrado debe seguir siendo instantáneo mediante `CollectionViewSource.Filter`.

## Consideraciones Técnicas
- **Modelo de Datos**: `UserSettings` agregará una propiedad `List<string> CustomAccessTypes`.
- **Lógica de Matching**: Refactorizar `MatchesTipoCarpeta` para que acepte `string` o maneje la lógica extendida.
- **UI Dinámica**: Los `MenuItem` del menú contextual se generarán en el evento `Loaded` del menú.

## Criterios de Aceptación
1. Puedo crear un tipo llamado "Proyectos_AI".
2. Puedo asignar una carpeta al tipo "Proyectos_AI" vía clic derecho.
3. Al elegir "Proyectos_AI" en el combo superior, solo veo esa carpeta.
4. `Ctrl + T` pasa por Personal -> Laboral -> Ambos -> Proyectos_AI.
5. Si cierro y abro el programa, mis tipos personalizados siguen ahí.
