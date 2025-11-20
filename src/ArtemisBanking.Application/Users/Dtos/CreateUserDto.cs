using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Users.Dtos;

public class CreateUserDto
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public UserRole Role { get; set; }
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public decimal? InitialAmount { get; set; }
}