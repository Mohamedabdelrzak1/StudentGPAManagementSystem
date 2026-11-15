using System.ComponentModel.DataAnnotations;

namespace Shared.Dto;

public class CreateCourseDto
{
    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(20, ErrorMessage = "Course code cannot exceed 20 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(150, ErrorMessage = "Course name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 3, ErrorMessage = "Credit hours must be between 1 and 3.")]
    public int CreditHours { get; set; }
}
