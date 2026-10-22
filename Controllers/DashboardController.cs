using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _svc;

    public DashboardController(IDashboardService svc) => _svc = svc;

    /// <summary>Tổng quan toàn công ty: có mặt, trễ, vắng, nghi vấn, tỉ lệ Face/QR và thống kê 7 ngày.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DashboardDto>> Admin() => Ok(await _svc.AdminAsync());

    /// <summary>Tổng quan phòng của Manager, kèm danh sách đơn chờ duyệt.</summary>
    [HttpGet("manager")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ManagerDashboardDto>> Manager() => Ok(await _svc.ManagerAsync(User));

    /// <summary>Thống kê của tôi: giờ làm tháng này, số lần trễ, ngày nghỉ được duyệt, trạng thái hôm nay.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyDashboardDto>> Me() => Ok(await _svc.MeAsync(User));
}
