using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using FinanceTracker.API.Features.Categories.Create;
using FinanceTracker.API.Features.Categories.GetAll;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Infrastructure.Persistence;

namespace FinanceTracker.API.Features.Categories;

public static class CategoriesEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/categories").RequireAuthorization().WithTags("Categories");

        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCategoriesQuery(user.GetUserId()));
            return Results.Ok(result);
        });

        group.MapPost("/", async (CreateCategoryCommand command, ClaimsPrincipal user, IMediator mediator) =>
        {
            var fullCommand = command with { UserId = user.GetUserId() };
            var result = await mediator.Send(fullCommand);
            return Results.Created($"/api/categories/{result.Id}", result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var category = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.GetUserId());

            if (category is null)
                return Results.NotFound();

            if (category.IsDefault)
                return Results.BadRequest(new { error = "No puedes eliminar una categoría por defecto." });

            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
