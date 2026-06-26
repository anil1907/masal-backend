using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class UserOperationClaimConfiguration : IEntityTypeConfiguration<UserOperationClaim>
{
    public void Configure(EntityTypeBuilder<UserOperationClaim> builder)
    {
        builder.ToTable("UserOperationClaims").HasKey(tm => tm.Id);

        builder.Property(tm => tm.Id).HasColumnName("Id").IsRequired();
        builder.Property(tm => tm.OperationClaimId).HasColumnName("OperationClaimId").IsRequired();
        builder.Property(tm => tm.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(tm => tm.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(tm => tm.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(tm => tm.DeletedDate).HasColumnName("DeletedDate");
        builder.HasOne(u => u.OperationClaim);
        builder.HasOne(u => u.User);

        builder.HasQueryFilter(tm => !tm.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
