using System;
using System.Collections.Generic;
using Domain.Enums;
using Service;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Gpa;

public class GpaCalculationTests
{
    [Fact]
    public void CalculateSemester_SpecSemester1_ProducesRegistered13Eligible8QualityPoints16Point6Raw2Point075Display2Point08()
    {
        var courses = new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 3.4m),
            new CourseGpaItem(2, EnrollmentStatus.Normal, 3.2m),
            new CourseGpaItem(3, EnrollmentStatus.Absent, null),
            new CourseGpaItem(2, EnrollmentStatus.Withdrawn, null),
            new CourseGpaItem(3, EnrollmentStatus.Incomplete, null)
        };

        var result = GpaCalculatorService.CalculateSemester(courses);

        Assert.Equal(13, result.RegisteredHours);
        Assert.Equal(8, result.GpaEligibleHours);
        Assert.Equal(16.6m, result.QualityPoints);
        decimal rawGpa = result.QualityPoints / (decimal)result.GpaEligibleHours;
        Assert.Equal(2.075m, rawGpa);
        Assert.Equal(2.08m, result.Gpa);
        Assert.Equal("2.08", result.GpaDisplay);
    }

    [Fact]
    public void CalculateSemester_SpecSemester2_ProducesEligible5QualityPoints16Point8Gpa3Point36()
    {
        var courses = new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 4.0m),
            new CourseGpaItem(2, EnrollmentStatus.Normal, 2.4m)
        };

        var result = GpaCalculatorService.CalculateSemester(courses);

        Assert.Equal(5, result.RegisteredHours);
        Assert.Equal(5, result.GpaEligibleHours);
        Assert.Equal(16.8m, result.QualityPoints);
        Assert.Equal(3.36m, result.Gpa);
        Assert.Equal("3.36", result.GpaDisplay);
    }

    [Fact]
    public void CalculateCumulative_AcrossBothSemesters_ProducesTotalQp33Point4Eligible13Display2Point57()
    {
        var sem1 = GpaCalculatorService.CalculateSemester(new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 3.4m),
            new CourseGpaItem(2, EnrollmentStatus.Normal, 3.2m),
            new CourseGpaItem(3, EnrollmentStatus.Absent, null),
            new CourseGpaItem(2, EnrollmentStatus.Withdrawn, null),
            new CourseGpaItem(3, EnrollmentStatus.Incomplete, null)
        });

        var sem2 = GpaCalculatorService.CalculateSemester(new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 4.0m),
            new CourseGpaItem(2, EnrollmentStatus.Normal, 2.4m)
        });

        var cumulative = GpaCalculatorService.CalculateCumulative(new[] { sem1, sem2 });

        Assert.Equal(18, cumulative.TotalRegisteredHours);
        Assert.Equal(13, cumulative.TotalGpaEligibleHours);
        Assert.Equal(33.4m, cumulative.TotalQualityPoints);
        Assert.Equal(2.57m, cumulative.CumulativeGpa);
        Assert.Equal("2.57", cumulative.CumulativeGpaDisplay);
    }

    [Fact]
    public void CalculateCumulative_VersusArithmeticMean_ProvesWeightedCumulativeDiffersFromAverageOfSemesterGpas()
    {
        var sem1 = GpaCalculatorService.CalculateSemester(new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 3.4m),
            new CourseGpaItem(2, EnrollmentStatus.Normal, 3.2m),
            new CourseGpaItem(3, EnrollmentStatus.Absent, null),
            new CourseGpaItem(2, EnrollmentStatus.Withdrawn, null),
            new CourseGpaItem(3, EnrollmentStatus.Incomplete, null)
        });

        var sem2 = GpaCalculatorService.CalculateSemester(new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 4.0m),
            new CourseGpaItem(2, EnrollmentStatus.Normal, 2.4m)
        });

        var cumulative = GpaCalculatorService.CalculateCumulative(new[] { sem1, sem2 });

        decimal arithmeticMean = (sem1.Gpa!.Value + sem2.Gpa!.Value) / 2m; // (2.08 + 3.36) / 2 = 2.72
        Assert.Equal(2.72m, arithmeticMean);
        Assert.Equal(2.57m, cumulative.CumulativeGpa);
        Assert.NotEqual(arithmeticMean, cumulative.CumulativeGpa);
    }

    [Fact]
    public void CalculateSemester_AbsentStatus_IncludesHoursInDenominatorWithZeroQualityPoints()
    {
        var courses = new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 4.0m),
            new CourseGpaItem(3, EnrollmentStatus.Absent, null)
        };

        var result = GpaCalculatorService.CalculateSemester(courses);

        Assert.Equal(6, result.RegisteredHours);
        Assert.Equal(6, result.GpaEligibleHours);
        Assert.Equal(12.0m, result.QualityPoints);
        Assert.Equal(2.00m, result.Gpa);
        Assert.Equal("2.00", result.GpaDisplay);
    }

    [Fact]
    public void CalculateSemester_WithdrawnAndIncomplete_ExcludedFromNumeratorAndDenominatorButCountInRegisteredHours()
    {
        var courses = new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Normal, 3.0m),
            new CourseGpaItem(3, EnrollmentStatus.Withdrawn, null),
            new CourseGpaItem(2, EnrollmentStatus.Incomplete, null)
        };

        var result = GpaCalculatorService.CalculateSemester(courses);

        Assert.Equal(8, result.RegisteredHours);
        Assert.Equal(3, result.GpaEligibleHours);
        Assert.Equal(9.0m, result.QualityPoints);
        Assert.Equal(3.00m, result.Gpa);
    }

    [Fact]
    public void CalculateSemester_OnlyWithdrawnAndIncompleteCourses_ReturnsNullGpaAndNADisplayWithoutException()
    {
        var courses = new[]
        {
            new CourseGpaItem(3, EnrollmentStatus.Withdrawn, null),
            new CourseGpaItem(3, EnrollmentStatus.Incomplete, null)
        };

        var result = GpaCalculatorService.CalculateSemester(courses);

        Assert.Equal(6, result.RegisteredHours);
        Assert.Equal(0, result.GpaEligibleHours);
        Assert.Equal(0m, result.QualityPoints);
        Assert.Null(result.Gpa);
        Assert.Equal("N/A", result.GpaDisplay);
    }

    [Fact]
    public void CalculateSemester_EmptyList_ReturnsNullGpaAndNADisplay()
    {
        var result = GpaCalculatorService.CalculateSemester(Array.Empty<CourseGpaItem>());

        Assert.Equal(0, result.RegisteredHours);
        Assert.Equal(0, result.GpaEligibleHours);
        Assert.Equal(0m, result.QualityPoints);
        Assert.Null(result.Gpa);
        Assert.Equal("N/A", result.GpaDisplay);
    }

    [Fact]
    public void CalculateSemester_MidpointRoundingAwayFromZero_Rounds2Point075UpAnd2Point074Down()
    {
        var caseRoundsUp = new[]
        {
            new CourseGpaItem(4, EnrollmentStatus.Normal, 2.075m)
        };
        var resultUp = GpaCalculatorService.CalculateSemester(caseRoundsUp);
        Assert.Equal(2.08m, resultUp.Gpa);
        Assert.Equal("2.08", resultUp.GpaDisplay);

        var caseRoundsDown = new[]
        {
            new CourseGpaItem(5, EnrollmentStatus.Normal, 2.074m)
        };
        var resultDown = GpaCalculatorService.CalculateSemester(caseRoundsDown);
        Assert.Equal(2.07m, resultDown.Gpa);
        Assert.Equal("2.07", resultDown.GpaDisplay);
    }

    [Fact]
    public void CalculateCumulative_UsingRawQualityPointsAndHours_DiffersFromUsingRoundedSemesterGpas()
    {
        var sem1 = new SemesterGpaCalculation(2, 2, 4.09m, 2.05m, "2.05");
        var sem2 = new SemesterGpaCalculation(1, 1, 2.04m, 2.04m, "2.04");

        var cumulative = GpaCalculatorService.CalculateCumulative(new[] { sem1, sem2 });

        decimal weightedUsingRounded = Math.Round((2.05m * 2m + 2.04m * 1m) / 3m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(2.05m, weightedUsingRounded);

        Assert.Equal(2.04m, cumulative.CumulativeGpa);
        Assert.Equal("2.04", cumulative.CumulativeGpaDisplay);
        Assert.NotEqual(weightedUsingRounded, cumulative.CumulativeGpa);
    }
}
