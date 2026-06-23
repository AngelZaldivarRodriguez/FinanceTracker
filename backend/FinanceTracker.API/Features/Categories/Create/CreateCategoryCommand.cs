using MediatR;

namespace FinanceTracker.API.Features.Categories.Create;

public record CreateCategoryCommand(Guid UserId, string Name, string Icon, string Color) : IRequest<CategoryCreatedResponse>;

public record CategoryCreatedResponse(Guid Id, string Name, string Icon, string Color);
