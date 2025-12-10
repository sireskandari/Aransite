namespace ImageProcessing.Domain.Entities.Timelapse;

using ImageProcessing.Domain.Common;
public class Timelapse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public TimelapseStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    // Optional – audit/debug fields
    public string? CameraId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

}
public enum TimelapseStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}