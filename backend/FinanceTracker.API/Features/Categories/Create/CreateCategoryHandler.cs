using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;

namespace FinanceTracker.API.Features.Categories.Create;

public class CreateCategoryHandler(AppDbContext db) : IRequestHandler<CreateCategoryCommand, CategoryCreatedResponse>
{
    public async Task<CategoryCreatedResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Icon = request.Icon,
            Color = request.Color,
            IsDefault = false,
            UserId = request.UserId
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CategoryCreatedResponse(category.Id, category.Name, category.Icon, category.Color);
    }
}
