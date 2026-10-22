using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;

namespace NguyenHoaiPhong_1681.Services;

public interface IEmailSender
{
    Task SendOtpAsync(string toEmail, string code, CancellationToken ct = default);
}

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _smtp;
    private readonly OtpOptions _otp;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpOptions> smtp, IOptions<OtpOptions> otp, ILogger<EmailSender> logger)
    {
        _smtp = smtp.Value;
        _otp = otp.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string toEmail, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_smtp.User) || string.IsNullOrWhiteSpace(_smtp.AppPassword))
        {
            // Chưa cấu hình SMTP: ở DevMode coi như đã gửi (mã được log ra console), ngoài DevMode báo lỗi.
            if (_otp.DevMode)
            {
                _logger.LogWarning("SMTP chưa cấu hình, bỏ qua gửi email tới {Email} (DevMode).", toEmail);
                return;
            }
            throw new AppException(502, "Không gửi được email, vui lòng thử lại sau.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.User));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Mã xác thực chấm công";
        message.Body = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family:Arial,sans-serif;max-width:420px;margin:auto">
                  <h2>Mã xác thực chấm công</h2>
                  <p>Mã của bạn là:</p>
                  <p style="font-size:32px;font-weight:bold;letter-spacing:6px">{code}</p>
                  <p>Mã hết hạn sau {_otp.ExpireMinutes} phút. Không chia sẻ mã này cho bất kỳ ai.</p>
                </div>
                """,
            TextBody = $"Ma xac thuc cham cong cua ban la {code}. Het han sau {_otp.ExpireMinutes} phut."
        }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.User, _smtp.AppPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email OTP thất bại");
            throw new AppException(502, "Không gửi được email, vui lòng thử lại sau.");
        }
    }
}
