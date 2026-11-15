using System.ComponentModel.DataAnnotations;

namespace Shared.Dto;

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
