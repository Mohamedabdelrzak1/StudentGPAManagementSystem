using System.Collections.Generic;

namespace Shared.Dto;

public class DashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalSemesters { get; set; }
    public int CompletedSemestersCount { get; set; }
    public decimal InstituteAverageGpa { get; set; }
    public int ActiveCoursesCount { get; set; }
    public int HonorStudentsCount { get; set; }
    public decimal HonorStudentsPercentage { get; set; }
    public List<HonorStudentItemDto> HonorStudents { get; set; } = new();
    public SemesterDto? CurrentSemester { get; set; }

    public List<SemesterTrendDto> PerformanceTrend { get; set; } = new();
    public GradeDistributionDto GradeDistribution { get; set; } = new();

    public List<StudentDto> Students { get; set; } = new();
}

public class HonorStudentItemDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public decimal CumulativeGpa { get; set; }
}

public class SemesterTrendDto
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public decimal AverageGpa { get; set; }
    public int EnrolledStudentsCount { get; set; }
}

public class GradeDistributionDto
{
    public int TotalStudentsEvaluated { get; set; }
    public GradeTierDto Excellent { get; set; } = new();  // 3.40 – 4.00
    public GradeTierDto VeryGood { get; set; } = new();   // 2.80 – 3.39
    public GradeTierDto Good { get; set; } = new();       // 2.20 – 2.79
    public GradeTierDto PassFail { get; set; } = new();   // < 2.20
}

public class GradeTierDto
{
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
