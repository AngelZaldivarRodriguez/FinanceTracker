namespace FinanceTracker.API.Domain.Entities;

public class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal AnnualRatePercent { get; set; }
    public int TotalPayments { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime StartDate { get; set; }

    public decimal CarPrice { get; set; }
    public decimal DownPayment { get; set; }

    public List<LoanPayment> Payments { get; set; } = [];
}

public class LoanPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public int PaymentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsPaid { get; set; }
}
