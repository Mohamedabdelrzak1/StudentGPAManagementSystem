using System.ComponentModel.DataAnnotations;

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

public class CreateEnrollmentDto
{
    [Required(ErrorMessage = "StudentId is required.")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "SemesterId is required.")]
    public int SemesterId { get; set; }

    [Required(ErrorMessage = "CourseId is required.")]
    public int CourseId { get; set; }

    /// <summary>
    /// Status: "Normal", "Absent", "Withdrawn", "Incomplete". Defaults to "Normal".
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Numeric score between 0 and 100. Required when Status is Normal; must be null when Status is Absent, Withdrawn, or Incomplete.
    /// </summary>
    public decimal? Score { get; set; }
}

public class UpdateEnrollmentDto
{
    /// <summary>
    /// Status: "Normal", "Absent", "Withdrawn", "Incomplete". Defaults to "Normal".
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Numeric score between 0 and 100. Required when Status is Normal; must be null when Status is Absent, Withdrawn, or Incomplete.
    /// </summary>
    public decimal? Score { get; set; }
}
