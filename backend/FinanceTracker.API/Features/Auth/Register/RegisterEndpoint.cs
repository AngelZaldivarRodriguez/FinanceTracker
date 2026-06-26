using MediatR;

namespace FinanceTracker.API.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
