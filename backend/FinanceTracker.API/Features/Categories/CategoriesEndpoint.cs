using System.Security.Claims;
using FinanceTracker.API.Common.Extensions;
using FinanceTracker.API.Features.Categories.Create;
using FinanceTracker.API.Features.Categories.GetAll;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Categories;

public static class CategoriesEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/categories").RequireAuthorization().WithTags("Categories");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapDelete("/{id:guid}", Delete);
    }

    private static async Task<IResult> GetAll(ClaimsPrincipal user, IMediator mediator)
    {
        var result = await mediator.Send(new GetCategoriesQuery(user.GetUserId()));
        return Results.Ok(result);
    }

    private static async Task<IResult> Create(CreateCategoryCommand command, ClaimsPrincipal user, IMediator mediator)
    {
        var fullCommand = command with { UserId = user.GetUserId() };
        var result = await mediator.Send(fullCommand);
        return Results.Created($"/api/categories/{result.Id}", result);
    }

    private static async Task<IResult> Delete(Guid id, ClaimsPrincipal user, AppDbContext db)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.GetUserId());
        if (category is null) return Results.NotFound();
        if (category.IsDefault) return Results.BadRequest(new { error = "No puedes eliminar una categoría por defecto." });
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
