using FinanceTracker.API.Domain.Enums;
using MediatR;

namespace FinanceTracker.API.Features.Transactions.Create;

public record CreateTransactionCommand(
    Guid UserId,
    decimal Amount,
    TransactionType Type,
    string Description,
    DateTime Date,
    Guid CategoryId
) : IRequest<TransactionCreatedResponse>;

public record TransactionCreatedResponse(Guid Id, decimal Amount, TransactionType Type, string Description, DateTime Date, string CategoryName);
