using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Loans;
using ArtemisBanking.Application.Loans.Dtos;
using ArtemisBanking.Core.Domain.Entities;
using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Persistence.Services;

public class LoanService : ILoanService
{
    private readonly ArtemisBankingDbContext _dbContext;

    public LoanService(ArtemisBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<LoanListItemDto>> GetActiveLoansAsync(
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageIndex <= 0) pageIndex = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbContext.Loans
            .AsNoTracking()
            .Where(l => l.Status == LoanStatus.Activo)
            .OrderByDescending(l => l.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var loans = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Include(l => l.Installments)
            .ToListAsync(cancellationToken);

        var userIds = loans.Select(l => l.UserId).Distinct().ToList();

        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var items = loans.Select(loan =>
        {
            var user = users.First(u => u.Id == loan.UserId);
            return MapToLoanListItemDto(loan, user);
        }).ToList();

        return new PagedResult<LoanListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<LoanListItemDto>> SearchLoansByNationalIdAsync(
        string nationalId,
        LoanStatus? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageIndex <= 0) pageIndex = 1;
        if (pageSize <= 0) pageSize = 20;

        var userQuery = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            userQuery = userQuery.Where(u => u.NationalId.Contains(nationalId));
        }

        var matchingUsers = await userQuery.ToListAsync(cancellationToken);
        var userIds = matchingUsers.Select(u => u.Id).ToList();

        if (!userIds.Any())
        {
            return new PagedResult<LoanListItemDto>
            {
                Items = new List<LoanListItemDto>(),
                TotalCount = 0,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        var loanQuery = _dbContext.Loans
            .AsNoTracking()
            .Where(l => userIds.Contains(l.UserId));

        if (status.HasValue)
        {
            loanQuery = loanQuery.Where(l => l.Status == status.Value);
        }
        else
        {
            loanQuery = loanQuery.Where(l =>
                l.Status == LoanStatus.Activo || l.Status == LoanStatus.Completado);
        }

        loanQuery = loanQuery
            .OrderByDescending(l => l.Status == LoanStatus.Activo)
            .ThenByDescending(l => l.Id);

        var totalCount2 = await loanQuery.CountAsync(cancellationToken);

        var loans = await loanQuery
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Include(l => l.Installments)
            .ToListAsync(cancellationToken);

        var items = loans.Select(loan =>
        {
            var user = matchingUsers.First(u => u.Id == loan.UserId);
            return MapToLoanListItemDto(loan, user);
        }).ToList();

        return new PagedResult<LoanListItemDto>
        {
            Items = items,
            TotalCount = totalCount2,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<Result<List<AssignableClientDto>>> GetAssignableClientsAsync(
        string? nationalId,
        CancellationToken cancellationToken = default)
    {
        var activeLoans = _dbContext.Loans
            .AsNoTracking()
            .Where(l => l.Status == LoanStatus.Activo);

        var systemLoans = await activeLoans.ToListAsync(cancellationToken);

        decimal systemAverageDebt = 0m;
        if (systemLoans.Any())
        {
            systemAverageDebt = systemLoans.Average(l => l.Capital);
        }

        var clientsQuery = _dbContext.Users
            .Where(u => u.Role == UserRole.Client && u.IsActive);

        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            clientsQuery = clientsQuery.Where(u => u.NationalId.Contains(nationalId));
        }

        var clients = await clientsQuery.ToListAsync(cancellationToken);
        var clientIds = clients.Select(c => c.Id).ToList();

        var activeLoansByClient = await _dbContext.Loans
            .Where(l => clientIds.Contains(l.UserId) && l.Status == LoanStatus.Activo)
            .ToListAsync(cancellationToken);

        var resultList = new List<AssignableClientDto>();

        foreach (var client in clients)
        {
            var clientLoans = activeLoansByClient.Where(l => l.UserId == client.Id).ToList();
            var currentDebt = clientLoans.Sum(l => l.Capital);
            var hasActiveLoan = clientLoans.Any();

            if (hasActiveLoan)
            {
                continue;
            }

            resultList.Add(new AssignableClientDto
            {
                ClientId = client.Id,
                FullName = $"{client.FirstName} {client.LastName}",
                NationalId = client.NationalId,
                CurrentDebt = currentDebt,
                SystemAverageDebt = systemAverageDebt,
                HasActiveLoan = hasActiveLoan
            });
        }

        return Result<List<AssignableClientDto>>.Success(resultList);
    }

    public async Task<Result<LoanDetailDto>> AssignLoanAsync(
     AssignLoanRequestDto dto,
     Guid adminUserId,
     CancellationToken cancellationToken = default)
    {
        if (dto.TermInMonths % 6 != 0)
        {
            return Result<LoanDetailDto>.Failure("El plazo debe ser múltiplo de 6 meses.");
        }

        if (dto.Amount <= 0)
        {
            return Result<LoanDetailDto>.Failure("El monto debe ser mayor que cero.");
        }

        if (dto.AnnualInterestRate <= 0)
        {
            return Result<LoanDetailDto>.Failure("La tasa anual debe ser mayor que cero.");
        }

        var client = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == dto.ClientId, cancellationToken);

        if (client == null || client.Role != UserRole.Client || !client.IsActive)
        {
            return Result<LoanDetailDto>.Failure("Cliente no válido o inactivo.");
        }

        var hasActiveLoan = await _dbContext.Loans
            .AnyAsync(l => l.UserId == dto.ClientId && l.Status == LoanStatus.Activo, cancellationToken);

        if (hasActiveLoan)
        {
            return Result<LoanDetailDto>.Failure("El cliente ya tiene un préstamo activo.");
        }

        var activeLoans = await _dbContext.Loans
            .Where(l => l.Status == LoanStatus.Activo)
            .ToListAsync(cancellationToken);

        var currentDebt = activeLoans
            .Where(l => l.UserId == dto.ClientId)
            .Sum(l => l.Capital);

        decimal systemAverageDebt = 0m;
        if (activeLoans.Any())
        {
            systemAverageDebt = activeLoans.Average(l => l.Capital);
        }

        var newTotalDebt = currentDebt + dto.Amount;
        var isHighRisk = systemAverageDebt > 0 && newTotalDebt > systemAverageDebt;

        var loanNumber = GenerateLoanNumber();

        var loan = Loan.Create(
            dto.ClientId,
            loanNumber,
            dto.Amount,
            dto.AnnualInterestRate,
            dto.TermInMonths,
            isHighRisk);

        GenerateFrenchSchedule(loan);

        var account = await _dbContext.SavingsAccounts
            .FirstOrDefaultAsync(
                a => a.UserId == dto.ClientId && a.AccountType == AccountType.Principal,
                cancellationToken);

        if (account == null)
        {
            return Result<LoanDetailDto>.Failure("El cliente no tiene una cuenta de ahorro principal.");
        }

        // Ajusta el balance y agrega la transacción en la entidad
        account.Credit(
            dto.Amount,
            $"Préstamo aprobado {loan.LoanNumber}",
            TransactionOrigin.Prestamo);

        _dbContext.Loans.Add(loan);

        foreach (var txEntry in _dbContext.ChangeTracker.Entries<SavingsAccountTransaction>())
        {
            if (txEntry.State == EntityState.Modified)
            {
                txEntry.State = EntityState.Unchanged;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var detail = await BuildLoanDetailDtoAsync(loan.Id, cancellationToken);

        if (detail is null)
        {
            return Result<LoanDetailDto>.Failure("Préstamo no encontrado después de crearlo.");
        }

        return Result<LoanDetailDto>.Success(detail);
    }

    public async Task<Result<LoanDetailDto>> GetLoanDetailsAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var dto = await BuildLoanDetailDtoAsync(loanId, cancellationToken);
        if (dto == null)
        {
            return Result<LoanDetailDto>.Failure("Préstamo no encontrado.");
        }

        return Result<LoanDetailDto>.Success(dto);
    }

    public async Task<Result<LoanDetailDto>> UpdateAnnualRateAsync(
        Guid loanId,
        decimal newAnnualRate,
        CancellationToken cancellationToken = default)
    {
        if (newAnnualRate <= 0)
        {
            return Result<LoanDetailDto>.Failure("La nueva tasa anual debe ser mayor que cero.");
        }

        var loan = await _dbContext.Loans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan == null)
        {
            return Result<LoanDetailDto>.Failure("Préstamo no encontrado.");
        }

        if (loan.Status != LoanStatus.Activo)
        {
            return Result<LoanDetailDto>.Failure("Solo se pueden modificar préstamos activos.");
        }

        var paidCapital = loan.Installments
            .Where(i => i.Status == LoanInstallmentStatus.Paid)
            .Sum(i => i.CapitalPortion);

        var remainingCapital = loan.Capital - paidCapital;

        var remainingInstallments = loan.Installments
            .Where(i => i.Status != LoanInstallmentStatus.Paid)
            .OrderBy(i => i.InstallmentNumber)
            .ToList();

        if (!remainingInstallments.Any())
        {
            return Result<LoanDetailDto>.Failure("No hay cuotas pendientes para recalcular.");
        }

        var paidInstallments = loan.Installments
            .Where(i => i.Status == LoanInstallmentStatus.Paid)
            .OrderBy(i => i.InstallmentNumber)
            .ToList();

        var firstDueDate = remainingInstallments.First().DueDate;

        loan.UpdateAnnualInterestRate(newAnnualRate);

        loan.Installments.Clear();
        foreach (var paid in paidInstallments)
        {
            loan.Installments.Add(paid);
        }

        RecalculateFrenchScheduleFrom(
            loan,
            remainingCapital,
            firstDueDate,
            remainingInstallments.Count);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var detail = await BuildLoanDetailDtoAsync(loan.Id, cancellationToken);
        if (detail is null)
        {
            return Result<LoanDetailDto>.Failure("Préstamo no encontrado después de actualizar la tasa.");
        }

        return Result<LoanDetailDto>.Success(detail);
    }

    public async Task<Result> MarkOverdueInstallmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var installments = await _dbContext.LoanInstallments
            .Include(i => i.Loan)
            .Where(i =>
                i.Status == LoanInstallmentStatus.Pending &&
                i.DueDate < today)
            .ToListAsync(cancellationToken);

        if (!installments.Any())
        {
            return Result.Success();
        }

        var affectedLoans = new HashSet<Guid>();

        foreach (var installment in installments)
        {
            installment.MarkAsOverdue();
            affectedLoans.Add(installment.LoanId);
        }

        var loans = await _dbContext.Loans
            .Where(l => affectedLoans.Contains(l.Id))
            .ToListAsync(cancellationToken);

        foreach (var loan in loans)
        {
            loan.ChangeStatus(LoanStatus.Moroso);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<LoanDetailDto?> BuildLoanDetailDtoAsync(
        Guid loanId,
        CancellationToken cancellationToken)
    {
        var loan = await _dbContext.Loans
            .AsNoTracking()
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan == null)
        {
            return null;
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == loan.UserId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var dto = new LoanDetailDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            UserId = loan.UserId,
            ClientFullName = $"{user.FirstName} {user.LastName}",
            NationalId = user.NationalId,
            Capital = loan.Capital,
            AnnualInterestRate = loan.AnnualInterestRate,
            TermInMonths = loan.TermInMonths,
            Status = loan.Status,
            IsHighRisk = loan.IsHighRisk,
            Installments = loan.Installments
                .OrderBy(i => i.InstallmentNumber)
                .Select(MapToLoanInstallmentDto)
                .ToList()
        };

        return dto;
    }

    private LoanListItemDto MapToLoanListItemDto(Loan loan, ApplicationUser user)
    {
        var totalInstallments = loan.Installments.Count;
        var paidInstallments = loan.Installments.Count(i => i.Status == LoanInstallmentStatus.Paid);

        var paidCapital = loan.Installments
            .Where(i => i.Status == LoanInstallmentStatus.Paid)
            .Sum(i => i.CapitalPortion);

        var pendingAmount = loan.Capital - paidCapital;

        var isInArrears = loan.Installments.Any(i => i.Status == LoanInstallmentStatus.Overdue);

        return new LoanListItemDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            ClientFullName = $"{user.FirstName} {user.LastName}",
            NationalId = user.NationalId,
            Capital = loan.Capital,
            TermInMonths = loan.TermInMonths,
            AnnualInterestRate = loan.AnnualInterestRate,
            TotalInstallments = totalInstallments,
            PaidInstallments = paidInstallments,
            PendingAmount = pendingAmount,
            Status = loan.Status,
            IsInArrears = isInArrears
        };
    }

    private LoanInstallmentDto MapToLoanInstallmentDto(LoanInstallment installment)
    {
        return new LoanInstallmentDto
        {
            Id = installment.Id,
            InstallmentNumber = installment.InstallmentNumber,
            DueDate = installment.DueDate,
            MonthlyPayment = installment.MonthlyPayment,
            CapitalPortion = installment.CapitalPortion,
            InterestPortion = installment.InterestPortion,
            RemainingBalance = installment.RemainingBalance,
            Status = installment.Status,
            IsOverdue = installment.Status == LoanInstallmentStatus.Overdue
        };
    }

    private void GenerateFrenchSchedule(Loan loan)
    {
        var capital = loan.Capital;
        var n = loan.TermInMonths;
        var monthlyRate = loan.AnnualInterestRate / 12m / 100m;

        var r = (double)monthlyRate;
        var p = (double)capital;
        var payment = (decimal)(p * r / (1 - Math.Pow(1 + r, -n)));

        var remaining = capital;
        var firstDueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddMonths(1);

        loan.Installments.Clear();

        for (int i = 1; i <= n; i++)
        {
            var interest = remaining * monthlyRate;
            var capitalPortion = payment - interest;

            if (i == n)
            {
                capitalPortion = remaining;
                payment = capitalPortion + interest;
            }

            remaining -= capitalPortion;

            var installment = LoanInstallment.Create(
                loan.Id,
                i,
                firstDueDate.AddMonths(i - 1),
                decimal.Round(payment, 2),
                decimal.Round(capitalPortion, 2),
                decimal.Round(interest, 2),
                decimal.Round(remaining, 2));

            loan.Installments.Add(installment);
        }
    }

    private void RecalculateFrenchScheduleFrom(
        Loan loan,
        decimal remainingCapital,
        DateOnly firstDueDate,
        int remainingPeriods)
    {
        var monthlyRate = loan.AnnualInterestRate / 12m / 100m;

        var r = (double)monthlyRate;
        var p = (double)remainingCapital;
        var n = remainingPeriods;
        var payment = (decimal)(p * r / (1 - Math.Pow(1 + r, -n)));

        var remaining = remainingCapital;

        for (int i = 0; i < remainingPeriods; i++)
        {
            var installmentNumber = loan.Installments.Count + 1;

            var interest = remaining * monthlyRate;
            var capitalPortion = payment - interest;

            if (i == remainingPeriods - 1)
            {
                capitalPortion = remaining;
                payment = capitalPortion + interest;
            }

            remaining -= capitalPortion;

            var installment = LoanInstallment.Create(
                loan.Id,
                installmentNumber,
                firstDueDate.AddMonths(i),
                decimal.Round(payment, 2),
                decimal.Round(capitalPortion, 2),
                decimal.Round(interest, 2),
                decimal.Round(remaining, 2));

            loan.Installments.Add(installment);
        }
    }

    private string GenerateLoanNumber()     
    {
        var year = DateTime.UtcNow.Year.ToString();
        var random = new Random();
        var sequence = random.Next(0, 999999).ToString("D6");
        return $"{year}-{sequence}";
    }
}
