using Domain.Entities.Children;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class ChildConfiguration : IEntityTypeConfiguration<Child>
{
    public void Configure(EntityTypeBuilder<Child> builder)
    {
        builder.ToTable("Children").HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("Id").IsRequired();
        builder.Property(c => c.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(c => c.HeroName).HasColumnName("HeroName").IsRequired();
        builder.Property(c => c.Fears).HasColumnName("Fears").HasColumnType("text[]").IsRequired();
        builder.Property(c => c.Interests).HasColumnName("Interests").HasColumnType("text[]").IsRequired();
        builder.Property(c => c.AgeBand).HasColumnName("AgeBand");
        builder.Property(c => c.Gender).HasColumnName("Gender");
        builder.Property(c => c.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(c => c.CreatedDate).HasColumnName("CreatedDate").IsRequired();
        builder.Property(c => c.UpdatedDate).HasColumnName("UpdatedDate");
        builder.Property(c => c.DeletedDate).HasColumnName("DeletedDate");

        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        // Several children per parent now (free: 1, premium: up to 3); one active at a time.
        builder.HasIndex(c => new { c.UserId, c.IsActive });

        builder.HasQueryFilter(c => !c.DeletedDate.HasValue);
        builder.HasBaseType((string)null!);
    }
}
