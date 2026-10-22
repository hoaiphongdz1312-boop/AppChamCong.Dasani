using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Options;

namespace NguyenHoaiPhong_1681.Data;

public class DbSeeder
{

    private readonly AppDbContext _db;
    private readonly SeedOptions _seed;
    private readonly AppOptions _app;
    private readonly QrOptions _qr;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext db, IOptions<SeedOptions> seed, IOptions<AppOptions> app,
        IOptions<QrOptions> qr, ILogger<DbSeeder> logger)
    {
        _db = db;
        _seed = seed.Value;
        _app = app.Value;
        _qr = qr.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.Users.IgnoreQueryFilters().AnyAsync()) return;

        // Phòng ban
        var tech = new Department { Name = "Phòng Kỹ thuật", Description = "Phát triển và vận hành hệ thống" };
        var sales = new Department { Name = "Phòng Kinh doanh", Description = "Kinh doanh và chăm sóc khách hàng" };
        _db.Departments.AddRange(tech, sales);
        await _db.SaveChangesAsync();

        // Người dùng
        User NewUser(string username, string fullName, Role role, string password, int? deptId, string phone) => new()
        {
            Username = username,
            Email = $"{username}@example.com",
            PhoneNumber = phone,
            FullName = fullName,
            Role = role,
            Status = UserStatus.Active,
            EmailVerified = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DepartmentId = deptId
        };

        var admin = NewUser("admin", "Quản trị viên", Role.Admin, "Admin@123", null, "+84900000000");
        var manager = NewUser("quanly", "Nguyễn Quản Lý", Role.Manager, "Manager@123", tech.Id, "+84900000002");
        var nv1 = NewUser("nhanvien1", "Trần Nhân Viên Một", Role.Employee, "Employee@123", tech.Id, "+84900000001");
        nv1.PhoneVerified = true;
        var nv2 = NewUser("nhanvien2", "Lê Nhân Viên Hai", Role.Employee, "Employee@123", tech.Id, "+84900000003");
        _db.Users.AddRange(admin, manager, nv1, nv2);
        await _db.SaveChangesAsync();

        tech.ManagerId = manager.Id;
        await _db.SaveChangesAsync();

        // Ca làm việc
        var morning = new Shift { Name = "Sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), LateToleranceMinutes = 10 };
        var afternoon = new Shift { Name = "Chiều", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(17, 0), LateToleranceMinutes = 10 };
        var office = new Shift { Name = "Hành chính", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), LateToleranceMinutes = 10 };
        _db.Shifts.AddRange(morning, afternoon, office);

        // Địa điểm
        var location = new WorkLocation
        {
            Name = "HUTECH",
            Address = "475A Điện Biên Phủ, Phường 25, Bình Thạnh, TP.HCM",
            Latitude = _seed.LocationLat,
            Longitude = _seed.LocationLng,
            RadiusMeters = _seed.RadiusMeters,
            KioskKey = RandomHelper.RandomKey(32),
            QrSecret = RandomNumberGenerator.GetBytes(32)
        };
        _db.WorkLocations.Add(location);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Kiosk URL: {Url}",
            $"{_app.BaseUrl.TrimEnd('/')}/kiosk.html?locationId={location.Id}&key={location.KioskKey}");

        // Phân ca: 7 ngày trước (để gắn bản ghi mẫu) + 14 ngày tới, bỏ Chủ nhật
        var today = VnTime.Today;
        var employees = new[] { nv1, nv2 };
        var assignments = new Dictionary<(int, DateOnly), ShiftAssignment>();
        for (var offset = -7; offset <= 14; offset++)
        {
            var date = today.AddDays(offset);
            if (date.DayOfWeek == DayOfWeek.Sunday) continue;
            foreach (var emp in employees)
            {
                var sa = new ShiftAssignment { UserId = emp.Id, ShiftId = office.Id, WorkLocationId = location.Id, WorkDate = date };
                assignments[(emp.Id, date)] = sa;
                _db.ShiftAssignments.Add(sa);
            }
        }
        await _db.SaveChangesAsync();

        // Chấm công mẫu 7 ngày trước
        var lateMinutes = new Dictionary<int, int[]>
        {
            [nv1.Id] = [0, 3, 25, 5, -2, 4, 0],
            [nv2.Id] = [2, 0, 6, 12, 1, 0, 3]
        };
        foreach (var emp in employees)
        {
            var k = 0;
            for (var offset = -7; offset <= -1; offset++, k++)
            {
                var date = today.AddDays(offset);
                if (!assignments.TryGetValue((emp.Id, date), out var sa)) continue;

                var viaFace = k % 2 == 0;
                var suspicious = emp.Id == nv2.Id && k == 5;
                var minutes = lateMinutes[emp.Id][k];
                var inLocal = date.ToDateTime(new TimeOnly(8, 0)).AddMinutes(minutes);
                var earlyLeave = emp.Id == nv1.Id && k == 4;
                var outLocal = date.ToDateTime(earlyLeave ? new TimeOnly(16, 30) : new TimeOnly(17, 5));

                _db.AttendanceRecords.Add(MakeRecord(emp, sa, location, AttendanceType.CheckIn, inLocal,
                    viaFace && !suspicious, suspicious,
                    minutes > office.LateToleranceMinutes ? AttendanceStatus.Late : AttendanceStatus.OnTime));
                _db.AttendanceRecords.Add(MakeRecord(emp, sa, location, AttendanceType.CheckOut, outLocal,
                    viaFace && !suspicious, suspicious,
                    earlyLeave ? AttendanceStatus.EarlyLeave : AttendanceStatus.OnTime));
            }
        }

        // Đơn nghỉ mẫu
        _db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                UserId = nv1.Id,
                Type = LeaveType.Leave,
                FromDate = today.AddDays(3),
                ToDate = today.AddDays(4),
                Reason = "Việc gia đình",
                Status = LeaveStatus.Pending
            },
            new LeaveRequest
            {
                UserId = nv2.Id,
                Type = LeaveType.Leave,
                FromDate = today.AddDays(6),
                ToDate = today.AddDays(6),
                Reason = "Khám sức khỏe định kỳ",
                Status = LeaveStatus.Approved,
                ReviewerId = manager.Id,
                ReviewNote = "Đồng ý",
                ReviewedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Đã seed dữ liệu mẫu.");
    }

    private AttendanceRecord MakeRecord(User user, ShiftAssignment sa, WorkLocation loc, AttendanceType type,
        DateTime localTime, bool viaFace, bool suspicious, AttendanceStatus status)
    {
        var utc = VnTime.ToUtc(localTime);
        var windowSeconds = Math.Max(_qr.WindowSeconds, 1);
        var rec = new AttendanceRecord
        {
            UserId = user.Id,
            ShiftAssignmentId = sa.Id,
            Type = type,
            Timestamp = utc,
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
            DistanceMeters = suspicious ? 150 : 12,
            IsMockLocation = suspicious,
            DeviceId = suspicious ? "unknown-device-xyz" : $"seed-device-{user.Id}",
            Method = viaFace ? AttendanceMethod.Face : AttendanceMethod.Qr,
            Status = status
        };

        if (viaFace)
        {
            rec.FaceScore = 0.72;
            rec.LivenessPassed = true;
            rec.TrustScore = 100;
        }
        else
        {
            rec.QrWindow = new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds() / windowSeconds;
            rec.TrustScore = 90;
        }

        if (suspicious)
        {
            rec.TrustScore = 90 - 40 - 25; // QR −10, mock −40, thiết bị lạ −25
            rec.IsSuspicious = true;
            rec.Note = "Bản ghi mẫu: vị trí giả lập và thiết bị lạ";
        }
        return rec;
    }

}
