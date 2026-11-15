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
