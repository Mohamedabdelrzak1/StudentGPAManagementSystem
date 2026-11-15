namespace Domain.Models;

public class GradingScale : BaseEntity<int>
{
    public decimal FromGrade { get; set; }
    public decimal ToGrade { get; set; }
    public decimal Points { get; set; }
    public string Grade { get; set; } = string.Empty;
}
