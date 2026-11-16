namespace Shared.Dto;

public class StudentDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Major { get; set; } = "علوم الحاسب";
    public decimal? Gpa { get; set; }
    public string? Status { get; set; } = "منتظم";
}
