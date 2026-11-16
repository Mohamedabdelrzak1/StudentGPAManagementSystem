using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments", t =>
        {
            t.HasCheckConstraint("CK_Enrollments_CreditHours", "[CreditHours] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_Enrollments_Status", "[Status] IN (1,2,3,4)");
            t.HasCheckConstraint("CK_Enrollments_ScoreRange", "[Score] IS NULL OR ([Score] >= 0 AND [Score] <= 100)");
            t.HasCheckConstraint("CK_Enrollments_ScoreOrStatus",
                "([Status] = 1 AND [Score] IS NOT NULL AND [GradingScaleId] IS NOT NULL) OR ([Status] <> 1 AND [Score] IS NULL AND [GradingScaleId] IS NULL)");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreditHours)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Score)
            .HasColumnType("decimal(5,2)")
            .IsRequired(false);

        builder.Property(e => e.GradingScaleId)
            .IsRequired(false);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Semester)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GradingScale)
            .WithMany()
            .HasForeignKey(e => e.GradingScaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StudentId, e.SemesterId, e.CourseId })
            .IsUnique()
            .HasDatabaseName("IX_Enrollments_Student_Semester_Course");

        builder.HasIndex(e => new { e.StudentId, e.SemesterId })
            .HasDatabaseName("IX_Enrollments_StudentId_SemesterId");

        builder.HasIndex(e => e.CourseId)
            .HasDatabaseName("IX_Enrollments_CourseId");
    }
}
