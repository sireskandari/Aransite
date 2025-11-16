using System.Net;

namespace ImageProcessing.Api.Models;

public sealed class ApiResponse
{
    public bool IsSuccess { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public object? Result { get; set; }
    public List<string>? ErrorMessages { get; set; }

    public static ApiResponse Ok(object? result) => new() { IsSuccess = true, StatusCode = HttpStatusCode.OK, Result = result };
    public static ApiResponse Created(object? result) => new() { IsSuccess = true, StatusCode = HttpStatusCode.Created, Result = result };
    public static ApiResponse NoContent() => new() { IsSuccess = true, StatusCode = HttpStatusCode.NoContent };
    public static ApiResponse Fail(HttpStatusCode code, params string[] errors) =>
        new() { IsSuccess = false, StatusCode = code, ErrorMessages = errors?.ToList() };
}
