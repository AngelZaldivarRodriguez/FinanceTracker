using MediatR;

namespace FinanceTracker.API.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
