namespace Shared.Dto;

public class GradingScaleDto
{
    public int Id { get; set; }
    public decimal FromGrade { get; set; }
    public decimal ToGrade { get; set; }
    public decimal Points { get; set; }
    public string Grade { get; set; } = string.Empty;
}
