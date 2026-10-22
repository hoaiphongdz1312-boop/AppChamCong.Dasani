namespace NguyenHoaiPhong_1681.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ManagerId { get; set; }
    public User? Manager { get; set; }
    public bool IsDeleted { get; set; }
}
