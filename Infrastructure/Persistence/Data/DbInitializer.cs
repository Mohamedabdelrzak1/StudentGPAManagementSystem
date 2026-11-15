using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Data;

public static class DbInitializer
{
    public static async Task SeedSampleDataAsync(StudentGpaDbContext context)
    {
        // 1. Ensure Database & Grading Scales exist
        if (!await context.GradingScales.AnyAsync())
        {
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
            var semesters = new[]
            {
                new Semester { Name = "Fall 2023", StartDate = new DateOnly(2023, 9, 1), EndDate = new DateOnly(2024, 1, 15) },
                new Semester { Name = "Spring 2024", StartDate = new DateOnly(2024, 2, 1), EndDate = new DateOnly(2024, 6, 15) },
                new Semester { Name = "Summer 2024", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 8, 31) },
                new Semester { Name = "Fall 2024", StartDate = new DateOnly(2024, 9, 1), EndDate = new DateOnly(2025, 1, 15) },
                new Semester { Name = "Spring 2025", StartDate = new DateOnly(2025, 2, 1), EndDate = new DateOnly(2025, 6, 15) }
            };

            await context.Semesters.AddRangeAsync(semesters);
            await context.SaveChangesAsync();
        }

        // 3. Seed Courses if none exist (all 1 to 3 credit hours)
        if (!await context.Courses.AnyAsync())
        {
            var courses = new[]
            {
                // Computer Science
                new Course { Code = "CS101", Name = "Introduction to Computer Science", CreditHours = 3 },
                new Course { Code = "CS102", Name = "Object-Oriented Programming", CreditHours = 3 },
                new Course { Code = "CS201", Name = "Data Structures & Algorithms", CreditHours = 3 },
                new Course { Code = "CS202", Name = "Database Management Systems", CreditHours = 3 },
                new Course { Code = "CS301", Name = "Web Application Development", CreditHours = 3 },
                new Course { Code = "CS302", Name = "Operating Systems", CreditHours = 3 },
                new Course { Code = "CS303", Name = "Computer Networks", CreditHours = 3 },
                new Course { Code = "CS304", Name = "Software Engineering Principles", CreditHours = 3 },
                new Course { Code = "CS401", Name = "Artificial Intelligence", CreditHours = 3 },
                new Course { Code = "CS402", Name = "Cloud Computing & DevOps", CreditHours = 3 },

                // Mathematics
                new Course { Code = "MATH101", Name = "Calculus I", CreditHours = 2 },
                new Course { Code = "MATH102", Name = "Linear Algebra", CreditHours = 2 },
                new Course { Code = "MATH201", Name = "Probability & Statistics", CreditHours = 3 },
                new Course { Code = "MATH202", Name = "Discrete Mathematics", CreditHours = 2 },

                // Basic Sciences
                new Course { Code = "PHY101", Name = "General Physics I", CreditHours = 2 },
                new Course { Code = "PHY102", Name = "General Physics II", CreditHours = 2 },

                // Languages & Communications
                new Course { Code = "ENG101", Name = "English Composition", CreditHours = 3 },
                new Course { Code = "ENG102", Name = "Technical Report Writing", CreditHours = 2 },

                // Information Technology
                new Course { Code = "IT101", Name = "Information Technology Fundamentals", CreditHours = 3 },
                new Course { Code = "IT201", Name = "Cybersecurity Essentials", CreditHours = 2 },

                // Humanities & Management
                new Course { Code = "BUS101", Name = "Principles of Project Management", CreditHours = 2 },
                new Course { Code = "BUS102", Name = "Engineering Economics", CreditHours = 2 },
                new Course { Code = "HUM101", Name = "Critical Thinking & Problem Solving", CreditHours = 1 },
                new Course { Code = "HUM102", Name = "Professional Ethics & Law", CreditHours = 1 },

                // Capstone Projects
                new Course { Code = "PRJ401", Name = "Graduation Capstone Project I", CreditHours = 2 },
                new Course { Code = "PRJ402", Name = "Graduation Capstone Project II", CreditHours = 3 }
            };

            await context.Courses.AddRangeAsync(courses);
            await context.SaveChangesAsync();
        }

        // 4. Seed Students with Egyptian Names if none exist
        if (!await context.Students.AnyAsync())
        {
            var students = new[]
            {
                new Student { Code = "STU001", Name = "أحمد محمد علي" },
                new Student { Code = "STU002", Name = "سارة محمود خليل" },
                new Student { Code = "STU003", Name = "عمر خالد عبد الرحمن" },
                new Student { Code = "STU004", Name = "مريم طارق فؤاد" },
                new Student { Code = "STU005", Name = "يوسف إبراهيم حسن" },
                new Student { Code = "STU006", Name = "منى كمال الشناوي" },
                new Student { Code = "STU007", Name = "مصطفى أحمد النجار" },
                new Student { Code = "STU008", Name = "نورهان عادل منصور" },
                new Student { Code = "STU009", Name = "زياد شريف الديب" },
                new Student { Code = "STU010", Name = "فاطمة حسن السعيد" }
            };

            await context.Students.AddRangeAsync(students);
            await context.SaveChangesAsync();

            // Load lookups
            var allSemesters = await context.Semesters.ToDictionaryAsync(s => s.Name);
            var allCourses = await context.Courses.ToDictionaryAsync(c => c.Code);
            var allGradingScales = await context.GradingScales.ToListAsync();

            int? ResolveScaleId(decimal? score)
            {
                if (!score.HasValue) return null;
                var found = allGradingScales.FirstOrDefault(g => score.Value >= g.FromGrade && score.Value < g.ToGrade);
                return found?.Id;
            }

            var enrollmentsToSeed = new List<Enrollment>();

            // -----------------------------------------------------------------------------------
            // Student 1: أحمد محمد علي (STU001) - Matches exact technical assessment verification example
            // -----------------------------------------------------------------------------------
            var s1 = students[0];
            var semFall24 = allSemesters["Fall 2024"];
            var semSpring25 = allSemesters["Spring 2025"];

            // Semester 1: Fall 2024 (13 registered hrs, 8 GPA-eligible hrs, 16.6 QP -> GPA = 2.08)
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 90m, GradingScaleId = ResolveScaleId(90m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 85m, GradingScaleId = ResolveScaleId(85m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semFall24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Absent, Score = null, GradingScaleId = null });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semFall24.Id, CourseId = allCourses["PHY101"].Id, CreditHours = 2, Status = EnrollmentStatus.Withdrawn, Score = null, GradingScaleId = null });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semFall24.Id, CourseId = allCourses["IT101"].Id, CreditHours = 3, Status = EnrollmentStatus.Incomplete, Score = null, GradingScaleId = null });

            // Semester 2: Spring 2025 (5 registered hrs, 5 GPA-eligible hrs, 16.8 QP -> GPA = 3.36, Cumulative GPA = 2.57)
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semSpring25.Id, CourseId = allCourses["CS102"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 96m, GradingScaleId = ResolveScaleId(96m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s1.Id, SemesterId = semSpring25.Id, CourseId = allCourses["MATH102"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 70m, GradingScaleId = ResolveScaleId(70m) });

            // -----------------------------------------------------------------------------------
            // Student 2: سارة محمود خليل (STU002) - Honors Student (Fall 2023, Spring 2024, Fall 2024)
            // -----------------------------------------------------------------------------------
            var s2 = students[1];
            var semFall23 = allSemesters["Fall 2023"];
            var semSpring24 = allSemesters["Spring 2024"];

            // Fall 2023
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall23.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 98m, GradingScaleId = ResolveScaleId(98m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall23.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 94m, GradingScaleId = ResolveScaleId(94m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall23.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 91m, GradingScaleId = ResolveScaleId(91m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall23.Id, CourseId = allCourses["HUM101"].Id, CreditHours = 1, Status = EnrollmentStatus.Normal, Score = 97m, GradingScaleId = ResolveScaleId(97m) });

            // Spring 2024
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semSpring24.Id, CourseId = allCourses["CS102"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 95m, GradingScaleId = ResolveScaleId(95m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semSpring24.Id, CourseId = allCourses["MATH102"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 92m, GradingScaleId = ResolveScaleId(92m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semSpring24.Id, CourseId = allCourses["PHY101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 89m, GradingScaleId = ResolveScaleId(89m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semSpring24.Id, CourseId = allCourses["ENG102"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 93m, GradingScaleId = ResolveScaleId(93m) });

            // Fall 2024
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS201"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 96m, GradingScaleId = ResolveScaleId(96m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS202"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 94m, GradingScaleId = ResolveScaleId(94m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s2.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH201"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 88m, GradingScaleId = ResolveScaleId(88m) });

            // -----------------------------------------------------------------------------------
            // Student 3: عمر خالد عبد الرحمن (STU003) - Heavy semester (6 courses = 16 hours)
            // -----------------------------------------------------------------------------------
            var s3 = students[2];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 82m, GradingScaleId = ResolveScaleId(82m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 78m, GradingScaleId = ResolveScaleId(78m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["PHY101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 75m, GradingScaleId = ResolveScaleId(75m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["IT101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 85m, GradingScaleId = ResolveScaleId(85m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 80m, GradingScaleId = ResolveScaleId(80m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["BUS101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 88m, GradingScaleId = ResolveScaleId(88m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s3.Id, SemesterId = semFall24.Id, CourseId = allCourses["HUM101"].Id, CreditHours = 1, Status = EnrollmentStatus.Normal, Score = 90m, GradingScaleId = ResolveScaleId(90m) });

            // -----------------------------------------------------------------------------------
            // Student 4: مريم طارق فؤاد (STU004) - Multi-semester with special statuses
            // -----------------------------------------------------------------------------------
            var s4 = students[3];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s4.Id, SemesterId = semSpring24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 87m, GradingScaleId = ResolveScaleId(87m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s4.Id, SemesterId = semSpring24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 84m, GradingScaleId = ResolveScaleId(84m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s4.Id, SemesterId = semSpring24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Withdrawn, Score = null, GradingScaleId = null });

            enrollmentsToSeed.Add(new Enrollment { StudentId = s4.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS102"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 89m, GradingScaleId = ResolveScaleId(89m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s4.Id, SemesterId = semFall24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 92m, GradingScaleId = ResolveScaleId(92m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s4.Id, SemesterId = semFall24.Id, CourseId = allCourses["IT101"].Id, CreditHours = 3, Status = EnrollmentStatus.Incomplete, Score = null, GradingScaleId = null });

            // -----------------------------------------------------------------------------------
            // Student 5: يوسف إبراهيم حسن (STU005) - Advanced Courses in Fall 2024 & Spring 2025
            // -----------------------------------------------------------------------------------
            var s5 = students[4];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS201"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 91m, GradingScaleId = ResolveScaleId(91m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS202"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 86m, GradingScaleId = ResolveScaleId(86m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH201"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 79m, GradingScaleId = ResolveScaleId(79m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semFall24.Id, CourseId = allCourses["BUS102"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 83m, GradingScaleId = ResolveScaleId(83m) });

            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semSpring25.Id, CourseId = allCourses["CS301"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 95m, GradingScaleId = ResolveScaleId(95m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semSpring25.Id, CourseId = allCourses["CS302"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 88m, GradingScaleId = ResolveScaleId(88m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s5.Id, SemesterId = semSpring25.Id, CourseId = allCourses["CS303"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 90m, GradingScaleId = ResolveScaleId(90m) });

            // -----------------------------------------------------------------------------------
            // Student 6: منى كمال الشناوي (STU006) - Consistent B+/A- Student
            // -----------------------------------------------------------------------------------
            var s6 = students[5];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s6.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 86m, GradingScaleId = ResolveScaleId(86m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s6.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 84m, GradingScaleId = ResolveScaleId(84m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s6.Id, SemesterId = semFall24.Id, CourseId = allCourses["PHY101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 82m, GradingScaleId = ResolveScaleId(82m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s6.Id, SemesterId = semFall24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 88m, GradingScaleId = ResolveScaleId(88m) });

            // -----------------------------------------------------------------------------------
            // Student 7: مصطفى أحمد النجار (STU007) - Fall 2024 with Absent course
            // -----------------------------------------------------------------------------------
            var s7 = students[6];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s7.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 74m, GradingScaleId = ResolveScaleId(74m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s7.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 68m, GradingScaleId = ResolveScaleId(68m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s7.Id, SemesterId = semFall24.Id, CourseId = allCourses["PHY101"].Id, CreditHours = 2, Status = EnrollmentStatus.Absent, Score = null, GradingScaleId = null });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s7.Id, SemesterId = semFall24.Id, CourseId = allCourses["HUM102"].Id, CreditHours = 1, Status = EnrollmentStatus.Normal, Score = 90m, GradingScaleId = ResolveScaleId(90m) });

            // -----------------------------------------------------------------------------------
            // Student 8: نورهان عادل منصور (STU008) - Outstanding Freshman
            // -----------------------------------------------------------------------------------
            var s8 = students[7];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s8.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 99m, GradingScaleId = ResolveScaleId(99m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s8.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 97m, GradingScaleId = ResolveScaleId(97m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s8.Id, SemesterId = semFall24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 94m, GradingScaleId = ResolveScaleId(94m) });

            // -----------------------------------------------------------------------------------
            // Student 9: زياد شريف الديب (STU009) - Senior Student (Capstone Projects)
            // -----------------------------------------------------------------------------------
            var s9 = students[8];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s9.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS304"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 88m, GradingScaleId = ResolveScaleId(88m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s9.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS401"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 91m, GradingScaleId = ResolveScaleId(91m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s9.Id, SemesterId = semFall24.Id, CourseId = allCourses["PRJ401"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 96m, GradingScaleId = ResolveScaleId(96m) });

            enrollmentsToSeed.Add(new Enrollment { StudentId = s9.Id, SemesterId = semSpring25.Id, CourseId = allCourses["CS402"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 93m, GradingScaleId = ResolveScaleId(93m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s9.Id, SemesterId = semSpring25.Id, CourseId = allCourses["PRJ402"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 97m, GradingScaleId = ResolveScaleId(97m) });

            // -----------------------------------------------------------------------------------
            // Student 10: فاطمة حسن السعيد (STU010) - High Achiever
            // -----------------------------------------------------------------------------------
            var s10 = students[9];
            enrollmentsToSeed.Add(new Enrollment { StudentId = s10.Id, SemesterId = semFall24.Id, CourseId = allCourses["CS101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 94m, GradingScaleId = ResolveScaleId(94m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s10.Id, SemesterId = semFall24.Id, CourseId = allCourses["MATH101"].Id, CreditHours = 2, Status = EnrollmentStatus.Normal, Score = 91m, GradingScaleId = ResolveScaleId(91m) });
            enrollmentsToSeed.Add(new Enrollment { StudentId = s10.Id, SemesterId = semFall24.Id, CourseId = allCourses["ENG101"].Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 87m, GradingScaleId = ResolveScaleId(87m) });

            await context.Enrollments.AddRangeAsync(enrollmentsToSeed);
            await context.SaveChangesAsync();
        }
    }
}
