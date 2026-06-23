using FinanceTracker.API.Features.Auth.Login;
using FinanceTracker.API.Features.Auth.Register;
using FinanceTracker.API.Features.Budgets;
using FinanceTracker.API.Features.Categories;
using FinanceTracker.API.Features.Import;
using FinanceTracker.API.Features.Transactions;

namespace FinanceTracker.API.Common.Extensions;

public static class EndpointExtensions
{
    public static void MapFeatureEndpoints(this WebApplication app)
    {
        RegisterEndpoint.Map(app);
        LoginEndpoint.Map(app);
        CategoriesEndpoint.Map(app);
        TransactionsEndpoint.Map(app);
        ImportEndpoint.Map(app);
        BudgetsEndpoint.Map(app);
    }
}
