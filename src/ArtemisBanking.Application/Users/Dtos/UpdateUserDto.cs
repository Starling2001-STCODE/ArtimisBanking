namespace ArtemisBanking.Application.Users.Dtos;

public class UpdateUserDto
{
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
    public decimal? AdditionalAmount { get; set; }
}