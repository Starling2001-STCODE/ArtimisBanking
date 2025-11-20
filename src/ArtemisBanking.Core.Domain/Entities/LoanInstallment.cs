using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class LoanInstallment : BaseAuditableEntity
{
    public Guid LoanId { get; private set; }
    public Loan Loan { get; private set; } = default!;

    public int InstallmentNumber { get; private set; }

    /// Fecha de vencimiento de la cuota.
    public DateOnly DueDate { get; private set; }

    /// Valor total de la cuota (capital + interés).
    public decimal MonthlyPayment { get; private set; }

    /// Porción de capital dentro de la cuota.
    public decimal CapitalPortion { get; private set; }

    /// Porción de interés dentro de la cuota.
    public decimal InterestPortion { get; private set; }

    /// Saldo pendiente del préstamo después de aplicar esta cuota.
    public decimal RemainingBalance { get; private set; }

    /// Estado de la cuota (Pendiente, Pagada, Atrasada).
    public LoanInstallmentStatus Status { get; private set; }

    protected LoanInstallment() { }

    private LoanInstallment(
        Guid loanId,
        int installmentNumber,
        DateOnly dueDate,
        decimal monthlyPayment,
        decimal capitalPortion,
        decimal interestPortion,
        decimal remainingBalance)
    {
        if (loanId == Guid.Empty)
            throw new ArgumentException("El préstamo es requerido.", nameof(loanId));

        if (installmentNumber <= 0)
            throw new ArgumentException("El número de cuota debe ser mayor que cero.", nameof(installmentNumber));

        if (monthlyPayment <= 0)
            throw new ArgumentException("El valor de la cuota debe ser mayor que cero.", nameof(monthlyPayment));

        if (capitalPortion < 0)
            throw new ArgumentException("La porción de capital no puede ser negativa.", nameof(capitalPortion));

        if (interestPortion < 0)
            throw new ArgumentException("La porción de interés no puede ser negativa.", nameof(interestPortion));

        if (remainingBalance < 0)
            throw new ArgumentException("El saldo restante no puede ser negativo.", nameof(remainingBalance));

        LoanId = loanId;
        InstallmentNumber = installmentNumber;
        DueDate = dueDate;
        MonthlyPayment = monthlyPayment;
        CapitalPortion = capitalPortion;
        InterestPortion = interestPortion;
        RemainingBalance = remainingBalance;
        Status = LoanInstallmentStatus.Pending;
    }

    public static LoanInstallment Create(
        Guid loanId,
        int installmentNumber,
        DateOnly dueDate,
        decimal monthlyPayment,
        decimal capitalPortion,
        decimal interestPortion,
        decimal remainingBalance)
        => new(loanId, installmentNumber, dueDate, monthlyPayment, capitalPortion, interestPortion, remainingBalance);

    public void MarkAsPaid()
    {
        if (Status == LoanInstallmentStatus.Paid)
            return;

        Status = LoanInstallmentStatus.Paid;
        SetUpdatedNow();
    }

    public void MarkAsOverdue()
    {
        if (Status == LoanInstallmentStatus.Paid)
            return;

        Status = LoanInstallmentStatus.Overdue;
        SetUpdatedNow();
    }
}
