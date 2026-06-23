using FinanceTracker.API.Domain.Enums;

namespace FinanceTracker.API.Infrastructure.PdfParsing;

public record BbvaParsedTransaction(
    DateTime Date,
    string Description,
    decimal Amount,
    TransactionType Type,
    string? Reference
);
