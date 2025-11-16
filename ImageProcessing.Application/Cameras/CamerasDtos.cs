namespace ImageProcessing.Application.Cameras;

public sealed record CreateCameraRequest(string Key, string Location, string RTSP, bool IsActive);
public sealed record UpdateCameraRequest(string Key, string Location, string RTSP, bool IsActive);
public sealed record CameraResponse(Guid Id, string Key, string Location, string RTSP, bool IsActive, DateTime CreatedUtc);
