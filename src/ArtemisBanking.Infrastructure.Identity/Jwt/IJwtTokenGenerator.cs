using ArtemisBanking.Infrastructure.Persistence.Models;

namespace ArtemisBanking.Infrastructure.Identity.Jwt;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user, IList<string> roles, out DateTime expiresAt);
}