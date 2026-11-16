using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Service;
using StudentGPAManagementSystem.Tests;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Dashboard;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardSummaryAsync_CalculatesMetricsAndDistributionCorrectly()
    {
        // Arrange
        await using var db = TestDbContextHelper.CreateDatabase();

        // Seed 2 courses
        var course1 = new Course { Code = "CS101", Name = "Intro CS", CreditHours = 3 };
        var course2 = new Course { Code = "MATH101", Name = "Calculus", CreditHours = 3 };
        db.Context.Courses.AddRange(course1, course2);

        // Seed 1 semester
        var semester = new Semester
        {
            Name = "Fall 2024",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 1, 15)
        };
        db.Context.Semesters.Add(semester);

        // Seed 2 students
        var student1 = new Student { Code = "STU01", Name = "Student High GPA" };
        var student2 = new Student { Code = "STU02", Name = "Student Low GPA" };
        db.Context.Students.AddRange(student1, student2);
        await db.Context.SaveChangesAsync();

        var scaleA = await db.Context.GradingScales.FirstAsync(g => g.Grade == "A+"); // 4.0 points
        var scaleC = await db.Context.GradingScales.FirstAsync(g => g.Grade == "C");  // 2.4 points

        // Student 1: 4.0 GPA (Honor student)
        db.Context.Enrollments.Add(new Enrollment
        {
            StudentId = student1.Id,
            SemesterId = semester.Id,
            CourseId = course1.Id,
            CreditHours = 3,
            Status = EnrollmentStatus.Normal,
            Score = 98m,
            GradingScaleId = scaleA.Id
        });

        // Student 2: 2.4 GPA
        db.Context.Enrollments.Add(new Enrollment
        {
            StudentId = student2.Id,
            SemesterId = semester.Id,
            CourseId = course2.Id,
            CreditHours = 3,
            Status = EnrollmentStatus.Normal,
            Score = 70m,
            GradingScaleId = scaleC.Id
        });
        await db.Context.SaveChangesAsync();

        var service = new DashboardService(db.UnitOfWork, db.Mapper);

        // Act
        var response = await service.GetDashboardSummaryAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Succeeded);
        Assert.NotNull(response.Payload);

        var dto = response.Payload;
        Assert.Equal(2, dto.TotalStudents);
        Assert.Equal(1, dto.TotalSemesters);
        Assert.Equal(0, dto.CompletedSemestersCount);
        Assert.Equal(2, dto.ActiveCoursesCount);
        Assert.Equal(3.20m, dto.InstituteAverageGpa); // (4.0 + 2.4) / 2 = 3.20
        Assert.Equal(1, dto.HonorStudentsCount);      // student 1 >= 3.5
        Assert.Equal(50.0m, dto.HonorStudentsPercentage);
        Assert.Single(dto.HonorStudents);
        Assert.Equal("STU01", dto.HonorStudents[0].StudentCode);
        Assert.Equal("Student High GPA", dto.HonorStudents[0].StudentName);
        Assert.Equal(4.00m, dto.HonorStudents[0].CumulativeGpa);

        // Students list
        Assert.Equal(2, dto.Students.Count);
        Assert.Equal("STU01", dto.Students[0].Code);
        Assert.Equal(4.00m, dto.Students[0].Gpa);
        Assert.Equal("متفوق", dto.Students[0].Status);
        Assert.Equal("علوم الحاسب", dto.Students[0].Major);

        // Performance trend
        Assert.Single(dto.PerformanceTrend);
        Assert.Equal("Fall 2025", dto.PerformanceTrend[0].SemesterName);
        Assert.Equal(3.20m, dto.PerformanceTrend[0].AverageGpa);
        Assert.Equal(2, dto.PerformanceTrend[0].EnrolledStudentsCount);
        Assert.NotNull(dto.CurrentSemester);
        Assert.Equal("Fall 2025", dto.CurrentSemester.Name);

        // Grade distribution
        Assert.Equal(2, dto.GradeDistribution.TotalStudentsEvaluated);
        Assert.Equal(1, dto.GradeDistribution.Excellent.Count); // 4.0 >= 3.40
        Assert.Equal(50.0m, dto.GradeDistribution.Excellent.Percentage);
        Assert.Equal(1, dto.GradeDistribution.Good.Count);      // 2.4 is between 2.20 and 2.79
        Assert.Equal(50.0m, dto.GradeDistribution.Good.Percentage);
    }
}
