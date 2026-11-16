using System.Net;
using System.Threading.Tasks;
using Domain.Models;
using Service;
using Shared.Dto;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Students;

public class StudentServiceTests
{
    [Fact]
    public async Task CreateStudentAsync_DuplicateStudentCodeCaseInsensitive_ReturnsConflict409()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new StudentService(db.UnitOfWork, db.Mapper);

        var first = await service.CreateStudentAsync(new CreateStudentDto { Code = "stu001", Name = "Ahmed Ali" });
        Assert.True(first.Succeeded);

        var duplicate = await service.CreateStudentAsync(new CreateStudentDto { Code = "STU001", Name = "Ahmed Ali Duplicate" });
        Assert.False(duplicate.Succeeded);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentAsync_StudentCodeIsImmutable_PreservesOriginalCodeAfterUpdate()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var service = new StudentService(db.UnitOfWork, db.Mapper);

        var created = await service.CreateStudentAsync(new CreateStudentDto { Code = "STU002", Name = "Sara Khalil" });
        Assert.True(created.Succeeded);

        var updateResult = await service.UpdateStudentAsync(created.Payload!.Id, new UpdateStudentDto
        {
            Name = "Sara Mahmoud Khalil"
        });

        Assert.True(updateResult.Succeeded);
        Assert.Equal("Sara Mahmoud Khalil", updateResult.Payload!.Name);
        Assert.Equal("STU002", updateResult.Payload.Code);
    }

    [Fact]
    public async Task DeleteStudentAsync_StudentWithExistingEnrollments_ReturnsBadRequest400()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var student = new Student { Code = "STU003", Name = "Omar Khaled" };
        var course = new Course { Code = "CS101", Name = "Intro CS", CreditHours = 3 };
        var semester = new Semester { Name = "Fall 2024", StartDate = new System.DateOnly(2024, 9, 1), EndDate = new System.DateOnly(2025, 1, 15) };
        db.Context.Students.Add(student);
        db.Context.Courses.Add(course);
        db.Context.Semesters.Add(semester);
        await db.Context.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            CreditHours = 3,
            Status = Domain.Enums.EnrollmentStatus.Normal,
            Score = 90m,
            GradingScaleId = 3
        };
        db.Context.Enrollments.Add(enrollment);
        await db.Context.SaveChangesAsync();

        var service = new StudentService(db.UnitOfWork, db.Mapper);
        var deleteResult = await service.DeleteStudentAsync(student.Id);

        Assert.False(deleteResult.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResult.StatusCode);
        Assert.Contains("registered enrollments", deleteResult.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllStudentsAsync_ReturnsStudentsWithCalculatedGpaAndAcademicStanding()
    {
        await using var db = TestDbContextHelper.CreateDatabase();
        var student = new Student { Code = "STU_HONOR", Name = "Honor Student" };
        var course = new Course { Code = "CS101", Name = "Intro CS", CreditHours = 3 };
        var semester = new Semester { Name = "Fall 2024", StartDate = new System.DateOnly(2024, 9, 1), EndDate = new System.DateOnly(2025, 1, 15) };
        db.Context.Students.Add(student);
        db.Context.Courses.Add(course);
        db.Context.Semesters.Add(semester);
        await db.Context.SaveChangesAsync();

        var scaleA = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Context.GradingScales, g => g.Grade == "A+");
        db.Context.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            SemesterId = semester.Id,
            CourseId = course.Id,
            CreditHours = 3,
            Status = Domain.Enums.EnrollmentStatus.Normal,
            Score = 98m,
            GradingScaleId = scaleA.Id
        });
        await db.Context.SaveChangesAsync();

        var service = new StudentService(db.UnitOfWork, db.Mapper);
        var response = await service.GetAllStudentsAsync();

        Assert.True(response.Succeeded);
        var stuDto = response.Payload!.First(s => s.Code == "STU_HONOR");
        Assert.Equal(4.00m, stuDto.Gpa);
        Assert.Equal("متفوق", stuDto.Status);
        Assert.Equal("علوم الحاسب", stuDto.Major);
    }
}
