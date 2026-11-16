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

    public string? Status { get; set; }

    public decimal? Score { get; set; }
}
