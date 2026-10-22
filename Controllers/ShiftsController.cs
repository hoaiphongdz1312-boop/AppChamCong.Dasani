using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _svc;

    public ShiftsController(IShiftService svc) => _svc = svc;

    /// <summary>Danh sách ca làm việc (mọi người dùng đã đăng nhập).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ShiftDto>>> List([FromQuery] ShiftQuery q) => Ok(await _svc.ListAsync(q));

    /// <summary>Chi tiết ca.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShiftDto>> Get(int id) => Ok(await _svc.GetAsync(id));

    /// <summary>Admin tạo ca.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShiftDto>> Create(ShiftRequest req)
    {
        var dto = await _svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Admin cập nhật ca.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShiftDto>> Update(int id, ShiftRequest req) => Ok(await _svc.UpdateAsync(id, req));

    /// <summary>Admin xóa mềm ca.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }
}
