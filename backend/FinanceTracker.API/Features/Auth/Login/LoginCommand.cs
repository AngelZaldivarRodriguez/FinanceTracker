using MediatR;

namespace FinanceTracker.API.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public record LoginResponse(string Token, string Name, string Email);
