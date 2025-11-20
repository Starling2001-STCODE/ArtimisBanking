using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Identity;
using ArtemisBanking.Application.Identity.Dtos;
using ArtemisBanking.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBanking.Infrastructure.Identity.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> SendPasswordResetEmailAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure("No existe un usuario con ese correo.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return Result.Failure("Las contraseñas no coinciden.");

        var user = await _userManager.FindByIdAsync(request.UserId);

        if (user is null)
            return Result.Failure("Usuario no encontrado.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(error);
        }

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return Result.Failure("Usuario no encontrado.");

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(error);
        }

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}