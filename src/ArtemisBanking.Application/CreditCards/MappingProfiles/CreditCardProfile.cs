using ArtemisBanking.Application.CreditCards.Dtos;
using ArtemisBanking.Core.Domain.Entities;
using AutoMapper;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ArtemisBanking.Application.CreditCards.MappingProfiles;

public class CreditCardProfile : Profile
{
    public CreditCardProfile()
    {
        CreateMap<CreditCard, CreditCardListItemDto>()
            .ForMember(d => d.CardNumberMasked, opt => opt.MapFrom(src => MaskCardNumber(src.CardNumber)))
            .ForMember(d => d.ClientFullName, opt => opt.Ignore())
            .ForMember(d => d.NationalId, opt => opt.Ignore());

        CreateMap<CreditCard, CreditCardDetailDto>()
            .ForMember(d => d.CardNumberMasked, opt => opt.MapFrom(src => MaskCardNumber(src.CardNumber)))
            .ForMember(d => d.ClientFullName, opt => opt.Ignore())
            .ForMember(d => d.NationalId, opt => opt.Ignore());

        CreateMap<CreditCardTransaction, CreditCardTransactionDto>();
    }

    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
            return "****";

        var last4 = cardNumber.Substring(cardNumber.Length - 4);
        return $"**** **** **** {last4}";
    }
}
