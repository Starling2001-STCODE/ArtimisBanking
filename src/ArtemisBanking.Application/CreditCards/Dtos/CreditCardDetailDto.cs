using System;
using System.Collections.Generic;
using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.CreditCards.Dtos;

public class CreditCardDetailDto
{
    public Guid Id { get; set; }
    public string CardNumberMasked { get; set; } = default!;
    public string ClientFullName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public decimal CreditLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    public int ExpirationMonth { get; set; }
    public int ExpirationYear { get; set; }
    public CreditCardStatus Status { get; set; }

    public List<CreditCardTransactionDto> Transactions { get; set; } =
        new List<CreditCardTransactionDto>();
}
