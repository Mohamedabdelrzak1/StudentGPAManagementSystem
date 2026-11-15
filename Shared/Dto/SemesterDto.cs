using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Dto;

public class SemesterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class CreateSemesterDto
{
    [Required(ErrorMessage = "Semester name is required.")]
    [StringLength(100, ErrorMessage = "Semester name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly EndDate { get; set; }
}

public class UpdateSemesterDto
{
    [Required(ErrorMessage = "Semester name is required.")]
    [StringLength(100, ErrorMessage = "Semester name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly EndDate { get; set; }
}
