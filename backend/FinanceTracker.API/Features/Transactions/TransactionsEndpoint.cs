using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Features.Transactions.Create;
using FinanceTracker.API.Features.Transactions.GetAll;
using FinanceTracker.API.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Transactions;

public static class TransactionsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/transactions").RequireAuthorization().WithTags("Transactions");

        group.MapGet("/", async (
            ClaimsPrincipal user,
            IMediator mediator,
            DateTime? from,
            DateTime? to,
            Guid? categoryId,
            TransactionType? type) =>
        {
            var result = await mediator.Send(new GetTransactionsQuery(
                user.GetUserId(), from, to, categoryId, type));
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            CreateTransactionCommand command,
            ClaimsPrincipal user,
            IMediator mediator,
            IValidator<CreateTransactionCommand> validator) =>
        {
            var fullCommand = command with { UserId = user.GetUserId() };
            var validation = await validator.ValidateAsync(fullCommand);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(fullCommand);
                return Results.Created($"/api/transactions/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.GetUserId());

            if (transaction is null)
                return Results.NotFound();

            db.Transactions.Remove(transaction);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            await db.Transactions
                .Where(t => t.UserId == user.GetUserId())
                .ExecuteDeleteAsync();
            return Results.NoContent();
        });
    }
}
