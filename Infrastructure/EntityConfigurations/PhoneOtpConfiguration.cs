using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class PhoneOtpConfiguration : IEntityTypeConfiguration<PhoneOtp>
{
    public void Configure(EntityTypeBuilder<PhoneOtp> builder)
    {
        builder.ToTable("PhoneOtps").HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("Id").IsRequired();
        builder.Property(o => o.PhoneNumber).HasColumnName("PhoneNumber").IsRequired();
        builder.Property(o => o.CodeHash).HasColumnName("CodeHash").IsRequired();
        builder.Property(o => o.CodeSalt).HasColumnName("CodeSalt").IsRequired();
        builder.Property(o => o.ExpiresAt).HasColumnName("ExpiresAt").IsRequired();
        builder.Property(o => o.AttemptCount).HasColumnName("AttemptCount").IsRequired();
        builder.Property(o => o.IsUsed).HasColumnName("IsUsed").IsRequired();
        builder.Property(o => o.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(o => o.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(o => o.DeletedDate).HasColumnName("DeletedDate");

        builder.HasIndex(o => new { o.PhoneNumber, o.IsUsed });

        builder.HasQueryFilter(o => !o.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
