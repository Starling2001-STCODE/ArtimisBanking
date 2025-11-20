using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Loans.Dtos;

public class LoanListItemDto
{
    public Guid Id { get; set; }
    public string LoanNumber { get; set; } = default!;
    public string ClientFullName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public decimal Capital { get; set; }
    public int TermInMonths { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public decimal PendingAmount { get; set; }
    public LoanStatus Status { get; set; }
    public bool IsInArrears { get; set; }
}
