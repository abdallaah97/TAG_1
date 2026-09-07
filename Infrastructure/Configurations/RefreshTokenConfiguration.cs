using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(200);
            builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(200);
            builder.Property(t => t.RevokedReason).HasMaxLength(200);
            builder.Property(t => t.CreatedByIp).HasMaxLength(60);
            builder.Property(t => t.RevokedByIp).HasMaxLength(60);

            builder.Ignore(t => t.IsExpired);
            builder.Ignore(t => t.IsRevoked);
            builder.Ignore(t => t.IsActive);

            // Every refresh hits this index, and a deleted user must not leave tokens behind.
            builder.HasIndex(t => t.TokenHash).IsUnique();
            builder.HasIndex(t => t.UserId);

            builder.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
