using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Features.Transactions.Create;
using FinanceTracker.API.Features.Transactions.GetAll;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Transactions;

public static class TransactionsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/transactions").RequireAuthorization().WithTags("Transactions");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapDelete("/{id:guid}", Delete);
        group.MapDelete("/", DeleteAll);
    }

    private static async Task<IResult> GetAll(
        ClaimsPrincipal user, IMediator mediator,
        DateTime? from, DateTime? to, Guid? categoryId,
        TransactionType? type, string? search,
        int page = 1, int pageSize = 20)
    {
        var result = await mediator.Send(new GetTransactionsQuery(
            user.GetUserId(), from, to, categoryId, type, search, page, pageSize));
        return Results.Ok(result);
    }

    private static async Task<IResult> Create(CreateTransactionCommand command, ClaimsPrincipal user, IMediator mediator)
    {
        var fullCommand = command with { UserId = user.GetUserId() };
        var result = await mediator.Send(fullCommand);
        return Results.Created($"/api/transactions/{result.Id}", result);
    }

    private static async Task<IResult> Delete(Guid id, ClaimsPrincipal user, AppDbContext db)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.GetUserId());
        if (transaction is null) return Results.NotFound();
        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAll(ClaimsPrincipal user, AppDbContext db)
    {
        await db.Transactions.Where(t => t.UserId == user.GetUserId()).ExecuteDeleteAsync();
        return Results.NoContent();
    }
}
