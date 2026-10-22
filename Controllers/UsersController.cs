using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Danh sách người dùng (Admin: tất cả; Manager: nhân viên phòng mình).</summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<PagedResult<UserDto>>> List([FromQuery] UserQuery q) => Ok(await _users.ListAsync(User, q));

    /// <summary>Thông tin tài khoản đang đăng nhập.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me() => Ok(await _users.GetMeAsync(User));

    /// <summary>Cập nhật hồ sơ của mình; đổi số điện thoại sẽ phải xác minh lại.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateMeRequest req) => Ok(await _users.UpdateMeAsync(User, req));

    /// <summary>Chi tiết người dùng (bản thân, Manager cùng phòng, hoặc Admin).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id) => Ok(await _users.GetAsync(User, id));

    /// <summary>Admin tạo người dùng (Active ngay).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest req)
    {
        var dto = await _users.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Admin cập nhật thông tin người dùng.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest req) => Ok(await _users.UpdateAsync(id, req));

    /// <summary>Admin xóa mềm người dùng.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _users.DeleteAsync(User, id);
        return NoContent();
    }

    /// <summary>Duyệt tài khoản đang chờ duyệt.</summary>
    [HttpPatch("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Approve(int id) => Ok(await _users.ApproveAsync(id));

    /// <summary>Khóa/mở khóa tài khoản (mở khóa sẽ reset bộ đếm OTP sai).</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> SetStatus(int id, SetStatusRequest req) => Ok(await _users.SetStatusAsync(User, id, req.Status));

    /// <summary>Đổi vai trò.</summary>
    [HttpPatch("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> SetRole(int id, SetRoleRequest req) => Ok(await _users.SetRoleAsync(User, id, req.Role));

    /// <summary>Gán phòng ban (null = bỏ khỏi phòng).</summary>
    [HttpPatch("{id:int}/department")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> SetDepartment(int id, SetDepartmentRequest req) => Ok(await _users.SetDepartmentAsync(id, req.DepartmentId));

    /// <summary>Gỡ thiết bị đã gắn với nhân viên.</summary>
    [HttpPatch("{id:int}/reset-device")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> ResetDevice(int id) => Ok(await _users.ResetDeviceAsync(id));

    /// <summary>Lịch sử gửi OTP của người dùng (không chứa mã).</summary>
    [HttpGet("{id:int}/otp-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<OtpSendLogDto>>> OtpLogs(int id, [FromQuery] PageQuery q) => Ok(await _users.OtpLogsAsync(id, q));
}
