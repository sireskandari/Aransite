using System.Text.Json.Serialization;

public sealed class DetectTargetsOptions
{
    [JsonPropertyName("default")]
    public List<string> Default { get; set; } = new() { "person" };

    [JsonPropertyName("cameras")]
    public List<DetectTargetsCamera> Cameras { get; set; } = new();
}

public sealed class DetectTargetsCamera
{
    public string Id { get; set; } = default!;
    public List<string> Targets { get; set; } = new();
}
