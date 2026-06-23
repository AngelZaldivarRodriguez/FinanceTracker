using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Dashboard;

public class GetDashboardHandler(AppDbContext db) : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var transactions = await db.Transactions
            .Include(t => t.Category)
            .Where(t =>
                t.UserId == request.UserId &&
                t.Date.Month == request.Month &&
                t.Date.Year == request.Year)
            .ToListAsync(cancellationToken);

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var spendingByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .Select(x => new CategorySpending(
                x.Category.Name,
                x.Category.Icon,
                x.Category.Color,
                x.Amount,
                totalExpenses > 0 ? Math.Round(x.Amount / totalExpenses * 100, 1) : 0
            ))
            .ToList();

        var recentTransactions = transactions
            .OrderByDescending(t => t.Date)
            .Take(10)
            .Select(t => new RecentTransaction(
                t.Id,
                t.Description,
                t.Amount,
                t.Type.ToString(),
                t.Date,
                t.Category.Name,
                t.Category.Icon,
                t.Category.Color
            ))
            .ToList();

        var dailyFlow = transactions
            .GroupBy(t => t.Date.Day)
            .OrderBy(g => g.Key)
            .Select(g => new DailyFlow(
                $"{request.Year}-{request.Month:D2}-{g.Key:D2}",
                g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            ))
            .ToList();

        return new DashboardResponse(
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses,
            spendingByCategory,
            recentTransactions,
            dailyFlow
        );
    }
}
