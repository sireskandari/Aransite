using System.ComponentModel.DataAnnotations;

public sealed class EdgeIngestRequest
{
    [Required]
    public string Meta { get; set; } = default!;

    public IFormFile? Frame_Raw { get; set; }
    public IFormFile? Frame_Annotated { get; set; }
}
