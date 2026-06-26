using Application.Features;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

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
        }
    }
}
