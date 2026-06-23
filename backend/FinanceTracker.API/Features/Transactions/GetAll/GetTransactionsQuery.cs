using FinanceTracker.API.Domain.Enums;
using MediatR;

namespace FinanceTracker.API.Features.Transactions.GetAll;

public record GetTransactionsQuery(
    Guid UserId,
    DateTime? From,
    DateTime? To,
    Guid? CategoryId,
    TransactionType? Type
) : IRequest<List<TransactionResponse>>;

public record TransactionResponse(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    string Description,
    DateTime Date,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    bool IsImported
);
