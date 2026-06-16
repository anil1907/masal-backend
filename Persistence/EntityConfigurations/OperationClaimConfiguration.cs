using Application.Features;
using Application.Features.Users.Constants;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations;

public class OperationClaimConfiguration : IEntityTypeConfiguration<OperationClaim>
{
    public void Configure(EntityTypeBuilder<OperationClaim> builder)
    {
        builder.ToTable("OperationClaims").HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("Name").IsRequired();
        builder.Property(t => t.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(t => t.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(t => t.DeletedDate).HasColumnName("DeletedDate");
        builder.HasQueryFilter(t => !t.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
        builder.HasData(_seeds);
    }

    private IEnumerable<OperationClaim> _seeds
    {
        get
        {
            yield return new OperationClaim { Id = 1, Name = OperationClaims.GeneralAdmin };
            yield return new OperationClaim { Id = 2, Name = UserOperationClaims.List };
            yield return new OperationClaim { Id = 3, Name = UserOperationClaims.View };
            yield return new OperationClaim { Id = 4, Name = UserOperationClaims.Create };
            yield return new OperationClaim { Id = 5, Name = UserOperationClaims.Update };
            yield return new OperationClaim { Id = 6, Name = UserOperationClaims.Delete };
        }
    }
}
