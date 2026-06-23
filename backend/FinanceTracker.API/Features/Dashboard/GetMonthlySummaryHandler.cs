using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Dashboard;

public record GetMonthlySummaryQuery(Guid UserId, int Months) : IRequest<List<MonthlySummary>>;

public record MonthlySummary(string Label, decimal Income, decimal Expenses);

public class GetMonthlySummaryHandler(AppDbContext db) : IRequestHandler<GetMonthlySummaryQuery, List<MonthlySummary>>
{
    public async Task<List<MonthlySummary>> Handle(GetMonthlySummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1).AddMonths(-(request.Months - 1));

        var transactions = await db.Transactions
            .Where(t => t.UserId == request.UserId && t.Date >= from)
            .ToListAsync(cancellationToken);

        var months = Enumerable.Range(0, request.Months)
            .Select(i => from.AddMonths(i))
            .ToList();

        return months.Select(m =>
        {
            var monthTx = transactions.Where(t => t.Date.Month == m.Month && t.Date.Year == m.Year).ToList();
            return new MonthlySummary(
                m.ToString("MMM yy").ToUpper(),
                monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            );
        }).ToList();
    }
}
