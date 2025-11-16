namespace ImageProcessing.Application.Users;

public sealed record CreateUserRequest(string Email, string Name);
public sealed record UserResponse(Guid Id, string Email, string Name, DateTime CreatedUtc);
