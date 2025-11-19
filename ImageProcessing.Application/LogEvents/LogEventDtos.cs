namespace ImageProcessing.Application.LogEvents;

public sealed record LogEventResponse(int Id, string Message, string MessageTemplate,
    string Level,string Exception,string Properties, DateTime TimeStamp);
