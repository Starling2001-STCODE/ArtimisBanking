using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArtemisBanking.Infrastructure.Persistence
{
    public class ArtemisBankingDbContextFactory : IDesignTimeDbContextFactory<ArtemisBankingDbContext>
    {
        public ArtemisBankingDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ArtemisBankingDbContext>();

            var connectionString = "Data Source=artemisbanking.db";

            optionsBuilder.UseSqlite(connectionString);

            return new ArtemisBankingDbContext(optionsBuilder.Options);
        }
    }
}
