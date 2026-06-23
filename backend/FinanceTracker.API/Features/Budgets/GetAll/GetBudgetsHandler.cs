using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Budgets.GetAll;

public class GetBudgetsHandler(AppDbContext db) : IRequestHandler<GetBudgetsQuery, List<BudgetResponse>>
{
    public async Task<List<BudgetResponse>> Handle(GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var budgets = await db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == request.UserId && b.Month == request.Month && b.Year == request.Year)
            .ToListAsync(cancellationToken);

        var categoryIds = budgets.Select(b => b.CategoryId).ToList();

        // Calcular lo gastado en cada categoria para el mes solicitado
        var spent = await db.Transactions
            .Where(t =>
                t.UserId == request.UserId &&
                t.Type == TransactionType.Expense &&
                t.Date.Month == request.Month &&
                t.Date.Year == request.Year &&
                categoryIds.Contains(t.CategoryId))
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total, cancellationToken);

        return budgets.Select(b =>
        {
            var spentAmount = spent.GetValueOrDefault(b.CategoryId, 0);
            var percentage = b.LimitAmount > 0 ? Math.Round(spentAmount / b.LimitAmount * 100, 1) : 0;

            return new BudgetResponse(
                b.Id,
                b.CategoryId,
                b.Category.Name,
                b.Category.Icon,
                b.Category.Color,
                b.LimitAmount,
                spentAmount,
                percentage,
                b.Month,
                b.Year
            );
        }).OrderByDescending(b => b.Percentage).ToList();
    }
}
