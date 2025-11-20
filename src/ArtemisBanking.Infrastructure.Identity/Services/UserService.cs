using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Users;
using ArtemisBanking.Application.Users.Dtos;
using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Entities;
using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ArtemisBankingDbContext _dbContext;

        public UserService(
            UserManager<ApplicationUser> userManager,
            ArtemisBankingDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(
            int pageIndex,
            int pageSize,
            UserRole? roleFilter,
            CancellationToken cancellationToken = default)
        {
            if (pageIndex <= 0) pageIndex = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _userManager.Users.AsQueryable();

            if (roleFilter.HasValue)
            {
                query = query.Where(u => u.Role == roleFilter.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToList();

            var principalAccounts = await _dbContext.SavingsAccounts
                .Where(a => userIds.Contains(a.UserId) && a.AccountType == AccountType.Principal)
                .ToListAsync(cancellationToken);

            var dtoList = users.Select(u =>
            {
                var account = principalAccounts.FirstOrDefault(a => a.UserId == u.Id);
                var balance = account?.Balance ?? 0m;

                return MapToUserDto(u, balance);
            }).ToList();

            return new PagedResult<UserDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return Result<UserDto>.Failure("Usuario no encontrado.");
            }

            var account = await _dbContext.SavingsAccounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AccountType == AccountType.Principal, cancellationToken);

            var dto = MapToUserDto(user, account?.Balance ?? 0m);

            return Result<UserDto>.Success(dto);
        }

        public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
            {
                return Result<UserDto>.Failure("Las contraseñas no coinciden.");
            }

            var existingByUserName = await _userManager.FindByNameAsync(dto.Username);
            if (existingByUserName != null)
            {
                return Result<UserDto>.Failure("El nombre de usuario ya está en uso.");
            }

            var existingByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingByEmail != null)
            {
                return Result<UserDto>.Failure("El correo electrónico ya está en uso.");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                NationalId = dto.NationalId,
                Role = dto.Role,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure($"Error al crear el usuario: {errors}");
            }

            var roleName = dto.Role.ToString();
            var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure($"Usuario creado, pero error al asignar el rol: {errors}");
            }

            if (dto.Role == UserRole.Client)
            {
                var accountNumber = GenerateAccountNumber(AccountType.Principal);
                var initialAmount = dto.InitialAmount ?? 0m;

                var account = SavingsAccount.CreatePrincipal(user.Id, accountNumber, initialAmount);

                _dbContext.SavingsAccounts.Add(account);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            decimal? balance = null;
            if (dto.Role == UserRole.Client)
            {
                balance = await _dbContext.SavingsAccounts
                    .Where(a => a.UserId == user.Id && a.AccountType == AccountType.Principal)
                    .Select(a => (decimal?)a.Balance)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var resultDto = MapToUserDto(user, balance ?? 0m);
            return Result<UserDto>.Success(resultDto);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return Result.Failure("Usuario no encontrado.");
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.NationalId = dto.NationalId;

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingByEmail = await _userManager.FindByEmailAsync(dto.Email);
                if (existingByEmail != null && existingByEmail.Id != user.Id)
                {
                    return Result.Failure("El correo electrónico ya está en uso por otro usuario.");
                }

                user.Email = dto.Email;
            }

            if (!string.Equals(user.UserName, dto.Username, StringComparison.Ordinal))
            {
                var existingByUserName = await _userManager.FindByNameAsync(dto.Username);
                if (existingByUserName != null && existingByUserName.Id != user.Id)
                {
                    return Result.Failure("El nombre de usuario ya está en uso por otro usuario.");
                }

                user.UserName = dto.Username;
            }

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (!string.Equals(dto.NewPassword, dto.ConfirmNewPassword, StringComparison.Ordinal))
                {
                    return Result.Failure("Las contraseñas nuevas no coinciden.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

                if (!passResult.Succeeded)
                {
                    var errors = string.Join(", ", passResult.Errors.Select(e => e.Description));
                    return Result.Failure($"Error al cambiar la contraseña: {errors}");
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return Result.Failure($"Error al actualizar el usuario: {errors}");
            }

            if (user.Role == UserRole.Client)
            {
                var account = await _dbContext.SavingsAccounts
                    .Include(a => a.Transactions)
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AccountType == AccountType.Principal, cancellationToken);

                if (account == null)
                {
                    var accountNumber = GenerateAccountNumber(AccountType.Principal);
                    account = SavingsAccount.CreatePrincipal(user.Id, accountNumber, 0m);
                    _dbContext.SavingsAccounts.Add(account);
                }

                if (dto.AdditionalAmount.HasValue && dto.AdditionalAmount.Value > 0)
                {
                    account.Credit(
                        dto.AdditionalAmount.Value,
                        "Depósito adicional desde actualización de usuario",
                        TransactionOrigin.Cajero
                    );
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }

        public async Task<Result> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return Result.Failure("Usuario no encontrado.");
            }

            user.IsActive = !user.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return Result.Failure($"Error al actualizar el estado del usuario: {errors}");
            }

            return Result.Success();
        }

        private static UserDto MapToUserDto(ApplicationUser user, decimal principalBalance)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalId = user.NationalId,
                Role = user.Role,
                IsActive = user.IsActive,
                PrincipalAccountBalance = principalBalance
            };
        }

        private static string GenerateAccountNumber(AccountType accountType)
        {
            string accountTypeCode = ((int)accountType).ToString("00");
            string year = DateTime.UtcNow.Year.ToString();

            var random = new Random();
            string sequence = random.Next(0, 9999).ToString("D4");

            string raw = $"{accountTypeCode}{year}{sequence}";

            int checksum = raw.Sum(c => c - '0') % 9;

            return $"{accountTypeCode}-{year}-{sequence}-{checksum}";
        }
    }
}
