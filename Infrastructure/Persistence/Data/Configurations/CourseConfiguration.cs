using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses", t =>
        {
            t.HasCheckConstraint("CK_Courses_CreditHours", "[CreditHours] BETWEEN 1 AND 3");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("IX_Courses_Code");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.CreditHours)
            .IsRequired();
    }
}
