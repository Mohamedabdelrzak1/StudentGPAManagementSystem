using System.ComponentModel.DataAnnotations;

namespace Shared.Dto;

public class CreateStudentDto
{
    [Required(ErrorMessage = "Student code is required.")]
    [StringLength(20, ErrorMessage = "Student code cannot exceed 20 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Student name is required.")]
    [StringLength(150, ErrorMessage = "Student name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;
}
