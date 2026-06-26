using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using MediatR;

namespace FinanceTracker.API.Features.CreditCards;

public static class CreditCardsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/credit-cards").RequireAuthorization().WithTags("CreditCards");

        group.MapPost("/parse-statement", ParseStatement);
        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}/update-statement", UpdateStatement);
    }

    private static async Task<IResult> ParseStatement(HttpRequest request, IMediator mediator)
    {
        if (!request.HasFormContentType) return Results.BadRequest("Multipart form expected");
        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file is null) return Results.BadRequest("No file uploaded");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var result = await mediator.Send(new ParseBbvaStatementCommand(ms.ToArray()));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAll(ClaimsPrincipal user, IMediator mediator)
    {
        var result = await mediator.Send(new GetCreditCardsQuery(user.GetUserId()));
        return Results.Ok(result);
    }

    private static async Task<IResult> Create(CreateCreditCardCommand command, ClaimsPrincipal user, IMediator mediator)
    {
        var cmd = command with { UserId = user.GetUserId() };
        var result = await mediator.Send(cmd);
        return Results.Created($"/api/credit-cards/{result.Id}", result);
    }

    private static async Task<IResult> UpdateStatement(Guid id, HttpRequest request, ClaimsPrincipal user, IMediator mediator)
    {
        if (!request.HasFormContentType) return Results.BadRequest("Multipart form expected");
        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file is null) return Results.BadRequest("No file uploaded");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var result = await mediator.Send(new UpdateFromStatementCommand(id, user.GetUserId(), ms.ToArray()));
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
