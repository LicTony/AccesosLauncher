using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace FaviconDownloader
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Descargador de Favicons ===\n");
            
            if (args.Length > 0)
            {
                await ProcesarUrl(args[0]);
            }
            else
            {
                await ModoInteractivo();
            }
        }

        static async Task ModoInteractivo()
        {
            while (true)
            {
                Console.Write("Ingresa una URL (o 'salir' para terminar): ");
                string input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "salir")
                    break;
                
                await ProcesarUrl(input);
                Console.WriteLine();
            }
        }

        static async Task ProcesarUrl(string url)
        {
            try
            {
                // Validar y normalizar URL
                if (!url.StartsWith("http"))
                    url = "https://" + url;

                var uri = new Uri(url);
                string domain = uri.Host;
                
                Console.WriteLine($"Procesando: {domain}");
                
                // Crear directorio de salida
                string outputDir = Path.Combine(Environment.CurrentDirectory, "favicons");
                Directory.CreateDirectory(outputDir);
                
                // Intentar obtener favicon
                string faviconUrl = await ObtenerUrlFavicon(url);
                
                if (faviconUrl != null)
                {
                    Console.WriteLine($"Favicon encontrado: {faviconUrl}");
                    await DescargarYConvertirFavicon(faviconUrl, domain, outputDir);
                }
                else
                {
                    Console.WriteLine("No se pudo encontrar favicon para esta URL");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task<string> ObtenerUrlFavicon(string url)
        {
            var uri = new Uri(url);
            string baseUrl = $"{uri.Scheme}://{uri.Host}";
            
            // Método 1: Buscar en el HTML
            try
            {
                string html = await httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                
                // Buscar enlaces de favicon en el HTML
                var faviconLinks = doc.DocumentNode.SelectNodes("//link[@rel='icon' or @rel='shortcut icon' or @rel='apple-touch-icon']");
                
                if (faviconLinks != null)
                {
                    foreach (var link in faviconLinks)
                    {
                        string href = link.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(href))
                        {
                            if (href.StartsWith("http"))
                                return href;
                            else if (href.StartsWith("/"))
                                return baseUrl + href;
                            else
                                return baseUrl + "/" + href;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al parsear HTML: {ex.Message}");
            }
            
            // Método 2: Intentar rutas estándar
            string[] rutasEstandar = {
                "/favicon.ico",
                "/favicon.png",
                "/apple-touch-icon.png",
                "/apple-touch-icon-precomposed.png"
            };
            
            foreach (string ruta in rutasEstandar)
            {
                string faviconUrl = baseUrl + ruta;
                if (await ExisteFavicon(faviconUrl))
                {
                    return faviconUrl;
                }
            }
            
            // Método 3: Usar servicio de Google como fallback
            return $"https://www.google.com/s2/favicons?domain={uri.Host}&sz=64";
        }

        static async Task<bool> ExisteFavicon(string url)
        {
            try
            {
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        static async Task DescargarYConvertirFavicon(string faviconUrl, string domain, string outputDir)
        {
            try
            {
                // Descargar favicon
                byte[] faviconData = await httpClient.GetByteArrayAsync(faviconUrl);
                
                // Limpiar nombre del dominio para usar como nombre de archivo
                string fileName = LimpiarNombreArchivo(domain);
                string outputPath = Path.Combine(outputDir, $"{fileName}.ico");
                
                // Si ya es ICO, guardarlo directamente
                if (faviconUrl.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    await File.WriteAllBytesAsync(outputPath, faviconData);
                    Console.WriteLine($"✓ Favicon guardado: {outputPath}");
                    return;
                }
                
                // Convertir imagen a ICO
                using (var memoryStream = new MemoryStream(faviconData))
                {
                    try
                    {
                        using (var image = Image.FromStream(memoryStream))
                        {
                            // Redimensionar si es necesario
                            var favicon = RedimensionarImagen(image, 32, 32);
                            
                            // Guardar como ICO
                            using (var fileStream = new FileStream(outputPath, FileMode.Create))
                            {
                                favicon.Save(fileStream, ImageFormat.Icon);
                            }
                            
                            Console.WriteLine($"✓ Favicon convertido y guardado: {outputPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Si la conversión falla, guardar el archivo original
                        string originalPath = Path.Combine(outputDir, $"{fileName}_original{Path.GetExtension(faviconUrl)}");
                        await File.WriteAllBytesAsync(originalPath, faviconData);
                        Console.WriteLine($"✓ Favicon guardado (formato original): {originalPath}");
                        Console.WriteLine($"  (No se pudo convertir a ICO: {ex.Message})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descargar favicon: {ex.Message}");
            }
        }

        static Bitmap RedimensionarImagen(Image imagen, int ancho, int alto)
        {
            var bitmap = new Bitmap(ancho, alto);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(imagen, 0, 0, ancho, alto);
            }
            return bitmap;
        }

        static string LimpiarNombreArchivo(string nombre)
        {
            // Remover caracteres no válidos para nombres de archivo
            string patron = @"[<>:""/\\|?*]";
            return Regex.Replace(nombre, patron, "_");
        }
    }
}