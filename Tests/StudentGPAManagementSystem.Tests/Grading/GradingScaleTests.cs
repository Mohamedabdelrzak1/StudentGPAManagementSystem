using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Service;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Grading;

public class GradingScaleTests
{
    [Theory]
    [InlineData(0.0, "F", 0.0)]
    [InlineData(49.99, "F", 0.0)]
    [InlineData(50.0, "D-", 1.0)]
    [InlineData(54.99, "D-", 1.0)]
    [InlineData(55.0, "D", 1.5)]
    [InlineData(59.99, "D", 1.5)]
    [InlineData(60.0, "D+", 2.0)]
    [InlineData(63.99, "D+", 2.0)]
    [InlineData(64.0, "C-", 2.2)]
    [InlineData(67.99, "C-", 2.2)]
    [InlineData(68.0, "C", 2.4)]
    [InlineData(71.99, "C", 2.4)]
    [InlineData(72.0, "C+", 2.6)]
    [InlineData(75.99, "C+", 2.6)]
    [InlineData(76.0, "B-", 2.8)]
    [InlineData(79.99, "B-", 2.8)]
    [InlineData(80.0, "B", 3.0)]
    [InlineData(83.99, "B", 3.0)]
    [InlineData(84.0, "B+", 3.2)]
    [InlineData(87.99, "B+", 3.2)]
    [InlineData(88.0, "A-", 3.4)]
    [InlineData(91.99, "A-", 3.4)]
    [InlineData(92.0, "A", 3.7)]
    [InlineData(95.99, "A", 3.7)]
    [InlineData(96.0, "A+", 4.0)]
    [InlineData(100.0, "A+", 4.0)]
    public async Task ResolveGradeAsync_SpecBoundaryScores_ReturnsExpectedGradeAndPoints(decimal score, string expectedGrade, decimal expectedPoints)
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        await db.SeedStandardGradingScalesAsync();

        var service = new GradingScaleService(db.UnitOfWork, db.Mapper);

        var result = await service.ResolveGradeAsync(score);

        Assert.NotNull(result);
        Assert.Equal(expectedGrade, result!.Grade);
        Assert.Equal(expectedPoints, result.Points);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public async Task ResolveGradeAsync_OutOfRangeScores_ReturnsNull(decimal score)
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        await db.SeedStandardGradingScalesAsync();

        var service = new GradingScaleService(db.UnitOfWork, db.Mapper);

        var result = await service.ResolveGradeAsync(score);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveGradeAsync_Boundary96WithReversedScaleOrder_ExcludesGradeAAndResolvesToAPlus()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        // Clear default seeded scales and insert with A (92-96) BEFORE A+ (96-101)
        db.Context.GradingScales.RemoveRange(db.Context.GradingScales);
        await db.Context.SaveChangesAsync();

        db.Context.GradingScales.AddRange(
            new Domain.Models.GradingScale { Id = 100, FromGrade = 92m, ToGrade = 96m, Points = 3.7m, Grade = "A" },
            new Domain.Models.GradingScale { Id = 101, FromGrade = 96m, ToGrade = 101m, Points = 4.0m, Grade = "A+" }
        );
        await db.Context.SaveChangesAsync();

        var service = new GradingScaleService(db.UnitOfWork, db.Mapper);

        var result = await service.ResolveGradeAsync(96m);

        Assert.NotNull(result);
        Assert.Equal("A+", result!.Grade);
        Assert.Equal(4.0m, result.Points);
    }

    [Fact]
    public async Task ResolveGradeAsync_DataDrivenScaleModifiedInDatabase_ReflectsNewBoundaryWithoutCodeChanges()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        await db.SeedStandardGradingScalesAsync();

        var service = new GradingScaleService(db.UnitOfWork, db.Mapper);

        var initial = await service.ResolveGradeAsync(90m);
        Assert.NotNull(initial);
        Assert.Equal("A-", initial!.Grade);

        var scaleA = await db.Context.GradingScales.FirstAsync(g => g.Grade == "A");
        var scaleAMinus = await db.Context.GradingScales.FirstAsync(g => g.Grade == "A-");

        scaleAMinus.ToGrade = 90m;
        scaleA.FromGrade = 90m;
        await db.Context.SaveChangesAsync();

        var afterModification = await service.ResolveGradeAsync(90m);
        Assert.NotNull(afterModification);
        Assert.Equal("A", afterModification!.Grade);
        Assert.Equal(3.7m, afterModification.Points);
    }
}
