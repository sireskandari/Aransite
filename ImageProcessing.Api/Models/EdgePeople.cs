
using System.Text.Json.Serialization;

public sealed class EdgePeople
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("confidence_avg")]
    public double ConfidenceAvg { get; set; }
}
