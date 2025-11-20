using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.CreditCards.Dtos;
using ArtemisBanking.Application.CreditCards.Interfaces;
using ArtemisBanking.Core.Domain.Entities;
using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Infrastructure.Shared.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Persistence.Services
{
    public class CreditCardService : ICreditCardService
    {
        private readonly ArtemisBankingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICryptoService _cryptoService;
        private readonly INotificationService _notificationService;
        private readonly Random _random = new();

        public CreditCardService(
            ArtemisBankingDbContext context,
            IMapper mapper,
            ICryptoService cryptoService,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _cryptoService = cryptoService;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<CreditCardListItemDto>> GetActiveCardsAsync(
            int pageIndex,
            int pageSize)
        {
            if (pageIndex <= 0) pageIndex = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.CreditCards
                .Where(c => c.Status == CreditCardStatus.Active)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();

            var cards = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = cards.Select(c => c.UserId).Distinct().ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var usersDict = users.ToDictionary(u => u.Id);

            var items = new List<CreditCardListItemDto>();

            foreach (var card in cards)
            {
                var dto = _mapper.Map<CreditCardListItemDto>(card);

                if (usersDict.TryGetValue(card.UserId, out var user))
                {
                    dto.ClientFullName = $"{user.FirstName} {user.LastName}";
                    dto.NationalId = user.NationalId;
                }

                items.Add(dto);
            }

            return new PagedResult<CreditCardListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<CreditCardListItemDto>> SearchCardsByNationalIdAsync(
            string nationalId,
            int pageIndex,
            int pageSize,
            string? statusFilter)
        {
            if (pageIndex <= 0) pageIndex = 1;
            if (pageSize <= 0) pageSize = 20;

            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                usersQuery = usersQuery.Where(u => u.NationalId == nationalId);
            }

            var userIds = await usersQuery
                .Select(u => u.Id)
                .ToListAsync();

            var cardsQuery = _context.CreditCards
                .Where(c => userIds.Contains(c.UserId));

            if (!string.IsNullOrWhiteSpace(statusFilter) &&
                Enum.TryParse<CreditCardStatus>(statusFilter, true, out var parsedStatus))
            {
                cardsQuery = cardsQuery.Where(c => c.Status == parsedStatus);
            }

            cardsQuery = cardsQuery
                .OrderBy(c => c.Status == CreditCardStatus.Active ? 0 : 1)
                .ThenByDescending(c => c.CreatedAt);

            var totalCount = await cardsQuery.CountAsync();

            var cards = await cardsQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var usersDict = users.ToDictionary(u => u.Id);

            var items = new List<CreditCardListItemDto>();

            foreach (var card in cards)
            {
                var dto = _mapper.Map<CreditCardListItemDto>(card);

                if (usersDict.TryGetValue(card.UserId, out var user))
                {
                    dto.ClientFullName = $"{user.FirstName} {user.LastName}";
                    dto.NationalId = user.NationalId;
                }

                items.Add(dto);
            }

            return new PagedResult<CreditCardListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<Result<List<AssignableCreditCardClientDto>>> GetAssignableClientsAsync()
        {
            var clients = await _context.Users
                .Where(u => u.Role == UserRole.Client && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var cardsQuery = _context.CreditCards.AsQueryable();

            decimal systemAverageDebt = 0m;
            if (await cardsQuery.AnyAsync())
            {
                systemAverageDebt = await cardsQuery.AverageAsync(c => c.CurrentDebt);
            }

            var result = clients
                .Select(c => new AssignableCreditCardClientDto
                {
                    ClientId = c.Id,
                    FullName = $"{c.FirstName} {c.LastName}",
                    NationalId = c.NationalId,
                    SystemAverageDebt = systemAverageDebt
                })
                .ToList();

            return Result<List<AssignableCreditCardClientDto>>.Success(result);
        }

        public async Task<Result<CreditCardDetailDto>> AssignCardAsync(
            AssignCreditCardRequestDto dto,
            Guid adminUserId)
        {
            if (dto.InitialLimit <= 0)
            {
                return Result<CreditCardDetailDto>.Failure("El límite inicial debe ser mayor que cero.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.ClientId);

            if (user == null || user.Role != UserRole.Client || !user.IsActive)
            {
                return Result<CreditCardDetailDto>.Failure("Cliente inválido o inactivo.");
            }

            var hasActiveCard = await _context.CreditCards
                .AnyAsync(c => c.UserId == user.Id && c.Status == CreditCardStatus.Active);

            if (hasActiveCard)
            {
                return Result<CreditCardDetailDto>.Failure("El cliente ya tiene una tarjeta activa.");
            }

            var cardNumber = await GenerateUniqueCardNumberAsync();

            var cvc = GenerateCvc();
            var cvcHash = _cryptoService.ComputeSha256(cvc);

            var now = DateTime.UtcNow;
            var expDate = now.AddYears(3);

            var card = CreditCard.AssignToClient(
                user.Id,
                adminUserId,
                cardNumber,
                dto.InitialLimit,
                expDate.Month,
                expDate.Year,
                cvcHash);

            await _context.CreditCards.AddAsync(card);
            await _context.SaveChangesAsync();

            var detail = _mapper.Map<CreditCardDetailDto>(card);
            detail.ClientFullName = $"{user.FirstName} {user.LastName}";
            detail.NationalId = user.NationalId;
            detail.Transactions = new List<CreditCardTransactionDto>();

            return Result<CreditCardDetailDto>.Success(detail);
        }

        private async Task<string> GenerateUniqueCardNumberAsync()
        {
            while (true)
            {
                var digits = new char[16];
                for (int i = 0; i < 16; i++)
                {
                    digits[i] = (char)('0' + _random.Next(0, 10));
                }

                var number = new string(digits);

                var exists = await _context.CreditCards
                    .AnyAsync(c => c.CardNumber == number);

                if (!exists)
                    return number;
            }
        }

        private string GenerateCvc()
        {
            var value = _random.Next(100, 1000);
            return value.ToString("D3");
        }

        public async Task<Result<CreditCardDetailDto>> GetCardDetailsAsync(Guid cardId)
        {
            var card = await _context.CreditCards
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
            {
                return Result<CreditCardDetailDto>.Failure("Tarjeta no encontrada.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == card.UserId);

            var dto = _mapper.Map<CreditCardDetailDto>(card);

            if (user != null)
            {
                dto.ClientFullName = $"{user.FirstName} {user.LastName}";
                dto.NationalId = user.NationalId;
            }

            dto.Transactions = card.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => _mapper.Map<CreditCardTransactionDto>(t))
                .ToList();

            return Result<CreditCardDetailDto>.Success(dto);
        }

        public async Task<Result> UpdateLimitAsync(Guid cardId, decimal newLimit)
        {
            var card = await _context.CreditCards
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
            {
                return Result.Failure("Tarjeta no encontrada.");
            }

            try
            {
                card.ChangeLimit(newLimit);
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == card.UserId);

            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var cardNumber = card.CardNumber ?? string.Empty;
                var last4 = cardNumber.Length >= 4
                    ? cardNumber.Substring(cardNumber.Length - 4)
                    : cardNumber;

                await _notificationService.SendCreditCardLimitChangedAsync(
                    user.Email,
                    last4,
                    newLimit);
            }

            return Result.Success();
        }

        public async Task<Result> CancelCardAsync(Guid cardId)
        {
            var card = await _context.CreditCards
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
            {
                return Result.Failure("Tarjeta no encontrada.");
            }

            try
            {
                card.Cancel();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }

            await _context.SaveChangesAsync();
            return Result.Success();
        }
    }
}
