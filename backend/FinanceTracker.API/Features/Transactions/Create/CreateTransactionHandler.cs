using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Transactions.Create;

public class CreateTransactionHandler(AppDbContext db) : IRequestHandler<CreateTransactionCommand, TransactionCreatedResponse>
{
    public async Task<TransactionCreatedResponse> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await db.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.UserId == request.UserId, cancellationToken);

        if (!categoryExists)
            throw new InvalidOperationException("La categoría no existe o no te pertenece.");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Type = request.Type,
            Description = request.Description,
            Date = request.Date,
            IsImported = false,
            UserId = request.UserId,
            CategoryId = request.CategoryId
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        var categoryName = await db.Categories
            .Where(c => c.Id == request.CategoryId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

        return new TransactionCreatedResponse(
            transaction.Id,
            transaction.Amount,
            transaction.Type,
            transaction.Description,
            transaction.Date,
            categoryName
        );
    }
}
