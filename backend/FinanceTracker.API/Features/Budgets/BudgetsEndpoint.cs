using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using FinanceTracker.API.Features.Budgets.Create;
using FinanceTracker.API.Features.Budgets.GetAll;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Budgets;

public static class BudgetsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/budgets").RequireAuthorization().WithTags("Budgets");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapDelete("/{id:guid}", Delete);
    }

    private static async Task<IResult> GetAll(ClaimsPrincipal user, IMediator mediator, int? month, int? year)
    {
        var now = DateTime.UtcNow;
        var result = await mediator.Send(new GetBudgetsQuery(user.GetUserId(), month ?? now.Month, year ?? now.Year));
        return Results.Ok(result);
    }

    private static async Task<IResult> Create(CreateBudgetCommand command, ClaimsPrincipal user, IMediator mediator)
    {
        var fullCommand = command with { UserId = user.GetUserId() };
        var result = await mediator.Send(fullCommand);
        return Results.Created($"/api/budgets/{result.Id}", result);
    }

    private static async Task<IResult> Delete(Guid id, ClaimsPrincipal user, AppDbContext db)
    {
        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.GetUserId());
        if (budget is null) return Results.NotFound();
        db.Budgets.Remove(budget);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
