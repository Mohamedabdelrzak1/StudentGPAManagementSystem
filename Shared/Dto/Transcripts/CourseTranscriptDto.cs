namespace Shared.Dto;

public class CourseTranscriptDto
{
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CreditHours { get; set; }

    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }

    public string DisplayedGrade { get; set; } = string.Empty;
    public decimal? Points { get; set; }
    public decimal? QualityPoints { get; set; }
    public bool IncludedInGpa { get; set; }
}
