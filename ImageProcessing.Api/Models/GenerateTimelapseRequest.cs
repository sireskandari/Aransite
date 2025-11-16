// Contracts/GenerateTimelapseRequest.cs
using System.ComponentModel.DataAnnotations;

public sealed class GenerateTimelapseRequest
{
    [Required]
    public List<string> Paths { get; set; } = new();

    public int Fps { get; set; } = 20;

    // Optional target width (keeps aspect ratio)
    public int Width { get; set; } = 1920;
}

public sealed class GenerateTimelapseResponse
{
    public string DownloadUrl { get; set; } = default!;
}
