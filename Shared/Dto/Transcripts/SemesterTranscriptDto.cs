using System.Collections.Generic;

namespace Shared.Dto;

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
