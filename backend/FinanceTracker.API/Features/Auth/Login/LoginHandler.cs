using FinanceTracker.API.Infrastructure.Auth;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Auth.Login;

public class LoginHandler(AppDbContext db, JwtService jwt) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        var token = jwt.GenerateToken(user);
        return new LoginResponse(token, user.Name, user.Email);
    }
}
