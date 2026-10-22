using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/work-locations")]
[Authorize(Policy = "ManagerOrAdmin")]
public class WorkLocationsController : ControllerBase
{
    private readonly IWorkLocationService _svc;

    public WorkLocationsController(IWorkLocationService svc) => _svc = svc;

    /// <summary>Danh sách địa điểm (chỉ Admin nhận kioskUrl).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkLocationDto>>> List([FromQuery] WorkLocationQuery q) => Ok(await _svc.ListAsync(User, q));

    /// <summary>Chi tiết địa điểm.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkLocationDto>> Get(int id) => Ok(await _svc.GetAsync(User, id));

    /// <summary>Admin tạo địa điểm (tự sinh KioskKey và QrSecret).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WorkLocationDto>> Create(WorkLocationRequest req)
    {
        var dto = await _svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Admin cập nhật địa điểm.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WorkLocationDto>> Update(int id, WorkLocationRequest req) => Ok(await _svc.UpdateAsync(id, req));

    /// <summary>Admin xóa mềm địa điểm.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Admin đổi KioskKey (link kiosk cũ sẽ không dùng được nữa).</summary>
    [HttpPost("{id:int}/regenerate-kiosk-key")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WorkLocationDto>> RegenerateKioskKey(int id) => Ok(await _svc.RegenerateKioskKeyAsync(id));
}
