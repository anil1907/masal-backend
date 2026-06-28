using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class StoryChapterConfiguration : IEntityTypeConfiguration<StoryChapter>
{
    public void Configure(EntityTypeBuilder<StoryChapter> builder)
    {
        builder.ToTable("StoryChapters").HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("Id").IsRequired();
        builder.Property(c => c.ChildId).HasColumnName("ChildId").IsRequired();
        builder.Property(c => c.SeriesId).HasColumnName("SeriesId").IsRequired();
        builder.Property(c => c.Number).HasColumnName("Number").IsRequired();
        builder.Property(c => c.Title).HasColumnName("Title").IsRequired();
        builder.Property(c => c.Text).HasColumnName("Text").IsRequired();
        builder.Property(c => c.Summary).HasColumnName("Summary").IsRequired();
        builder.Property(c => c.AudioObjectKey).HasColumnName("AudioObjectKey").IsRequired();
        builder.Property(c => c.DurationSeconds).HasColumnName("DurationSeconds").IsRequired();
        builder.Property(c => c.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(c => c.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(c => c.DeletedDate).HasColumnName("DeletedDate");

        builder.HasOne(c => c.Child).WithMany().HasForeignKey(c => c.ChildId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Series).WithMany().HasForeignKey(c => c.SeriesId).OnDelete(DeleteBehavior.Cascade);
        // One row per (series, chapter number); also the lookup for "latest chapter in a series".
        builder.HasIndex(c => new { c.SeriesId, c.Number }).IsUnique();
        builder.HasIndex(c => c.ChildId);   // weekly free-limit count

        builder.HasQueryFilter(c => !c.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
