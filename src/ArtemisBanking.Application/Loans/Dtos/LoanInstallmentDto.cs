using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Loans.Dtos;

public class LoanInstallmentDto
{
    public Guid Id { get; set; }

    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal CapitalPortion { get; set; }
    public decimal InterestPortion { get; set; }
    public decimal RemainingBalance { get; set; }
    public LoanInstallmentStatus Status { get; set; }
    public bool IsOverdue { get; set; }
}
