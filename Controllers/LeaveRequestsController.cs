using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _svc;

    public LeaveRequestsController(ILeaveRequestService svc) => _svc = svc;

    /// <summary>Danh sách đơn theo quyền (Employee: của mình; Manager: phòng mình; Admin: tất cả).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<LeaveRequestDto>>> List([FromQuery] LeaveRequestQuery q) => Ok(await _svc.ListAsync(User, q));

    /// <summary>Chi tiết đơn.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestDto>> Get(int id) => Ok(await _svc.GetAsync(User, id));

    /// <summary>Tạo đơn nghỉ / giải trình / báo đến trễ.</summary>
    [HttpPost]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<ActionResult<LeaveRequestDto>> Create(LeaveRequestRequest req)
    {
        var dto = await _svc.CreateAsync(User, req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Sửa đơn của mình khi còn chờ duyệt.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<ActionResult<LeaveRequestDto>> Update(int id, LeaveRequestRequest req) => Ok(await _svc.UpdateAsync(User, id, req));

    /// <summary>Hủy đơn của mình khi còn chờ duyệt (chuyển sang Cancelled).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _svc.CancelAsync(User, id);
        return NoContent();
    }

    /// <summary>Manager/Admin duyệt hoặc từ chối đơn.</summary>
    [HttpPatch("{id:int}/review")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<LeaveRequestDto>> Review(int id, ReviewLeaveRequest req) => Ok(await _svc.ReviewAsync(User, id, req));
}
