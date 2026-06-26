using MediatR;

namespace FinanceTracker.API.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterCommand command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
