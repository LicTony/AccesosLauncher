using System.Text.Json.Serialization;

namespace AccesosLauncher.Core;

public class HerramientaConfig
{
    [JsonPropertyName("Orden")]
    public int Orden { get; set; }

    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("TabColor")]
    public string TabColor { get; set; } = string.Empty;

    [JsonPropertyName("Parametro")]
    public string Parametro { get; set; } = string.Empty;
}
