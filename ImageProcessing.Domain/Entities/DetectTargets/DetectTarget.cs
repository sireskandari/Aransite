namespace ImageProcessing.Domain.Entities.DetectTargets;

using ImageProcessing.Domain.Common;

/// <summary>
/// Minimal User aggregate root for the Domain layer only.
/// No EF attributes, no persistence details.
/// </summary>
public class DetectTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CameraKey { get; set; }
    public string Targets { get; set; }
    public DateTime CreatedUtc { get; set; }

}
