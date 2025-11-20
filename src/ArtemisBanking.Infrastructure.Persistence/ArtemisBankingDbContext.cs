using ArtemisBanking.Core.Domain.Entities;
using ArtemisBanking.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ArtemisBanking.Infrastructure.Persistence;

public class ArtemisBankingDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ArtemisBankingDbContext(DbContextOptions<ArtemisBankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<SavingsAccountTransaction> SavingsAccountTransactions => Set<SavingsAccountTransaction>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<CreditCard> CreditCards { get; set; } = default!;
    public DbSet<CreditCardTransaction> CreditCardTransactions { get; set; } = default!;


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ArtemisBankingDbContext).Assembly);
    }
}

