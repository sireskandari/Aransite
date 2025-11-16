using Microsoft.AspNetCore.Routing;

namespace ImageProcessing.Api.Endpoints.Abstractions;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
