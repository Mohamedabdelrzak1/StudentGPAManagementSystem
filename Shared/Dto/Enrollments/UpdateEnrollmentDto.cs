namespace Shared.Dto;

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
