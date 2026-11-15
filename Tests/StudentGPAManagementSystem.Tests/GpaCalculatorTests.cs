using System;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositorys;
using Service;
using Xunit;

namespace StudentGPAManagementSystem.Tests;

public class GpaCalculatorTests
{
    [Fact]
    public async Task CalculateGpa_MatchesTechnicalAssessmentExample_Exactly()
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync(nameof(CalculateGpa_MatchesTechnicalAssessmentExample_Exactly));
        var unitOfWork = new UnitOfWork(context);

        var student = new Student { Code = "STU001", Name = "John Doe" };
        var sem1 = new Semester { Name = "Semester 1", StartDate = new DateOnly(2024, 9, 1), EndDate = new DateOnly(2025, 1, 15) };
        var sem2 = new Semester { Name = "Semester 2", StartDate = new DateOnly(2025, 2, 1), EndDate = new DateOnly(2025, 6, 15) };

        var cs101 = new Course { Code = "CS101", Name = "CS101", CreditHours = 3 };
        var math101 = new Course { Code = "MATH101", Name = "MATH101", CreditHours = 2 };
        var eng101 = new Course { Code = "ENG101", Name = "ENG101", CreditHours = 3 };
        var phy101 = new Course { Code = "PHY101", Name = "PHY101", CreditHours = 2 };
        var it101 = new Course { Code = "IT101", Name = "IT101", CreditHours = 3 };

        var cs102 = new Course { Code = "CS102", Name = "CS102", CreditHours = 3 };
        var math102 = new Course { Code = "MATH102", Name = "MATH102", CreditHours = 2 };

        await context.Students.AddAsync(student);
        await context.Semesters.AddRangeAsync(sem1, sem2);
        await context.Courses.AddRangeAsync(cs101, math101, eng101, phy101, it101, cs102, math102);
        await context.SaveChangesAsync();

        var gradeAMinus = await context.GradingScales.FirstAsync(g => g.Grade == "A-");
        var gradeBPlus = await context.GradingScales.FirstAsync(g => g.Grade == "B+");
        var gradeAPlus = await context.GradingScales.FirstAsync(g => g.Grade == "A+");
        var gradeC = await context.GradingScales.FirstAsync(g => g.Grade == "C");

        // Semester 1 Enrollments:
        // CS101: 3 hrs, 90 (A-, 3.4) -> 10.2 QP
        // MATH101: 2 hrs, 85 (B+, 3.2) -> 6.4 QP
        // ENG101: 3 hrs, Absent -> ABS (0.0) -> 0.0 QP
        // PHY101: 2 hrs, Withdrawn -> W -> Excluded
        // IT101: 3 hrs, Incomplete -> I -> Excluded
        var e1 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = cs101.Id, CreditHours = cs101.CreditHours, Status = EnrollmentStatus.Normal, Score = 90m, GradingScaleId = gradeAMinus.Id };
        var e2 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = math101.Id, CreditHours = math101.CreditHours, Status = EnrollmentStatus.Normal, Score = 85m, GradingScaleId = gradeBPlus.Id };
        var e3 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = eng101.Id, CreditHours = eng101.CreditHours, Status = EnrollmentStatus.Absent, Score = null, GradingScaleId = null };
        var e4 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = phy101.Id, CreditHours = phy101.CreditHours, Status = EnrollmentStatus.Withdrawn, Score = null, GradingScaleId = null };
        var e5 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = it101.Id, CreditHours = it101.CreditHours, Status = EnrollmentStatus.Incomplete, Score = null, GradingScaleId = null };

        // Semester 2 Enrollments:
        // CS102: 3 hrs, 96 (A+, 4.0) -> 12.0 QP
        // MATH102: 2 hrs, 70 (C, 2.4) -> 4.8 QP
        var e6 = new Enrollment { StudentId = student.Id, SemesterId = sem2.Id, CourseId = cs102.Id, CreditHours = cs102.CreditHours, Status = EnrollmentStatus.Normal, Score = 96m, GradingScaleId = gradeAPlus.Id };
        var e7 = new Enrollment { StudentId = student.Id, SemesterId = sem2.Id, CourseId = math102.Id, CreditHours = math102.CreditHours, Status = EnrollmentStatus.Normal, Score = 70m, GradingScaleId = gradeC.Id };

        await context.Enrollments.AddRangeAsync(e1, e2, e3, e4, e5, e6, e7);
        await context.SaveChangesAsync();

        var mapper = TestDbContextHelper.CreateTestMapper();
        var gpaService = new GpaCalculatorService(unitOfWork, mapper);

        // Act
        var result = await gpaService.CalculateGpaAsync(student.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);

        var payload = result.Payload;

        // Semester 1 Verification:
        var semester1 = payload.Semesters[0];
        Assert.Equal(13, semester1.RegisteredHours);
        Assert.Equal(8, semester1.GpaEligibleHours); // 3 + 2 + 3 (Absent counts in denominator)
        Assert.Equal(16.6m, semester1.QualityPoints); // 10.2 + 6.4 + 0.0
        Assert.Equal(2.08m, semester1.SemesterGpa); // 16.6 / 8 = 2.075 -> 2.08
        Assert.Equal("2.08", semester1.SemesterGpaDisplay);

        // Semester 2 Verification:
        var semester2 = payload.Semesters[1];
        Assert.Equal(5, semester2.RegisteredHours);
        Assert.Equal(5, semester2.GpaEligibleHours);
        Assert.Equal(16.8m, semester2.QualityPoints); // 12.0 + 4.8
        Assert.Equal(3.36m, semester2.SemesterGpa); // 16.8 / 5 = 3.36
        Assert.Equal("3.36", semester2.SemesterGpaDisplay);

        // Cumulative GPA Verification:
        // (16.6 + 16.8) / (8 + 5) = 33.4 / 13 = 2.5692... -> 2.57
        Assert.Equal(18, payload.TotalRegisteredHours); // 13 + 5
        Assert.Equal(13, payload.TotalGpaEligibleHours); // 8 + 5
        Assert.Equal(33.4m, payload.TotalQualityPoints);
        Assert.Equal(2.57m, payload.CumulativeGpa);
        Assert.Equal("2.57", payload.CumulativeGpaDisplay);
    }

    [Fact]
    public async Task CalculateGpa_NoEligibleHours_ReturnsNa_WithoutDivisionByZero()
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync(nameof(CalculateGpa_NoEligibleHours_ReturnsNa_WithoutDivisionByZero));
        var unitOfWork = new UnitOfWork(context);

        var student = new Student { Code = "STU002", Name = "Jane Doe" };
        var sem = new Semester { Name = "Fall 2024", StartDate = new DateOnly(2024, 9, 1), EndDate = new DateOnly(2025, 1, 15) };
        var course = new Course { Code = "PHY101", Name = "Physics", CreditHours = 3 };

        await context.Students.AddAsync(student);
        await context.Semesters.AddAsync(sem);
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();

        // Student only has Withdrawn course -> 0 GPA-eligible hours
        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            SemesterId = sem.Id,
            CourseId = course.Id,
            CreditHours = 3,
            Status = EnrollmentStatus.Withdrawn
        };
        await context.Enrollments.AddAsync(enrollment);
        await context.SaveChangesAsync();

        var mapper = TestDbContextHelper.CreateTestMapper();
        var gpaService = new GpaCalculatorService(unitOfWork, mapper);

        // Act
        var result = await gpaService.CalculateGpaAsync(student.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);
        Assert.Null(result.Payload.CumulativeGpa);
        Assert.Equal("N/A", result.Payload.CumulativeGpaDisplay);
        Assert.Equal(0, result.Payload.TotalGpaEligibleHours);
        Assert.Equal(3, result.Payload.TotalRegisteredHours);
    }

    [Fact]
    public async Task GetTranscript_MatchesTechnicalAssessmentExample_AllGradesAndStatuses()
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync(nameof(GetTranscript_MatchesTechnicalAssessmentExample_AllGradesAndStatuses));
        var unitOfWork = new UnitOfWork(context);

        var student = new Student { Code = "STU001", Name = "John Doe" };
        var sem1 = new Semester { Name = "Semester 1", StartDate = new DateOnly(2024, 9, 1), EndDate = new DateOnly(2025, 1, 15) };
        var sem2 = new Semester { Name = "Semester 2", StartDate = new DateOnly(2025, 2, 1), EndDate = new DateOnly(2025, 6, 15) };

        var cs101 = new Course { Code = "CS101", Name = "Intro CS", CreditHours = 3 };
        var math101 = new Course { Code = "MATH101", Name = "Calculus I", CreditHours = 2 };
        var eng101 = new Course { Code = "ENG101", Name = "English", CreditHours = 3 };
        var phy101 = new Course { Code = "PHY101", Name = "Physics", CreditHours = 2 };
        var it101 = new Course { Code = "IT101", Name = "IT Fundamentals", CreditHours = 3 };

        await context.Students.AddAsync(student);
        await context.Semesters.AddRangeAsync(sem1, sem2);
        await context.Courses.AddRangeAsync(cs101, math101, eng101, phy101, it101);
        await context.SaveChangesAsync();

        var gradeAMinus = await context.GradingScales.FirstAsync(g => g.Grade == "A-");
        var gradeBPlus = await context.GradingScales.FirstAsync(g => g.Grade == "B+");

        var e1 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = cs101.Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 90m, GradingScaleId = gradeAMinus.Id };
        var e2 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = math101.Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 85m, GradingScaleId = gradeBPlus.Id };
        var e3 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = eng101.Id, CreditHours = 3, Status = EnrollmentStatus.Absent, Score = null, GradingScaleId = null };
        var e4 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = phy101.Id, CreditHours = 2, Status = EnrollmentStatus.Withdrawn, Score = null, GradingScaleId = null };
        var e5 = new Enrollment { StudentId = student.Id, SemesterId = sem1.Id, CourseId = it101.Id, CreditHours = 3, Status = EnrollmentStatus.Incomplete, Score = null, GradingScaleId = null };

        await context.Enrollments.AddRangeAsync(e1, e2, e3, e4, e5);
        await context.SaveChangesAsync();

        var mapper = TestDbContextHelper.CreateTestMapper();
        var gpaService = new GpaCalculatorService(unitOfWork, mapper);

        // Act
        var result = await gpaService.GetTranscriptAsync(student.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);
        var transcript = result.Payload;

        Assert.Equal("STU001", transcript.StudentCode);
        Assert.Single(transcript.Semesters);

        var semTranscript = transcript.Semesters[0];
        Assert.Equal(5, semTranscript.Courses.Count);

        // Normal grade course: CS101
        var c1 = semTranscript.Courses.First(c => c.CourseCode == "CS101");
        Assert.Equal("A-", c1.DisplayedGrade);
        Assert.Equal(3.4m, c1.Points);
        Assert.Equal(10.2m, c1.QualityPoints);
        Assert.True(c1.IncludedInGpa);

        // Absent course: ENG101
        var c3 = semTranscript.Courses.First(c => c.CourseCode == "ENG101");
        Assert.Equal("ABS", c3.DisplayedGrade);
        Assert.Equal(0.0m, c3.Points);
        Assert.Equal(0.0m, c3.QualityPoints);
        Assert.True(c3.IncludedInGpa);

        // Withdrawn course: PHY101
        var c4 = semTranscript.Courses.First(c => c.CourseCode == "PHY101");
        Assert.Equal("W", c4.DisplayedGrade);
        Assert.Null(c4.Points);
        Assert.Null(c4.QualityPoints);
        Assert.False(c4.IncludedInGpa);

        // Incomplete course: IT101
        var c5 = semTranscript.Courses.First(c => c.CourseCode == "IT101");
        Assert.Equal("I", c5.DisplayedGrade);
        Assert.Null(c5.Points);
        Assert.Null(c5.QualityPoints);
        Assert.False(c5.IncludedInGpa);
    }
}
