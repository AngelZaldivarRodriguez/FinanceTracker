using MediatR;

namespace FinanceTracker.API.Features.Dashboard;

public record GetDashboardQuery(Guid UserId, int Month, int Year) : IRequest<DashboardResponse>;

public record DashboardResponse(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    List<CategorySpending> SpendingByCategory,
    List<RecentTransaction> RecentTransactions,
    List<DailyFlow> DailyFlow
);

public record DailyFlow(string Date, decimal Income, decimal Expenses);

public record CategorySpending(
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal Amount,
    decimal Percentage
);

public record RecentTransaction(
    Guid Id,
    string Description,
    decimal Amount,
    string Type,
    DateTime Date,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor
);
