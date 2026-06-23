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
    decimal TotalInterestPaid, decimal TotalCapitalPaid, int DaysUntilNextPayment,
    decimal CarPrice, decimal DownPayment
);

public record AmortizationRow(
    int Number, DateTime DueDate, decimal Capital, decimal CapitalSeguro,
    decimal InterestWithIva, decimal SeguroVida, decimal Total, decimal Balance,
    bool IsPaid, DateTime? PaidDate
);

public record LoanDetailDto(LoanSummaryDto Summary, List<AmortizationRow> Schedule);

// --- Queries / Commands ---

public record GetLoansQuery(Guid UserId) : IRequest<List<LoanSummaryDto>>;
public record GetLoanDetailQuery(Guid LoanId, Guid UserId) : IRequest<LoanDetailDto?>;
public record CreateLoanCommand(
    Guid UserId, string Name, decimal OriginalAmount,
    decimal AnnualRatePercent, int TotalPayments, decimal MonthlyPayment,
    DateTime StartDate, decimal CarPrice, decimal DownPayment,
    int InitialPaidPayments
) : IRequest<LoanSummaryDto>;
public record MarkPaymentPaidCommand(Guid LoanId, Guid UserId, int PaymentNumber, DateTime PaidDate) : IRequest<bool>;

// --- Hardcoded KIA Finance schedule from official amortization table ---
// Format: (capitalVehiculo, capitalSeguro, interestPlusIva, seguroVida, total, balanceAfter)
file static class KiaSchedule
{
    public static readonly (decimal CapVeh, decimal CapSeg, decimal IntIva, decimal SegVida, decimal Total, decimal Balance)[] Rows =
    [
        (3607.13m,  0m,       4944.94m, 170.04m, 8722.11m,  337619.13m),
        (3659.41m,  0m,       4892.66m, 170.04m, 8722.11m,  333959.72m),
        (3712.44m,  0m,       4839.63m, 170.04m, 8722.11m,  330247.28m),
        (3766.24m,  0m,       4785.83m, 170.04m, 8722.11m,  326481.04m),
        (3820.82m,  0m,       4731.25m, 170.04m, 8722.11m,  322660.22m),
        (3876.19m,  0m,       4675.88m, 170.04m, 8722.11m,  318784.03m),
        (3932.36m,  0m,       4619.71m, 170.04m, 8722.11m,  314851.67m),
        (3989.34m,  0m,       4562.73m, 170.04m, 8722.11m,  310862.33m),
        (4047.16m,  0m,       4504.91m, 170.04m, 8722.11m,  306815.17m),
        (4105.81m,  0m,       4446.26m, 170.04m, 8722.11m,  302709.36m),
        (4165.31m,  0m,       4386.76m, 170.04m, 8722.11m,  298544.05m),
        (4225.67m,  0m,       4326.40m, 170.04m, 8722.11m,  311826.84m), // sube: renueva seguro año 2
        (4286.91m,  1346.37m, 4518.89m, 213.67m, 10365.84m, 306193.56m),
        (4349.03m,  1365.88m, 4437.26m, 213.67m, 10365.84m, 300478.65m),
        (4412.06m,  1385.68m, 4354.43m, 213.67m, 10365.84m, 294680.91m),
        (4475.99m,  1405.76m, 4270.42m, 213.67m, 10365.84m, 288799.16m),
        (4540.86m,  1426.13m, 4185.18m, 213.67m, 10365.84m, 282832.17m),
        (4606.66m,  1446.80m, 4098.71m, 213.67m, 10365.84m, 276778.71m),
        (4673.42m,  1467.76m, 4010.99m, 213.67m, 10365.84m, 270637.53m),
        (4741.15m,  1489.03m, 3921.99m, 213.67m, 10365.84m, 264407.35m),
        (4809.85m,  1510.61m, 3831.71m, 213.67m, 10365.84m, 258086.89m),
        (4879.56m,  1532.50m, 3740.11m, 213.67m, 10365.84m, 251674.83m),
        (4950.27m,  1554.71m, 3647.19m, 213.67m, 10365.84m, 245169.85m),
        (5022.01m,  1577.23m, 3552.93m, 213.67m, 10365.84m, 256079.07m), // sube: renueva seguro año 3
        (5094.78m,  1346.37m, 3711.02m, 213.67m, 10365.84m, 249637.92m),
        (5168.62m,  1365.88m, 3617.67m, 213.67m, 10365.84m, 243103.42m),
        (5243.52m,  1385.68m, 3522.97m, 213.67m, 10365.84m, 236474.22m),
        (5319.51m,  1405.76m, 3426.90m, 213.67m, 10365.84m, 229748.95m),
        (5396.59m,  1426.13m, 3329.45m, 213.67m, 10365.84m, 222926.23m),
        (5474.80m,  1446.80m, 3230.57m, 213.67m, 10365.84m, 216004.63m),
        (5554.14m,  1467.76m, 3130.27m, 213.67m, 10365.84m, 208982.73m),
        (5634.63m,  1489.03m, 3028.51m, 213.67m, 10365.84m, 201859.07m),
        (5716.28m,  1510.61m, 2925.28m, 213.67m, 10365.84m, 194632.18m),
        (5799.12m,  1532.50m, 2820.55m, 213.67m, 10365.84m, 187300.56m),
        (5883.16m,  1554.71m, 2714.30m, 213.67m, 10365.84m, 179862.69m),
        (5968.42m,  1577.23m, 2606.52m, 213.67m, 10365.84m, 189825.50m), // sube: renueva seguro año 4
        (6054.91m,  1346.37m, 2750.89m, 213.67m, 10365.84m, 182424.22m),
        (6142.65m,  1365.88m, 2643.64m, 213.67m, 10365.84m, 174915.69m),
        (6231.67m,  1385.68m, 2534.82m, 213.67m, 10365.84m, 167298.34m),
        (6321.98m,  1405.76m, 2424.43m, 213.67m, 10365.84m, 159570.60m),
        (6413.60m,  1426.13m, 2312.44m, 213.67m, 10365.84m, 151730.87m),
        (6506.54m,  1446.80m, 2198.83m, 213.67m, 10365.84m, 143777.53m),
        (6600.83m,  1467.76m, 2083.58m, 213.67m, 10365.84m, 135708.94m),
        (6696.49m,  1489.03m, 1966.65m, 213.67m, 10365.84m, 127523.42m),
        (6793.53m,  1510.61m, 1848.03m, 213.67m, 10365.84m, 119219.28m),
        (6891.98m,  1532.50m, 1727.69m, 213.67m, 10365.84m, 110794.80m),
        (6991.86m,  1554.71m, 1605.60m, 213.67m, 10365.84m, 102248.23m),
        (7093.18m,  1577.23m, 1481.76m, 213.67m, 10365.84m, 111086.28m), // sube: renueva seguro año 5
        (7195.97m,  1346.37m, 1609.83m, 213.67m, 10365.84m, 102543.94m),
        (7300.25m,  1365.88m, 1486.04m, 213.67m, 10365.84m, 93877.81m),
        (7406.05m,  1385.68m, 1360.44m, 213.67m, 10365.84m, 85086.08m),
        (7513.37m,  1405.76m, 1233.04m, 213.67m, 10365.84m, 76166.95m),
        (7622.25m,  1426.13m, 1103.79m, 213.67m, 10365.84m, 67118.57m),
        (7732.71m,  1446.80m, 972.66m,  213.67m, 10365.84m, 57939.06m),
        (7844.77m,  1467.76m, 839.64m,  213.67m, 10365.84m, 48626.53m),
        (7958.46m,  1489.03m, 704.68m,  213.67m, 10365.84m, 39179.04m),
        (8073.79m,  1510.61m, 567.77m,  213.67m, 10365.84m, 29594.64m),
        (8190.79m,  1532.50m, 428.88m,  213.67m, 10365.84m, 19871.35m),
        (8309.49m,  1554.71m, 287.97m,  213.67m, 10365.84m, 10007.15m),
        (8429.92m,  1577.23m, 145.02m,  213.67m, 10365.84m, 0m),
    ];
}

// --- Schedule builder ---

public static class AmortizationCalculator
{
    public static List<AmortizationRow> BuildSchedule(DateTime startDate, List<LoanPayment> paidPayments)
    {
        var paidSet = paidPayments.ToDictionary(p => p.PaymentNumber);
        var rows = new List<AmortizationRow>();

        for (int i = 0; i < KiaSchedule.Rows.Length; i++)
        {
            var num = i + 1;
            var r = KiaSchedule.Rows[i];
            var dueDate = startDate.AddMonths(num);
            var isPaid = paidSet.TryGetValue(num, out var p) && p.IsPaid;
            rows.Add(new AmortizationRow(num, dueDate, r.CapVeh, r.CapSeg, r.IntIva, r.SegVida, r.Total, r.Balance, isPaid, p?.PaidDate));
        }

        return rows;
    }

    public static LoanSummaryDto ToSummary(Loan loan, List<AmortizationRow> schedule)
    {
        var paid = schedule.Where(r => r.IsPaid).ToList();
        var nextUnpaid = schedule.FirstOrDefault(r => !r.IsPaid);

        decimal currentBalance = paid.Count == 0
            ? loan.OriginalAmount
            : schedule[paid.Count - 1].Balance;

        var nextDue = nextUnpaid?.DueDate ?? schedule.Last().DueDate;
        var daysUntil = (int)(nextDue - DateTime.Today).TotalDays;

        return new LoanSummaryDto(
            loan.Id, loan.Name, loan.OriginalAmount, currentBalance,
            loan.AnnualRatePercent, loan.TotalPayments, paid.Count,
            loan.MonthlyPayment, loan.StartDate, nextDue,
            paid.Sum(r => r.InterestWithIva),
            paid.Sum(r => r.Capital),
            daysUntil,
            loan.CarPrice, loan.DownPayment
        );
    }
}

// --- Handlers ---

public class GetLoansHandler(AppDbContext db) : IRequestHandler<GetLoansQuery, List<LoanSummaryDto>>
{
    public async Task<List<LoanSummaryDto>> Handle(GetLoansQuery request, CancellationToken ct)
    {
        var loans = await db.Loans.Include(l => l.Payments).Where(l => l.UserId == request.UserId).ToListAsync(ct);
        return loans.Select(l =>
        {
            var schedule = AmortizationCalculator.BuildSchedule(l.StartDate, l.Payments);
            return AmortizationCalculator.ToSummary(l, schedule);
        }).ToList();
    }
}

public class GetLoanDetailHandler(AppDbContext db) : IRequestHandler<GetLoanDetailQuery, LoanDetailDto?>
{
    public async Task<LoanDetailDto?> Handle(GetLoanDetailQuery request, CancellationToken ct)
    {
        var loan = await db.Loans.Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == request.LoanId && l.UserId == request.UserId, ct);
        if (loan is null) return null;
        var schedule = AmortizationCalculator.BuildSchedule(loan.StartDate, loan.Payments);
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
            CarPrice = request.CarPrice,
            DownPayment = request.DownPayment,
        };
        db.Loans.Add(loan);
        await db.SaveChangesAsync(ct);

        // Auto-mark already completed payments
        for (int i = 1; i <= request.InitialPaidPayments && i <= KiaSchedule.Rows.Length; i++)
        {
            db.LoanPayments.Add(new LoanPayment
            {
                LoanId = loan.Id,
                PaymentNumber = i,
                DueDate = request.StartDate.AddMonths(i),
                IsPaid = true,
                PaidDate = request.StartDate.AddMonths(i),
            });
        }
        await db.SaveChangesAsync(ct);

        var schedule = AmortizationCalculator.BuildSchedule(loan.StartDate, loan.Payments);
        return AmortizationCalculator.ToSummary(loan, schedule);
    }
}

public class MarkPaymentPaidHandler(AppDbContext db) : IRequestHandler<MarkPaymentPaidCommand, bool>
{
    public async Task<bool> Handle(MarkPaymentPaidCommand request, CancellationToken ct)
    {
        var loan = await db.Loans.Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == request.LoanId && l.UserId == request.UserId, ct);
        if (loan is null) return false;

        var existing = loan.Payments.FirstOrDefault(p => p.PaymentNumber == request.PaymentNumber);
        if (existing is not null) { existing.IsPaid = true; existing.PaidDate = request.PaidDate; }
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
