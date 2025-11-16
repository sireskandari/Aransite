using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ImageProcessing.Api.Endpoints.Abstractions;

namespace ImageProcessing.Api.Endpoints.Bootstrap;

public static class EndpointRegistrar
{
    public static void MapDiscoveredEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointType = typeof(IEndpoint);
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => endpointType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var t in types)
        {
            if (Activator.CreateInstance(t) is IEndpoint ep)
            {
                ep.Map(app);
            }
        }
    }
}
