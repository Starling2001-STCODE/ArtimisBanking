using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class SavingsAccountTransaction : BaseAuditableEntity
{
    public Guid SavingsAccountId { get; private set; }
    public SavingsAccount SavingsAccount { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionOrigin Origin { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal BalanceAfterTransaction { get; private set; }

    protected SavingsAccountTransaction() { }

    private SavingsAccountTransaction(
        Guid savingsAccountId,
        decimal amount,
        TransactionType type,
        TransactionOrigin origin,
        string description,
        decimal balanceAfter)
    {
        SavingsAccountId = savingsAccountId;
        Amount = amount;
        Type = type;
        Origin = origin;
        Description = description;
        BalanceAfterTransaction = balanceAfter;
        Status = TransactionStatus.Aprobada;
    }

    public static SavingsAccountTransaction Create(
        Guid savingsAccountId,
        decimal amount,
        TransactionType type,
        TransactionOrigin origin,
        string description,
        decimal balanceAfter)
        => new(savingsAccountId, amount, type, origin, description, balanceAfter);
}