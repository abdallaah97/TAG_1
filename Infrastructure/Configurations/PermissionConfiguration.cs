using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");

            builder.HasKey(p => p.Id);

            // The ids come from the permission catalog in the application layer, they are never generated.
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(250);
            builder.Property(p => p.Group).IsRequired().HasMaxLength(100);

            builder.HasIndex(p => p.Name).IsUnique();
        }
    }
}
