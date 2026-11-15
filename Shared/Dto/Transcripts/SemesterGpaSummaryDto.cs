namespace Shared.Dto;

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
