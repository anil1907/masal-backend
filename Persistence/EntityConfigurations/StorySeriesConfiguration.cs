using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations;

public class StorySeriesConfiguration : IEntityTypeConfiguration<StorySeries>
{
    public void Configure(EntityTypeBuilder<StorySeries> builder)
    {
        builder.ToTable("StorySeries").HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("Id").IsRequired();
        builder.Property(s => s.ChildId).HasColumnName("ChildId").IsRequired();
        builder.Property(s => s.Title).HasColumnName("Title").IsRequired();
        builder.Property(s => s.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(s => s.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(s => s.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(s => s.DeletedDate).HasColumnName("DeletedDate");

        builder.HasOne(s => s.Child).WithMany().HasForeignKey(s => s.ChildId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.ChildId, s.IsActive });

        builder.HasQueryFilter(s => !s.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
