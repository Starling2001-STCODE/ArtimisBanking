namespace ArtemisBanking.Application.Identity.Dtos;

public class ConfirmEmailRequestDto
{
    public string UserId { get; set; } = default!;
    public string Token { get; set; } = default!;
}