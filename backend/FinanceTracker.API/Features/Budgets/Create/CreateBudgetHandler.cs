using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Budgets.Create;

public class CreateBudgetHandler(AppDbContext db) : IRequestHandler<CreateBudgetCommand, BudgetCreatedResponse>
{
    public async Task<BudgetCreatedResponse> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == request.UserId, cancellationToken);

        if (category is null)
            throw new InvalidOperationException("La categoría no existe o no te pertenece.");

        var exists = await db.Budgets.AnyAsync(b =>
            b.UserId == request.UserId &&
            b.CategoryId == request.CategoryId &&
            b.Month == request.Month &&
            b.Year == request.Year, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Ya existe un presupuesto para esta categoría en ese mes.");

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            LimitAmount = request.LimitAmount,
            Month = request.Month,
            Year = request.Year,
            AlertSent = false,
            UserId = request.UserId,
            CategoryId = request.CategoryId
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetCreatedResponse(budget.Id, category.Id, category.Name, budget.LimitAmount, budget.Month, budget.Year);
    }
}
