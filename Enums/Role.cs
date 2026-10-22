namespace NguyenHoaiPhong_1681.Enums;

public enum Role
{
    Admin,
    Manager,
    Employee
}

public enum UserStatus
{
    PendingApproval,
    Active,
    Locked
}

public enum AttendanceType
{
    CheckIn,
    CheckOut
}

public enum AttendanceMethod
{
    Face,
    Qr,
    Manual
}

public enum AttendanceStatus
{
    OnTime,
    Late,
    EarlyLeave
}

public enum LeaveType
{
    Leave,
    Explanation,
    LateNotice
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public enum OtpPurpose
{
    RegisterVerify,
    Login,
    PhoneVerify
}

public enum OtpChannel
{
    Email,
    Sms
}
