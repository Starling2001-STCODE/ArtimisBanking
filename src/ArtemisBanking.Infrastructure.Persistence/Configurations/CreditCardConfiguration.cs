using ArtemisBanking.Core.Domain.Entities;
using ArtemisBanking.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBanking.Infrastructure.Persistence.Configurations;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("CreditCards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CardNumber)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.CardNumber)
            .IsUnique();

        builder.Property(x => x.CvcHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.CreditLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CurrentDebt)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.CreditCard)
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
