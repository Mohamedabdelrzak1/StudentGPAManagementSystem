using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Data.Configurations;

public class GradingScaleConfiguration : IEntityTypeConfiguration<GradingScale>
{
    public void Configure(EntityTypeBuilder<GradingScale> builder)
    {
        builder.ToTable("GradingScales", t =>
        {
            t.HasCheckConstraint("CK_GradingScales_Range", "[FromGrade] >= 0 AND [ToGrade] > [FromGrade] AND [ToGrade] <= 101");
            t.HasCheckConstraint("CK_GradingScales_Points", "[Points] BETWEEN 0 AND 4");
        });

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.FromGrade)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(g => g.ToGrade)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(g => g.Points)
            .IsRequired()
            .HasColumnType("decimal(3,1)");

        builder.Property(g => g.Grade)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasIndex(g => g.Grade)
            .IsUnique()
            .HasDatabaseName("IX_GradingScales_Grade");

        builder.HasIndex(g => g.FromGrade)
            .IsUnique()
            .HasDatabaseName("IX_GradingScales_FromGrade");

        builder.HasData(
            new GradingScale { Id = 1, FromGrade = 96m, ToGrade = 101m, Points = 4.0m, Grade = "A+" },
            new GradingScale { Id = 2, FromGrade = 92m, ToGrade = 96m, Points = 3.7m, Grade = "A" },
            new GradingScale { Id = 3, FromGrade = 88m, ToGrade = 92m, Points = 3.4m, Grade = "A-" },
            new GradingScale { Id = 4, FromGrade = 84m, ToGrade = 88m, Points = 3.2m, Grade = "B+" },
            new GradingScale { Id = 5, FromGrade = 80m, ToGrade = 84m, Points = 3.0m, Grade = "B" },
            new GradingScale { Id = 6, FromGrade = 76m, ToGrade = 80m, Points = 2.8m, Grade = "B-" },
            new GradingScale { Id = 7, FromGrade = 72m, ToGrade = 76m, Points = 2.6m, Grade = "C+" },
            new GradingScale { Id = 8, FromGrade = 68m, ToGrade = 72m, Points = 2.4m, Grade = "C" },
            new GradingScale { Id = 9, FromGrade = 64m, ToGrade = 68m, Points = 2.2m, Grade = "C-" },
            new GradingScale { Id = 10, FromGrade = 60m, ToGrade = 64m, Points = 2.0m, Grade = "D+" },
            new GradingScale { Id = 11, FromGrade = 55m, ToGrade = 60m, Points = 1.5m, Grade = "D" },
            new GradingScale { Id = 12, FromGrade = 50m, ToGrade = 55m, Points = 1.0m, Grade = "D-" },
            new GradingScale { Id = 13, FromGrade = 0m, ToGrade = 50m, Points = 0.0m, Grade = "F" }
        );
    }
}
