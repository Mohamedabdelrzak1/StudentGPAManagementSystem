using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Data;

public class StudentGpaDbContext : DbContext
{
    public StudentGpaDbContext(DbContextOptions<StudentGpaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<GradingScale> GradingScales => Set<GradingScale>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyReference).Assembly);
    }
}
