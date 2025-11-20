using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.CreditCards.Dtos;

namespace ArtemisBanking.Application.CreditCards.Interfaces;

public interface ICreditCardService
{
    Task<PagedResult<CreditCardListItemDto>> GetActiveCardsAsync(
        int pageIndex,
        int pageSize);

    Task<PagedResult<CreditCardListItemDto>> SearchCardsByNationalIdAsync(
        string nationalId,
        int pageIndex,
        int pageSize,
        string? statusFilter);

    Task<Result<List<AssignableCreditCardClientDto>>> GetAssignableClientsAsync();

    Task<Result<CreditCardDetailDto>> AssignCardAsync(
        AssignCreditCardRequestDto dto,
        Guid adminUserId);

    Task<Result<CreditCardDetailDto>> GetCardDetailsAsync(Guid cardId);

    Task<Result> UpdateLimitAsync(Guid cardId, decimal newLimit);

    Task<Result> CancelCardAsync(Guid cardId);
}
