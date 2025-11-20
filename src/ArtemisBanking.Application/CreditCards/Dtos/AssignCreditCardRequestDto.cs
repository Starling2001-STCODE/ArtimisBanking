using System;

namespace ArtemisBanking.Application.CreditCards.Dtos;

public class AssignCreditCardRequestDto
{
    public Guid ClientId { get; set; }
    public decimal InitialLimit { get; set; }
}
