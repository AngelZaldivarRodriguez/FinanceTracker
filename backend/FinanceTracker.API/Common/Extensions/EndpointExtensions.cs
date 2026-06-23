using FinanceTracker.API.Features.Auth.Login;
using FinanceTracker.API.Features.Auth.Register;

namespace FinanceTracker.API.Common.Extensions;

public static class EndpointExtensions
{
    public static void MapFeatureEndpoints(this WebApplication app)
    {
        RegisterEndpoint.Map(app);
        LoginEndpoint.Map(app);
    }
}
