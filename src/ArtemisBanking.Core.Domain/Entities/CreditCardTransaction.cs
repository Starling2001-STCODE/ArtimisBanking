using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class CreditCardTransaction : BaseAuditableEntity
{
    public Guid CreditCardId { get; private set; }
    public CreditCard CreditCard { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public string Description { get; private set; } = default!;
    public CreditCardTransactionType TransactionType { get; private set; }
    public CreditCardTransactionStatus Status { get; private set; }

    private CreditCardTransaction() { }

    public static CreditCardTransaction Create(
        Guid creditCardId,
        decimal amount,
        string description,
        CreditCardTransactionType transactionType)
    {
        return new CreditCardTransaction
        {
            CreditCardId = creditCardId,
            Amount = amount,
            Description = description,
            TransactionType = transactionType,
            Status = CreditCardTransactionStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsApproved()
    {
        Status = CreditCardTransactionStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRejected()
    {
        Status = CreditCardTransactionStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }
}
