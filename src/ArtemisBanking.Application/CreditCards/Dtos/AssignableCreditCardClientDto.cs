using System;

namespace ArtemisBanking.Application.CreditCards.Dtos;

public class AssignableCreditCardClientDto
{
    public Guid ClientId { get; set; }
    public string FullName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public decimal SystemAverageDebt { get; set; }
}
