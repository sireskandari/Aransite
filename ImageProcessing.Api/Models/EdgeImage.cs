
using System.Text.Json.Serialization;

public sealed class EdgeImage
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}
