using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("DeviceTokens").HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("Id").IsRequired();
        builder.Property(d => d.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(d => d.Token).HasColumnName("Token").IsRequired();
        builder.Property(d => d.Platform).HasColumnName("Platform").IsRequired();
        builder.Property(d => d.IsProduction).HasColumnName("IsProduction").IsRequired();
        builder.Property(d => d.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(d => d.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(d => d.DeletedDate).HasColumnName("DeletedDate");

        builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.Token).IsUnique();
        builder.HasIndex(d => d.UserId);

        builder.HasQueryFilter(d => !d.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
