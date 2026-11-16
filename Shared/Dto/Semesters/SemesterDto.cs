using System;

namespace Shared.Dto;

public class SemesterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int EnrolledStudentsCount { get; set; }
}
