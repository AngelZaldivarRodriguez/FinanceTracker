using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.PdfParsing;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.CreditCards;

// --- DTOs ---

public record CreditCardTransactionDto(
    Guid Id,
    DateTime OperationDate,
    DateTime ChargeDate,
    string Description,
    decimal Amount,
    bool IsCredit,
    string StatementPeriod
);

public record MsiPromotionDto(
    Guid Id, string Description, DateTime PurchaseDate,
    decimal OriginalAmount, decimal PendingBalance, decimal MonthlyPayment,
    int TotalMonths, int PaidMonths, bool IsCompleted,
    decimal ProgressPercent
);

public record CreditCardDto(
    Guid Id, string LastFourDigits, string Name,
    decimal CreditLimit, decimal AvailableCredit,
    decimal RegularBalance, decimal MsiBalance, decimal TotalBalance,
    decimal PaymentToAvoidInterest, decimal MinimumPayment,
    int CutoffDay, int PaymentDueDay,
    DateTime LastStatementDate, DateTime NextPaymentDueDate,
    int DaysUntilPayment,
    List<MsiPromotionDto> Promotions,
    List<CreditCardTransactionDto> RecentTransactions
);

public record ParsedStatementData(
    string LastFourDigits,
    decimal CreditLimit,
    decimal AvailableCredit,
    decimal RegularBalance,
    decimal MsiBalance,
    decimal TotalBalance,
    decimal PaymentToAvoidInterest,
    decimal MinimumPayment,
    int CutoffDay,
    int PaymentDueDay,
    DateTime LastStatementDate,
    DateTime NextPaymentDueDate,
    List<ParsedPromotion> Promotions,
    List<ParsedRegularTransactionDto> RegularTransactions
);

public record ParsedRegularTransactionDto(
    DateTime OperationDate,
    DateTime ChargeDate,
    string Description,
    decimal Amount,
    bool IsCredit
);

public record ParsedPromotion(
    string Description,
    DateTime PurchaseDate,
    decimal OriginalAmount,
    decimal PendingBalance,
    decimal MonthlyPayment,
    int TotalMonths,
    int PaidMonths
);

// --- Commands / Queries ---

public record ParseBbvaStatementCommand(byte[] PdfBytes) : IRequest<ParsedStatementData>;

public record CreateCreditCardCommand(
    Guid UserId,
    string LastFourDigits,
    string Name,
    decimal CreditLimit,
    decimal AvailableCredit,
    decimal RegularBalance,
    decimal MsiBalance,
    decimal TotalBalance,
    decimal PaymentToAvoidInterest,
    decimal MinimumPayment,
    int CutoffDay,
    int PaymentDueDay,
    DateTime LastStatementDate,
    DateTime NextPaymentDueDate,
    List<ParsedPromotion> Promotions,
    List<ParsedRegularTransactionDto> RegularTransactions
) : IRequest<CreditCardDto>;

public record GetCreditCardsQuery(Guid UserId) : IRequest<List<CreditCardDto>>;

public record UpdateFromStatementCommand(Guid CardId, Guid UserId, byte[] PdfBytes) : IRequest<CreditCardDto?>;

// --- DTO mapper ---

internal static class CreditCardMapper
{
    internal static CreditCardDto ToDto(CreditCard card)
    {
        var days = (int)(card.NextPaymentDueDate.Date - DateTime.Today).TotalDays;
        return new CreditCardDto(
            card.Id, card.LastFourDigits, card.Name,
            card.CreditLimit, card.AvailableCredit,
            card.RegularBalance, card.MsiBalance, card.TotalBalance,
            card.PaymentToAvoidInterest, card.MinimumPayment,
            card.CutoffDay, card.PaymentDueDay,
            card.LastStatementDate, card.NextPaymentDueDate,
            DaysUntilPayment: days,
            Promotions: card.Promotions.OrderBy(p => p.PurchaseDate).Select(p => new MsiPromotionDto(
                p.Id, p.Description, p.PurchaseDate,
                p.OriginalAmount, p.PendingBalance, p.MonthlyPayment,
                p.TotalMonths, p.PaidMonths, p.IsCompleted,
                ProgressPercent: p.TotalMonths > 0 ? Math.Round((decimal)p.PaidMonths / p.TotalMonths * 100, 1) : 0
            )).ToList(),
            RecentTransactions: card.CreditCardTransactions
                .GroupBy(t => t.StatementPeriod)
                .OrderByDescending(g => g.Key)
                .FirstOrDefault()
                ?.OrderByDescending(t => t.OperationDate)
                .Select(t => new CreditCardTransactionDto(
                    t.Id, t.OperationDate, t.ChargeDate, t.Description, t.Amount, t.IsCredit, t.StatementPeriod
                )).ToList() ?? []
        );
    }
}

// --- PdfPig PDF parser (C# nativo, sin dependencia de Python) ---

public class ParseBbvaStatementHandler : IRequestHandler<ParseBbvaStatementCommand, ParsedStatementData>
{
    public Task<ParsedStatementData> Handle(ParseBbvaStatementCommand request, CancellationToken ct)
    {
        using var stream = new MemoryStream(request.PdfBytes);
        var result = BbvaCreditCardParser.Parse(stream);

        return Task.FromResult(new ParsedStatementData(
            LastFourDigits: result.LastFourDigits,
            CreditLimit: result.CreditLimit,
            AvailableCredit: result.AvailableCredit,
            RegularBalance: result.RegularBalance,
            MsiBalance: result.MsiBalance,
            TotalBalance: result.TotalBalance,
            PaymentToAvoidInterest: result.PaymentToAvoidInterest,
            MinimumPayment: result.MinimumPayment,
            CutoffDay: result.CutoffDay,
            PaymentDueDay: result.PaymentDueDay,
            LastStatementDate: result.LastStatementDate,
            NextPaymentDueDate: result.NextPaymentDueDate,
            Promotions: result.Promotions.Select(p => new ParsedPromotion(
                Description: p.Description,
                PurchaseDate: p.PurchaseDate,
                OriginalAmount: p.OriginalAmount,
                PendingBalance: p.PendingBalance,
                MonthlyPayment: p.MonthlyPayment,
                TotalMonths: p.TotalMonths,
                PaidMonths: p.PaidMonths
            )).ToList(),
            RegularTransactions: result.RegularTransactions.Select(t => new ParsedRegularTransactionDto(
                OperationDate: t.OperationDate,
                ChargeDate: t.ChargeDate,
                Description: t.Description,
                Amount: t.Amount,
                IsCredit: t.IsCredit
            )).ToList()
        ));
    }
}

// --- Handlers ---

public class CreateCreditCardHandler(AppDbContext db) : IRequestHandler<CreateCreditCardCommand, CreditCardDto>
{
    public async Task<CreditCardDto> Handle(CreateCreditCardCommand request, CancellationToken ct)
    {
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            LastFourDigits = request.LastFourDigits,
            Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : $"BBVA *{request.LastFourDigits}",
            CreditLimit = request.CreditLimit,
            AvailableCredit = request.AvailableCredit,
            RegularBalance = request.RegularBalance,
            MsiBalance = request.MsiBalance,
            TotalBalance = request.TotalBalance,
            PaymentToAvoidInterest = request.PaymentToAvoidInterest,
            MinimumPayment = request.MinimumPayment,
            CutoffDay = request.CutoffDay,
            PaymentDueDay = request.PaymentDueDay,
            LastStatementDate = request.LastStatementDate,
            NextPaymentDueDate = request.NextPaymentDueDate,
            CreatedAt = DateTime.UtcNow,
            Promotions = request.Promotions.Select(p => new MsiPromotion
            {
                Id = Guid.NewGuid(),
                Description = p.Description,
                PurchaseDate = p.PurchaseDate,
                OriginalAmount = p.OriginalAmount,
                PendingBalance = p.PendingBalance,
                MonthlyPayment = p.MonthlyPayment,
                TotalMonths = p.TotalMonths,
                PaidMonths = p.PaidMonths,
                IsCompleted = p.PaidMonths >= p.TotalMonths,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        db.CreditCards.Add(card);
        await db.SaveChangesAsync(ct);

        var statementPeriod = request.LastStatementDate.ToString("yyyy-MM");
        var transactions = request.RegularTransactions.Select(t => new Domain.Entities.CreditCardTransaction
        {
            Id = Guid.NewGuid(),
            CreditCardId = card.Id,
            OperationDate = t.OperationDate,
            ChargeDate = t.ChargeDate,
            Description = t.Description,
            Amount = t.Amount,
            IsCredit = t.IsCredit,
            StatementPeriod = statementPeriod,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        if (transactions.Count > 0)
        {
            db.CreditCardTransactions.AddRange(transactions);
            await db.SaveChangesAsync(ct);
            card.CreditCardTransactions = transactions;
        }

        return CreditCardMapper.ToDto(card);
    }
}

public class GetCreditCardsHandler(AppDbContext db) : IRequestHandler<GetCreditCardsQuery, List<CreditCardDto>>
{
    public async Task<List<CreditCardDto>> Handle(GetCreditCardsQuery request, CancellationToken ct)
    {
        var cards = await db.CreditCards
            .Include(c => c.Promotions)
            .Include(c => c.CreditCardTransactions)
            .Where(c => c.UserId == request.UserId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return cards.Select(CreditCardMapper.ToDto).ToList();
    }
}

public class UpdateFromStatementHandler(AppDbContext db, IMediator mediator)
    : IRequestHandler<UpdateFromStatementCommand, CreditCardDto?>
{
    public async Task<CreditCardDto?> Handle(UpdateFromStatementCommand request, CancellationToken ct)
    {
        var card = await db.CreditCards
            .Include(c => c.Promotions)
            .Include(c => c.CreditCardTransactions)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.UserId == request.UserId, ct);

        if (card is null) return null;

        var parsed = await mediator.Send(new ParseBbvaStatementCommand(request.PdfBytes), ct);

        card.AvailableCredit = parsed.AvailableCredit;
        card.RegularBalance = parsed.RegularBalance;
        card.MsiBalance = parsed.MsiBalance;
        card.TotalBalance = parsed.TotalBalance;
        card.PaymentToAvoidInterest = parsed.PaymentToAvoidInterest;
        card.MinimumPayment = parsed.MinimumPayment;
        card.LastStatementDate = parsed.LastStatementDate;
        card.NextPaymentDueDate = parsed.NextPaymentDueDate;
        card.CutoffDay = parsed.CutoffDay;
        card.PaymentDueDay = parsed.PaymentDueDay;

        foreach (var pp in parsed.Promotions)
        {
            var existing = card.Promotions.FirstOrDefault(
                p => p.Description == pp.Description && p.PurchaseDate.Date == pp.PurchaseDate.Date);

            if (existing is not null)
            {
                existing.PendingBalance = pp.PendingBalance;
                existing.PaidMonths = pp.PaidMonths;
                existing.IsCompleted = pp.PaidMonths >= pp.TotalMonths;
            }
            else
            {
                card.Promotions.Add(new MsiPromotion
                {
                    Id = Guid.NewGuid(),
                    CreditCardId = card.Id,
                    Description = pp.Description,
                    PurchaseDate = pp.PurchaseDate,
                    OriginalAmount = pp.OriginalAmount,
                    PendingBalance = pp.PendingBalance,
                    MonthlyPayment = pp.MonthlyPayment,
                    TotalMonths = pp.TotalMonths,
                    PaidMonths = pp.PaidMonths,
                    IsCompleted = pp.PaidMonths >= pp.TotalMonths,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Mark promotions not in the new statement as completed
        var incomingKeys = parsed.Promotions
            .Select(p => (p.Description, p.PurchaseDate.Date))
            .ToHashSet();

        foreach (var p in card.Promotions.Where(p => !p.IsCompleted))
        {
            if (!incomingKeys.Contains((p.Description, p.PurchaseDate.Date)))
                p.IsCompleted = true;
        }

        // Delete existing transactions for this statement period and re-insert
        var statementPeriod = parsed.LastStatementDate.ToString("yyyy-MM");
        var toRemove = card.CreditCardTransactions
            .Where(t => t.StatementPeriod == statementPeriod)
            .ToList();
        db.CreditCardTransactions.RemoveRange(toRemove);

        var newTransactions = parsed.RegularTransactions.Select(t => new Domain.Entities.CreditCardTransaction
        {
            Id = Guid.NewGuid(),
            CreditCardId = card.Id,
            OperationDate = t.OperationDate,
            ChargeDate = t.ChargeDate,
            Description = t.Description,
            Amount = t.Amount,
            IsCredit = t.IsCredit,
            StatementPeriod = statementPeriod,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        db.CreditCardTransactions.AddRange(newTransactions);

        await db.SaveChangesAsync(ct);

        // Reload for DTO mapping
        card.CreditCardTransactions = card.CreditCardTransactions
            .Where(t => t.StatementPeriod != statementPeriod)
            .Concat(newTransactions)
            .ToList();

        return CreditCardMapper.ToDto(card);
    }
}
