using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Service;
using Shared.Dto;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Enrollments;

public class EnrollmentRuleTests
{
    private static async Task<(SqliteTestDatabase db, EnrollmentService enrollmentService, CourseService courseService, GpaCalculatorService gpaService, int studentId, int semesterId, int courseId)> SetupBaseAsync()
    {
        var db = TestDbContextHelper.CreateDatabase();
        await db.SeedStandardGradingScalesAsync();

        var student = new Student { Code = "STU001", Name = "Ahmed Ali" };
        var semester = new Semester { Name = "Fall 2024", StartDate = new DateOnly(2024, 9, 1), EndDate = new DateOnly(2025, 1, 15) };
        var course = new Course { Code = "CS101", Name = "Intro CS", CreditHours = 3 };

        db.Context.Students.Add(student);
        db.Context.Semesters.Add(semester);
        db.Context.Courses.Add(course);
        await db.Context.SaveChangesAsync();

        var gradingService = new GradingScaleService(db.UnitOfWork, db.Mapper);
        var enrollmentService = new EnrollmentService(db.UnitOfWork, gradingService, db.Mapper);
        var courseService = new CourseService(db.UnitOfWork, db.Mapper);
        var gpaService = new GpaCalculatorService(db.UnitOfWork, db.Mapper);

        return (db, enrollmentService, courseService, gpaService, student.Id, semester.Id, course.Id);
    }

    [Fact]
    public async Task EnrollCourseAsync_Exactly18HoursAccepted_EnrollmentMaking19HoursIsRejected()
    {
        await using var setup = (await SetupBaseAsync()).db;
        var (db, enrollmentService, _, _, studentId, semesterId, _) = await SetupBaseAsync();

        for (int i = 1; i <= 6; i++)
        {
            var course = new Course { Code = $"C{i:D2}", Name = $"Course {i}", CreditHours = 3 };
            db.Context.Courses.Add(course);
            await db.Context.SaveChangesAsync();

            var enrollResult = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
            {
                StudentId = studentId,
                SemesterId = semesterId,
                CourseId = course.Id,
                Status = "Normal",
                Score = 85m
            });
            Assert.True(enrollResult.Succeeded);
        }

        var extraCourse = new Course { Code = "EXTRA1H", Name = "Extra 1 Hour", CreditHours = 1 };
        db.Context.Courses.Add(extraCourse);
        await db.Context.SaveChangesAsync();

        var overflowResult = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = extraCourse.Id,
            Status = "Normal",
            Score = 85m
        });

        Assert.False(overflowResult.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, overflowResult.StatusCode);
        Assert.Contains("18 credit hours", overflowResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnrollCourseAsync_WithdrawnAndIncompleteEnrollments_CountTowardThe18HourLimit()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, _) = await SetupBaseAsync();
        await using var _ = db;

        for (int i = 1; i <= 5; i++)
        {
            var course = new Course { Code = $"W{i:D2}", Name = $"Withdrawn Course {i}", CreditHours = 3 };
            db.Context.Courses.Add(course);
            await db.Context.SaveChangesAsync();

            var result = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
            {
                StudentId = studentId,
                SemesterId = semesterId,
                CourseId = course.Id,
                Status = i % 2 == 0 ? "Incomplete" : "Withdrawn"
            });
            Assert.True(result.Succeeded);
        }

        var normal3h = new Course { Code = "N3H", Name = "Normal 3h Course", CreditHours = 3 };
        db.Context.Courses.Add(normal3h);
        await db.Context.SaveChangesAsync();

        var exact18 = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = normal3h.Id,
            Status = "Normal",
            Score = 90m
        });
        Assert.True(exact18.Succeeded);

        var course19 = new Course { Code = "OVER", Name = "Over Limit", CreditHours = 1 };
        db.Context.Courses.Add(course19);
        await db.Context.SaveChangesAsync();

        var reject19 = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = course19.Id,
            Status = "Normal",
            Score = 90m
        });

        Assert.False(reject19.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, reject19.StatusCode);
    }

    [Fact]
    public async Task UpdateEnrollmentAsync_ExistingEnrollment_DoesNotDoubleCountItselfAgainst18HourLimit()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, _) = await SetupBaseAsync();
        await using var _ = db;

        for (int i = 1; i <= 6; i++)
        {
            var course = new Course { Code = $"C{i:D2}", Name = $"Course {i}", CreditHours = 3 };
            db.Context.Courses.Add(course);
            await db.Context.SaveChangesAsync();

            await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
            {
                StudentId = studentId,
                SemesterId = semesterId,
                CourseId = course.Id,
                Status = "Normal",
                Score = 80m
            });
        }

        var existingEnrollment = await db.Context.Enrollments.FirstAsync(e => e.StudentId == studentId);

        var updateResult = await enrollmentService.UpdateEnrollmentAsync(existingEnrollment.Id, new UpdateEnrollmentDto
        {
            Status = "Normal",
            Score = 95m
        });

        Assert.True(updateResult.Succeeded);
        Assert.Equal(95m, updateResult.Payload!.Score);
    }

    [Fact]
    public async Task EnrollCourseAsync_DuplicateStudentSemesterCourse_ReturnsConflict409()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var first = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = 80m
        });
        Assert.True(first.Succeeded);

        var duplicate = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = 90m
        });

        Assert.False(duplicate.Succeeded);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task EnrollCourseAsync_SameCourseInDifferentSemesterForSameStudent_IsAllowed()
    {
        var (db, enrollmentService, _, _, studentId, sem1Id, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var sem2 = new Semester { Name = "Spring 2025", StartDate = new DateOnly(2025, 2, 1), EndDate = new DateOnly(2025, 6, 15) };
        db.Context.Semesters.Add(sem2);
        await db.Context.SaveChangesAsync();

        var sem1Enrollment = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = sem1Id,
            CourseId = courseId,
            Status = "Normal",
            Score = 75m
        });
        Assert.True(sem1Enrollment.Succeeded);

        var sem2Enrollment = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = sem2.Id,
            CourseId = courseId,
            Status = "Normal",
            Score = 85m
        });
        Assert.True(sem2Enrollment.Succeeded);
        Assert.Equal(HttpStatusCode.Created, sem2Enrollment.StatusCode);
    }

    [Fact]
    public async Task EnrollCourseAsync_NormalWithoutScoreOrSpecialWithScore_ReturnsBadRequest()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var normalNoScore = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = null
        });
        Assert.False(normalNoScore.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, normalNoScore.StatusCode);

        var absentWithScore = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Absent",
            Score = 80m
        });
        Assert.False(absentWithScore.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, absentWithScore.StatusCode);

        var withdrawnWithScore = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Withdrawn",
            Score = 80m
        });
        Assert.False(withdrawnWithScore.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, withdrawnWithScore.StatusCode);

        var incompleteWithScore = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Incomplete",
            Score = 80m
        });
        Assert.False(incompleteWithScore.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, incompleteWithScore.StatusCode);
    }

    [Fact]
    public async Task EnrollCourseAsync_DecimalScorePrecision_Resolves95Point99ToAInsteadOfAPlus()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var result = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = 95.99m
        });

        Assert.True(result.Succeeded);
        Assert.Equal(95.99m, result.Payload!.Score);
        Assert.Equal("A", result.Payload.Grade);
        Assert.Equal(3.7m, result.Payload.GradePoints);
    }

    [Fact]
    public async Task EnrollCourseAsync_CreditHoursCopiedAtRegistration_PreservesSnapshotWhenCourseUpdated()
    {
        var (db, enrollmentService, courseService, gpaService, studentId, semesterId, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var enrollmentResult = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = 90m // A-, 3.4 points * 3 hours = 10.2 QP, GPA = 3.40
        });
        Assert.True(enrollmentResult.Succeeded);
        Assert.Equal(3, enrollmentResult.Payload!.CreditHours);

        var courseUpdateResult = await courseService.UpdateCourseAsync(courseId, new UpdateCourseDto
        {
            Name = "Intro CS Renamed",
            CreditHours = 1
        });
        Assert.True(courseUpdateResult.Succeeded);
        Assert.Equal(1, courseUpdateResult.Payload!.CreditHours);

        var enrollmentInDb = await db.Context.Enrollments.FirstAsync(e => e.Id == enrollmentResult.Payload.Id);
        Assert.Equal(3, enrollmentInDb.CreditHours);

        var gpaResponse = await gpaService.CalculateGpaAsync(studentId);
        Assert.True(gpaResponse.Succeeded);
        Assert.Equal(3.40m, gpaResponse.Payload!.CumulativeGpa);
        Assert.Equal(3, gpaResponse.Payload.TotalRegisteredHours);
    }

    [Fact]
    public async Task UpdateEnrollmentAsync_SwitchingStatus_ClearsOrResolvesScoreAndGradingScaleId()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var created = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = 90m
        });
        Assert.True(created.Succeeded);
        Assert.NotNull(created.Payload!.GradingScaleId);

        var switchedToWithdrawn = await enrollmentService.UpdateEnrollmentAsync(created.Payload.Id, new UpdateEnrollmentDto
        {
            Status = "Withdrawn",
            Score = null
        });
        Assert.True(switchedToWithdrawn.Succeeded);
        Assert.Null(switchedToWithdrawn.Payload!.Score);
        Assert.Null(switchedToWithdrawn.Payload.GradingScaleId);
        Assert.Equal("W", switchedToWithdrawn.Payload.Grade);

        var switchedBackToNormal = await enrollmentService.UpdateEnrollmentAsync(created.Payload.Id, new UpdateEnrollmentDto
        {
            Status = "Normal",
            Score = 96m
        });
        Assert.True(switchedBackToNormal.Succeeded);
        Assert.Equal(96m, switchedBackToNormal.Payload!.Score);
        Assert.NotNull(switchedBackToNormal.Payload.GradingScaleId);
        Assert.Equal("A+", switchedBackToNormal.Payload.Grade);
        Assert.Equal(4.0m, switchedBackToNormal.Payload.GradePoints);
    }

    [Fact]
    public async Task CalculateGpaAsync_AfterEditingScore_ReflectsNewScoreOnDemand()
    {
        var (db, enrollmentService, _, gpaService, studentId, semesterId, courseId) = await SetupBaseAsync();
        await using var _ = db;

        var enrollmentResult = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = courseId,
            Status = "Normal",
            Score = 70m // C, 2.4 points -> GPA = 2.40
        });

        var initialGpa = await gpaService.CalculateGpaAsync(studentId);
        Assert.Equal(2.40m, initialGpa.Payload!.CumulativeGpa);

        await enrollmentService.UpdateEnrollmentAsync(enrollmentResult.Payload!.Id, new UpdateEnrollmentDto
        {
            Status = "Normal",
            Score = 96m // A+, 4.0 points -> GPA = 4.00
        });

        var recalculatedGpa = await gpaService.CalculateGpaAsync(studentId);
        Assert.Equal(4.00m, recalculatedGpa.Payload!.CumulativeGpa);
    }

    [Fact]
    public async Task EnrollCourseAsync_WhenDbUpdateConcurrencyExceptionOccurs_MapsToConflict409()
    {
        var (db, enrollmentService, _, _, studentId, semesterId, _) = await SetupBaseAsync();
        await using var _ = db;

        var course = new Course { Code = "CONCUR101", Name = "Concurrency Course", CreditHours = 2 };
        db.Context.Courses.Add(course);
        await db.Context.SaveChangesAsync();

        // Change the RowVersion directly in the SQLite database to simulate concurrent modification
        await db.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Students SET RowVersion = randomblob(8) WHERE Id = {0}", studentId);

        var result = await enrollmentService.EnrollCourseAsync(new CreateEnrollmentDto
        {
            StudentId = studentId,
            SemesterId = semesterId,
            CourseId = course.Id,
            Status = "Normal",
            Score = 85m
        });

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Contains("concurrency conflict", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
