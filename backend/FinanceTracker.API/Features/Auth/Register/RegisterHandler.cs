using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Auth;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Auth.Register;

public class RegisterHandler(AppDbContext db, JwtService jwt) : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private static readonly (string Name, string Icon, string Color)[] DefaultCategories =
    [
        ("Comida", "🍕", "#EF4444"),
        ("Transporte", "🚗", "#F97316"),
        ("Conveniencia", "🏪", "#EAB308"),
        ("Entretenimiento", "🎮", "#8B5CF6"),
        ("Servicios", "💡", "#3B82F6"),
        ("Salud", "💊", "#10B981"),
        ("Suscripciones", "📱", "#EC4899"),
        ("Transferencias", "💸", "#6B7280"),
        ("Ingresos", "💰", "#22C55E"),
        ("Otros", "📦", "#94A3B8"),
    ];

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

        var categories = DefaultCategories.Select(c => new Category
        {
            Id = Guid.NewGuid(),
            Name = c.Name,
            Icon = c.Icon,
            Color = c.Color,
            IsDefault = true,
            UserId = user.Id
        });

        db.Users.Add(user);
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(cancellationToken);

        var token = jwt.GenerateToken(user);
        return new RegisterResponse(token, user.Name, user.Email);
    }
}
