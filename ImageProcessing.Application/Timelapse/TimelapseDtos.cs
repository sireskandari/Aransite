using ImageProcessing.Domain.Entities.Timelapse;

namespace ImageProcessing.Application.Timelapses;

public sealed record CreateTimelapseRequest(string FilePath, string FileFormat, string FileSize, TimelapseStatus Status, string ErrorMessage);
public sealed record UpdateTimelapseRequest(string FilePath, string FileFormat, string FileSize, TimelapseStatus Status, string ErrorMessage);
public sealed record TimelapseResponse(Guid Id, string FilePath, string FileFormat, string FileSize, TimelapseStatus Status, string ErrorMessage, DateTime CreatedUtc);
