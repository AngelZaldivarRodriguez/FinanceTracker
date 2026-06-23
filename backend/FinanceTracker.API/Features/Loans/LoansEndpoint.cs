using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using MediatR;

namespace FinanceTracker.API.Features.Loans;

public static class LoansEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/loans").RequireAuthorization().WithTags("Loans");

        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetLoansQuery(user.GetUserId()));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetLoanDetailQuery(id, user.GetUserId()));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (CreateLoanCommand body, ClaimsPrincipal user, IMediator mediator) =>
        {
            var cmd = body with { UserId = user.GetUserId() };
            var result = await mediator.Send(cmd);
            return Results.Created($"/api/loans/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/payments/{number:int}/pay", async (
            Guid id, int number, ClaimsPrincipal user, IMediator mediator,
            MarkPaymentRequest body) =>
        {
            var ok = await mediator.Send(new MarkPaymentPaidCommand(id, user.GetUserId(), number, body.PaidDate));
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}

public record MarkPaymentRequest(DateTime PaidDate);
