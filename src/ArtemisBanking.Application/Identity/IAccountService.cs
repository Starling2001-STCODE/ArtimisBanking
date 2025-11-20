using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Identity.Dtos;

namespace ArtemisBanking.Application.Identity;

public interface IAccountService
{
    Task<Result> SendPasswordResetEmailAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken cancellationToken = default);
}