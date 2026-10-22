using Microsoft.Extensions.Options;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NguyenHoaiPhong_1681.Services;

public interface ISmsSender
{
    Task SendOtpAsync(string toPhoneE164, string code);
}

public class SmsSender : ISmsSender
{
    private readonly TwilioOptions _twilio;
    private readonly OtpOptions _otp;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IOptions<TwilioOptions> twilio, IOptions<OtpOptions> otp, ILogger<SmsSender> logger)
    {
        _twilio = twilio.Value;
        _otp = otp.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string toPhoneE164, string code)
    {
        // Nội dung KHÔNG DẤU để tương thích mọi nhà mạng.
        var body = $"Ma xac thuc cham cong cua ban la {code}. Het han sau {_otp.ExpireMinutes} phut. Khong chia se ma nay.";

        if (!_twilio.Enabled)
        {
            _logger.LogInformation("[SMS giả lập] Tới {Phone}: {Body}", toPhoneE164, body);
            return;
        }

        try
        {
            var client = new TwilioRestClient(_twilio.AccountSid, _twilio.AuthToken);
            await MessageResource.CreateAsync(
                new CreateMessageOptions(new PhoneNumber(toPhoneE164))
                {
                    From = new PhoneNumber(_twilio.FromNumber),
                    Body = body
                },
                client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi SMS qua Twilio thất bại");
            throw new AppException(502, "Không gửi được SMS, vui lòng chọn Email");
        }
    }
}
