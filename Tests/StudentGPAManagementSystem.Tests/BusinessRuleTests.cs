using System;
using System.Threading.Tasks;
using Domain.Models;
using Persistence.Repositorys;
using Service;
using Shared.Dto;
using Xunit;

namespace StudentGPAManagementSystem.Tests;

public class BusinessRuleTests
{
    [Fact]
    public async Task EnrollCourse_Exceeding18HoursLimit_ReturnsBadRequest()
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync(nameof(EnrollCourse_Exceeding18HoursLimit_ReturnsBadRequest));
        var unitOfWork = new UnitOfWork(context);
        var mapper = TestDbContextHelper.CreateTestMapper();
        var gradingScaleService = new GradingScaleService(unitOfWork, mapper);
        var enrollmentService = new EnrollmentService(unitOfWork, gradingScaleService, mapper);

        var student = new Student { Code = "STU100", Name = "Limit Tester" };
        var semester = new Semester { Name = "Spring 2025", StartDate = new DateOnly(2025, 2, 1), EndDate = new DateOnly(2025, 6, 1) };

        await context.Students.AddAsync(student);
        await context.Semesters.AddAsync(semester);

        // Add 6 courses of 3 credit hours each (Total = 18 hours)
        for (int i = 1; i <= 6; i++)
        {
            var course = new Course { Code = $"CRS{i}", Name = $"Course {i}", CreditHours = 3 };
            await context.Courses.AddAsync(course);
            await context.SaveChangesAsync();

            var enrollResult = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
            {
                StudentId = student.Id,
                SemesterId = semester.Id,
                CourseId = course.Id,
                Status = "Normal",
                Score = 80m
            });

            Assert.True(enrollResult.Succeeded);
        }

        // Try to add a 7th course (exceeding 18 hours)
        var extraCourse = new Course { Code = "CRS7", Name = "Course 7", CreditHours = 2 };
        await context.Courses.AddAsync(extraCourse);
        await context.SaveChangesAsync();

        // Act
        var result = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = extraCourse.Id,
            Status = "Normal",
            Score = 90m
        });

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("exceed the maximum limit of 18 credit hours", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnrollCourse_SameCourseTwiceInSameSemester_ReturnsConflict()
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync(nameof(EnrollCourse_SameCourseTwiceInSameSemester_ReturnsConflict));
        var unitOfWork = new UnitOfWork(context);
        var mapper = TestDbContextHelper.CreateTestMapper();
        var gradingScaleService = new GradingScaleService(unitOfWork, mapper);
        var enrollmentService = new EnrollmentService(unitOfWork, gradingScaleService, mapper);

        var student = new Student { Code = "STU101", Name = "Duplicate Tester" };
        var semester = new Semester { Name = "Fall 2025", StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 1, 15) };
        var course = new Course { Code = "CS201", Name = "Algorithms", CreditHours = 3 };

        await context.Students.AddAsync(student);
        await context.Semesters.AddAsync(semester);
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();

        // First enrollment: Success
        var first = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            Status = "Normal",
            Score = 85m
        });
        Assert.True(first.Succeeded);

        // Act: Second enrollment of same course in same semester
        var second = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            Status = "Normal",
            Score = 90m
        });

        // Assert
        Assert.False(second.Succeeded);
        Assert.Contains("already registered", second.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(100, "A+", 4.0)]
    [InlineData(96, "A+", 4.0)]
    [InlineData(95.99, "A", 3.7)]
    [InlineData(92, "A", 3.7)]
    [InlineData(90, "A-", 3.4)]
    [InlineData(85, "B+", 3.2)]
    [InlineData(80, "B", 3.0)]
    [InlineData(70, "C", 2.4)]
    [InlineData(50, "D-", 1.0)]
    [InlineData(49.99, "F", 0.0)]
    [InlineData(0, "F", 0.0)]
    public async Task GradingScale_MapsScoresCorrectly_InclusiveLower_ExclusiveUpper(decimal score, string expectedGrade, decimal expectedPoints)
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync($"GradingScale_{score}");
        var unitOfWork = new UnitOfWork(context);
        var mapper = TestDbContextHelper.CreateTestMapper();
        var gradingScaleService = new GradingScaleService(unitOfWork, mapper);

        // Act
        var result = await gradingScaleService.ResolveGradeAsync(score);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedGrade, result.Grade);
        Assert.Equal(expectedPoints, result.Points);
    }

    [Fact]
    public async Task EnrollCourse_SpecialStatusWithScore_ReturnsBadRequest()
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync(nameof(EnrollCourse_SpecialStatusWithScore_ReturnsBadRequest));
        var unitOfWork = new UnitOfWork(context);
        var mapper = TestDbContextHelper.CreateTestMapper();
        var gradingScaleService = new GradingScaleService(unitOfWork, mapper);
        var enrollmentService = new EnrollmentService(unitOfWork, gradingScaleService, mapper);

        var student = new Student { Code = "STU102", Name = "Status Tester" };
        var semester = new Semester { Name = "Spring 2026", StartDate = new DateOnly(2026, 2, 1), EndDate = new DateOnly(2026, 6, 1) };
        var course = new Course { Code = "ENG102", Name = "English II", CreditHours = 3 };

        await context.Students.AddAsync(student);
        await context.Semesters.AddAsync(semester);
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();

        // Act: Passing both Withdrawn AND a score (violates Rule 3.5)
        var result = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            Status = "Withdrawn",
            Score = 75m
        });

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("cannot have both a numeric score and a special academic status", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task CreateCourse_CreditHoursOutside1To3_ReturnsBadRequest(int creditHours)
    {
        // Arrange
        var context = await TestDbContextHelper.CreateInMemoryDbContextAsync($"CourseCredit_{creditHours}");
        var unitOfWork = new UnitOfWork(context);
        var mapper = TestDbContextHelper.CreateTestMapper();
        var courseService = new CourseService(unitOfWork, mapper);

        // Act
        var result = await courseService.CreateCourseAsync(new CreateCourseDto
        {
            Code = $"CRS_{creditHours}",
            Name = "Invalid Course",
            CreditHours = creditHours
        });

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("between 1 and 3", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
