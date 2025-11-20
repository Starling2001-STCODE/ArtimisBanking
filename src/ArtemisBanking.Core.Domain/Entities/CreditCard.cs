using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class CreditCard : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid AssignedByAdminUserId { get; private set; }

    public string CardNumber { get; private set; } = default!;
    public string CvcHash { get; private set; } = default!;
    public int ExpirationMonth { get; private set; }
    public int ExpirationYear { get; private set; }

    public decimal CreditLimit { get; private set; }
    public decimal CurrentDebt { get; private set; }

    public CreditCardStatus Status { get; private set; }

    public ICollection<CreditCardTransaction> Transactions { get; private set; } =
        new List<CreditCardTransaction>();

    private CreditCard() { }

    public static CreditCard AssignToClient(
        Guid userId,
        Guid assignedByAdminUserId,
        string cardNumber,
        decimal limit,
        int expMonth,
        int expYear,
        string cvcHash)
    {
        var card = new CreditCard
        {
            UserId = userId,
            AssignedByAdminUserId = assignedByAdminUserId,
            CardNumber = cardNumber,
            CvcHash = cvcHash,
            CreditLimit = limit,
            CurrentDebt = 0,
            ExpirationMonth = expMonth,
            ExpirationYear = expYear,
            Status = CreditCardStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return card;
    }

    public void RegisterTransaction(CreditCardTransaction transaction)
    {
        if (Status != CreditCardStatus.Active)
            throw new InvalidOperationException("No se pueden registrar transacciones en una tarjeta cancelada o bloqueada.");

        if (transaction.TransactionType == CreditCardTransactionType.Payment)
        {
            CurrentDebt -= transaction.Amount;

            if (CurrentDebt < 0)
                CurrentDebt = 0;
        }
        else
        {
            CurrentDebt += transaction.Amount;

            if (CurrentDebt > CreditLimit)
            {
                transaction.MarkAsRejected();
                return;
            }
        }

        Transactions.Add(transaction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeLimit(decimal newLimit)
    {
        if (newLimit < CurrentDebt)
            throw new InvalidOperationException("El nuevo límite no puede ser menor que la deuda actual.");

        CreditLimit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (CurrentDebt > 0)
            throw new InvalidOperationException("No se puede cancelar una tarjeta con deuda pendiente.");

        Status = CreditCardStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
