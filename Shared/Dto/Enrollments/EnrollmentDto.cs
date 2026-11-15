namespace Shared.Dto;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;

    public int CreditHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }

    public int? GradingScaleId { get; set; }
    public string? Grade { get; set; }
    public decimal? GradePoints { get; set; }
    public decimal? QualityPoints { get; set; }
    public bool IncludedInGpa { get; set; }
}
