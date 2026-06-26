using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users").HasKey(to => to.Id);

        builder.Property(to => to.Id).HasColumnName("Id").IsRequired();
        builder.Property(to => to.Username).HasColumnName("Username").IsRequired();
        builder.Property(to => to.Email).HasColumnName("Email").IsRequired();
        builder.Property(to => to.PhoneNumber).HasColumnName("PhoneNumber");
        builder.HasIndex(to => to.PhoneNumber).IsUnique();
        builder.Property(to => to.AppleUserId).HasColumnName("AppleUserId");
        builder.HasIndex(to => to.AppleUserId).IsUnique();
        builder.Property(to => to.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(to => to.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(to => to.DeletedDate).HasColumnName("DeletedDate");
        builder.HasQueryFilter(to => !to.DeletedDate.HasValue);
        builder.HasMany(to => to.UserOperationClaims);

        builder.HasBaseType((string)null!);
    }
}
