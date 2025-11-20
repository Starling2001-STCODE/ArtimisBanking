using ArtemisBanking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBanking.Infrastructure.Persistence.Configurations;

public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
{
    public void Configure(EntityTypeBuilder<LoanInstallment> builder)
    {
        builder.ToTable("LoanInstallments");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.InstallmentNumber)
            .IsRequired();

        builder.Property(li => li.DueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(li => li.MonthlyPayment)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.CapitalPortion)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.InterestPortion)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.RemainingBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.Status)
            .IsRequired();
    }
}
