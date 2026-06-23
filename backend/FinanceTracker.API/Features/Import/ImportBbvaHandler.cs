using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Persistence;
using FinanceTracker.API.Infrastructure.PdfParsing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Import;

public class ImportBbvaHandler(AppDbContext db) : IRequestHandler<ImportBbvaCommand, ImportBbvaResponse>
{
    public async Task<ImportBbvaResponse> Handle(ImportBbvaCommand request, CancellationToken cancellationToken)
    {
        var parsed = BbvaStatementParser.Parse(request.PdfStream);

        if (parsed.Count == 0)
            return new ImportBbvaResponse(0, 0, ["No se encontraron transacciones en el PDF."]);

        // Cargar categorias del usuario una sola vez
        var categories = await db.Categories
            .Where(c => c.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        // Deduplicacion por fecha+monto+descripcion (la referencia ******7658 es el numero de tarjeta enmascarado, no es unica)
        var existingKeys = await db.Transactions
            .Where(t => t.UserId == request.UserId && t.IsImported)
            .Select(t => t.Date.ToString("yyyyMMdd") + "|" + t.Amount.ToString() + "|" + t.Description)
            .ToHashSetAsync(cancellationToken);

        var toInsert = new List<Transaction>();
        var skipped = 0;
        var errors = new List<string>();

        foreach (var item in parsed)
        {
            var key = item.Date.ToString("yyyyMMdd") + "|" + item.Amount.ToString() + "|" + item.Description;
            if (existingKeys.Contains(key))
            {
                skipped++;
                continue;
            }

            var suggestedCategoryName = CategoryMapper.Suggest(item.Description, item.Type);
            var category = categories.FirstOrDefault(c => c.Name == suggestedCategoryName)
                ?? categories.First(c => c.Name == "Otros");

            toInsert.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = item.Amount,
                Type = item.Type,
                Description = item.Description,
                Date = item.Date,
                Reference = item.Reference,
                IsImported = true,
                UserId = request.UserId,
                CategoryId = category.Id
            });
        }

        db.Transactions.AddRange(toInsert);
        await db.SaveChangesAsync(cancellationToken);

        return new ImportBbvaResponse(toInsert.Count, skipped, errors);
    }
}
