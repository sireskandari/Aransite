namespace ImageProcessing.Domain.Entities.LogEvents;
 
using ImageProcessing.Domain.Common;

/// <summary>
/// Minimal User aggregate root for the Domain layer only.
/// No EF attributes, no persistence details.
/// </summary>
public class LogEvent
{
    public int Id { get; set; }

    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Level { get; set; }

    public DateTime TimeStamp { get; set; }

    public string? Exception { get; set; }

    // XML / JSON blob with structured properties
    public string? Properties { get; set; }
}