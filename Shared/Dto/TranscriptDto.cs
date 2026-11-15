using System.Collections.Generic;

namespace Shared.Dto;

public class TranscriptDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    public decimal? CumulativeGpa { get; set; }
    public string CumulativeGpaDisplay { get; set; } = "N/A";

    public int TotalRegisteredHours { get; set; }
    public int TotalGpaEligibleHours { get; set; }
    public decimal TotalQualityPoints { get; set; }

    public List<SemesterTranscriptDto> Semesters { get; set; } = new();
}

public class SemesterTranscriptDto
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;

    public decimal? SemesterGpa { get; set; }
    public string SemesterGpaDisplay { get; set; } = "N/A";

    public int RegisteredHours { get; set; }
    public int GpaEligibleHours { get; set; }
    public decimal TotalQualityPoints { get; set; }

    public List<CourseTranscriptDto> Courses { get; set; } = new();
}

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

public class GpaResultDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    public decimal? CumulativeGpa { get; set; }
    public string CumulativeGpaDisplay { get; set; } = "N/A";

    public int TotalRegisteredHours { get; set; }
    public int TotalGpaEligibleHours { get; set; }
    public decimal TotalQualityPoints { get; set; }

    public List<SemesterGpaSummaryDto> Semesters { get; set; } = new();
}

public class SemesterGpaSummaryDto
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;

    public decimal? SemesterGpa { get; set; }
    public string SemesterGpaDisplay { get; set; } = "N/A";

    public int RegisteredHours { get; set; }
    public int GpaEligibleHours { get; set; }
    public decimal QualityPoints { get; set; }
}
