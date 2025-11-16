namespace ImageProcessing.Domain.Entities.Users;

using ImageProcessing.Domain.Common;

/// <summary>
/// Minimal User aggregate root for the Domain layer only.
/// No EF attributes, no persistence details.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string PasswordHash { get; set; } = ""; // new
    public string Role { get; set; } = "User";     // new (e.g., "Admin","User")
    public string? ProfileImagePath { get; set; }
    public DateTime CreatedUtc { get; set; }

}
