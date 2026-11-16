using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Data;
using Persistence.Repositorys;
using Service;

namespace StudentGPAManagementSystem.Tests;

public class TestStudentGpaDbContext : StudentGpaDbContext
{
    public TestStudentGpaDbContext(DbContextOptions<StudentGpaDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>()
            .Property(s => s.RowVersion)
            .HasDefaultValueSql("randomblob(8)");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GenerateRowVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        GenerateRowVersions();
        return base.SaveChanges();
    }

    private void GenerateRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Student>())
        {
            if (entry.State == EntityState.Added && (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0))
            {
                var newVersion = new byte[8];
                RandomNumberGenerator.Fill(newVersion);
                entry.Entity.RowVersion = newVersion;
            }
        }
    }
}

public static class TestDbContextHelper
{
    private static readonly Lazy<IMapper> _lazyMapper = new(() =>
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    });

    public static IMapper CreateMapper() => _lazyMapper.Value;

    public static List<GradingScale> SeedGradingScales() =>
    [
        new GradingScale { Id = 1,  FromGrade = 96m, ToGrade = 101m, Points = 4.0m, Grade = "A+" },
        new GradingScale { Id = 2,  FromGrade = 92m, ToGrade = 96m,  Points = 3.7m, Grade = "A"  },
        new GradingScale { Id = 3,  FromGrade = 88m, ToGrade = 92m,  Points = 3.4m, Grade = "A-" },
        new GradingScale { Id = 4,  FromGrade = 84m, ToGrade = 88m,  Points = 3.2m, Grade = "B+" },
        new GradingScale { Id = 5,  FromGrade = 80m, ToGrade = 84m,  Points = 3.0m, Grade = "B"  },
        new GradingScale { Id = 6,  FromGrade = 76m, ToGrade = 80m,  Points = 2.8m, Grade = "B-" },
        new GradingScale { Id = 7,  FromGrade = 72m, ToGrade = 76m,  Points = 2.6m, Grade = "C+" },
        new GradingScale { Id = 8,  FromGrade = 68m, ToGrade = 72m,  Points = 2.4m, Grade = "C"  },
        new GradingScale { Id = 9,  FromGrade = 64m, ToGrade = 68m,  Points = 2.2m, Grade = "C-" },
        new GradingScale { Id = 10, FromGrade = 60m, ToGrade = 64m,  Points = 2.0m, Grade = "D+" },
        new GradingScale { Id = 11, FromGrade = 55m, ToGrade = 60m,  Points = 1.5m, Grade = "D"  },
        new GradingScale { Id = 12, FromGrade = 50m, ToGrade = 55m,  Points = 1.0m, Grade = "D-" },
        new GradingScale { Id = 13, FromGrade = 0m,  ToGrade = 50m,  Points = 0.0m, Grade = "F"  }
    ];

    public static SqliteTestDatabase CreateDatabase() => new();
}

public class SqliteTestDatabase : IAsyncDisposable, IDisposable
{
    public SqliteConnection Connection { get; }
    public TestStudentGpaDbContext Context { get; }
    public UnitOfWork UnitOfWork { get; }
    public IMapper Mapper { get; }

    public SqliteTestDatabase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<StudentGpaDbContext>()
            .UseSqlite(Connection)
            .Options;

        Context = new TestStudentGpaDbContext(options);
        Context.Database.EnsureCreated();

        UnitOfWork = new UnitOfWork(Context);
        Mapper = TestDbContextHelper.CreateMapper();
    }

    public async Task SeedStandardGradingScalesAsync()
    {
        if (!await Context.GradingScales.AnyAsync())
        {
            Context.GradingScales.AddRange(TestDbContextHelper.SeedGradingScales());
            await Context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
