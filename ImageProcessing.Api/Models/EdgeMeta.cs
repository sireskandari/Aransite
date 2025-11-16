
using System.Text.Json.Serialization;

public sealed class EdgeMeta
{
    [JsonPropertyName("timestamp_utc")]
    public string TimestampUtc { get; set; } = default!;

    [JsonPropertyName("camera_id")]
    public string CameraId { get; set; } = default!;

    [JsonPropertyName("image")]
    public EdgeImage? Image { get; set; }

    [JsonPropertyName("compute")]
    public EdgeCompute? Compute { get; set; }

    [JsonPropertyName("people")]
    public EdgePeople? People { get; set; }

    [JsonPropertyName("detections")]
    public List<EdgeDetection> Detections { get; set; } = new();
}
