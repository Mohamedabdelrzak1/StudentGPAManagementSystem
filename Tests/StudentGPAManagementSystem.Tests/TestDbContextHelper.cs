using System;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Data;
using Service;

namespace StudentGPAManagementSystem.Tests;

public static class TestDbContextHelper
{
    public static IMapper CreateTestMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    public static async Task<StudentGpaDbContext> CreateInMemoryDbContextAsync(string dbName)
    {
        var options = new DbContextOptionsBuilder<StudentGpaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new StudentGpaDbContext(options);

        // Seed Grading Scales
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

        if (!await context.GradingScales.AnyAsync())
        {
            await context.GradingScales.AddRangeAsync(scales);
            await context.SaveChangesAsync();
        }

        return context;
    }
}
