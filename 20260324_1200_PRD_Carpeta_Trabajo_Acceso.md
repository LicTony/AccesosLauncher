# Product Requirements Document (PRD)

## Acceso Tipo "Carpeta de Trabajo"

| **Campo** | **Valor** |
|-----------|-----------|
| **Proyecto** | AccesosLauncher |
| **Versión** | 2.0 |
| **Fecha** | 24 de marzo de 2026 |
| **Estado** | Propuesta |
| **Prioridad** | Alta |

---

## 1. Resumen Ejecutivo

Agregar un nuevo tipo de acceso llamado **"Carpeta de Trabajo"** que permita al usuario configurar accesos directos a directorios de proyecto. Al ejecutarse, este tipo de acceso abrirá **Windows Terminal** con tres pestañas preconfiguradas (lazygit, qwen, shell) en el contexto del directorio especificado.

---

## 2. Objetivo del Producto

### 2.1 Problema a Resolver

Los desarrolladores necesitan abrir frecuentemente múltiples herramientas de línea de comandos en un directorio de proyecto específico. Actualmente, este proceso requiere:
- Abrir una terminal manualmente
- Navegar al directorio del proyecto
- Abrir múltiples pestañas/ventanas para cada herramienta
- Configurar cada herramienta individualmente

### 2.2 Solución Propuesta

Un tipo de acceso que, con un solo click:
- Valida que el directorio exista
- Abre Windows Terminal automáticamente
- Configura 3 pestañas con herramientas esenciales del flujo de desarrollo
- Establece el directorio de trabajo en todas las pestañas

---

## 3. Requisitos Funcionales

### RF-001: Tipo de Acceso "Carpeta de Trabajo"
| **ID** | RF-001 |
|--------|--------|
| **Descripción** | El sistema debe permitir crear accesos de tipo "Carpeta de Trabajo" |
| **Criterio de Aceptación** | En el formulario de "Agregar Acceso", el usuario puede seleccionar "Carpeta de Trabajo" como tipo de acceso |

### RF-002: Campo de Ruta Obligatoria
| **ID** | RF-002 |
|--------|--------|
| **Descripción** | El tipo de acceso "Carpeta de Trabajo" debe solicitar una ruta de directorio como campo obligatorio |
| **Criterio de Aceptación** | El formulario valida que la ruta no esté vacía y corresponde a un directorio existente antes de guardar el acceso |

### RF-003: Ejecución de Windows Terminal
| **ID** | RF-003 |
|--------|--------|
| **Descripción** | Al hacer click en un acceso tipo "Carpeta de Trabajo", el sistema debe ejecutar Windows Terminal con configuración específica |
| **Criterio de Aceptación** | Se abre Windows Terminal con las pestañas configuradas en el archivo `CarpetaDeTrabajoHerramientas.json` |

### RF-004: Configuración de Pestañas desde Archivo JSON
| **ID** | RF-004 |
|--------|--------|
| **Descripción** | Cada pestaña debe tener configuración específica de orden, título, color y comando, leída desde un archivo de configuración JSON |
| **Criterio de Aceptación** | El sistema lee el archivo `CarpetaDeTrabajoHerramientas.json` y configura cada pestaña según los campos: `Orden`, `Title`, `TabColor`, `Parametro` |

### RF-005: Directorio de Trabajo Compartido
| **ID** | RF-005 |
|--------|--------|
| **Descripción** | Todas las pestañas deben abrirse en el directorio especificado en el acceso |
| **Criterio de Aceptación** | Las pestañas tienen como working directory el path configurado en el acceso |

### RF-006: Ordenamiento de Pestañas
| **ID** | RF-006 |
|--------|--------|
| **Descripción** | Las pestañas deben crearse en el orden especificado en el archivo de configuración |
| **Criterio de Aceptación** | Las pestañas se generan ordenadas ascendentemente por el campo `Orden` |

### RF-007: Manejo de Errores - Archivo JSON No Válido
| **ID** | RF-007 |
|--------|--------|
| **Descripción** | El sistema debe validar que el archivo JSON exista y tenga formato válido |
| **Criterio de Aceptación** | Si el archivo no existe o es inválido, se muestra un MessageBox con mensaje descriptivo y se usan valores por defecto |

### RF-008: Manejo de Errores - Directorio No Existente
| **ID** | RF-008 |
|--------|--------|
| **Descripción** | El sistema debe validar que el directorio exista antes de intentar abrir la terminal |
| **Criterio de Aceptación** | Si el directorio no existe, se muestra un MessageBox con mensaje: "El directorio no existe: {ruta}" y icono de advertencia |

### RF-009: Manejo de Errores - Error Genérico
| **ID** | RF-009 |
|--------|--------|
| **Descripción** | El sistema debe capturar y mostrar errores inesperados durante la ejecución |
| **Criterio de Aceptación** | Cualquier excepción no manejada muestra un MessageBox con el mensaje de error y icono de error |

### RF-010: Valores por Defecto
| **ID** | RF-010 |
|--------|--------|
| **Descripción** | Si el archivo JSON no existe o está vacío, el sistema debe usar una configuración por defecto |
| **Criterio de Aceptación** | Se usan 3 pestañas por defecto: lazygit, qwen, shell con sus colores y comandos estándar |

---

## 4. Requisitos Técnicos

### RT-001: Formato de URL
| **ID** | RT-001 |
|--------|--------|
| **Descripción** | La ruta debe ser procesada como una URL de tipo `file://` |
| **Implementación** | Usar `System.Uri` para convertir `file:///` URLs a paths locales |

### RT-002: Comando de Windows Terminal
| **ID** | RT-002 |
|--------|--------|
| **Descripción** | El sistema debe invocar `wt.exe` con argumentos específicos |
| **Implementación** | Usar `ProcessStartInfo` con `UseShellExecute = true` |

### RT-003: Archivo de Configuración JSON
| **ID** | RT-003 |
|--------|--------|
| **Descripción** | La configuración de herramientas debe leerse desde un archivo JSON externo |
| **Implementación** | Usar `System.Text.Json` para deserializar `CarpetaDeTrabajoHerramientas.json` |

### RT-004: Ubicación del Archivo JSON
| **ID** | RT-004 |
|--------|--------|
| **Descripción** | El archivo JSON debe ubicarse en el directorio de la aplicación |
| **Implementación** | Ruta: `AppDomain.CurrentDomain.BaseDirectory\CarpetaDeTrabajoHerramientas.json` |

### RT-005: Modelo de Datos de Herramienta
| **ID** | RT-005 |
|--------|--------|
| **Descripción** | Cada herramienta debe tener 4 campos: Orden, Title, TabColor, Parametro |
| **Implementación** | Clase `HerramientaConfig` con propiedades: `int Orden`, `string Title`, `string TabColor`, `string Parametro` |

### RT-006: Comandos por Pestaña
| **ID** | RT-006 |
|--------|--------|
| **Descripción** | Cada pestaña ejecuta un comando específico definido en el campo `Parametro` |
| **Implementación** | El campo `Parametro` contiene el comando completo: `pwsh -NoExit -Command <comando>` |

### RT-007: No Bloqueo del UI Thread
| **ID** | RT-007 |
|--------|--------|
| **Descripción** | La ejecución de la terminal no debe bloquear la interfaz de usuario |
| **Implementación** | Usar `Process.Start()` sin esperar la finalización del proceso |

### RT-008: Validación de Esquema JSON
| **ID** | RT-008 |
|--------|--------|
| **Descripción** | El sistema debe validar que el JSON tenga la estructura esperada |
| **Implementación** | Validar que el root sea un array de objetos con las propiedades requeridas |

---

## 5. Especificación de Implementación

### 5.1 Archivo de Configuración: `CarpetaDeTrabajoHerramientas.json`

**Ubicación:** Directorio de la aplicación (`AppDomain.CurrentDomain.BaseDirectory`)

**Estructura:**

```json
[
  {
    "Orden": 1,
    "Title": "lazygit",
    "TabColor": "#1e6a4a",
    "Parametro": "pwsh -NoExit -Command lazygit"
  },
  {
    "Orden": 2,
    "Title": "qwen",
    "TabColor": "#4a3a8a",
    "Parametro": "pwsh -NoExit -Command qwen"
  },
  {
    "Orden": 3,
    "Title": "shell",
    "TabColor": "#8a4a1e",
    "Parametro": ""
  }
]
```

**Campos:**

| **Campo** | **Tipo** | **Obligatorio** | **Descripción** |
|-----------|----------|-----------------|-----------------|
| `Orden` | `int` | Sí | Orden de creación de la pestaña (ascendente) |
| `Title` | `string` | Sí | Título visible en la pestaña |
| `TabColor` | `string` | Sí | Color en formato hexadecimal `#RRGGBB` |
| `Parametro` | `string` | No | Comando a ejecutar. Vacío = solo shell |

### 5.2 Clase: `WindowsTerminalLauncher`

```csharp
namespace AccesosLauncher.Core
{
    public static class WindowsTerminalLauncher
    {
        /// <summary>
        /// Abre Windows Terminal con las pestañas configuradas en el JSON
        /// en el directorio extraído de la URL local provista.
        /// </summary>
        /// <param name="fileUrl">Ej: file:///C:\_Tony\CS\AccesosLauncher</param>
        public static void OpenInWindowsTerminal(string fileUrl);
        
        // Métodos privados de soporte
        private static string ConvertFileUrlToPath(string fileUrl);
        private static List<HerramientaConfig> LoadHerramientasConfig();
        private static string BuildWtArguments(string dir, List<HerramientaConfig> herramientas);
        private static List<HerramientaConfig> GetDefaultHerramientas();
    }
    
    public class HerramientaConfig
    {
        public int Orden { get; set; }
        public string Title { get; set; }
        public string TabColor { get; set; }
        public string Parametro { get; set; }
    }
}
```

### 5.3 Flujo de Ejecución

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Usuario hace click en acceso "Carpeta de Trabajo"        │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Convertir file:// URL a path local                       │
│    - Validar que sea URL de tipo file://                    │
│    - Extraer LocalPath y normalizar separadores             │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Validar existencia del directorio                        │
│    - Si NO existe → Mostrar MessageBox (Warning)            │
│    - Si existe → Continuar                                  │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Cargar configuración desde JSON                          │
│    - Leer CarpetaDeTrabajoHerramientas.json                 │
│    - Si falla → Usar configuración por defecto              │
│    - Ordenar herramientas por campo Orden                   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Construir argumentos para wt.exe                         │
│    - Iterar herramientas ordenadas                          │
│    - Generar new-tab por cada una con Title, TabColor,      │
│      Parametro                                              │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Ejecutar wt.exe con Process.Start()                      │
│    - UseShellExecute = true                                 │
│    - WindowStyle = Normal                                   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. Capturar excepciones y mostrar MessageBox si corresponde │
└─────────────────────────────────────────────────────────────┘
```

### 5.3 Argumentos de Windows Terminal

**Formato dinámico (según JSON):**

```
-d "{directorio}" 
new-tab --title "{Title_1}" --tabColor "{TabColor_1}" -- {Parametro_1} ;
new-tab --title "{Title_2}" --tabColor "{TabColor_2}" -d "{directorio}" -- {Parametro_2} ;
...
new-tab --title "{Title_N}" --tabColor "{TabColor_N}" -d "{directorio}" -- {Parametro_N}
```

**Nota:** Si `Parametro` está vacío, se omite el `-- {Parametro}` y la pestaña abre solo el shell por defecto.

---

## 6. Interfaz de Usuario

### 6.1 Formulario "Agregar Acceso"

| **Campo** | **Tipo** | **Obligatorio** | **Validación** |
|-----------|----------|-----------------|----------------|
| Tipo de Acceso | ComboBox | Sí | Debe incluir opción "Carpeta de Trabajo" |
| Ruta | TextBox / FolderBrowser | Sí | Debe ser un directorio existente en el filesystem |
| Nombre | TextBox | Sí | Nombre descriptivo del acceso |

### 6.2 Mensajes de Error

| **Escenario** | **Título** | **Mensaje** | **Icono** |
|---------------|------------|-------------|-----------|
| Directorio no existe | "Directorio no encontrado" | `El directorio no existe: {ruta}` | Warning |
| Error genérico | "Error al abrir terminal" | `{ex.Message}` | Error |

---

## 7. Dependencias

| **Dependencia** | **Versión** | **Requerida** |
|-----------------|-------------|---------------|
| Windows Terminal | 1.0+ | Sí (debe estar instalado en el sistema) |
| .NET 8 | 8.0+ | Sí (ya disponible en el proyecto) |
| System.Text.Json | 8.0+ | Sí (incluido en .NET 8) |
| PowerShell | 7.0+ | Sí (para ejecutar comandos en las pestañas) |
| lazygit | Latest | Opcional (se ejecuta si está disponible) |
| qwen | Latest | Opcional (se ejecuta si está disponible) |

---

## 8. Consideraciones de Seguridad

| **ID** | **Consideración** |
|--------|-------------------|
| SEC-001 | Validar que la URL sea de tipo `file://` para evitar ejecución de protocolos no deseados |
| SEC-002 | No ejecutar procesos con privilegios elevados |
| SEC-003 | Sanitizar la ruta para evitar inyección de argumentos en `wt.exe` |

---

## 9. Métricas de Éxito

| **Métrica** | **Objetivo** |
|-------------|--------------|
| Tiempo de apertura de terminal | < 3 segundos |
| Tasa de éxito de apertura | > 95% |
| Reducción de clicks del usuario | De ~15 clicks a 1 click |

---

## 10. Tareas de Implementación

- [ ] Crear clase `HerramientaConfig` con propiedades: Orden, Title, TabColor, Parametro
- [ ] Crear clase `WindowsTerminalLauncher` en proyecto AccesosLauncher
- [ ] Implementar método `LoadHerramientasConfig()` que lee desde JSON
- [ ] Implementar método `GetDefaultHerramientas()` para configuración por defecto
- [ ] Actualizar método `BuildWtArguments()` para recibir `List<HerramientaConfig>`
- [ ] Crear archivo `CarpetaDeTrabajoHerramientas.json` con configuración inicial
- [ ] Agregar tipo "Carpeta de Trabajo" al ComboBox de tipos de acceso
- [ ] Agregar campo de ruta con validación de directorio existente
- [ ] Integrar llamada a `WindowsTerminalLauncher.OpenInWindowsTerminal()` en el handler del click
- [ ] Agregar manejo de excepciones con MessageBox apropiados
- [ ] Agregar tests unitarios para:
  - [ ] `ConvertFileUrlToPath()`
  - [ ] `LoadHerramientasConfig()` (con JSON válido, inválido, vacío)
  - [ ] `BuildWtArguments()` (con diferentes configuraciones)
  - [ ] `GetDefaultHerramientas()`
- [ ] Documentar feature en README.md
- [ ] Documentar formato del archivo JSON en README.md

---

## 11. Historial de Versiones

| **Versión** | **Fecha** | **Autor** | **Cambios** |
|-------------|-----------|-----------|-------------|
| 1.0 | 24 de marzo de 2026 | | Documento inicial |
| 2.0 | 24 de marzo de 2026 | | Configuración externalizada a JSON: `CarpetaDeTrabajoHerramientas.json` con campos Orden, Title, TabColor, Parametro |

---

## 12. Apéndice: Código de Referencia

### WindowsTerminalLauncher.cs

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AccesosLauncher.Core
{
    public class HerramientaConfig
    {
        public int Orden { get; set; }
        public string Title { get; set; }
        public string TabColor { get; set; }
        public string Parametro { get; set; }
    }

    public static class WindowsTerminalLauncher
    {
        private static readonly string ConfigFileName = "CarpetaDeTrabajoHerramientas.json";

        /// <summary>
        /// Abre Windows Terminal con las pestañas configuradas en el JSON
        /// en el directorio extraído de la URL local provista.
        /// </summary>
        /// <param name="fileUrl">Ej: file:///C:\_Tony\CS\AccesosLauncher</param>
        public static void OpenInWindowsTerminal(string fileUrl)
        {
            // 1. Convertir file:// URL a path local real
            string localPath = ConvertFileUrlToPath(fileUrl);

            // 2. Validar que el directorio exista antes de intentar abrir
            if (!Directory.Exists(localPath))
                throw new DirectoryNotFoundException($"El directorio no existe: {localPath}");

            // 3. Cargar configuración desde JSON (o usar defaults)
            List<HerramientaConfig> herramientas = LoadHerramientasConfig();

            // 4. Armar los argumentos para wt.exe
            string wtArgs = BuildWtArguments(localPath, herramientas);

            // 5. Lanzar el proceso — sin ventana propia, sin bloquear el hilo UI
            var psi = new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = wtArgs,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(psi);
        }

        // -------------------------------------------------------

        private static string ConvertFileUrlToPath(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("La URL no puede estar vacía.");

            var uri = new Uri(fileUrl);

            if (!uri.IsFile)
                throw new ArgumentException($"No es una URL de archivo local: {fileUrl}");

            return uri.LocalPath.TrimEnd(Path.DirectorySeparatorChar);
        }

        private static List<HerramientaConfig> LoadHerramientasConfig()
        {
            try
            {
                string configPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    ConfigFileName
                );

                if (!File.Exists(configPath))
                    return GetDefaultHerramientas();

                string json = File.ReadAllText(configPath);
                var herramientas = JsonSerializer.Deserialize<List<HerramientaConfig>>(json);

                return herramientas?.OrderBy(h => h.Orden).ToList() 
                       ?? GetDefaultHerramientas();
            }
            catch (Exception)
            {
                // Si falla la lectura, usar configuración por defecto
                return GetDefaultHerramientas();
            }
        }

        private static string BuildWtArguments(string dir, List<HerramientaConfig> herramientas)
        {
            // Envolvemos el path en comillas para manejar espacios
            string q = $"\"{dir}\"";
            var args = new List<string> { $"-d {q}" };

            foreach (var herramienta in herramientas)
            {
                var tabArgs = new List<string>
                {
                    $"new-tab --title \"{herramienta.Title}\" --tabColor \"{herramienta.TabColor}\""
                };

                // Solo agregar -d si no es la primera pestaña
                if (args.Count > 1)
                    tabArgs.Insert(1, $"-d {q}");

                // Agregar comando si existe
                if (!string.IsNullOrWhiteSpace(herramienta.Parametro))
                    tabArgs.Add($"-- {herramienta.Parametro}");

                args.Add(string.Join(" ", tabArgs));
            }

            return string.Join(" ; ", args);
        }

        private static List<HerramientaConfig> GetDefaultHerramientas()
        {
            return new List<HerramientaConfig>
            {
                new() { Orden = 1, Title = "lazygit", TabColor = "#1e6a4a", Parametro = "pwsh -NoExit -Command lazygit" },
                new() { Orden = 2, Title = "qwen", TabColor = "#4a3a8a", Parametro = "pwsh -NoExit -Command qwen" },
                new() { Orden = 3, Title = "shell", TabColor = "#8a4a1e", Parametro = "" }
            };
        }
    }
}
```

### Ejemplo de Uso en WPF

```csharp
// Desde un comando, botón, o donde sea en tu WPF
try
{
    WindowsTerminalLauncher.OpenInWindowsTerminal("file:///C:\\_Tony\\CS\\AccesosLauncher");
}
catch (DirectoryNotFoundException ex)
{
    MessageBox.Show(ex.Message, "Directorio no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message, "Error al abrir terminal", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

### Ejemplo: `CarpetaDeTrabajoHerramientas.json`

```json
[
  {
    "Orden": 1,
    "Title": "lazygit",
    "TabColor": "#1e6a4a",
    "Parametro": "pwsh -NoExit -Command lazygit"
  },
  {
    "Orden": 2,
    "Title": "qwen",
    "TabColor": "#4a3a8a",
    "Parametro": "pwsh -NoExit -Command qwen"
  },
  {
    "Orden": 3,
    "Title": "shell",
    "TabColor": "#8a4a1e",
    "Parametro": ""
  }
]
```

### Ejemplo: Configuración Personalizada (5 pestañas)

```json
[
  {
    "Orden": 1,
    "Title": "git",
    "TabColor": "#1e6a4a",
    "Parametro": "pwsh -NoExit -Command lazygit"
  },
  {
    "Orden": 2,
    "Title": "ai",
    "TabColor": "#4a3a8a",
    "Parametro": "pwsh -NoExit -Command qwen"
  },
  {
    "Orden": 3,
    "Title": "build",
    "TabColor": "#8a4a1e",
    "Parametro": "pwsh -NoExit -Command dotnet watch build"
  },
  {
    "Orden": 4,
    "Title": "test",
    "TabColor": "#8a1e4a",
    "Parametro": "pwsh -NoExit -Command dotnet test --watch"
  },
  {
    "Orden": 5,
    "Title": "shell",
    "TabColor": "#4a8a1e",
    "Parametro": ""
  }
]
```
