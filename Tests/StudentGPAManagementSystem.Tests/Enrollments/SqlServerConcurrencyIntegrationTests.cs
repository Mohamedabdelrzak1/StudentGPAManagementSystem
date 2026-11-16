using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Data;
using Persistence.Repositorys;
using Service;
using Shared.Dto;
using Xunit;

namespace StudentGPAManagementSystem.Tests.Enrollments;

[Trait("Category", "Integration")]
public class SqlServerConcurrencyIntegrationTests
{
    private static async Task<(string? BaseConn, string? FailureReason)> ResolveAccessibleSqlServerAsync()
    {
        string? envConn = Environment.GetEnvironmentVariable("TEST_SQLSERVER");
        string[] candidates = !string.IsNullOrWhiteSpace(envConn)
            ? new[] { envConn }
            : new[]
            {
                "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;",
                "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
            };

        string lastError = string.Empty;
        foreach (var candidate in candidates)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(candidate)
                {
                    InitialCatalog = "master",
                    ConnectTimeout = 3
                };

                await using var conn = new SqlConnection(builder.ConnectionString);
                await conn.OpenAsync();
                return (candidate, null);
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return (null, lastError);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EnrollCourseAsync_ConcurrentRegistrationsExceeding18HoursOnSqlServer_OneSucceedsAndOneReturnsConflict409()
    {
        // 1. Resolve reachable SQL Server instance or skip gracefully
        var (baseConn, failureReason) = await ResolveAccessibleSqlServerAsync();
        if (baseConn == null)
        {
            // xUnit 2.x has no public skip API; return early so the test reports Passed (not Failed).
            // Run with --filter "Category=Integration" to include; it is effectively skipped when
            // TEST_SQLSERVER is not set and no local SQL Server instance is reachable.
            Console.WriteLine($"[SKIPPED] SQL Server is unreachable ({failureReason}). Test not executed.");
            return;
        }

        // 2. Build isolated temporary database name and connection string
        var tempDbName = $"StudentGpaDb_Tests_{Guid.NewGuid():N}";
        var connBuilder = new SqlConnectionStringBuilder(baseConn)
        {
            InitialCatalog = tempDbName,
            TrustServerCertificate = true
        };
        string testConnectionString = connBuilder.ConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<StudentGpaDbContext>()
            .UseSqlServer(testConnectionString);

        try
        {
            // 3. Migrate and seed the temporary database
            await using (var db = new StudentGpaDbContext(optionsBuilder.Options))
            {
                await db.Database.MigrateAsync();
                await DbInitializer.SeedSampleDataAsync(db);
            }

            // 4. Setup isolated test data inside temporary database
            int studentId;
            int semesterId;
            int raceACourseId;
            int raceBCourseId;

            await using (var db = new StudentGpaDbContext(optionsBuilder.Options))
            {
                var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
                var student = new Student { Code = $"STU_CON_{uniqueSuffix}", Name = "Concurrency Test Student" };
                var semester = new Semester { Name = $"Sem_CON_{uniqueSuffix}", StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 1, 15) };
                var c1 = new Course { Code = $"C1_{uniqueSuffix}", Name = "Course 1", CreditHours = 3 };
                var c2 = new Course { Code = $"C2_{uniqueSuffix}", Name = "Course 2", CreditHours = 3 };
                var c3 = new Course { Code = $"C3_{uniqueSuffix}", Name = "Course 3", CreditHours = 3 };
                var c4 = new Course { Code = $"C4_{uniqueSuffix}", Name = "Course 4", CreditHours = 3 };
                var c5 = new Course { Code = $"C5_{uniqueSuffix}", Name = "Course 5", CreditHours = 3 };
                var cRaceA = new Course { Code = $"RA_{uniqueSuffix}", Name = "Race Course A", CreditHours = 3 };
                var cRaceB = new Course { Code = $"RB_{uniqueSuffix}", Name = "Race Course B", CreditHours = 3 };

                db.Students.Add(student);
                db.Semesters.Add(semester);
                db.Courses.AddRange(c1, c2, c3, c4, c5, cRaceA, cRaceB);
                await db.SaveChangesAsync();

                var scale = await db.GradingScales.FirstAsync(g => g.Grade == "B");

                // Seed 15 hours (5 courses x 3 credit hours)
                db.Enrollments.AddRange(
                    new Enrollment { StudentId = student.Id, SemesterId = semester.Id, CourseId = c1.Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 80m, GradingScaleId = scale.Id },
                    new Enrollment { StudentId = student.Id, SemesterId = semester.Id, CourseId = c2.Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 80m, GradingScaleId = scale.Id },
                    new Enrollment { StudentId = student.Id, SemesterId = semester.Id, CourseId = c3.Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 80m, GradingScaleId = scale.Id },
                    new Enrollment { StudentId = student.Id, SemesterId = semester.Id, CourseId = c4.Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 80m, GradingScaleId = scale.Id },
                    new Enrollment { StudentId = student.Id, SemesterId = semester.Id, CourseId = c5.Id, CreditHours = 3, Status = EnrollmentStatus.Normal, Score = 80m, GradingScaleId = scale.Id }
                );
                await db.SaveChangesAsync();

                studentId = student.Id;
                semesterId = semester.Id;
                raceACourseId = cRaceA.Id;
                raceBCourseId = cRaceB.Id;
            }

            // 5. Configure DI container connected to temporary database
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<StudentGpaDbContext>(opt => opt.UseSqlServer(testConnectionString));
            services.AddScoped<Domain.Contracts.IUnitOfWork, UnitOfWork>();
            services.AddApplicationServices();
            var sp = services.BuildServiceProvider();

            // 6. Fire two concurrent registration requests at the exact same millisecond
            var taskA = Task.Run(async () =>
            {
                await using var scopeA = sp.CreateAsyncScope();
                var enrollSvc = scopeA.ServiceProvider.GetRequiredService<ServiceAbstraction.IEnrollmentService>();
                return await enrollSvc.EnrollCourseAsync(new CreateEnrollmentDto
                {
                    StudentId = studentId,
                    SemesterId = semesterId,
                    CourseId = raceACourseId,
                    Status = "Normal",
                    Score = 85m
                });
            });

            var taskB = Task.Run(async () =>
            {
                await using var scopeB = sp.CreateAsyncScope();
                var enrollSvc = scopeB.ServiceProvider.GetRequiredService<ServiceAbstraction.IEnrollmentService>();
                return await enrollSvc.EnrollCourseAsync(new CreateEnrollmentDto
                {
                    StudentId = studentId,
                    SemesterId = semesterId,
                    CourseId = raceBCourseId,
                    Status = "Normal",
                    Score = 85m
                });
            });

            var results = await Task.WhenAll(taskA, taskB);

            // 7. Assert: Exactly one succeeded (201 Created), one was rejected with Concurrency Conflict (409 Conflict)
            int successCount = results.Count(r => r.Succeeded && r.StatusCode == HttpStatusCode.Created);
            int conflictCount = results.Count(r => !r.Succeeded && r.StatusCode == HttpStatusCode.Conflict);

            Assert.Equal(1, successCount);
            Assert.Equal(1, conflictCount);

            // 8. Assert: Total hours in temporary database must be strictly 18, never 21
            await using (var verifyDb = new StudentGpaDbContext(optionsBuilder.Options))
            {
                var finalEnrollments = await verifyDb.Enrollments
                    .Where(e => e.StudentId == studentId && e.SemesterId == semesterId)
                    .ToListAsync();

                int totalFinalHours = finalEnrollments.Sum(e => e.CreditHours);
                Assert.Equal(18, totalFinalHours);
                Assert.Equal(6, finalEnrollments.Count);
            }
        }
        finally
        {
            // 9. Teardown: ensure temporary test database is dropped completely
            try
            {
                await using var dropDb = new StudentGpaDbContext(optionsBuilder.Options);
                await dropDb.Database.EnsureDeletedAsync();
            }
            catch
            {
                // Silently ignore teardown exceptions
            }
        }
    }
}
