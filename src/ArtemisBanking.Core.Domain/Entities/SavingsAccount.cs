using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class SavingsAccount : BaseAuditableEntity
{
    public string AccountNumber { get; private set; } = default!;
    public decimal Balance { get; private set; }
    public AccountType AccountType { get; private set; }
    public AccountStatus Status { get; private set; }
    public Guid UserId { get; private set; }
    public ICollection<SavingsAccountTransaction> Transactions { get; private set; } =
        new List<SavingsAccountTransaction>();

    protected SavingsAccount() { }

    private SavingsAccount(Guid userId, string accountNumber, decimal initialAmount, bool isPrincipal)
    {
        UserId = userId;
        AccountNumber = accountNumber;
        Balance = initialAmount;
        AccountType = isPrincipal ? AccountType.Principal : AccountType.Secundaria;
        Status = AccountStatus.Activa;
    }

    public static SavingsAccount CreatePrincipal(Guid userId, string accountNumber, decimal initialAmount)
        => new(userId, accountNumber, initialAmount, true);

    public static SavingsAccount CreateSecondary(Guid userId, string accountNumber, decimal initialAmount)
        => new(userId, accountNumber, initialAmount, false);

    public SavingsAccountTransaction Credit(decimal amount, string description, TransactionOrigin origin)
    {
        if (amount <= 0) throw new ArgumentException("El monto debe ser mayor que cero.", nameof(amount));

        Balance += amount;

        var tx = SavingsAccountTransaction.Create(
            this.Id,
            amount,
            TransactionType.Credito,
            origin,
            description,
            Balance
        );

        Transactions.Add(tx);
        SetUpdatedNow();
        return tx;
    }

    public SavingsAccountTransaction Debit(decimal amount, string description, TransactionOrigin origin)
    {
        if (amount <= 0) throw new ArgumentException("El monto debe ser mayor que cero.", nameof(amount));
        if (Balance < amount) throw new InvalidOperationException("Fondos insuficientes.");

        Balance -= amount;

        var tx = SavingsAccountTransaction.Create(
            this.Id,
            amount,
            TransactionType.Debito,
            origin,
            description,
            Balance
        );

        Transactions.Add(tx);
        SetUpdatedNow();
        return tx;
    }
}