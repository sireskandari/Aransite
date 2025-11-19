namespace ImageProcessing.Application.LogEvents;

public sealed record LogEventResponse(int Id,
    string? Timestamp,
    string? Level,
    string? Template,
    string? Message,
    string? Exception,
    string? Properties);
