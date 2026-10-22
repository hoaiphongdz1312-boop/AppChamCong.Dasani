using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _svc;

    public AttendanceController(IAttendanceService svc) => _svc = svc;

    /// <summary>Chấm công vào (method=Face: image + challengeToken + livenessPassed; method=Qr: qrToken).</summary>
    [HttpPost("check-in")]
    [Consumes("multipart/form-data")]
    public Task<ActionResult<CheckResponse>> CheckIn([FromForm] CheckRequest req) => Check(AttendanceType.CheckIn, req);

    /// <summary>Chấm công ra (cùng tham số với check-in).</summary>
    [HttpPost("check-out")]
    [Consumes("multipart/form-data")]
    public Task<ActionResult<CheckResponse>> CheckOut([FromForm] CheckRequest req) => Check(AttendanceType.CheckOut, req);

    private async Task<ActionResult<CheckResponse>> Check(AttendanceType type, CheckRequest req)
    {
        Stream? stream = null;
        try
        {
            if (req.Method == AttendanceMethod.Face)
            {
                FaceController.ValidateFile(req.Image);
                stream = req.Image!.OpenReadStream();
            }
            return Ok(await _svc.CheckAsync(User, type, req, stream));
        }
        finally
        {
            if (stream != null) await stream.DisposeAsync();
        }
    }

    /// <summary>Lịch sử chấm công của tôi (mặc định 30 ngày gần nhất).</summary>
    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<AttendanceRecordDto>>> My([FromQuery] MyAttendanceQuery q) => Ok(await _svc.MyAsync(User, q));

    /// <summary>Các ca hôm nay của tôi, đã check-in/out chưa và giờ chấm.</summary>
    [HttpGet("today")]
    public async Task<ActionResult<TodayDto>> Today() => Ok(await _svc.TodayAsync(User));

    /// <summary>Manager xem chấm công của phòng mình theo ngày.</summary>
    [HttpGet("department")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<PagedResult<AttendanceRecordDto>>> Department([FromQuery] DepartmentAttendanceQuery q) =>
        Ok(await _svc.DepartmentAsync(User, q));

    /// <summary>Admin xem toàn bộ chấm công, lọc theo nhân viên/phòng/ngày/phương thức/nghi vấn.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<AttendanceRecordDto>>> List([FromQuery] AttendanceQuery q) => Ok(await _svc.ListAsync(q));

    /// <summary>Admin xem các bản ghi nghi vấn (TrustScore &lt; 60).</summary>
    [HttpGet("suspicious")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<AttendanceRecordDto>>> Suspicious([FromQuery] SuspiciousQuery q) => Ok(await _svc.SuspiciousAsync(q));

    /// <summary>Admin sửa bản ghi chấm công (bắt buộc editReason).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AttendanceRecordDto>> Edit(int id, EditAttendanceRequest req) => Ok(await _svc.EditAsync(User, id, req));
}
