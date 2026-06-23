using MediatR;

namespace FinanceTracker.API.Features.Budgets.Create;

public record CreateBudgetCommand(Guid UserId, Guid CategoryId, decimal LimitAmount, int Month, int Year) : IRequest<BudgetCreatedResponse>;

public record BudgetCreatedResponse(Guid Id, Guid CategoryId, string CategoryName, decimal LimitAmount, int Month, int Year);
