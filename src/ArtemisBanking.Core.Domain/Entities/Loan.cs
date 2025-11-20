using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class Loan : BaseAuditableEntity
{
    public string LoanNumber { get; private set; } = default!;
    public Guid UserId { get; private set; }

    public decimal Capital { get; private set; }

    public decimal AnnualInterestRate { get; private set; }

    public int TermInMonths { get; private set; }

    public LoanStatus Status { get; private set; }


    public bool IsHighRisk { get; private set; }

    public ICollection<LoanInstallment> Installments { get; private set; } =
        new List<LoanInstallment>();

    protected Loan() { }

    private Loan(
        Guid userId,
        string loanNumber,
        decimal capital,
        decimal annualInterestRate,
        int termInMonths,
        bool isHighRisk)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("El cliente es requerido.", nameof(userId));

        if (string.IsNullOrWhiteSpace(loanNumber))
            throw new ArgumentException("El número de préstamo es requerido.", nameof(loanNumber));

        if (capital <= 0)
            throw new ArgumentException("El monto del préstamo debe ser mayor que cero.", nameof(capital));

        if (annualInterestRate <= 0)
            throw new ArgumentException("La tasa anual debe ser mayor que cero.", nameof(annualInterestRate));

        if (termInMonths <= 0)
            throw new ArgumentException("El plazo en meses debe ser mayor que cero.", nameof(termInMonths));

        UserId = userId;
        LoanNumber = loanNumber;
        Capital = capital;
        AnnualInterestRate = annualInterestRate;
        TermInMonths = termInMonths;
        Status = LoanStatus.Activo;
        IsHighRisk = isHighRisk;
    }


    public static Loan Create(
        Guid userId,
        string loanNumber,
        decimal capital,
        decimal annualInterestRate,
        int termInMonths,
        bool isHighRisk)
        => new(userId, loanNumber, capital, annualInterestRate, termInMonths, isHighRisk);

    public void MarkAsHighRisk()
    {
        IsHighRisk = true;
        SetUpdatedNow();
    }

    public void ChangeStatus(LoanStatus newStatus)
    {
        Status = newStatus;
        SetUpdatedNow();
    }
    public void UpdateAnnualInterestRate(decimal newAnnualRate)
    {
        if (newAnnualRate <= 0)
            throw new ArgumentException("La tasa anual debe ser mayor que cero.", nameof(newAnnualRate));

        AnnualInterestRate = newAnnualRate;
        SetUpdatedNow();
    }
}
