using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Identity.Dtos;

namespace ArtemisBanking.Application.Identity;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}