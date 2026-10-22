using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/shift-assignments")]
[Authorize]
public class ShiftAssignmentsController : ControllerBase
{
    private readonly IShiftAssignmentService _svc;

    public ShiftAssignmentsController(IShiftAssignmentService svc) => _svc = svc;

    /// <summary>Danh sách phân ca (Admin: tất cả; Manager: nhân viên phòng mình).</summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<PagedResult<ShiftAssignmentDto>>> List([FromQuery] ShiftAssignmentQuery q) => Ok(await _svc.ListAsync(User, q));

    /// <summary>Lịch làm việc của tôi trong khoảng ngày (mặc định 30 ngày từ hôm nay).</summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<ShiftAssignmentDto>>> My([FromQuery] MyAssignmentQuery q) => Ok(await _svc.MyAsync(User, q.From, q.To));

    /// <summary>Chi tiết phân ca.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShiftAssignmentDto>> Get(int id) => Ok(await _svc.GetAsync(User, id));

    /// <summary>Tạo phân ca (chặn trùng nhân viên + ngày + ca).</summary>
    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<ShiftAssignmentDto>> Create(ShiftAssignmentRequest req)
    {
        var dto = await _svc.CreateAsync(User, req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Phân ca hàng loạt theo khoảng ngày, bỏ qua bản ghi đã có.</summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<BulkShiftAssignmentResult>> Bulk(BulkShiftAssignmentRequest req) => Ok(await _svc.BulkAsync(User, req));

    /// <summary>Cập nhật phân ca.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<ShiftAssignmentDto>> Update(int id, ShiftAssignmentRequest req) => Ok(await _svc.UpdateAsync(User, id, req));

    /// <summary>Xóa phân ca (không xóa được khi đã có chấm công).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(User, id);
        return NoContent();
    }
}
