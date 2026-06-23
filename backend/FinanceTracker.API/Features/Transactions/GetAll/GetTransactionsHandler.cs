using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Transactions.GetAll;

public class GetTransactionsHandler(AppDbContext db) : IRequestHandler<GetTransactionsQuery, PagedTransactionsResponse>
{
    public async Task<PagedTransactionsResponse> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
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

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(t => t.Description.Contains(request.Search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return new PagedTransactionsResponse(
            items,
            total,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)total / request.PageSize)
        );
    }
}
