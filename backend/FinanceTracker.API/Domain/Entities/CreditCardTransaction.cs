namespace FinanceTracker.API.Domain.Entities;

public class CreditCardTransaction
{
    public Guid Id { get; set; }
    public Guid CreditCardId { get; set; }
    public CreditCard CreditCard { get; set; } = null!;
    public DateTime OperationDate { get; set; }
    public DateTime ChargeDate { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }          // always positive
    public bool IsCredit { get; set; }           // true = abono (payment/-), false = cargo (+)
    public string StatementPeriod { get; set; } = ""; // e.g. "2026-06"
    public DateTime CreatedAt { get; set; }
}
