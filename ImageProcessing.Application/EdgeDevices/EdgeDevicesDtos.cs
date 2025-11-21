namespace ImageProcessing.Application.EdgeDevices;

public sealed record CreateEdgeDeviceRequest(string DeviceId, string? Hostname, string? LocalIp, DateTime? CaptureSinceUtc, DateTime? LastCaptureUtc, string? AppVersion, string? Status);
public sealed record EdgeDeviceResponse(Guid Id, string DeviceId, string? Hostname, string? LocalIp, DateTime? CaptureSinceUtc, DateTime? LastCaptureUtc, string? AppVersion, string? Status);
