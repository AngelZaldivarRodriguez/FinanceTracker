using MediatR;

namespace FinanceTracker.API.Features.Auth.Register;

public record RegisterCommand(string Name, string Email, string Password) : IRequest<RegisterResponse>;

public record RegisterResponse(string Token, string Name, string Email);
