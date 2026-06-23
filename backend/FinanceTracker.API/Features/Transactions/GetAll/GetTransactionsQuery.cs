using FinanceTracker.API.Domain.Enums;
using MediatR;

namespace FinanceTracker.API.Features.Transactions.GetAll;

public record GetTransactionsQuery(
    Guid UserId,
    DateTime? From,
    DateTime? To,
    Guid? CategoryId,
    TransactionType? Type,
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedTransactionsResponse>;

public record PagedTransactionsResponse(
    List<TransactionResponse> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages
);

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
