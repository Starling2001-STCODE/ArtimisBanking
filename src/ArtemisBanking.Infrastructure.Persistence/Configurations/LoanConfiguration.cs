using ArtemisBanking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBanking.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LoanNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(l => l.Capital)
            .HasColumnType("decimal(18,2)");

        builder.Property(l => l.AnnualInterestRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(l => l.TermInMonths)
            .IsRequired();

        builder.Property(l => l.Status)
            .IsRequired();

        builder.Property(l => l.IsHighRisk)
            .IsRequired();

        builder.HasMany(l => l.Installments)
            .WithOne(i => i.Loan)
            .HasForeignKey(i => i.LoanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
