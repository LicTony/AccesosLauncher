using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AccesosLauncher.Core;

public static class WindowsTerminalLauncher
{
    private const string ConfigFileName = "CarpetaDeTrabajoHerramientas.json";

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
            WindowStyle = ProcessWindowStyle.Maximized
        };

        Process.Start(psi);
    }

    /// <summary>
    /// Convierte una URL de tipo file:// a un path local.
    /// </summary>
    /// <param name="fileUrl">URL en formato file://</param>
    /// <returns>Path local sin separador final</returns>
    /// <exception cref="ArgumentException">Cuando la URL es vacía o no es de tipo file://</exception>
    internal static string ConvertFileUrlToPath(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("La URL no puede estar vacía.", nameof(fileUrl));

        var uri = new Uri(fileUrl);

        if (!uri.IsFile)
            throw new ArgumentException($"No es una URL de archivo local: {fileUrl}", nameof(fileUrl));

        return uri.LocalPath.TrimEnd(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Carga la configuración de herramientas desde el archivo JSON.
    /// Si el archivo no existe o es inválido, retorna la configuración por defecto.
    /// </summary>
    internal static List<HerramientaConfig> LoadHerramientasConfig()
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
        catch (JsonException)
        {
            // Si el JSON es inválido, usar configuración por defecto
            return GetDefaultHerramientas();
        }
        catch (Exception)
        {
            // Si falla cualquier otra cosa, usar configuración por defecto
            return GetDefaultHerramientas();
        }
    }

    /// <summary>
    /// Construye los argumentos para wt.exe basados en la configuración de herramientas.
    /// </summary>
    /// <param name="dir">Directorio de trabajo</param>
    /// <param name="herramientas">Lista de configuraciones de herramientas</param>
    /// <returns>Argumentos formateados para wt.exe</returns>
    internal static string BuildWtArguments(string dir, List<HerramientaConfig> herramientas)
    {
        // Envolvemos el path en comillas para manejar espacios
        string quotedDir = $"\"{dir}\"";
        var args = new List<string> { $"-d {quotedDir}" };

        foreach (var herramienta in herramientas)
        {
            var tabArgs = new List<string>
            {
                $"new-tab --title \"{herramienta.Title}\" --tabColor \"{herramienta.TabColor}\""
            };

            // Solo agregar -d si no es la primera pestaña
            if (args.Count > 1)
                tabArgs.Insert(1, $"-d {quotedDir}");

            // Agregar comando si existe
            if (!string.IsNullOrWhiteSpace(herramienta.Parametro))
                tabArgs.Add($"-- {herramienta.Parametro}");

            args.Add(string.Join(" ", tabArgs));
        }

        return string.Join(" ; ", args);
    }

    /// <summary>
    /// Retorna la configuración por defecto de herramientas (3 pestañas).
    /// </summary>
    internal static List<HerramientaConfig> GetDefaultHerramientas()
    {
        return new List<HerramientaConfig>
        {
            new() { Orden = 1, Title = "lazygit", TabColor = "#1e6a4a", Parametro = "pwsh -NoExit -Command lazygit" },
            new() { Orden = 2, Title = "qwen", TabColor = "#4a3a8a", Parametro = "pwsh -NoExit -Command qwen" },
            new() { Orden = 3, Title = "shell", TabColor = "#8a4a1e", Parametro = "" }
        };
    }
}
