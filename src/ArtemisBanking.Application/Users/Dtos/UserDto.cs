using ArtemisBanking.Core.Domain.Enums;

namespace ArtemisBanking.Application.Users.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public decimal? PrincipalAccountBalance { get; set; }
}