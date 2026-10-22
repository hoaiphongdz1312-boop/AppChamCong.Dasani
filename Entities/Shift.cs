namespace NguyenHoaiPhong_1681.Entities;

public class Shift : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int LateToleranceMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
