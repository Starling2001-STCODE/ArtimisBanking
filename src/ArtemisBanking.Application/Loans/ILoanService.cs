using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Loans.Dtos;
using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Loans;

public interface ILoanService
{
    Task<PagedResult<LoanListItemDto>> GetActiveLoansAsync(
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<LoanListItemDto>> SearchLoansByNationalIdAsync(
        string nationalId,
        LoanStatus? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<List<AssignableClientDto>>> GetAssignableClientsAsync(
        string? nationalId,
        CancellationToken cancellationToken = default);

    Task<Result<LoanDetailDto>> AssignLoanAsync(
        AssignLoanRequestDto dto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<Result<LoanDetailDto>> GetLoanDetailsAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<Result<LoanDetailDto>> UpdateAnnualRateAsync(
        Guid loanId,
        decimal newAnnualRate,
        CancellationToken cancellationToken = default);

    Task<Result> MarkOverdueInstallmentsAsync(
        CancellationToken cancellationToken = default);
}
