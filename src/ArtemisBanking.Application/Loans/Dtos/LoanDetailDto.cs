using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Loans.Dtos;

public class LoanDetailDto
{
    public Guid Id { get; set; }
    public string LoanNumber { get; set; } = default!;
    public Guid UserId { get; set; }

    public string ClientFullName { get; set; } = default!;

    public string NationalId { get; set; } = default!;

    public decimal Capital { get; set; }

    public decimal AnnualInterestRate { get; set; }

    public int TermInMonths { get; set; }

    public LoanStatus Status { get; set; }

    public bool IsHighRisk { get; set; }

    public List<LoanInstallmentDto> Installments { get; set; } = new();
}
