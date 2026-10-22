namespace NguyenHoaiPhong_1681.Entities;

public class ShiftAssignment : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public int ShiftId { get; set; }
    public Shift? Shift { get; set; }
    public int WorkLocationId { get; set; }
    public WorkLocation? WorkLocation { get; set; }
    public DateOnly WorkDate { get; set; }
}
