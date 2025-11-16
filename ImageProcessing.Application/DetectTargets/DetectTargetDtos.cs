namespace ImageProcessing.Application.DetectTargets;

public sealed record CreateDetectTargetRequest(string CameraKey, string Targets);
public sealed record UpdateDetectTargetRequest(string CameraKey, string Targets);
public sealed record DetectTargetResponse(Guid Id, string CameraKey, string Targets, DateTime CreatedUtc);
