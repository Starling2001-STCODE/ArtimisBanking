using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Identity;
using ArtemisBanking.Application.Identity.Dtos;
using ArtemisBanking.Infrastructure.Identity.Jwt;
using ArtemisBanking.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBanking.Infrastructure.Identity.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(request.UsernameOrEmail)
                   ?? await _userManager.FindByEmailAsync(request.UsernameOrEmail);

        if (user is null)
            return Result<LoginResponseDto>.Failure("Usuario o contraseña incorrectos.");

        if (!user.IsActive)
            return Result<LoginResponseDto>.Failure("El usuario está inactivo. Debe activar su cuenta.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
            return Result<LoginResponseDto>.Failure("Usuario o contraseña incorrectos.");

        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenGenerator.GenerateToken(user, roles, out var expiresAt);

        var dto = new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.UserName ?? "",
            Role = roles.FirstOrDefault() ?? ""
        };

        return Result<LoginResponseDto>.Success(dto);
    }
}