namespace ImageProcessing.Api.Models
{
    public  class GenerateFromEdgeRequest
    {
        public string? Search { get; set; }
        public string? CameraId { get; set; }
        public int Fps { get; set; } = 20;
        public int Width { get; set; } = 0;          // 0 = keep native resolution
        public int MaxFrames { get; set; } = 5000;
        public int Crf { get; set; } = 18;           // lower = higher quality
        public string Preset { get; set; } = "veryfast"; // "slow" for higher quality
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }
}
