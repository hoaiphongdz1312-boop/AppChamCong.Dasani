using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;

namespace NguyenHoaiPhong_1681.Services;

public interface IAttendanceService
{
    /// <summary>Chấm công vào/ra bằng Face hoặc QR; tính TrustScore.</summary>
    Task<CheckResponse> CheckAsync(ClaimsPrincipal user, AttendanceType type, CheckRequest req, Stream? image);

    Task<PagedResult<AttendanceRecordDto>> MyAsync(ClaimsPrincipal user, MyAttendanceQuery q);
    Task<TodayDto> TodayAsync(ClaimsPrincipal user);
    Task<PagedResult<AttendanceRecordDto>> DepartmentAsync(ClaimsPrincipal user, DepartmentAttendanceQuery q);
    Task<PagedResult<AttendanceRecordDto>> ListAsync(AttendanceQuery q);
    Task<PagedResult<AttendanceRecordDto>> SuspiciousAsync(SuspiciousQuery q);

    /// <summary>Admin sửa bản ghi chấm công, bắt buộc có lý do, lưu người sửa.</summary>
    Task<AttendanceRecordDto> EditAsync(ClaimsPrincipal user, int id, EditAttendanceRequest req);
}

public class AttendanceService : IAttendanceService
{
    private const int SuspiciousBelow = 60;

    private readonly AppDbContext _db;
    private readonly IFaceService _face;
    private readonly IFaceChallengeService _challenges;
    private readonly IQrService _qr;
    private readonly FaceOptions _faceOpt;

    public AttendanceService(AppDbContext db, IFaceService face, IFaceChallengeService challenges,
        IQrService qr, IOptions<FaceOptions> faceOpt)
    {
        _db = db;
        _face = face;
        _challenges = challenges;
        _qr = qr;
        _faceOpt = faceOpt.Value;
    }

    private static IQueryable<AttendanceRecordDto> Project(IQueryable<AttendanceRecord> q) => q.Select(x => new AttendanceRecordDto
    {
        Id = x.Id, UserId = x.UserId, UserName = x.User!.FullName, ShiftAssignmentId = x.ShiftAssignmentId,
        Type = x.Type, Timestamp = x.Timestamp, Latitude = x.Latitude, Longitude = x.Longitude,
        DistanceMeters = x.DistanceMeters, IsMockLocation = x.IsMockLocation, DeviceId = x.DeviceId,
        WifiBssid = x.WifiBssid, Method = x.Method, FaceScore = x.FaceScore, LivenessPassed = x.LivenessPassed,
        TrustScore = x.TrustScore, IsSuspicious = x.IsSuspicious, Status = x.Status, Note = x.Note,
        EditedById = x.EditedById, EditReason = x.EditReason
    });

    // ---------------------------------------------------------------- chấm công

    public async Task<CheckResponse> CheckAsync(ClaimsPrincipal principal, AttendanceType type, CheckRequest req, Stream? image)
    {
        if (req.Method is not (AttendanceMethod.Face or AttendanceMethod.Qr))
            throw new AppException(400, "Phương thức chấm công chỉ có thể là Face hoặc Qr");

        var userId = principal.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new AppException(401, "Phiên đăng nhập không hợp lệ.");
        if (user.Status != UserStatus.Active) throw new AppException(403, "Tài khoản không còn hoạt động.");

        // 1. Ca làm hôm nay (giờ Việt Nam) + chặn trùng / chưa check-in.
        var assignment = await SelectAssignmentAsync(userId, type);
        var shift = assignment.Shift!;
        var loc = assignment.WorkLocation!;

        // 2. Khoảng cách tới địa điểm của ca.
        var distance = GeoHelper.Haversine(req.Latitude, req.Longitude, loc.Latitude, loc.Longitude);
        if (distance > loc.RadiusMeters)
            throw new AppException(400, $"Bạn đang cách điểm chấm công {Math.Round(distance)} m");

        // 3. Xác minh danh tính.
        double? faceScore = null;
        bool? liveness = null;
        long? qrWindow = null;
        if (req.Method == AttendanceMethod.Face)
        {
            if (image == null || req.ChallengeToken == null)
                throw new AppException(400, "Thiếu ảnh khuôn mặt hoặc mã thử thách");
            await _challenges.ConsumeAsync(req.ChallengeToken.Value, userId);

            var match = await _face.VerifyUserAsync(userId, image);
            if (!match.Matched)
                throw new AppException(400, "Khuôn mặt không khớp, vui lòng thử lại")
                {
                    Extra = new() { ["score"] = match.Score, ["retry"] = match.Retry }
                };
            faceScore = match.Score;
            liveness = req.LivenessPassed;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(req.QrToken)) throw new AppException(400, "Thiếu mã QR");
            qrWindow = await _qr.ValidateAsync(req.QrToken, loc.Id);
            var used = await _db.AttendanceRecords.AnyAsync(r =>
                r.UserId == userId && r.QrWindow == qrWindow && r.ShiftAssignment!.WorkLocationId == loc.Id);
            if (used) throw new AppException(400, "Mã QR này bạn đã dùng");
        }

        // 4. Thiết bị.
        var deviceMismatch = false;
        if (string.IsNullOrEmpty(user.BoundDeviceId)) user.BoundDeviceId = req.DeviceId;
        else deviceMismatch = !string.Equals(user.BoundDeviceId, req.DeviceId, StringComparison.Ordinal);

        // 5. TrustScore.
        var trust = 100;
        var reasons = new List<string>();
        void Penalty(int points, string reason) { trust -= points; reasons.Add($"{reason} (−{points})"); }

        if (req.IsMockLocation) Penalty(40, "Phát hiện vị trí giả lập");
        if (deviceMismatch) Penalty(25, "Thiết bị khác với thiết bị đã đăng ký");
        if (!string.IsNullOrWhiteSpace(loc.WifiBssid) &&
            !string.Equals(loc.WifiBssid.Trim(), req.WifiBssid?.Trim(), StringComparison.OrdinalIgnoreCase))
            Penalty(10, "Wi-Fi không khớp với địa điểm chấm công");
        if (req.Method == AttendanceMethod.Face)
        {
            if (req.LivenessPassed == false) Penalty(30, "Không vượt qua kiểm tra người thật");
            if (faceScore < _faceOpt.MatchThreshold + 0.1) Penalty(10, "Độ khớp khuôn mặt thấp");
        }
        else
        {
            Penalty(10, "Chấm công bằng QR, không xác minh bằng khuôn mặt");
        }
        trust = Math.Clamp(trust, 0, 100);
        var suspicious = trust < SuspiciousBelow;

        // 6. Trạng thái theo giờ Việt Nam.
        var nowUtc = DateTime.UtcNow;
        var localTime = TimeOnly.FromDateTime(VnTime.ToVietnam(nowUtc));
        AttendanceStatus status;
        if (type == AttendanceType.CheckIn)
            status = localTime > shift.StartTime.AddMinutes(shift.LateToleranceMinutes) ? AttendanceStatus.Late : AttendanceStatus.OnTime;
        else
            status = localTime < shift.EndTime ? AttendanceStatus.EarlyLeave : AttendanceStatus.OnTime;

        var note = reasons.Count > 0 ? string.Join("; ", reasons) : null;
        var record = new AttendanceRecord
        {
            UserId = userId,
            ShiftAssignmentId = assignment.Id,
            Type = type,
            Timestamp = nowUtc,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            DistanceMeters = Math.Round(distance, 1),
            IsMockLocation = req.IsMockLocation,
            DeviceId = req.DeviceId,
            WifiBssid = req.WifiBssid,
            Method = req.Method,
            FaceScore = faceScore,
            LivenessPassed = liveness,
            QrWindow = qrWindow,
            TrustScore = trust,
            IsSuspicious = suspicious,
            Status = status,
            Note = note is { Length: > 512 } ? note[..512] : note
        };
        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        return new CheckResponse
        {
            RecordId = record.Id,
            Type = type,
            Status = status,
            TrustScore = trust,
            IsSuspicious = suspicious,
            DistanceMeters = record.DistanceMeters,
            Method = req.Method,
            Timestamp = nowUtc,
            Reasons = reasons
        };
    }

    /// <summary>Chọn ca phù hợp để check-in/out trong ngày hôm nay (giờ VN) và chặn các trường hợp trùng.</summary>
    private async Task<ShiftAssignment> SelectAssignmentAsync(int userId, AttendanceType type)
    {
        var today = VnTime.Today;
        var assignments = (await _db.ShiftAssignments
                .Include(a => a.Shift).Include(a => a.WorkLocation)
                .Where(a => a.UserId == userId && a.WorkDate == today)
                .ToListAsync())
            .OrderBy(a => a.Shift!.StartTime).ToList();
        if (assignments.Count == 0) throw new AppException(400, "Hôm nay bạn không có ca làm");

        var ids = assignments.Select(a => a.Id).ToList();
        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.ShiftAssignmentId != null && ids.Contains(r.ShiftAssignmentId.Value))
            .Select(r => new { Id = r.ShiftAssignmentId!.Value, r.Type })
            .ToListAsync();
        var ins = records.Where(r => r.Type == AttendanceType.CheckIn).Select(r => r.Id).ToHashSet();
        var outs = records.Where(r => r.Type == AttendanceType.CheckOut).Select(r => r.Id).ToHashSet();

        if (type == AttendanceType.CheckIn)
        {
            var open = assignments.Where(a => !ins.Contains(a.Id)).ToList();
            if (open.Count == 0) throw new AppException(400, "Bạn đã chấm công vào cho ca hôm nay rồi");
            var nowT = TimeOnly.FromDateTime(VnTime.Now);
            return open.FirstOrDefault(a => a.Shift!.EndTime >= nowT) ?? open.Last();
        }

        var pending = assignments.Where(a => ins.Contains(a.Id) && !outs.Contains(a.Id)).ToList();
        if (pending.Count == 0)
            throw new AppException(400, ins.Count == 0 ? "Bạn chưa chấm công vào" : "Bạn đã chấm công ra cho ca hôm nay rồi");
        return pending.First();
    }

    // ---------------------------------------------------------------- truy vấn

    public async Task<PagedResult<AttendanceRecordDto>> MyAsync(ClaimsPrincipal principal, MyAttendanceQuery q)
    {
        var userId = principal.GetUserId();
        var to = q.To ?? VnTime.Today;
        var from = q.From ?? to.AddDays(-30);
        var query = ApplyRange(_db.AttendanceRecords.AsNoTracking().Where(r => r.UserId == userId), from, to);
        return await Project(query.OrderByDescending(r => r.Timestamp)).ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<TodayDto> TodayAsync(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        var today = VnTime.Today;
        var assignments = (await _db.ShiftAssignments.AsNoTracking()
                .Include(a => a.Shift).Include(a => a.WorkLocation)
                .Where(a => a.UserId == userId && a.WorkDate == today)
                .ToListAsync())
            .OrderBy(a => a.Shift!.StartTime).ToList();

        var ids = assignments.Select(a => a.Id).ToList();
        var records = await Project(_db.AttendanceRecords.AsNoTracking()
                .Where(r => r.ShiftAssignmentId != null && ids.Contains(r.ShiftAssignmentId.Value))
                .OrderBy(r => r.Timestamp))
            .ToListAsync();

        var result = new TodayDto { Date = today };
        foreach (var a in assignments)
        {
            var mine = records.Where(r => r.ShiftAssignmentId == a.Id).ToList();
            var inRec = mine.FirstOrDefault(r => r.Type == AttendanceType.CheckIn);
            var outRec = mine.FirstOrDefault(r => r.Type == AttendanceType.CheckOut);
            result.Shifts.Add(new TodayShiftDto
            {
                AssignmentId = a.Id,
                ShiftName = a.Shift!.Name,
                StartTime = a.Shift.StartTime,
                EndTime = a.Shift.EndTime,
                LateToleranceMinutes = a.Shift.LateToleranceMinutes,
                WorkLocationId = a.WorkLocationId,
                WorkLocationName = a.WorkLocation!.Name,
                LocationLatitude = a.WorkLocation.Latitude,
                LocationLongitude = a.WorkLocation.Longitude,
                LocationRadiusMeters = a.WorkLocation.RadiusMeters,
                CheckIn = inRec,
                CheckOut = outRec,
                CanCheckIn = inRec == null,
                CanCheckOut = inRec != null && outRec == null
            });
        }
        return result;
    }

    public async Task<PagedResult<AttendanceRecordDto>> DepartmentAsync(ClaimsPrincipal principal, DepartmentAttendanceQuery q)
    {
        var actor = await Actor.FromAsync(principal, _db);
        if (actor.DepartmentId == null) throw new AppException(403, "Bạn chưa thuộc phòng ban nào.");

        var date = q.Date ?? VnTime.Today;
        var query = ApplyRange(
            _db.AttendanceRecords.AsNoTracking().Where(r => r.User!.DepartmentId == actor.DepartmentId), date, date);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(r => r.User!.FullName.Contains(s) || r.User.Username.Contains(s));
        }
        return await Project(query.OrderBy(r => r.User!.FullName).ThenBy(r => r.Timestamp)).ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<PagedResult<AttendanceRecordDto>> ListAsync(AttendanceQuery q)
    {
        var query = _db.AttendanceRecords.AsNoTracking().AsQueryable();
        if (q.UserId != null) query = query.Where(r => r.UserId == q.UserId);
        if (q.DepartmentId != null) query = query.Where(r => r.User!.DepartmentId == q.DepartmentId);
        if (q.Method != null) query = query.Where(r => r.Method == q.Method);
        if (q.IsSuspicious != null) query = query.Where(r => r.IsSuspicious == q.IsSuspicious);
        if (q.From != null || q.To != null) query = ApplyRange(query, q.From, q.To);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(r => r.User!.FullName.Contains(s) || r.User.Username.Contains(s));
        }
        return await Project(query.OrderByDescending(r => r.Timestamp)).ToPagedAsync(q.Page, q.PageSize);
    }

    public Task<PagedResult<AttendanceRecordDto>> SuspiciousAsync(SuspiciousQuery q) =>
        ListAsync(new AttendanceQuery
        {
            IsSuspicious = true, From = q.From, To = q.To, Search = q.Search, Page = q.Page, PageSize = q.PageSize
        });

    /// <summary>Lọc theo khoảng ngày giờ Việt Nam, [from 00:00, to+1 00:00).</summary>
    private static IQueryable<AttendanceRecord> ApplyRange(IQueryable<AttendanceRecord> query, DateOnly? from, DateOnly? to)
    {
        if (from != null)
        {
            var start = VnTime.ToUtc(from.Value.ToDateTime(TimeOnly.MinValue));
            query = query.Where(r => r.Timestamp >= start);
        }
        if (to != null)
        {
            var end = VnTime.ToUtc(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            query = query.Where(r => r.Timestamp < end);
        }
        return query;
    }

    // ---------------------------------------------------------------- admin sửa

    public async Task<AttendanceRecordDto> EditAsync(ClaimsPrincipal principal, int id, EditAttendanceRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.EditReason))
            throw new AppException(400, "Bắt buộc nhập lý do chỉnh sửa");

        var r = await _db.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy bản ghi chấm công");

        // DateTime không kèm múi giờ → giờ Việt Nam; có offset/UTC → đổi về UTC.
        r.Timestamp = req.Timestamp.Kind == DateTimeKind.Unspecified
            ? VnTime.ToUtc(req.Timestamp)
            : req.Timestamp.ToUniversalTime();
        r.Type = req.Type;
        r.Status = req.Status;
        r.Note = req.Note;
        r.EditedById = principal.GetUserId();
        r.EditReason = req.EditReason.Trim();
        await _db.SaveChangesAsync();

        return await Project(_db.AttendanceRecords.AsNoTracking().Where(x => x.Id == id)).FirstAsync();
    }
}
