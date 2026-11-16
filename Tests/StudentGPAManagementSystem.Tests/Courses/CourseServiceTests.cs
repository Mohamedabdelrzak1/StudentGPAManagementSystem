using System.Net;
using System.Threading.Tasks;
using Domain.Models;
using Service;
using Shared.Dto;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Courses;

public class CourseServiceTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task CreateCourseAsync_CreditHoursValidation_Rejects0And4AndAccepts1And2And3(int creditHours, bool expectedSuccess)
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new CourseService(db.UnitOfWork, db.Mapper);

        var result = await service.CreateCourseAsync(new CreateCourseDto
        {
            Code = $"CS{creditHours}H",
            Name = $"Course {creditHours}H",
            CreditHours = creditHours
        });

        if (expectedSuccess)
        {
            Assert.True(result.Succeeded);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }
        else
        {
            Assert.False(result.Succeeded);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Contains("between 1 and 3", result.Message);
        }
    }

    [Fact]
    public async Task CreateCourseAsync_DuplicateCourseCodeCaseInsensitive_ReturnsConflict409()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new CourseService(db.UnitOfWork, db.Mapper);

        var first = await service.CreateCourseAsync(new CreateCourseDto { Code = "cs101", Name = "Intro CS", CreditHours = 3 });
        Assert.True(first.Succeeded);

        var duplicate = await service.CreateCourseAsync(new CreateCourseDto { Code = "CS101", Name = "Another Intro CS", CreditHours = 3 });
        Assert.False(duplicate.Succeeded);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task UpdateCourseAsync_CourseCodeIsImmutable_PreservesOriginalCodeAfterUpdate()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new CourseService(db.UnitOfWork, db.Mapper);

        var created = await service.CreateCourseAsync(new CreateCourseDto { Code = "CS201", Name = "Data Structures", CreditHours = 3 });
        Assert.True(created.Succeeded);

        var updateResult = await service.UpdateCourseAsync(created.Payload!.Id, new UpdateCourseDto
        {
            Name = "Data Structures and Algorithms",
            CreditHours = 2
        });

        Assert.True(updateResult.Succeeded);
        Assert.Equal("Data Structures and Algorithms", updateResult.Payload!.Name);
        Assert.Equal("CS201", updateResult.Payload.Code);
        Assert.Equal(2, updateResult.Payload.CreditHours);
    }

    [Fact]
    public async Task DeleteCourseAsync_CourseWithExistingEnrollments_ReturnsBadRequest400()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var course = new Course { Code = "CS301", Name = "Web Dev", CreditHours = 3 };
        var student = new Student { Code = "STU100", Name = "Ali" };
        var semester = new Semester { Name = "Fall 2024", StartDate = new System.DateOnly(2024, 9, 1), EndDate = new System.DateOnly(2025, 1, 15) };
        db.Context.Courses.Add(course);
        db.Context.Students.Add(student);
        db.Context.Semesters.Add(semester);
        await db.Context.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            CreditHours = 3,
            Status = Domain.Enums.EnrollmentStatus.Normal,
            Score = 80m,
            GradingScaleId = 5
        };
        db.Context.Enrollments.Add(enrollment);
        await db.Context.SaveChangesAsync();

        var service = new CourseService(db.UnitOfWork, db.Mapper);
        var deleteResult = await service.DeleteCourseAsync(course.Id);

        Assert.False(deleteResult.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResult.StatusCode);
        Assert.Contains("existing student enrollments", deleteResult.Message, System.StringComparison.OrdinalIgnoreCase);
    }
}
