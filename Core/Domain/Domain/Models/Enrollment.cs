namespace Domain.Models;

public class Enrollment : BaseEntity<int>
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int CreditHours { get; set; }
    public EnrollmentStatus Status { get; set; }
    public decimal? Score { get; set; }

    public int? GradingScaleId { get; set; }
    public GradingScale? GradingScale { get; set; }
}
