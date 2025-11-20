namespace ArtemisBanking.Application.Identity.Dtos;

public class LoginRequestDto
{
    public string UsernameOrEmail { get; set; } = default!;
    public string Password { get; set; } = default!;
}