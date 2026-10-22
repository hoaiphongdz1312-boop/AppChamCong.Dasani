namespace NguyenHoaiPhong_1681.DTOs;

public class DayStatDto
{
    public DateOnly Date { get; set; }
    public int Present { get; set; }
    public int Late { get; set; }
    public int Absent { get; set; }
}

public class MethodRatioDto
{
    public int FaceCount { get; set; }
    public int QrCount { get; set; }
    public double FacePercent { get; set; }
    public double QrPercent { get; set; }
}

public class DashboardDto
{
    /// <summary>Nhân viên (vai trò Employee) đang Active trong phạm vi thống kê.</summary>
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int LateToday { get; set; }

    /// <summary>Có ca hôm nay nhưng chưa check-in sau giờ bắt đầu + thời gian trễ cho phép (không tính người nghỉ phép được duyệt).</summary>
    public int AbsentToday { get; set; }

    public int SuspiciousLast7Days { get; set; }
    public MethodRatioDto MethodRatio { get; set; } = new();
    public List<DayStatDto> Last7Days { get; set; } = new();
}

public class ManagerDashboardDto : DashboardDto
{
    public int PendingLeaveCount { get; set; }
    public List<LeaveRequestDto> PendingLeaveRequests { get; set; } = new();
}

public class MyDashboardDto
{
    public string Month { get; set; } = string.Empty;
    public double TotalWorkHours { get; set; }
    public int LateCount { get; set; }

    /// <summary>Số ngày nghỉ phép đã được duyệt trong tháng (không tính Chủ nhật).</summary>
    public int ApprovedLeaveDays { get; set; }

    public bool OnLeaveToday { get; set; }
    public TodayDto Today { get; set; } = new();
}
