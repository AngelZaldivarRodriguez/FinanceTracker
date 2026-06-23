using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using MediatR;

namespace FinanceTracker.API.Features.Import;

public static class ImportEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/import/bbva", async (
            IFormFile file,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "Debes subir un archivo PDF." });

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "El archivo debe ser un PDF." });

            using var stream = file.OpenReadStream();
            var result = await mediator.Send(new ImportBbvaCommand(user.GetUserId(), stream));
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Import")
        .DisableAntiforgery();
    }
}
