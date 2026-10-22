using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _svc;

    public DepartmentsController(IDepartmentService svc) => _svc = svc;

    /// <summary>Danh sách phòng ban (Admin: tất cả; Manager: phòng mình).</summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<PagedResult<DepartmentDto>>> List([FromQuery] PageQuery q) => Ok(await _svc.ListAsync(User, q));

    /// <summary>Chi tiết phòng ban.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<DepartmentDto>> Get(int id) => Ok(await _svc.GetAsync(User, id));

    /// <summary>Admin tạo phòng ban.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Create(DepartmentRequest req)
    {
        var dto = await _svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Admin cập nhật phòng ban.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, DepartmentRequest req) => Ok(await _svc.UpdateAsync(id, req));

    /// <summary>Admin xóa mềm phòng ban (không xóa được khi còn nhân viên).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }
}
