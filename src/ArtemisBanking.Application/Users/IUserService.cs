using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Users.Dtos;
using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(
        int pageIndex,
        int pageSize,
        UserRole? roleFilter,
        CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<Result> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
}