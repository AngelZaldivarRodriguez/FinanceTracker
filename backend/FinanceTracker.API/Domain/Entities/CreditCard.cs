namespace FinanceTracker.API.Domain.Entities;

public class CreditCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LastFourDigits { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal RegularBalance { get; set; }
    public decimal MsiBalance { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal PaymentToAvoidInterest { get; set; }
    public decimal MinimumPayment { get; set; }
    public int CutoffDay { get; set; }
    public int PaymentDueDay { get; set; }
    public DateTime LastStatementDate { get; set; }
    public DateTime NextPaymentDueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MsiPromotion> Promotions { get; set; } = new();
    public List<CreditCardTransaction> CreditCardTransactions { get; set; } = new();
}
