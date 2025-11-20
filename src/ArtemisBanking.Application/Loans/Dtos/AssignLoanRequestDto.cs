using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.Application.Loans.Dtos;

public class AssignLoanRequestDto
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    [Range(1, 999999999, ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    [Required]
    [Range(6, 60, ErrorMessage = "El plazo debe estar entre 6 y 60 meses.")]
    public int TermInMonths { get; set; }

    [Required]
    [Range(0.01, 100, ErrorMessage = "La tasa anual debe ser mayor que cero.")]
    public decimal AnnualInterestRate { get; set; }
}
