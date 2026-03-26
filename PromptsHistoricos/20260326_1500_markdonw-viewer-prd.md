# PRD: Visualizador de Markdown con WebView2

**Proyecto:** AccesosLauncher  
**Feature:** Reemplazo de RichTextBox por visualizador de Markdown  
**Fecha:** 2026-03-26  
**Estado:** Propuesta

---

## 1. Resumen Ejecutivo

Reemplazar el control `RichTextBox` actual utilizado para mostrar/editar la descripción larga de los proyectos por un visualizador de Markdown basado en **WebView2 + Markdig**, permitiendo a los usuarios escribir y previsualizar descripciones en formato Markdown con renderizado profesional.

---

## 2. Problema Actual

### 2.1 Situación Actual

- La descripción larga se muestra en un `RichTextBox` (`rtxtDescripcion`)
- El control permite edición básica de texto plano
- **No hay soporte para formato** (negritas, listas, código, tablas, etc.)
- La experiencia de edición es limitada y poco intuitiva

### 2.2 Limitaciones

| Limitación | Impacto |
|------------|---------|
| Sin formato Markdown | Descripciones planas, sin estructura visual |
| Sin syntax highlighting | Código no se distingue del texto normal |
| Sin previsualización | No se ve cómo quedará el contenido final |
| Edición inline confusa | El RichTextBox en modo edición no es claro para el usuario |

---

## 3. Objetivos

### 3.1 Objetivo Principal

Proporcionar un visualizador de Markdown que permita:
- **Previsualizar** la descripción larga con renderizado profesional
- **Editar** en formato Markdown con feedback visual inmediato
- **Guardar** el contenido Markdown en la base de datos

### 3.2 Objetivos Secundarios

1. Mantener compatibilidad con descripciones existentes (texto plano)
2. Preservar el flujo de edición actual (botones editar/guardar/cancelar)
3. Minimizar cambios en la arquitectura existente
4. Agregar ayuda contextual sobre sintaxis Markdown

---

## 4. Alcance

### 4.1 Incluye

- ✅ Integración de `Microsoft.Web.WebView2` en la UI
- ✅ Integración de `Markdig` para parsing Markdown → HTML
- ✅ Vista de previsualización con estilo GitHub-like
- ✅ Vista de edición con textarea para Markdown
- ✅ Botones para alternar entre vista previa y edición
- ✅ Syntax highlighting para bloques de código (highlight.js)
- ✅ Help panel con referencia rápida de Markdown
- ✅ Migración de contenido existente (texto plano → Markdown válido)

### 4.2 No Incluye

- ❌ Editor WYSIWYG (será solo Markdown raw + preview)
- ❌ Soporte para imágenes embebidas (solo URLs)
- ❌ Exportación a otros formatos (PDF, HTML, etc.)
- ❌ Colaboración en tiempo real
- ❌ Versionado de descripciones

---

## 5. Requisitos Funcionales

### RF-01: Previsualización de Markdown

| Campo | Valor |
|-------|-------|
| ID | RF-01 |
| Descripción | El sistema debe renderizar el contenido Markdown en formato HTML visual |
| Prioridad | Alta |
| Criterios de Aceptación | <ul><li>El contenido se renderiza con estilos similares a GitHub</li><li>Soporta headers, negritas, cursivas, listas, tablas, código, blockquotes, enlaces</li><li>El renderizado es legible y profesional</li></ul> |

### RF-02: Edición de Markdown

| Campo | Valor |
|-------|-------|
| ID | RF-02 |
| Descripción | El usuario debe poder editar el contenido Markdown en un textarea |
| Prioridad | Alta |
| Criterios de Aceptación | <ul><li>Existe un modo edición con textarea editable</li><li>El textarea muestra el Markdown raw</li><li>Se puede alternar entre modo edición y vista previa</li></ul> |

### RF-03: Persistencia

| Campo | Valor |
|-------|-------|
| ID | RF-03 |
| Descripción | El contenido Markdown debe guardarse en la base de datos |
| Prioridad | Alta |
| Criterios de Aceptación | <ul><li>Al guardar, el Markdown se almacena en `descripcion_larga`</li><li>El contenido se recupera correctamente al seleccionar otro proyecto</li><li>No hay pérdida de datos al cancelar edición</li></ul> |

### RF-04: Compatibilidad con Contenido Existente

| Campo | Valor |
|-------|-------|
| ID | RF-04 |
| Descripción | Las descripciones existentes (texto plano) deben mostrarse correctamente |
| Prioridad | Media |
| Criterios de Aceptación | <ul><li>Texto plano se muestra como párrafo válido</li><li>No se rompen descripciones legacy</li><li>No requiere migración manual</li></ul> |

### RF-05: Ayuda de Sintaxis

| Campo | Valor |
|-------|-------|
| ID | RF-05 |
| Descripción | El sistema debe proveer referencia de sintaxis Markdown |
| Prioridad | Baja |
| Criterios de Aceptación | <ul><li>Panel o tooltip con ejemplos de sintaxis</li><li>Accesible desde la vista de edición</li><li>Incluye ejemplos de headers, listas, código, tablas</li></ul> |

### RF-06: Syntax Highlighting

| Campo | Valor |
|-------|-------|
| ID | RF-06 |
| Descripción | Los bloques de código deben tener resaltado de sintaxis |
| Prioridad | Media |
| Criterios de Aceptación | <ul><li>Se detecta el lenguaje en bloques fenced (```csharp)</li><li>Se aplica highlight.js para colorear el código</li><li>Soporta al menos: C#, JavaScript, Python, Bash, JSON, XML</li></ul> |

---

## 6. Requisitos No Funcionales

### RNF-01: Rendimiento

| Campo | Valor |
|-------|-------|
| ID | RNF-01 |
| Descripción | El renderizado debe ser rápido |
| Métrica | < 500ms para descripciones de hasta 10KB |

### RNF-02: Compatibilidad

| Campo | Valor |
|-------|-------|
| ID | RNF-02 |
| Descripción | Debe funcionar en Windows 10 y Windows 11 |
| Requisito | WebView2 runtime debe estar presente (viene preinstalado en Windows 10/11 actualizados) |

### RNF-03: Memoria

| Campo | Valor |
|-------|-------|
| ID | RNF-03 |
| Descripción | El impacto de memoria debe ser razonable |
| Métrica | < 100MB adicionales por instancia de WebView2 |

### RNF-04: Mantenibilidad

| Campo | Valor |
|-------|-------|
| ID | RNF-04 |
| Descripción | El código debe seguir convenciones del proyecto |
| Requisito | Código en C# 12, nullable enabled, sin MVVM framework |

### RNF-05: Accesibilidad

| Campo | Valor |
|-------|-------|
| ID | RNF-05 |
| Descripción | La UI debe ser accesible |
| Requisito | Tooltips descriptivos, labels claros, contraste adecuado |

---

## 7. Arquitectura Técnica

### 7.1 Componentes

```
┌─────────────────────────────────────────────────────────┐
│                    MainWindow.xaml                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │            Panel de Descripción                   │  │
│  │  ┌─────────────┐  ┌─────────────────────────────┐ │  │
│  │  │   TextArea  │  │        WebView2             │ │  │
│  │  │   (Edición) │  │     (Previsualización)      │ │  │
│  │  │             │  │                             │ │  │
│  │  └─────────────┘  └─────────────────────────────┘ │  │
│  │  [👁️ Preview] [✏️ Edit] [❓ Ayuda]                │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                   Markdig Parser                        │
│         Markdown → HTML con extensiones                 │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│              WebView2 (Chromium Embedded)               │
│    Renderiza HTML + CSS (github-markdown.css)          │
│    + highlight.js para código                           │
└─────────────────────────────────────────────────────────┘
```

### 7.2 Dependencias NuGet

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Markdig` | 0.37.0+ | Parser Markdown → HTML |
| `Microsoft.Web.WebView2` | 1.0.2210.29+ | Control de renderizado |

### 7.3 Recursos Embebidos

| Recurso | Propósito |
|---------|-----------|
| `github-markdown.css` | Estilos de visualización GitHub-like |
| `highlight.js` (CDN o local) | Syntax highlighting para código |

---

## 8. Diseño de UI/UX

### 8.1 Estados de la Vista

#### Estado: Solo Lectura (Default)
```
┌─────────────────────────────────────────────┐
│  Descripción                                │
│  ┌─────────────────────────────────────────┐│
│  │                                         ││
│  │   [Contenido renderizado en Markdown]   ││
│  │                                         ││
│  │                                         ││
│  └─────────────────────────────────────────┘│
│  [✏️ Editar]                                │
└─────────────────────────────────────────────┘
```

#### Estado: Edición
```
┌─────────────────────────────────────────────┐
│  Descripción                                │
│  ┌─────────────────────────────────────────┐│
│  │ # Título                                ││
│  │ **Texto** en negrita                    ││
│  │ - Lista                                 ││
│  │ ```csharp                               ││
│  │ code here                               ││
│  │ ```                                     ││
│  │                                         ││
│  └─────────────────────────────────────────┘│
│  [👁️ Preview] [❓ Ayuda] [💾 Guardar] [❌ Cancelar]│
└─────────────────────────────────────────────┘
```

### 8.2 Help Panel (Modal o Sidebar)
```
┌─────────────────────────────────────────────┐
│  Sintaxis Markdown                          │
│  ─────────────────────────────────────────  │
│  # Título           **Negrita**             │
│  ## Subtítulo       *Cursiva*               │
│  - Lista            [Link](url)             │
│  1. Numerada        `Código inline`         │
│  ```lenguaje        > Blockquote            │
│  código             | Tabla |               │
│  ```                                        │
│  ─────────────────────────────────────────  │
│  [Cerrar]                                   │
└─────────────────────────────────────────────┘
```

---

## 9. Modelo de Datos

### 9.1 Cambios en Database

**No se requieren cambios.** El campo `descripcion_larga` (TEXT) almacena el contenido Markdown como string.

### 9.2 Cambios en Modelo Proyecto

**No se requieren cambios.** La propiedad `DescripcionLarga` (string) contendrá Markdown en lugar de texto plano.

---

## 10. Migración de Datos

### 10.1 Estrategia

- **Contenido existente:** Se trata como Markdown válido (texto plano es Markdown válido)
- **No requiere migración:** El parser de Markdig renderiza texto plano correctamente
- **Upgrade transparente:** Los usuarios ven el mismo contenido, ahora con mejor formato si agregan sintaxis

---

## 11. Riesgos y Mitigaciones

| Riesgo | Impacto | Probabilidad | Mitigación |
|--------|---------|--------------|------------|
| WebView2 no está instalado | Alto | Baja | Incluir detector + link de descarga; Windows 10/11 modernos ya lo incluyen |
| Performance en descripciones grandes | Medio | Media | Limitar preview a 50KB; lazy loading |
| CSS/JS externo no disponible | Bajo | Baja | Incluir recursos como embebidos (Embedded Resource) |
| Usuarios no conocen Markdown | Medio | Alta | Incluir help panel con referencia rápida |
| Imágenes rotas (URLs inválidas) | Bajo | Media | No bloquear render; mostrar alt text si falla |

---

## 12. Criterios de Aceptación Generales

1. ✅ El visualizador renderiza correctamente Markdown básico (headers, párrafos, listas)
2. ✅ El visualizador renderiza código con syntax highlighting
3. ✅ El visualizador renderiza tablas Markdown
4. ✅ Se puede alternar entre modo edición y vista previa sin perder datos
5. ✅ Al guardar, el contenido persiste en la base de datos
6. ✅ Al cancelar, se restaura el contenido original
7. ✅ Contenido legacy (texto plano) se visualiza correctamente
8. ✅ La UI es consistente con el estilo dark theme de la aplicación
9. ✅ No hay regresiones en otras funcionalidades del proyecto
10. ✅ Tests existentes continúan pasando

---

## 13. Métricas de Éxito

| Métrica | Target |
|---------|--------|
| Tiempo de renderizado inicial | < 500ms |
| Tiempo de switch edición ↔ preview | < 200ms |
| Uso de memoria adicional | < 100MB |
| Compatibilidad con Markdown CommonMark | 95%+ |

---

## 14. Dependencias Externas

### 14.1 WebView2 Runtime

- **Requisito:** Microsoft Edge WebView2 Runtime
- **Disponibilidad:** Preinstalado en Windows 10 1803+ y Windows 11
- **Fallback:** Proveer link de descarga: https://developer.microsoft.com/en-us/microsoft-edge/webview2/

### 14.2 github-markdown.css

- **Licencia:** MIT
- **Fuente:** https://github.com/sindresorhus/github-markdown-css
- **Versión:** 5.5.0+

### 14.3 highlight.js

- **Licencia:** BSD-3-Clause
- **Fuente:** https://highlightjs.org/
- **Versión:** 11.9.0+

---

## 15. Timeline Estimado

| Fase | Duración | Actividades |
|------|----------|-------------|
| Setup | 0.5 días | Instalar paquetes, configurar WebView2, recursos |
| UI | 1 día | Diseñar XAML, modos edición/preview, botones |
| Integración | 1 día | Markdig parser, WebView2 binding, eventos |
| Styling | 0.5 días | CSS GitHub-like, dark theme, highlight.js |
| Testing | 0.5 días | Tests manuales, edge cases, performance |
| **Total** | **3.5 días** | |

---

## 16. Apéndices

### A. Ejemplo de Markdown Soportado

```markdown
# Nombre del Proyecto

## Descripción
Este es un proyecto **importante** con *énfasis*.

### Características
- Feature 1
- Feature 2
- Feature 3

### Código de Ejemplo
```csharp
public class Ejemplo {
    public void Metodo() {
        Console.WriteLine("Hola");
    }
}
```

### Tabla
| Columna 1 | Columna 2 |
|-----------|-----------|
| Valor 1   | Valor 2   |

> Esto es un blockquote

[Enlace a documentación](https://example.com)
```

### B. Referencias

- [Markdig Documentation](https://github.com/xoofx/markdig)
- [WebView2 Documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/)
- [GitHub Markdown CSS](https://github.com/sindresorhus/github-markdown-css)
- [highlight.js](https://highlightjs.org/)
- [CommonMark Spec](https://spec.commonmark.org/)

---

## 17. Aprobaciones

| Rol | Nombre | Fecha | Estado |
|-----|--------|-------|--------|
| Product Owner | | | Pendiente |
| Tech Lead | | | Pendiente |
| QA | | | Pendiente |

---

**Documento elaborado por:** Asistente AI  
**Revisión:** Pendiente
