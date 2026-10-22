using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/qr")]
public class QrController : ControllerBase
{
    private readonly IQrService _qr;

    public QrController(IQrService qr) => _qr = qr;

    /// <summary>QR động cho trang kiosk (xác thực bằng KioskKey trong đường dẫn, không cần đăng nhập).</summary>
    [HttpGet("kiosk")]
    [AllowAnonymous]
    [EnableRateLimiting("kiosk")]
    public async Task<ActionResult<QrResponse>> Kiosk([FromQuery] int locationId, [FromQuery] string? key)
    {
        NoCache();
        return Ok(await _qr.GetForKioskAsync(locationId, key));
    }

    /// <summary>QR động hiện tại của một địa điểm, cho Manager/Admin mở trên app.</summary>
    [HttpGet("current")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<QrResponse>> Current([FromQuery] int locationId)
    {
        NoCache();
        return Ok(await _qr.GetCurrentAsync(locationId));
    }

    // QR đổi liên tục nên không được cache ở trình duyệt/proxy.
    private void NoCache()
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
    }
}
