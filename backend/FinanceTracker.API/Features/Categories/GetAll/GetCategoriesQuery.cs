using MediatR;

namespace FinanceTracker.API.Features.Categories.GetAll;

public record GetCategoriesQuery(Guid UserId) : IRequest<List<CategoryResponse>>;

public record CategoryResponse(Guid Id, string Name, string Icon, string Color, bool IsDefault);
