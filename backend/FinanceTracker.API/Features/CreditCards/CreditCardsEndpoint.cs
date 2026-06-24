using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using MediatR;

namespace FinanceTracker.API.Features.CreditCards;

public static class CreditCardsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/credit-cards").RequireAuthorization().WithTags("CreditCards");

        // Parse PDF without saving
        group.MapPost("/parse-statement", async (HttpRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Multipart form expected");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest("No file uploaded");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            try
            {
                var result = await mediator.Send(new ParseBbvaStatementCommand(ms.ToArray()));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // Get all cards
        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCreditCardsQuery(user.GetUserId()));
            return Results.Ok(result);
        });

        // Create card from parsed data
        group.MapPost("/", async (CreateCreditCardCommand body, ClaimsPrincipal user, IMediator mediator) =>
        {
            var cmd = body with { UserId = user.GetUserId() };
            var result = await mediator.Send(cmd);
            return Results.Created($"/api/credit-cards/{result.Id}", result);
        });

        // Update existing card from new statement PDF
        group.MapPut("/{id:guid}/update-statement", async (Guid id, HttpRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Multipart form expected");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest("No file uploaded");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            try
            {
                var result = await mediator.Send(new UpdateFromStatementCommand(id, user.GetUserId(), ms.ToArray()));
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}
