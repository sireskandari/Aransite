
using System.Text.Json.Serialization;

public sealed class EdgeDetection
{
    [JsonPropertyName("class_id")]
    public int ClassId { get; set; }

    [JsonPropertyName("class_name")]
    public string ClassName { get; set; } = "person";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("bbox_xyxy")]
    public double[] BboxXyxy { get; set; } = Array.Empty<double>();

    // tracking info you asked for
    [JsonPropertyName("track_id")]
    public int? TrackId { get; set; }
}