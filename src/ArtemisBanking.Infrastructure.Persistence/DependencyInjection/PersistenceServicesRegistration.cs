using ArtemisBanking.Application.CreditCards.Interfaces;
using ArtemisBanking.Application.Loans;
using ArtemisBanking.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBanking.Infrastructure.Persistence.DependencyInjection;

public static class PersistenceServicesRegistration
{
    public static IServiceCollection AddPersistenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=artemisbanking.db";

        services.AddDbContext<ArtemisBankingDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<ICreditCardService, CreditCardService>();


        return services;
    }
}
