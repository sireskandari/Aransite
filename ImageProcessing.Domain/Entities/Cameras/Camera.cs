namespace ImageProcessing.Domain.Entities.Cameras;

using ImageProcessing.Domain.Common;

/// <summary>
/// Minimal User aggregate root for the Domain layer only.
/// No EF attributes, no persistence details.
/// </summary>
public class Camera
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; }
    public string Location { get; set; }
    public string RTSP { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }

}
