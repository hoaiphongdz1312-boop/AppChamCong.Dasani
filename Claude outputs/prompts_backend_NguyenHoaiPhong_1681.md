# Prompt sinh backend NguyenHoaiPhong_1681 (9 bước)

Cách dùng: dán lần lượt từng bước. Bước trước `dotnet build` 0 error mới dán bước sau. Lỗi thì gửi log.

| Bước | Nội dung | Phụ thuộc |
|---|---|---|
| 1 | Dọn project, cài thư viện, cấu hình nền | – |
| 2 | Entities, DbContext, migration, seed | 1 |
| 3 | Auth: JWT, OTP Email/SMS, Google | 2 |
| 4 | CRUD + phân quyền | 3 |
| 5 | Khuôn mặt (FaceAiSharp) | 3 |
| 6 | QR động + trang kiosk | 4 |
| 7 | Chấm công + Trust Score | 5, 6 |
| 8 | Chia sẻ vị trí + Dashboard | 7 |
| 9 | File .http, README, kiểm tra .gitignore | 8 |

---

## Bước 1 – Dọn project, cài thư viện, cấu hình nền

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0, Controllers, đã có Swashbuckle.AspNetCore 6.6.2. KHÔNG tạo project mới, KHÔNG đổi tên project/namespace/TargetFramework. Đọc code hiện có trước khi sửa. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây, không làm trước bước sau. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 1 – DỌN, CÀI THƯ VIỆN, CẤU HÌNH NỀN
1. Xóa WeatherForecast.cs, Controllers/WeatherForecastController.cs, request weatherforecast trong NguyenHoaiPhong_1681.http.
2. Tạo thư mục: Controllers/, Services/, Data/, Entities/, Enums/, DTOs/, Middleware/, Options/, Helpers/, wwwroot/.
3. Cài thư viện. Với mỗi gói: `dotnet package search <Tên> --exact-match` → chọn bản mới nhất tương thích net8.0 → `dotnet add package <Tên> --version <version cụ thể>`. Gói Microsoft.* PHẢI là 8.0.x. Không bịa tên/version; gói nào không có hoặc không tương thích net8.0 thì DỪNG và báo tôi.
   - Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Design, Microsoft.EntityFrameworkCore.Tools (8.0.x)
   - Microsoft.AspNetCore.Authentication.JwtBearer (8.0.x)
   - BCrypt.Net-Next, Google.Apis.Auth, MailKit, Twilio, QRCoder
   - FaceAiSharp: chạy `dotnet package search FaceAiSharp`, cài gói chính + gói model detector SCRFD + gói model ArcFace (hoặc gói Bundle nếu có); SixLabors.ImageSharp đúng version FaceAiSharp yêu cầu.
   - Rate limiting dùng sẵn trong .NET 8, không cài thêm.
   - dotnet-ef: `dotnet tool install --global dotnet-ef --version 8.*` nếu chưa có.
4. Options pattern (Options/): JwtOptions {Issuer, Audience, Key, AccessMinutes, RefreshDays}; GoogleOptions {WebClientId}; SmtpOptions {Host, Port, User, AppPassword, FromName}; TwilioOptions {Enabled, AccountSid, AuthToken, FromNumber}; FaceOptions {MatchThreshold, GrayZone}; QrOptions {WindowSeconds}; AuthOptions {AutoApprove}; OtpOptions {DevMode, ExpireMinutes, ResendSeconds, MaxSendPerSession, MaxSmsPerPhonePerDay, MaxAttempts, LockMinutes}; AppOptions {BaseUrl}; SeedOptions {LocationLat, LocationLng, RadiusMeters}.
5. appsettings.json (giữ Logging, AllowedHosts) chỉ placeholder: ConnectionStrings:Default = "Server=localhost\\SQLEXPRESS;Database=NguyenHoaiPhong_1681Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"; App:BaseUrl "http://localhost:5280"; Jwt (AccessMinutes 30, RefreshDays 7, Key placeholder ≥ 32 ký tự); Face (0.45, 0.05); Qr:WindowSeconds 30; Auth:AutoApprove false; Otp (DevMode true, 5, 60, 5, 10, 5, 15); Twilio:Enabled false; Seed (RadiusMeters 200). Chạy `dotnet user-secrets init`.
6. Properties/launchSettings.json: GIỮ profile http (5280) và https (7092); THÊM profile "lan" applicationUrl "http://0.0.0.0:5280", ASPNETCORE_ENVIRONMENT Development, launchUrl swagger.
7. Middleware/ExceptionMiddleware: trả ProblemDetails, có field `message` tiếng Việt, ẩn stack trace ngoài Development. Tạo AppException(statusCode, message) để service ném lỗi nghiệp vụ.
8. Program.cs: bind Options; JWT Bearer authentication (validate issuer, audience, key, lifetime, ClockSkew 1 phút; RoleClaimType = "role"); authorization policy "ManagerOrAdmin"; CORS AllowAll ở Development; rate limiter fixed window policy "auth" 10 req/phút/IP và policy "kiosk" 30 req/phút/IP; Swagger có nút Authorize Bearer + XML comments (<GenerateDocumentationFile>true</GenerateDocumentationFile>, NoWarn 1591 trong csproj) + hỗ trợ IFormFile; Development KHÔNG UseHttpsRedirection. Thứ tự: ExceptionMiddleware → UseStaticFiles → CORS → RateLimiter → Authentication → Authorization → MapControllers.
9. Tạo Helpers/ClaimsExtensions (GetUserId, GetRole, GetDepartmentId), Helpers/PhoneHelper (chuẩn hóa 0xxxxxxxxx → +84xxxxxxxxx, che số dạng +84 9** *** 567) và Helpers/MaskHelper cho email.
10. Tạo file .gitignore chuẩn Visual Studio (bin/, obj/, .vs/, *.user, appsettings.*.local.json). Chỉ tạo file, không chạy lệnh git.
```

## Bước 2 – Entities, DbContext, migration, seed

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Bước trước đã cài thư viện và cấu hình nền; đọc code hiện có trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 2 – ENTITIES, DBCONTEXT, MIGRATION, SEED
1. Enums/: Role (Admin, Manager, Employee); UserStatus (PendingApproval, Active, Locked); AttendanceType (CheckIn, CheckOut); AttendanceMethod (Face, Qr, Manual); AttendanceStatus (OnTime, Late, EarlyLeave); LeaveType (Leave, Explanation, LateNotice); LeaveStatus (Pending, Approved, Rejected, Cancelled); OtpPurpose (RegisterVerify, Login, PhoneVerify); OtpChannel (Email, Sms).
2. Entities/ (Id int identity; CreatedAt, UpdatedAt UTC qua base class + tự set trong SaveChangesAsync):
   - User: Username (unique), Email (unique), PhoneNumber (E.164), PhoneVerified, PasswordHash (nullable), FullName, AvatarUrl, Role, Status, EmailVerified, GoogleSubjectId (nullable, unique có filter), DepartmentId?, BoundDeviceId?, FailedOtpCount, LockoutUntil?, IsDeleted
   - Department: Name, Description, ManagerId?, IsDeleted
   - Shift: Name, StartTime (TimeOnly), EndTime (TimeOnly), LateToleranceMinutes, IsActive, IsDeleted
   - WorkLocation: Name, Address, Latitude, Longitude, RadiusMeters, WifiBssid?, KioskKey (32 ký tự ngẫu nhiên), QrSecret (byte[32] ngẫu nhiên), IsActive, IsDeleted
   - ShiftAssignment: UserId, ShiftId, WorkLocationId, WorkDate (DateOnly); unique (UserId, WorkDate, ShiftId)
   - AttendanceRecord: UserId, ShiftAssignmentId?, Type, Timestamp, Latitude, Longitude, DistanceMeters, IsMockLocation, DeviceId, WifiBssid?, Method, FaceScore?, LivenessPassed?, QrWindow (long?), TrustScore (int), IsSuspicious, Status, Note?, EditedById?, EditReason?
   - LeaveRequest: UserId, Type, FromDate, ToDate (DateOnly), Reason, Status, ReviewerId?, ReviewNote?, ReviewedAt?
   - OtpCode: UserId, SessionId (Guid, index), Purpose, Channel?, CodeHash?, ExpiresAt, Attempts, SendCount, LastSentAt?, Used
   - OtpSendLog: UserId, Channel, Destination (đã che), Success, Error?
   - RefreshToken: UserId, TokenHash (index), ExpiresAt, RevokedAt?, DeviceId
   - FaceEmbedding: UserId (unique), Vector (float[512] → varbinary qua ValueConverter + ValueComparer), SampleCount
   - FaceChallenge: UserId?, Token (Guid, index), Actions (string), ExpiresAt, Used
   - LocationShare: UserId, Latitude, Longitude, Accuracy, Timestamp, IsSharing
3. Data/AppDbContext: Fluent API, enum lưu dạng string, global query filter IsDeleted cho User/Department/Shift/WorkLocation, DeleteBehavior.Restrict cho mọi FK tới User (tránh multiple cascade path SQL Server), Latitude/Longitude kiểu double. Đăng ký DbContext trong Program.cs với ConnectionStrings:Default.
4. Data/DbSeeder (chạy khi DB trống):
   - admin / Admin@123 (Admin); quanly / Manager@123 (Manager, trưởng "Phòng Kỹ thuật"); nhanvien1, nhanvien2 / Employee@123 (Employee, Phòng Kỹ thuật). Tất cả Active, EmailVerified; nhanvien1 PhoneVerified=true với số placeholder +84900000001. Mật khẩu hash BCrypt.
   - 2 phòng ban (Phòng Kỹ thuật, Phòng Kinh doanh); 3 ca: Sáng 08:00–12:00, Chiều 13:00–17:00, Hành chính 08:00–17:00, trễ cho phép 10 phút.
   - 1 WorkLocation "HUTECH" lấy Seed:LocationLat/LocationLng/RadiusMeters, KioskKey + QrSecret bằng RandomNumberGenerator; log ra console kioskUrl = "{App:BaseUrl}/kiosk.html?locationId={id}&key={KioskKey}".
   - Phân ca Hành chính cho nhanvien1, nhanvien2 trong 14 ngày tới (trừ Chủ nhật).
   - Bản ghi chấm công 7 ngày trước cho 2 nhân viên (có cả Method Face và Qr, ≥1 Late, ≥1 IsSuspicious với TrustScore < 60) + 2 đơn nghỉ (1 Pending, 1 Approved).
5. Program.cs: lúc khởi động tạo scope → `Database.MigrateAsync()` → DbSeeder.SeedAsync().
6. Chạy `dotnet ef migrations add InitialCreate -o Data/Migrations` rồi `dotnet build`.
```

## Bước 3 – Auth: JWT, OTP Email/SMS, Google

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Đã có: thư viện, Options, ExceptionMiddleware/AppException, Entities, AppDbContext, seed. Đọc code hiện có trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 3 – AUTH
Services (interface + impl, đăng ký DI): TokenService, OtpService, EmailSender (MailKit), SmsSender (Twilio), AuthService.
1. TokenService: JWT claims sub, unique_name, role, departmentId; access token AccessMinutes; refresh token 64 byte ngẫu nhiên, lưu hash SHA256, rotate khi refresh.
2. OtpService:
   - Sinh OTP 6 số bằng RandomNumberGenerator, lưu CodeHash SHA256, so sánh CryptographicOperations.FixedTimeEquals.
   - Hạn Otp:ExpireMinutes (5); gửi lại sau ≥ Otp:ResendSeconds (60); tối đa MaxSendPerSession (5) lần/phiên và MaxSmsPerPhonePerDay (10) SMS/số/ngày; gửi mã mới thì mã cũ của phiên hết hiệu lực.
   - Sai MaxAttempts (5) lần → User.LockoutUntil = now + LockMinutes (15).
   - Lỗi xác thực luôn là "Mã không đúng hoặc đã hết hạn".
   - Otp:DevMode=true → luôn log OTP ra console.
   - Mỗi lần gửi ghi OtpSendLog (không lưu mã).
3. EmailSender: MailKit qua Smtp (smtp.gmail.com:587, StartTls, app password), email HTML đơn giản có mã OTP.
4. SmsSender: Twilio:Enabled=false → chỉ log console, coi như thành công. Enabled=true → gửi qua Twilio Programmable Messaging (không dùng Twilio Verify), nội dung KHÔNG DẤU: "Ma xac thuc cham cong cua ban la {code}. Het han sau 5 phut. Khong chia se ma nay." Lỗi → AppException 502 "Không gửi được SMS, vui lòng chọn Email".
5. AuthController /api/auth (gắn rate limiter policy "auth"), DTO + DataAnnotations:
   1. POST /register {username, email, phone, password (≥8 ký tự, có chữ và số), fullName} → User PendingApproval, EmailVerified=false, chuẩn hóa phone → gửi OTP email (RegisterVerify) → {otpSessionId}
   2. POST /register/verify {otpSessionId, code} → EmailVerified=true; Auth:AutoApprove=true thì Active, ngược lại giữ PendingApproval.
   3. POST /login {username, password} → sai: 401 "Tên đăng nhập hoặc mật khẩu không đúng"; đang LockoutUntil → 423 kèm thời gian còn lại; Status khác Active → 403 ("Tài khoản đang chờ duyệt" / "Tài khoản đã bị khóa"); đúng → tạo OtpCode Login → {otpSessionId, availableChannels, maskedEmail, maskedPhone}; availableChannels luôn có "Email", chỉ có "Sms" khi PhoneVerified. CHƯA cấp token.
   4. POST /login/send-otp {otpSessionId, channel} → kiểm tra kênh hợp lệ → gửi → {expiresAt, resendAfterSeconds}
   5. POST /login/verify-otp {otpSessionId, code, deviceId} → {accessToken, refreshToken, expiresAt, user:{id, fullName, role, departmentId, hasFaceEnrolled}}; reset FailedOtpCount.
   6. POST /google {idToken, deviceId} → GoogleJsonWebSignature.ValidateAsync (Audience = Google:WebClientId) → tìm theo GoogleSubjectId rồi email (có email thì gắn GoogleSubjectId); chưa có → tạo Employee PendingApproval, EmailVerified=true, Username từ email; Active thì cấp token như bước 5, không thì 403.
   7. POST /refresh {refreshToken} → rotate; POST /logout {refreshToken} [Authorize] → revoke.
   8. POST /phone/send-otp [Authorize] → gửi SMS OTP (PhoneVerify) tới PhoneNumber của user → {otpSessionId}; POST /phone/verify {otpSessionId, code} [Authorize] → PhoneVerified=true.
6. XML comment ngắn cho từng endpoint. Không làm face-login ở bước này.
```

## Bước 4 – CRUD + phân quyền

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Đã có: Options, ExceptionMiddleware/AppException, Entities, AppDbContext, seed, Auth/JWT, ClaimsExtensions. Đọc code hiện có trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 4 – CRUD + PHÂN QUYỀN
Quy tắc chung:
- Controller mỏng, logic ở Service (interface + impl). DTO request/response riêng, validate DataAnnotations; không trả Entity ra ngoài; không trả PasswordHash, KioskKey (trừ kioskUrl cho Admin), QrSecret, Vector.
- List: page, pageSize (mặc định 20, tối đa 100), search, filter → trả {items, page, pageSize, totalCount}.
- DELETE = soft delete (IsDeleted) với User/Department/Shift/WorkLocation; bảng khác xóa thật nếu nghiệp vụ cho phép.
- Phân quyền kiểm tra ở Service bằng userId/role/departmentId từ claims: Manager chỉ thao tác nhân viên cùng DepartmentId; Employee chỉ dữ liệu của mình. Vi phạm → 403 message tiếng Việt.

Controllers:
1. /api/users: Admin CRUD (tạo user có mật khẩu, Active luôn); PATCH /{id}/approve; PATCH /{id}/status {status} (mở khóa thì reset FailedOtpCount, LockoutUntil); PATCH /{id}/role; PATCH /{id}/department; PATCH /{id}/reset-device (BoundDeviceId=null); GET /{id}/otp-logs (Admin); GET /me, PUT /me (FullName, AvatarUrl, PhoneNumber – đổi số thì PhoneVerified=false) cho mọi user; Manager GET danh sách nhân viên phòng mình.
2. /api/departments: Admin CRUD (không xóa được phòng còn nhân viên); Manager GET phòng mình.
3. /api/shifts: Admin CRUD (StartTime < EndTime); mọi user GET.
4. /api/work-locations: Admin CRUD (RadiusMeters 20–5000; tạo mới thì sinh KioskKey + QrSecret); POST /{id}/regenerate-kiosk-key; response cho Admin có kioskUrl = "{App:BaseUrl}/kiosk.html?locationId={id}&key={KioskKey}".
5. /api/shift-assignments: Admin/Manager CRUD (Manager chỉ phân ca cho phòng mình; chặn trùng UserId+WorkDate+ShiftId); POST /bulk {userIds, shiftId, workLocationId, fromDate, toDate, skipSunday}; Employee GET /my?from&to.
6. /api/leave-requests: Employee POST, PUT/DELETE khi còn Pending (DELETE → Cancelled); Manager/Admin PATCH /{id}/review {approve, note}; GET theo quyền (Employee: của mình; Manager: phòng mình; Admin: tất cả) + filter status/from/to.
7. /api/attendance (chỉ phần sửa ở bước này): Admin PUT /{id} {type, timestamp, status, note, editReason (bắt buộc)} → lưu EditedById.
XML comment ngắn cho từng endpoint.
```

## Bước 5 – Khuôn mặt (FaceAiSharp)

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Đã có: Options (FaceOptions), Entities FaceEmbedding/FaceChallenge, Auth/TokenService, CRUD. Đã cài FaceAiSharp + model SCRFD + ArcFace + ImageSharp. Đọc code hiện có (kể cả API thật của FaceAiSharp đã cài, không đoán API) trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 5 – KHUÔN MẶT
1. FaceService (Singleton, load model 1 lần, thread-safe):
   - GetEmbedding(Stream image): ImageSharp load (tối đa 2MB, JPEG/PNG) → SCRFD detect → bắt buộc đúng 1 mặt (0 → "Không tìm thấy khuôn mặt", >1 → "Phát hiện nhiều khuôn mặt") → align theo 5 landmark → ArcFace embedding → L2 normalize → kiểm tra mắt mở nếu FaceAiSharp có hàm eye state.
   - CosineSimilarity(float[] a, float[] b).
2. FaceChallengeService: tạo challenge 2 action ngẫu nhiên khác nhau trong [blink, turn_left, turn_right, smile], hạn 30s; Consume(token, userId?) → hết hạn / đã dùng / sai user → AppException 400.
3. FaceController /api/face:
   - POST /challenge (AllowAnonymous, nhận username tùy chọn) → {challengeToken, actions, expiresAt}
   - POST /enroll [Authorize] multipart: images (3–5 file) + challengeToken → embedding từng ảnh → trung bình → normalize → lưu/ghi đè FaceEmbedding (SampleCount). Ảnh không lưu.
   - POST /verify [Authorize] multipart: image + challengeToken + livenessPassed → {score, matched, retry}
4. Quy tắc so khớp: matched = score ≥ Face:MatchThreshold (0.45); score trong [threshold − GrayZone, threshold) → matched=false, retry=true; livenessPassed=false → matched=false. Chưa enroll → 400 "Bạn chưa đăng ký khuôn mặt".
5. Thêm POST /api/auth/face-login (vào AuthController, rate limit "auth") multipart: username + image + challengeToken + livenessPassed + deviceId → so 1:1 với vector của username → matched và user Active thì cấp token như /login/verify-otp; không thì 401 kèm retry.
6. Đưa logic verify vào method dùng lại được (vd FaceService.VerifyUserAsync(userId, stream)) để bước chấm công gọi lại.
```

## Bước 6 – QR động + trang kiosk

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Đã có: QrOptions, WorkLocation (KioskKey, QrSecret), rate limiter policy "kiosk", UseStaticFiles, thư viện QRCoder. Đọc code hiện có trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 6 – QR ĐỘNG TỰ SINH
1. QrService:
   - window = floor(UnixSeconds / Qr:WindowSeconds) (30s).
   - Generate(location) → token = Base64Url( locationId | window | HMACSHA256(QrSecret, $"{locationId}|{window}") lấy 16 byte đầu ); expiresAt = đầu window kế tiếp. Không lưu token vào DB.
   - Validate(token, expectedLocationId) → giải mã → đúng locationId → window bằng window hiện tại hoặc liền trước → so HMAC bằng FixedTimeEquals → trả window. Sai/hết hạn → AppException 400 "Mã QR đã hết hạn, vui lòng quét lại mã đang hiển thị".
   - RenderPng(token) bằng QRCoder PngByteQRCode, ECC level M, 20 pixel/module → base64.
2. QrController /api/qr:
   - GET /kiosk?locationId&key [AllowAnonymous, rate limiter "kiosk"] → so KioskKey bằng FixedTimeEquals (sai → 401) → {token, expiresAt, secondsLeft, locationName, qrPngBase64}
   - GET /current?locationId [Authorize ManagerOrAdmin] → giống trên (cho quản lý mở trên app).
3. wwwroot/kiosk.html: HTML + CSS + JS thuần trong 1 file, KHÔNG dùng CDN. Đọc locationId & key từ query string; gọi /api/qr/kiosk; hiển thị QR lớn giữa màn hình (giữ tỉ lệ vuông, nền trắng để dễ quét), tên địa điểm, đồng hồ giờ hiện tại, thanh đếm ngược tới lần đổi mã; tự gọi lại khi secondsLeft ≤ 2; mất mạng → hiện "Mất kết nối, đang thử lại" và thử lại mỗi 3 giây; sai key → "Đường dẫn kiosk không hợp lệ". Giao diện tối giản, chữ tiếng Việt, chạy tốt trên laptop và máy tính bảng.
4. Không đụng tới chấm công ở bước này (bước sau mới dùng QrService.Validate).
```

## Bước 7 – Chấm công + Trust Score

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Đã có: Entities, Auth, CRUD (kể cả Admin PUT /api/attendance/{id}), FaceService + FaceChallengeService, QrService. Đọc code hiện có trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG có vân tay. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 7 – CHẤM CÔNG + TRUST SCORE
1. Helpers/GeoHelper.Haversine (mét). Helpers/VnTime: TimeZoneInfo "SE Asia Standard Time", fallback "Asia/Ho_Chi_Minh"; hàm Now, Today.
2. AttendanceService + AttendanceController /api/attendance [Authorize]:
   - POST /check-in, POST /check-out, multipart:
     chung: latitude, longitude, accuracy, isMockLocation, deviceId, wifiBssid?, method (Face | Qr)
     method=Face: image, challengeToken, livenessPassed
     method=Qr: qrToken
   Xử lý theo thứ tự:
   1. Tìm ShiftAssignment hôm nay (giờ VN) của user → không có → 400 "Hôm nay bạn không có ca làm".
   2. Haversine tới WorkLocation của ca; vượt RadiusMeters → 400 "Bạn đang cách điểm chấm công {x} m".
   3. Face → consume challenge + FaceService verify; không matched → 400 kèm {score, retry}. Qr → QrService.Validate(qrToken, workLocationId); cùng user đã dùng (locationId, window) này → 400 "Mã QR này bạn đã dùng".
   4. BoundDeviceId null → gán deviceId; khác → vẫn ghi nhận, đánh dấu deviceMismatch.
   5. TrustScore = 100; isMockLocation −40; deviceMismatch −25; WorkLocation có WifiBssid mà wifiBssid không khớp −10; Face: !livenessPassed −30, score < threshold + 0.1 −10; Qr: −10. Clamp 0–100; < 60 → IsSuspicious = true.
   6. Status: check-in sau StartTime + LateToleranceMinutes → Late, ngược lại OnTime; check-out trước EndTime → EarlyLeave.
   7. Chặn check-in trùng trong cùng ca; chặn check-out khi chưa check-in.
   Trả {recordId, type, status, trustScore, isSuspicious, distanceMeters, method, timestamp, reasons[] (lý do bị trừ điểm, tiếng Việt)}.
   - GET /my?from&to ; GET /today (ca hôm nay, đã check-in/out chưa, giờ) ; GET /department?date (Manager) ; GET / (Admin, filter userId/departmentId/from/to/method/isSuspicious, phân trang) ; GET /suspicious (Admin).
3. Giữ nguyên Admin PUT /{id} đã có.
```

## Bước 8 – Chia sẻ vị trí + Dashboard

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. KHÔNG đổi tên project/namespace/TargetFramework. Đã có: Entities (LocationShare, AttendanceRecord...), Auth, CRUD, chấm công, VnTime. Đọc code hiện có trước khi làm. Kết thúc bằng `dotnet build` 0 error và tóm tắt file đã tạo/sửa. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 8 – CHIA SẺ VỊ TRÍ + DASHBOARD
1. LocationsController /api/locations [Authorize]:
   - POST /share {latitude, longitude, accuracy, isSharing} (Employee; app gọi mỗi 30s khi bật; isSharing=false là tắt chia sẻ)
   - GET /live → Manager: nhân viên phòng mình đang chia sẻ, vị trí mới nhất trong 5 phút; Admin: tất cả. Trả {userId, fullName, latitude, longitude, accuracy, timestamp}.
   - GET /history/{userId}?date → danh sách điểm trong ngày (theo quyền).
2. DashboardController /api/dashboard [Authorize]:
   - GET /admin (Admin): tổng nhân viên Active, có mặt hôm nay, đi trễ hôm nay, vắng hôm nay (có ca nhưng chưa check-in sau giờ bắt đầu + tolerance), số bản ghi nghi vấn 7 ngày, tỉ lệ Face/Qr, mảng 7 ngày gần nhất {date, present, late, absent}.
   - GET /manager (Manager): như trên nhưng chỉ phòng mình + danh sách đơn chờ duyệt.
   - GET /me: tổng giờ làm tháng này, số lần trễ, số ngày nghỉ được duyệt, trạng thái hôm nay.
3. Truy vấn dùng AsNoTracking, tính theo giờ VN.
```

## Bước 9 – File .http, README, kiểm tra .gitignore

```text
NGỮ CẢNH: Thư mục hiện tại là ASP.NET Core Web API đã tạo bằng Visual Studio 2026 cho backend app "chấm công nhân viên" (client Flutter Android). Solution NguyenHoaiPhong_1681.slnx, project NguyenHoaiPhong_1681.csproj nằm ngay thư mục gốc, namespace NguyenHoaiPhong_1681, net8.0. Backend đã đủ chức năng. Đọc toàn bộ controller hiện có trước khi viết tài liệu, không bịa endpoint. Kết thúc bằng `dotnet build` 0 error. CHỈ làm phần yêu cầu dưới đây. KHÔNG chạy lệnh git dưới bất kỳ hình thức nào; việc git người dùng tự làm.

BƯỚC 9 – FILE TEST + TÀI LIỆU
1. NguyenHoaiPhong_1681.http: @host = http://localhost:5280; request mẫu theo luồng: login → login/send-otp (Email) → login/verify-otp (lưu accessToken vào biến) → GET users/me → GET qr/current → POST attendance/check-in method=Qr → GET dashboard/admin. Ghi chú lấy OTP từ console khi Otp:DevMode=true.
2. README.md tiếng Việt:
   - Yêu cầu: Visual Studio 2026, .NET 8 SDK, SQL Server Express + SSMS.
   - Chạy: clone → mở NguyenHoaiPhong_1681.slnx → set user-secrets (Jwt:Key, Smtp:User, Smtp:AppPassword, Google:WebClientId, Twilio:*, Seed:LocationLat/Lng) với lệnh mẫu → F5 (tự tạo DB + dữ liệu mẫu). Phương án connection string "(localdb)\\MSSQLLocalDB".
   - Gọi từ app: máy ảo Android http://10.0.2.2:5280; điện thoại thật dùng profile "lan" (mở firewall cổng 5280) hoặc Dev Tunnels (persistent, public).
   - Trang kiosk QR: lấy kioskUrl ở console lúc seed hoặc API work-locations, mở trên laptop/máy tính bảng.
   - Tài khoản seed, bảng danh sách endpoint (method, path, quyền, mô tả), luật OTP, công thức Trust Score.
   - Hướng dẫn lấy Google Web Client ID, Gmail App Password, Twilio (dùng tài khoản trial, không cần nâng cấp; trial chỉ gửi tới số đã thêm ở Verified Caller IDs; Twilio:Enabled=false thì OTP in ra console).
   - Ghi chú model ArcFace/InsightFace chỉ dùng học tập phi thương mại.
3. KHÔNG chạy bất kỳ lệnh git nào (init, add, commit, branch, push, remote...). Chỉ:
   - Kiểm tra file .gitignore (đã tạo ở bước 1) loại đủ bin/, obj/, .vs/, *.user, appsettings.*.local.json; thiếu thì bổ sung vào file.
   - Thêm vào README mục "Hướng dẫn Git cho nhóm" gồm các lệnh để người dùng TỰ chạy: git config tên/email lần đầu; tạo repo Private trên GitHub (không tick README/.gitignore); git init tại thư mục cha D:\KiemTraMon, git branch -M main, git add ., kiểm tra git status không có bin/obj/.vs, commit, git remote add origin, push main; tạo và push nhánh dev; mời thành viên ở Settings → Collaborators; quy trình hằng ngày: checkout dev → pull → tạo feature/<tên-việc> → commit → push → Pull Request vào dev; ghi chú user-secrets nằm ngoài repo (%APPDATA%\Microsoft\UserSecrets) nên không bị đẩy lên.
```
