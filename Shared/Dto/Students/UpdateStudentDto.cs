using System.ComponentModel.DataAnnotations;

namespace Shared.Dto;

public class UpdateStudentDto
{
    [Required(ErrorMessage = "Student name is required.")]
    [StringLength(150, ErrorMessage = "Student name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;
}
