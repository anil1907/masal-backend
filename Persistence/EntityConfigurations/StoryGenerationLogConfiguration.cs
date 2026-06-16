using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations;

public class StoryGenerationLogConfiguration : IEntityTypeConfiguration<StoryGenerationLog>
{
    public void Configure(EntityTypeBuilder<StoryGenerationLog> builder)
    {
        builder.ToTable("StoryGenerationLogs").HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("Id").IsRequired();
        builder.Property(l => l.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(l => l.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(l => l.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(l => l.DeletedDate).HasColumnName("DeletedDate");

        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => new { l.UserId, l.CreatedDate });   // quota count query

        builder.HasQueryFilter(l => !l.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
