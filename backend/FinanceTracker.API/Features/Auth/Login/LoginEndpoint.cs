using FluentValidation;
using MediatR;

namespace FinanceTracker.API.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginCommand command, IMediator mediator, IValidator<LoginCommand> validator) =>
        {
            var validation = await validator.ValidateAsync(command);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
