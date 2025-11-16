
using System.Text.Json.Serialization;

public sealed class EdgeCompute
{
    [JsonPropertyName("inference_ms")]
    public double InferenceMs { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
