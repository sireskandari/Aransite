namespace ImageProcessing.Application.EdgeEvents;

public sealed record CreateEdgeEventsRequest(DateTime CaptureTimestampUtc, string CameraId, string ComputeModel, double? ComputeInferenceMs, int ImageWidth, int ImageHeight, string Detections, string FrameRawUrl, string FrameAnnotatedUrl);
public sealed record EdgeEventsResponse(Guid Id, DateTime CaptureTimestampUtc, DateTime CreatedUtc, string CameraId, string ComputeModel, double? ComputeInferenceMs, int ImageWidth, int ImageHeight, string Detections, string FrameRawUrl, string FrameAnnotatedUrl);
