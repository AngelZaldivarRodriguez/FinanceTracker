using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Auth;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Auth.Register;

public class RegisterHandler(AppDbContext db, JwtService jwt) : IRequestHandler<RegisterCommand, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (exists)
            throw new InvalidOperationException("El email ya está registrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var token = jwt.GenerateToken(user);
        return new RegisterResponse(token, user.Name, user.Email);
    }
}
