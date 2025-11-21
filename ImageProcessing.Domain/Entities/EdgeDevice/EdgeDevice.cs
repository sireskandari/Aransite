namespace ImageProcessing.Domain.Entities.EdgeDevices;

using ImageProcessing.Domain.Common;
using System;

/// <summary>
/// Minimal User aggregate root for the Domain layer only.
/// No EF attributes, no persistence details.
/// </summary>
public sealed class EdgeDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = default!;
    public string? Hostname { get; set; }
    public string? LocalIp { get; set; }        
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public DateTime? CaptureSinceUtc { get; set; }
    public DateTime? LastCaptureUtc { get; set; }
    public string? AppVersion { get; set; }
    public string? Status { get; set; }
}

