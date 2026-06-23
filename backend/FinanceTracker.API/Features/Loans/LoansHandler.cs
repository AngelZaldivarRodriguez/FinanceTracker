using FinanceTracker.API.Domain.Entities;
using FinanceTracker.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Features.Loans;

// --- DTOs ---

public record LoanSummaryDto(
    Guid Id, string Name, decimal OriginalAmount, decimal CurrentBalance,
    decimal AnnualRatePercent, int TotalPayments, int PaidPayments,
    decimal MonthlyPayment, DateTime StartDate, DateTime NextDueDate,
    decimal TotalInterestPaid, decimal TotalCapitalPaid, int DaysUntilNextPayment
);

public record AmortizationRow(
    int Number, DateTime DueDate, decimal Capital, decimal Interest,
    decimal Iva, decimal Total, decimal Balance, bool IsPaid, DateTime? PaidDate
);

public record LoanDetailDto(LoanSummaryDto Summary, List<AmortizationRow> Schedule);

// --- Queries / Commands ---

public record GetLoansQuery(Guid UserId) : IRequest<List<LoanSummaryDto>>;
public record GetLoanDetailQuery(Guid LoanId, Guid UserId) : IRequest<LoanDetailDto?>;
public record CreateLoanCommand(
    Guid UserId, string Name, decimal OriginalAmount,
    decimal AnnualRatePercent, int TotalPayments, decimal MonthlyPayment,
    DateTime StartDate
) : IRequest<LoanSummaryDto>;
public record MarkPaymentPaidCommand(Guid LoanId, Guid UserId, int PaymentNumber, DateTime PaidDate) : IRequest<bool>;

// --- Amortization Calculator ---

public static class AmortizationCalculator
{
    private const decimal IvaRate = 0.16m;

    public static List<AmortizationRow> Calculate(
        decimal originalAmount, decimal annualRatePercent, int totalPayments,
        decimal monthlyPayment, DateTime startDate, List<LoanPayment> paidPayments)
    {
        var monthlyRate = annualRatePercent / 100m / 12m;
        var balance = originalAmount;
        var rows = new List<AmortizationRow>();
        var paidSet = paidPayments.ToDictionary(p => p.PaymentNumber);

        for (int i = 1; i <= totalPayments; i++)
        {
            var dueDate = startDate.AddMonths(i);
            var interest = Math.Round(balance * monthlyRate, 2);
            var iva = Math.Round(interest * IvaRate, 2);
            var interestWithIva = interest + iva;
            var capital = Math.Round(monthlyPayment - interestWithIva, 2);

            if (i == totalPayments)
                capital = balance;

            capital = Math.Min(capital, balance);
            balance = Math.Round(balance - capital, 2);

            var isPaid = paidSet.TryGetValue(i, out var payment) && payment.IsPaid;

            rows.Add(new AmortizationRow(i, dueDate, capital, interest, iva,
                capital + interestWithIva, balance, isPaid, payment?.PaidDate));
        }

        return rows;
    }

    public static LoanSummaryDto ToSummary(Loan loan, List<AmortizationRow> schedule)
    {
        var paid = schedule.Where(r => r.IsPaid).ToList();
        var nextUnpaid = schedule.FirstOrDefault(r => !r.IsPaid);
        var currentBalance = nextUnpaid != null
            ? schedule.First(r => r.Number == nextUnpaid.Number - 1 || nextUnpaid.Number == 1 ? r.Number == 0 : false)?.Balance
              ?? (nextUnpaid.Number == 1 ? loan.OriginalAmount : schedule[nextUnpaid.Number - 2].Balance)
            : 0m;

        if (paid.Count == 0)
            currentBalance = loan.OriginalAmount;
        else
            currentBalance = schedule[paid.Count - 1].Balance;

        var nextDue = nextUnpaid?.DueDate ?? schedule.Last().DueDate;
        var daysUntil = (int)(nextDue - DateTime.Today).TotalDays;

        return new LoanSummaryDto(
            loan.Id, loan.Name, loan.OriginalAmount, currentBalance,
            loan.AnnualRatePercent, loan.TotalPayments, paid.Count,
            loan.MonthlyPayment, loan.StartDate, nextDue,
            paid.Sum(r => r.Interest + r.Iva),
            paid.Sum(r => r.Capital),
            daysUntil
        );
    }
}

// --- Handlers ---

public class GetLoansHandler(AppDbContext db) : IRequestHandler<GetLoansQuery, List<LoanSummaryDto>>
{
    public async Task<List<LoanSummaryDto>> Handle(GetLoansQuery request, CancellationToken ct)
    {
        var loans = await db.Loans
            .Include(l => l.Payments)
            .Where(l => l.UserId == request.UserId)
            .ToListAsync(ct);

        return loans.Select(l =>
        {
            var schedule = AmortizationCalculator.Calculate(
                l.OriginalAmount, l.AnnualRatePercent, l.TotalPayments,
                l.MonthlyPayment, l.StartDate, l.Payments);
            return AmortizationCalculator.ToSummary(l, schedule);
        }).ToList();
    }
}

public class GetLoanDetailHandler(AppDbContext db) : IRequestHandler<GetLoanDetailQuery, LoanDetailDto?>
{
    public async Task<LoanDetailDto?> Handle(GetLoanDetailQuery request, CancellationToken ct)
    {
        var loan = await db.Loans
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == request.LoanId && l.UserId == request.UserId, ct);

        if (loan is null) return null;

        var schedule = AmortizationCalculator.Calculate(
            loan.OriginalAmount, loan.AnnualRatePercent, loan.TotalPayments,
            loan.MonthlyPayment, loan.StartDate, loan.Payments);

        return new LoanDetailDto(AmortizationCalculator.ToSummary(loan, schedule), schedule);
    }
}

public class CreateLoanHandler(AppDbContext db) : IRequestHandler<CreateLoanCommand, LoanSummaryDto>
{
    public async Task<LoanSummaryDto> Handle(CreateLoanCommand request, CancellationToken ct)
    {
        var loan = new Loan
        {
            UserId = request.UserId,
            Name = request.Name,
            OriginalAmount = request.OriginalAmount,
            AnnualRatePercent = request.AnnualRatePercent,
            TotalPayments = request.TotalPayments,
            MonthlyPayment = request.MonthlyPayment,
            StartDate = request.StartDate,
        };

        db.Loans.Add(loan);
        await db.SaveChangesAsync(ct);

        var schedule = AmortizationCalculator.Calculate(
            loan.OriginalAmount, loan.AnnualRatePercent, loan.TotalPayments,
            loan.MonthlyPayment, loan.StartDate, loan.Payments);

        return AmortizationCalculator.ToSummary(loan, schedule);
    }
}

public class MarkPaymentPaidHandler(AppDbContext db) : IRequestHandler<MarkPaymentPaidCommand, bool>
{
    public async Task<bool> Handle(MarkPaymentPaidCommand request, CancellationToken ct)
    {
        var loan = await db.Loans
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == request.LoanId && l.UserId == request.UserId, ct);

        if (loan is null) return false;

        var existing = loan.Payments.FirstOrDefault(p => p.PaymentNumber == request.PaymentNumber);
        if (existing is not null)
        {
            existing.IsPaid = true;
            existing.PaidDate = request.PaidDate;
        }
        else
        {
            db.LoanPayments.Add(new LoanPayment
            {
                LoanId = loan.Id,
                PaymentNumber = request.PaymentNumber,
                DueDate = loan.StartDate.AddMonths(request.PaymentNumber),
                IsPaid = true,
                PaidDate = request.PaidDate,
            });
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
