using System;
using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.CreditCards.Dtos;

public class CreditCardTransactionDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = default!;
    public CreditCardTransactionType TransactionType { get; set; }
    public CreditCardTransactionStatus Status { get; set; }
}
