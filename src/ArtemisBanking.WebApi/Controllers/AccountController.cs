using ArtemisBanking.Application.Identity;
using ArtemisBanking.Application.Identity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBanking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAccountService _accountService;

    public AccountController(IAuthService authService, IAccountService accountService)
    {
        _authService = authService;
        _accountService = accountService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Succeeded)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var result = await _accountService.SendPasswordResetEmailAsync(request);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var result = await _accountService.ResetPasswordAsync(request);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        return Ok();
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request)
    {
        var result = await _accountService.ConfirmEmailAsync(request);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        return Ok();
    }
}