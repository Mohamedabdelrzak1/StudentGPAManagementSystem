namespace Domain.Models;

public class Student : BaseEntity<int>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
