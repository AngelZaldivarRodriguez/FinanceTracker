using MediatR;

namespace FinanceTracker.API.Features.Budgets.GetAll;

public record GetBudgetsQuery(Guid UserId, int Month, int Year) : IRequest<List<BudgetResponse>>;

public record BudgetResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal Percentage,
    int Month,
    int Year
);
