using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using MediatR;

namespace FinanceTracker.API.Features.Dashboard;

public static class DashboardEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/dashboard", async (ClaimsPrincipal user, IMediator mediator, int? month, int? year) =>
        {
            var now = DateTime.UtcNow;
            var result = await mediator.Send(new GetDashboardQuery(
                user.GetUserId(),
                month ?? now.Month,
                year ?? now.Year));
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Dashboard");

        app.MapGet("/api/dashboard/monthly", async (ClaimsPrincipal user, IMediator mediator, int? months) =>
        {
            var result = await mediator.Send(new GetMonthlySummaryQuery(user.GetUserId(), months ?? 6));
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
