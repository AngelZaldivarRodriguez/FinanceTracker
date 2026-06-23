using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Transactions.GetAll;

public class GetTransactionsHandler(AppDbContext db) : IRequestHandler<GetTransactionsQuery, List<TransactionResponse>>
{
    public async Task<List<TransactionResponse>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == request.UserId);

        if (request.From.HasValue)
            query = query.Where(t => t.Date >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => t.Date <= request.To.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionResponse(
                t.Id,
                t.Amount,
                t.Type,
                t.Description,
                t.Date,
                t.CategoryId,
                t.Category.Name,
                t.Category.Icon,
                t.Category.Color,
                t.IsImported
            ))
            .ToListAsync(cancellationToken);
    }
}
