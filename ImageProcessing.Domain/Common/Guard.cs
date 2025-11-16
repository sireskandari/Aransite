namespace ImageProcessing.Domain.Common;
/// <summary>
/// Tiny guard helper to keep entity methods readable.
/// </summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(string? value, string fieldName, int? maxLen = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} is required.");

        var trimmed = value.Trim();
        if (maxLen.HasValue && trimmed.Length > maxLen.Value)
            throw new DomainException($"{fieldName} must be <= {maxLen.Value} characters.");

        return trimmed;
    }
}
