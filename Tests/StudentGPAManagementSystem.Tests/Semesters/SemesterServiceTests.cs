using System;
using System.Net;
using System.Threading.Tasks;
using Domain.Models;
using Service;
using Shared.Dto;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Semesters;

public class SemesterServiceTests
{
    [Theory]
    [InlineData("2024-09-01", "2024-08-01")] // endDate < startDate
    [InlineData("2024-09-01", "2024-09-01")] // endDate == startDate
    public async Task CreateSemesterAsync_EndDateLessThanOrEqualToStartDate_ReturnsBadRequest400(string startStr, string endStr)
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new SemesterService(db.UnitOfWork, db.Mapper);

        var result = await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "Invalid Dates Semester",
            StartDate = DateOnly.Parse(startStr),
            EndDate = DateOnly.Parse(endStr)
        });

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("greater than start date", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSemesterAsync_DuplicateSemesterName_ReturnsConflict409()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new SemesterService(db.UnitOfWork, db.Mapper);

        var first = await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "Fall 2024",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 1, 15)
        });
        Assert.True(first.Succeeded);

        var duplicate = await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "FALL 2024",
            StartDate = new DateOnly(2024, 10, 1),
            EndDate = new DateOnly(2025, 2, 15)
        });
        Assert.False(duplicate.Succeeded);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateSemesterAsync_OverlappingDates_ReturnsConflict409()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new SemesterService(db.UnitOfWork, db.Mapper);

        var first = await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "Fall 2024",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 1, 15)
        });
        Assert.True(first.Succeeded);

        var overlapping = await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "Spring 2025",
            StartDate = new DateOnly(2025, 1, 15),
            EndDate = new DateOnly(2025, 6, 15)
        });

        Assert.False(overlapping.Succeeded);
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);
        Assert.Contains("overlap", overlapping.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllSemestersAsync_LatestSemester_IsMarkedAsCurrent()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new SemesterService(db.UnitOfWork, db.Mapper);

        await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "Fall 2024",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 1, 15)
        });
        await service.CreateSemesterAsync(new CreateSemesterDto
        {
            Name = "Spring 2025",
            StartDate = new DateOnly(2025, 2, 1),
            EndDate = new DateOnly(2025, 6, 15)
        });

        var result = await service.GetAllSemestersAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Payload!.Count(s => s.IsCurrent));
        Assert.Equal("Spring 2025", result.Payload.Single(s => s.IsCurrent).Name);
    }

    [Fact]
    public async Task DeleteSemesterAsync_SemesterWithExistingEnrollments_ReturnsBadRequest400()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var student = new Student { Code = "STU101", Name = "Mariam" };
        var course = new Course { Code = "MATH101", Name = "Calculus", CreditHours = 2 };
        var semester = new Semester { Name = "Spring 2024", StartDate = new DateOnly(2024, 2, 1), EndDate = new DateOnly(2024, 6, 15) };
        db.Context.Students.Add(student);
        db.Context.Courses.Add(course);
        db.Context.Semesters.Add(semester);
        await db.Context.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            CreditHours = 2,
            Status = Domain.Enums.EnrollmentStatus.Normal,
            Score = 85m,
            GradingScaleId = 4
        };
        db.Context.Enrollments.Add(enrollment);
        await db.Context.SaveChangesAsync();

        var service = new SemesterService(db.UnitOfWork, db.Mapper);
        var deleteResult = await service.DeleteSemesterAsync(semester.Id);

        Assert.False(deleteResult.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResult.StatusCode);
        Assert.Contains("registered enrollments", deleteResult.Message, StringComparison.OrdinalIgnoreCase);
    }
}
