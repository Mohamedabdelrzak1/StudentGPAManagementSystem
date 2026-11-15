using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;

namespace Persistence.Data;

public static class DbInitializer
{
    public static async Task SeedSampleDataAsync(StudentGpaDbContext context)
    {
        // 1. Ensure Database & Grading Scales exist
        if (!await context.GradingScales.AnyAsync())
        {
            // Seed grading scales if not populated by EF HasData
            var scales = new[]
            {
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
            };
            await context.GradingScales.AddRangeAsync(scales);
            await context.SaveChangesAsync();
        }

        // 2. Seed Semesters if none exist
        if (!await context.Semesters.AnyAsync())
        {
            var sem1 = new Semester { Name = "Fall 2024", StartDate = new DateOnly(2024, 9, 1), EndDate = new DateOnly(2025, 1, 15) };
            var sem2 = new Semester { Name = "Spring 2025", StartDate = new DateOnly(2025, 2, 1), EndDate = new DateOnly(2025, 6, 15) };

            await context.Semesters.AddRangeAsync(sem1, sem2);
            await context.SaveChangesAsync();
        }

        // 3. Seed Courses if none exist
        if (!await context.Courses.AnyAsync())
        {
            var courses = new[]
            {
                new Course { Code = "CS101", Name = "Introduction to Computer Science", CreditHours = 3 },
                new Course { Code = "MATH101", Name = "Calculus I", CreditHours = 2 },
                new Course { Code = "ENG101", Name = "English Composition", CreditHours = 3 },
                new Course { Code = "PHY101", Name = "General Physics", CreditHours = 2 },
                new Course { Code = "IT101", Name = "Information Technology Fundamentals", CreditHours = 3 },
                new Course { Code = "CS102", Name = "Data Structures & Algorithms", CreditHours = 3 },
                new Course { Code = "MATH102", Name = "Linear Algebra", CreditHours = 2 }
            };

            await context.Courses.AddRangeAsync(courses);
            await context.SaveChangesAsync();
        }

        // 4. Seed Students if none exist
        if (!await context.Students.AnyAsync())
        {
            var s1 = new Student { Code = "STU001", Name = "John Doe" };
            var s2 = new Student { Code = "STU002", Name = "Sarah Smith" };

            await context.Students.AddRangeAsync(s1, s2);
            await context.SaveChangesAsync();

            // Fetch references
            var sem1 = await context.Semesters.FirstAsync(s => s.Name == "Fall 2024");
            var sem2 = await context.Semesters.FirstAsync(s => s.Name == "Spring 2025");

            var cs101 = await context.Courses.FirstAsync(c => c.Code == "CS101");
            var math101 = await context.Courses.FirstAsync(c => c.Code == "MATH101");
            var eng101 = await context.Courses.FirstAsync(c => c.Code == "ENG101");
            var phy101 = await context.Courses.FirstAsync(c => c.Code == "PHY101");
            var it101 = await context.Courses.FirstAsync(c => c.Code == "IT101");

            var cs102 = await context.Courses.FirstAsync(c => c.Code == "CS102");
            var math102 = await context.Courses.FirstAsync(c => c.Code == "MATH102");

            var gradeAMinus = await context.GradingScales.FirstAsync(g => g.Grade == "A-");
            var gradeBPlus = await context.GradingScales.FirstAsync(g => g.Grade == "B+");
            var gradeAPlus = await context.GradingScales.FirstAsync(g => g.Grade == "A+");
            var gradeA = await context.GradingScales.FirstAsync(g => g.Grade == "A");
            var gradeB = await context.GradingScales.FirstAsync(g => g.Grade == "B");
            var gradeC = await context.GradingScales.FirstAsync(g => g.Grade == "C");

            // Seed Student 1 (matches exact assessment verification example)
            // Semester 1: CS101(90, A-), MATH101(85, B+), ENG101(Absent), PHY101(Withdrawn), IT101(Incomplete)
            var s1Enrollments = new[]
            {
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem1.Id,
                    CourseId = cs101.Id,
                    CreditHours = cs101.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 90m,
                    GradingScaleId = gradeAMinus.Id
                },
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem1.Id,
                    CourseId = math101.Id,
                    CreditHours = math101.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 85m,
                    GradingScaleId = gradeBPlus.Id
                },
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem1.Id,
                    CourseId = eng101.Id,
                    CreditHours = eng101.CreditHours,
                    Status = EnrollmentStatus.Absent,
                    Score = null,
                    GradingScaleId = null
                },
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem1.Id,
                    CourseId = phy101.Id,
                    CreditHours = phy101.CreditHours,
                    Status = EnrollmentStatus.Withdrawn,
                    Score = null,
                    GradingScaleId = null
                },
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem1.Id,
                    CourseId = it101.Id,
                    CreditHours = it101.CreditHours,
                    Status = EnrollmentStatus.Incomplete,
                    Score = null,
                    GradingScaleId = null
                },
                // Semester 2: CS102(96, A+), MATH102(70, C)
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem2.Id,
                    CourseId = cs102.Id,
                    CreditHours = cs102.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 96m,
                    GradingScaleId = gradeAPlus.Id
                },
                new Enrollment
                {
                    StudentId = s1.Id,
                    SemesterId = sem2.Id,
                    CourseId = math102.Id,
                    CreditHours = math102.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 70m,
                    GradingScaleId = gradeC.Id
                }
            };

            // Seed Student 2 (another student across multiple semesters)
            var s2Enrollments = new[]
            {
                new Enrollment
                {
                    StudentId = s2.Id,
                    SemesterId = sem1.Id,
                    CourseId = cs101.Id,
                    CreditHours = cs101.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 94m,
                    GradingScaleId = gradeA.Id
                },
                new Enrollment
                {
                    StudentId = s2.Id,
                    SemesterId = sem1.Id,
                    CourseId = math101.Id,
                    CreditHours = math101.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 89m,
                    GradingScaleId = gradeAMinus.Id
                },
                new Enrollment
                {
                    StudentId = s2.Id,
                    SemesterId = sem1.Id,
                    CourseId = eng101.Id,
                    CreditHours = eng101.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 82m,
                    GradingScaleId = gradeB.Id
                },
                new Enrollment
                {
                    StudentId = s2.Id,
                    SemesterId = sem2.Id,
                    CourseId = cs102.Id,
                    CreditHours = cs102.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 88m,
                    GradingScaleId = gradeAMinus.Id
                },
                new Enrollment
                {
                    StudentId = s2.Id,
                    SemesterId = sem2.Id,
                    CourseId = math102.Id,
                    CreditHours = math102.CreditHours,
                    Status = EnrollmentStatus.Normal,
                    Score = 97m,
                    GradingScaleId = gradeAPlus.Id
                }
            };

            await context.Enrollments.AddRangeAsync(s1Enrollments);
            await context.Enrollments.AddRangeAsync(s2Enrollments);
            await context.SaveChangesAsync();
        }
    }
}
