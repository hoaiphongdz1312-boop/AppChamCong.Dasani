using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _svc;

    public LocationsController(ILocationService svc) => _svc = svc;

    /// <summary>Nhân viên gửi vị trí hiện tại (app gọi mỗi 30 giây khi bật chia sẻ; isSharing=false để tắt).</summary>
    [HttpPost("share")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Share(ShareLocationRequest req)
    {
        await _svc.ShareAsync(User, req);
        return NoContent();
    }

    /// <summary>Vị trí trực tiếp trong 5 phút gần nhất (Manager: phòng mình; Admin: tất cả).</summary>
    [HttpGet("live")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<List<LiveLocationDto>>> Live() => Ok(await _svc.LiveAsync(User));

    /// <summary>Lịch sử vị trí của một nhân viên trong ngày (theo quyền).</summary>
    [HttpGet("history/{userId:int}")]
    public async Task<ActionResult<List<LocationPointDto>>> History(int userId, [FromQuery] LocationHistoryQuery q) =>
        Ok(await _svc.HistoryAsync(User, userId, q.Date));
}
