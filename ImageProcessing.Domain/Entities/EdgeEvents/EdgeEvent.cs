namespace ImageProcessing.Domain.Entities.EdgeEvents;

using ImageProcessing.Domain.Common;

/// <summary>
/// Minimal User aggregate root for the Domain layer only.
/// No EF attributes, no persistence details.
/// </summary>
public class EdgeEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? FrameAnnotatedUrl { get; set; }
    public string? FrameRawUrl { get; set; }
    public string? Detections { get; set; }
    public string? ComputeModel { get; set; }
    public double? ComputeInferenceMs { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public string? CameraId { get; set; }
    public DateTime CaptureTimestampUtc { get; set; }
    public DateTime CreatedUtc { get; set; }

}
