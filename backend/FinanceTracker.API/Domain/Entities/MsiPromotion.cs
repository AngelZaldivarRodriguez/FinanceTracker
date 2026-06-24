namespace FinanceTracker.API.Domain.Entities;

public class MsiPromotion
{
    public Guid Id { get; set; }
    public Guid CreditCardId { get; set; }
    public CreditCard CreditCard { get; set; } = null!;
    public string Description { get; set; } = "";
    public DateTime PurchaseDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal MonthlyPayment { get; set; }
    public int TotalMonths { get; set; }
    public int PaidMonths { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
