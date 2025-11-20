using ArtemisBanking.Core.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBanking.Infrastructure.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}