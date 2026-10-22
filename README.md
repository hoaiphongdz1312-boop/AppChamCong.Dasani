# Backend Chấm công nhân viên (NguyenHoaiPhong_1681)

ASP.NET Core 8 Web API cho ứng dụng di động **chấm công nhân viên** (client Flutter, Android).

Tính năng chính:

- Đăng nhập 2 bước: mật khẩu → OTP qua Email hoặc SMS. Có thêm đăng nhập Google và đăng nhập bằng khuôn mặt.
- Phân quyền Admin / Manager / Employee, quản lý người dùng, phòng ban, ca làm, địa điểm, phân ca, đơn nghỉ.
- Chấm công bằng **khuôn mặt** (SCRFD + ArcFace, liveness chạy ở app) hoặc **QR động** tự sinh, có điểm tin cậy (Trust Score) chống gian lận.
- Chia sẻ vị trí trực tiếp, dashboard cho Admin / Manager / nhân viên.
- Trang kiosk `kiosk.html` hiển thị QR động cho địa điểm chấm công.

Thư mục `mobile/` chứa app Flutter mẫu (đăng nhập OTP, xem lịch ca) để thử kết nối với backend.

---

## 1. Yêu cầu

| Thành phần | Ghi chú |
|---|---|
| Visual Studio 2026 | Workload **ASP.NET and web development** |
| .NET 8 SDK | Project dùng `net8.0` |
| SQL Server Express + SSMS | Hoặc LocalDB (xem mục 2.4) |
| Android Studio + Flutter | Chỉ cần khi chạy app trong `mobile/` |

---

## 2. Chạy backend

### 2.1. Lấy mã nguồn và mở project

```bash
git clone <địa-chỉ-repo>
```

Mở file `NguyenHoaiPhong_1681.slnx` bằng Visual Studio.

### 2.2. Đặt cấu hình bí mật (user-secrets)

Các giá trị nhạy cảm **không** nằm trong repo. Mở terminal tại thư mục chứa `NguyenHoaiPhong_1681.csproj` và chạy lần lượt (thay giá trị trong dấu `<>`). Project đã có sẵn `UserSecretsId`, không cần chạy `dotnet user-secrets init`.

```bash
# Bắt buộc: khóa ký JWT, tối thiểu 32 ký tự ngẫu nhiên
dotnet user-secrets set "Jwt:Key" "<chuoi-ngau-nhien-it-nhat-32-ky-tu>"

# Chuỗi kết nối SQL Server của máy bạn (nếu khác mặc định trong appsettings)
dotnet user-secrets set "ConnectionStrings:Default" "Server=<TEN-MAY>\SQLEXPRESS;Database=NguyenHoaiPhong_1681Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

# Tọa độ địa điểm HUTECH dùng để seed (lấy từ Google Maps: chuột phải → copy tọa độ)
dotnet user-secrets set "Seed:LocationLat" "10.8021"
dotnet user-secrets set "Seed:LocationLng" "106.7147"

# Gửi OTP qua Email (xem mục 9.2)
dotnet user-secrets set "Smtp:User" "<tai-khoan>@gmail.com"
dotnet user-secrets set "Smtp:AppPassword" "<app-password-16-ky-tu>"

# Đăng nhập Google (xem mục 9.1)
dotnet user-secrets set "Google:WebClientId" "<xxxx>.apps.googleusercontent.com"

# Gửi OTP qua SMS bằng Twilio (xem mục 9.3)
dotnet user-secrets set "Twilio:Enabled" "true"
dotnet user-secrets set "Twilio:AccountSid" "<AC...>"
dotnet user-secrets set "Twilio:AuthToken" "<auth-token>"
dotnet user-secrets set "Twilio:FromNumber" "<+1...>"
```

Xem lại các giá trị đã đặt:

```bash
dotnet user-secrets list
```

> Không cấu hình Smtp/Google/Twilio vẫn chạy được: khi `Otp:DevMode=true`, mã OTP luôn được in ra console của backend (dòng `[DEV OTP] ...`), email không gửi thật nếu chưa có Smtp, và SMS chỉ in ra console khi `Twilio:Enabled=false`.

Giá trị `Seed:LocationLat/Lng` chỉ có tác dụng **lần chạy đầu tiên** (lúc DB còn trống). Muốn đổi sau này, sửa địa điểm qua `PUT /api/work-locations/{id}`.

### 2.3. Chạy (F5)

Chọn profile **http** (cổng 5280) hoặc **https** (cổng 7092) rồi bấm F5. Khi khởi động, ứng dụng tự:

1. Chạy migration và tạo database `NguyenHoaiPhong_1681Db`.
2. Seed dữ liệu mẫu nếu DB còn trống (xem mục 5).
3. Nạp model khuôn mặt ở chế độ nền (lần đầu mất vài giây).

Swagger: `http://localhost:5280/swagger`. Ở môi trường Development không chuyển hướng HTTPS, để app Flutter gọi được bằng `http://`.

### 2.4. Phương án LocalDB

Nếu máy không có SQL Server Express, dùng LocalDB bằng cách đặt chuỗi kết nối:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\MSSQLLocalDB;Database=NguyenHoaiPhong_1681Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Chuỗi mặc định trong `appsettings.json` / `appsettings.Development.json` có thể là tên máy của người đã tạo project. Giá trị trong user-secrets luôn được ưu tiên và ghi đè các file này, nên mỗi thành viên chỉ cần đặt chuỗi của riêng mình.

### 2.5. Reset database

Xóa database (trong SSMS hoặc `dotnet ef database drop`) rồi chạy lại để seed mới. Tạo migration mới khi sửa Entity:

```bash
dotnet ef migrations add TenMigration -o Data/Migrations
```

---

## 3. Gọi API từ app và thiết bị

| Cách chạy app | Địa chỉ backend |
|---|---|
| Máy ảo Android (emulator) | `http://10.0.2.2:5280` |
| Điện thoại thật cùng Wi-Fi | `http://<IP-LAN-máy-tính>:5280` (dùng profile `lan`) |
| Điện thoại thật khác mạng | URL Dev Tunnels (xem bên dưới) |

`10.0.2.2` là địa chỉ máy tính nhìn từ máy ảo Android, không dùng `localhost`.

### 3.1. Điện thoại thật bằng profile `lan`

1. Chọn profile **lan** trong Visual Studio (nghe ở `http://0.0.0.0:5280`), có thể cần chạy Visual Studio bằng quyền Administrator.
2. Mở cổng 5280 cho Windows Firewall (PowerShell quyền Administrator):

```powershell
New-NetFirewallRule -DisplayName "ChamCong API 5280" -Direction Inbound -Protocol TCP -LocalPort 5280 -Action Allow
```

3. Lấy IP LAN của máy bằng `ipconfig` (dòng IPv4) và dùng `http://<IP>:5280` trong app.

### 3.2. Dev Tunnels (persistent, public)

Dùng khi điện thoại khác mạng hoặc mạng chặn kết nối giữa các thiết bị:

1. Trong Visual Studio: **Debug → Dev Tunnels...** → **Create A Tunnel**.
2. Chọn **Tunnel Type: Persistent**, **Access: Public**.
3. Chạy backend, dùng URL dạng `https://xxxx.devtunnels.ms` làm địa chỉ API trong app.
4. Đặt `App:BaseUrl` bằng URL tunnel để `kioskUrl` được sinh đúng:

```bash
dotnet user-secrets set "App:BaseUrl" "https://xxxx.devtunnels.ms"
```

Tunnel Public nghĩa là ai có link cũng truy cập được, chỉ dùng để thử nghiệm.

### 3.3. App Flutter trong `mobile/`

Android Studio → **File → Open** → chọn thư mục `mobile`. Chạy backend trước, bật máy ảo rồi Run. Với điện thoại thật:

```bash
flutter run --dart-define=API_BASE=http://192.168.1.10:5280
```

App đã bật `usesCleartextTraffic` và khai báo quyền INTERNET, CAMERA, vị trí trong `AndroidManifest.xml`.

---

## 4. Trang kiosk QR

Trang `wwwroot/kiosk.html` hiển thị QR động (đổi mỗi 30 giây) cho một địa điểm. Mở trên laptop hoặc máy tính bảng đặt tại cổng.

Lấy đường dẫn kiosk:

- Dòng log `Kiosk URL: ...` trong console lúc seed, hoặc
- Gọi `GET /api/work-locations` bằng tài khoản Admin, trường `kioskUrl`.

Dạng đường dẫn:

```
http://localhost:5280/kiosk.html?locationId=1&key=<KioskKey>
```

- `key` sai hoặc thiếu: trang báo "Đường dẫn kiosk không hợp lệ".
- Mất mạng: trang hiện "Mất kết nối, đang thử lại" và thử lại mỗi 3 giây.
- Đổi key khi bị lộ link: `POST /api/work-locations/{id}/regenerate-kiosk-key` (Admin).
- Máy tính bảng truy cập qua mạng LAN thì dùng IP máy chủ thay cho `localhost`.

Quản lý/Admin cũng có thể lấy QR hiện tại trong app qua `GET /api/qr/current?locationId=...`.

---

## 5. Tài khoản và dữ liệu seed

| Tài khoản | Mật khẩu | Vai trò | Ghi chú |
|---|---|---|---|
| `admin` | `Admin@123` | Admin | |
| `quanly` | `Manager@123` | Manager | Trưởng "Phòng Kỹ thuật" |
| `nhanvien1` | `Employee@123` | Employee | Phòng Kỹ thuật, đã xác minh số điện thoại (`+84900000001`) |
| `nhanvien2` | `Employee@123` | Employee | Phòng Kỹ thuật |

Tất cả đã Active và đã xác minh email. Dữ liệu mẫu khác:

- 2 phòng ban: Phòng Kỹ thuật, Phòng Kinh doanh.
- 3 ca: Sáng 08:00–12:00, Chiều 13:00–17:00, Hành chính 08:00–17:00 (trễ cho phép 10 phút).
- 1 địa điểm "HUTECH" (bán kính theo `Seed:RadiusMeters`, mặc định 200 m).
- Phân ca Hành chính cho `nhanvien1`, `nhanvien2` quanh hôm nay (7 ngày trước đến 14 ngày sau, trừ Chủ nhật).
- Chấm công mẫu 7 ngày trước (có cả Face và QR, có bản đi trễ, có bản nghi vấn) và 2 đơn nghỉ (1 chờ duyệt, 1 đã duyệt).

Đổi mật khẩu các tài khoản seed trước khi đưa lên môi trường thật.

---

## 6. API

Mọi response lỗi có dạng ProblemDetails kèm trường `message` tiếng Việt. Lỗi validate trả 400 kèm danh sách lỗi trong `errors`. Danh sách trả về dạng `{ items, page, pageSize, totalCount }` (`pageSize` mặc định 20, tối đa 100). Giờ nghiệp vụ tính theo giờ Việt Nam, mọi thời điểm trong response là UTC (có hậu tố `Z`).

Quyền: **Mọi user** = đã đăng nhập (kể cả Admin), **Mgr/Admin** = Manager hoặc Admin.

### 6.1. Auth — `/api/auth` (rate limit 10 request/phút/IP)

| Method | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/auth/register` | Công khai | Đăng ký, gửi OTP xác minh email |
| POST | `/api/auth/register/verify` | Công khai | Xác minh email bằng OTP (Active ngay nếu `Auth:AutoApprove=true`, nếu không chờ Admin duyệt) |
| POST | `/api/auth/login` | Công khai | Bước 1: kiểm tra mật khẩu, trả `otpSessionId` và kênh khả dụng (chưa cấp token) |
| POST | `/api/auth/login/send-otp` | Công khai | Gửi OTP qua `Email` hoặc `Sms` |
| POST | `/api/auth/login/verify-otp` | Công khai | Bước 2: xác minh OTP, cấp access token (30 phút) và refresh token (7 ngày) |
| POST | `/api/auth/google` | Công khai | Đăng nhập bằng Google ID token |
| POST | `/api/auth/face-login` | Công khai | Đăng nhập bằng khuôn mặt (multipart) |
| POST | `/api/auth/refresh` | Công khai | Đổi refresh token lấy cặp token mới (rotate) |
| POST | `/api/auth/logout` | Mọi user | Thu hồi refresh token |
| POST | `/api/auth/phone/send-otp` | Mọi user | Gửi SMS OTP xác minh số điện thoại |
| POST | `/api/auth/phone/verify` | Mọi user | Xác minh số điện thoại |

### 6.2. Người dùng — `/api/users`

| Method | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/users` | Mgr/Admin | Danh sách (Admin: tất cả; Manager: phòng mình). Lọc: `search`, `role`, `status`, `departmentId` |
| GET | `/api/users/me` | Mọi user | Thông tin của tôi |
| PUT | `/api/users/me` | Mọi user | Sửa hồ sơ (đổi số điện thoại thì phải xác minh lại) |
| GET | `/api/users/{id}` | Mọi user | Chi tiết (bản thân, Manager cùng phòng, hoặc Admin) |
| POST | `/api/users` | Admin | Tạo người dùng (Active ngay) |
| PUT | `/api/users/{id}` | Admin | Cập nhật thông tin |
| DELETE | `/api/users/{id}` | Admin | Xóa mềm |
| PATCH | `/api/users/{id}/approve` | Admin | Duyệt tài khoản chờ duyệt |
| PATCH | `/api/users/{id}/status` | Admin | Khóa/mở khóa (mở khóa reset bộ đếm OTP sai) |
| PATCH | `/api/users/{id}/role` | Admin | Đổi vai trò |
| PATCH | `/api/users/{id}/department` | Admin | Gán phòng ban |
| PATCH | `/api/users/{id}/reset-device` | Admin | Gỡ thiết bị đã gắn |
| GET | `/api/users/{id}/otp-logs` | Admin | Lịch sử gửi OTP (không chứa mã) |

### 6.3. Phòng ban, ca, địa điểm

| Method | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/departments` | Mgr/Admin | Danh sách (Manager chỉ thấy phòng mình) |
| GET | `/api/departments/{id}` | Mgr/Admin | Chi tiết |
| POST / PUT / DELETE | `/api/departments[/{id}]` | Admin | Tạo / sửa / xóa mềm (không xóa được khi còn nhân viên) |
| GET | `/api/shifts` | Mọi user | Danh sách ca |
| GET | `/api/shifts/{id}` | Mọi user | Chi tiết ca |
| POST / PUT / DELETE | `/api/shifts[/{id}]` | Admin | Tạo / sửa / xóa mềm (giờ bắt đầu < giờ kết thúc) |
| GET | `/api/work-locations` | Mgr/Admin | Danh sách địa điểm (chỉ Admin nhận `kioskUrl`) |
| GET | `/api/work-locations/{id}` | Mgr/Admin | Chi tiết |
| POST / PUT / DELETE | `/api/work-locations[/{id}]` | Admin | Tạo (tự sinh KioskKey, QrSecret) / sửa / xóa mềm. Bán kính 20–5000 m |
| POST | `/api/work-locations/{id}/regenerate-kiosk-key` | Admin | Đổi KioskKey |

### 6.4. Phân ca và đơn nghỉ

| Method | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/shift-assignments` | Mgr/Admin | Danh sách phân ca (Manager: phòng mình) |
| GET | `/api/shift-assignments/my` | Mọi user | Lịch của tôi, tham số `from`, `to` |
| GET | `/api/shift-assignments/{id}` | Mọi user | Chi tiết (theo quyền) |
| POST | `/api/shift-assignments` | Mgr/Admin | Tạo phân ca (chặn trùng nhân viên + ngày + ca) |
| POST | `/api/shift-assignments/bulk` | Mgr/Admin | Phân ca hàng loạt `{userIds, shiftId, workLocationId, fromDate, toDate, skipSunday}` |
| PUT / DELETE | `/api/shift-assignments/{id}` | Mgr/Admin | Sửa / xóa (không xóa được khi đã có chấm công) |
| GET | `/api/leave-requests` | Mọi user | Danh sách theo quyền. Lọc: `status`, `type`, `userId`, `from`, `to` |
| GET | `/api/leave-requests/{id}` | Mọi user | Chi tiết (theo quyền) |
| POST | `/api/leave-requests` | Employee, Manager | Tạo đơn nghỉ / giải trình / báo đến trễ |
| PUT | `/api/leave-requests/{id}` | Employee, Manager | Sửa đơn của mình khi còn chờ duyệt |
| DELETE | `/api/leave-requests/{id}` | Employee, Manager | Hủy đơn của mình (chuyển sang Cancelled) |
| PATCH | `/api/leave-requests/{id}/review` | Mgr/Admin | Duyệt/từ chối `{approve, note}` |

### 6.5. Khuôn mặt và QR

| Method | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/face/challenge` | Công khai | Tạo thử thách liveness (2 hành động ngẫu nhiên, hiệu lực 30 giây). Body tùy chọn `{username}` |
| POST | `/api/face/enroll` | Mọi user | Đăng ký khuôn mặt: multipart `images` (3–5 ảnh) + `challengeToken` |
| POST | `/api/face/verify` | Mọi user | So khớp: multipart `image`, `challengeToken`, `livenessPassed` → `{score, matched, retry}` |
| GET | `/api/qr/kiosk` | Công khai (cần `key`) | QR động cho kiosk, rate limit 30 request/phút/IP |
| GET | `/api/qr/current` | Mgr/Admin | QR động hiện tại của một địa điểm |

### 6.6. Chấm công, vị trí, dashboard

| Method | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/attendance/check-in` | Mọi user | Chấm công vào (multipart, `method=Face` hoặc `Qr`) |
| POST | `/api/attendance/check-out` | Mọi user | Chấm công ra (cùng tham số) |
| GET | `/api/attendance/my` | Mọi user | Lịch sử của tôi (mặc định 30 ngày) |
| GET | `/api/attendance/today` | Mọi user | Các ca hôm nay, đã check-in/out chưa |
| GET | `/api/attendance/department` | Manager | Chấm công phòng mình theo `date` |
| GET | `/api/attendance` | Admin | Tất cả. Lọc: `userId`, `departmentId`, `from`, `to`, `method`, `isSuspicious` |
| GET | `/api/attendance/suspicious` | Admin | Bản ghi nghi vấn |
| PUT | `/api/attendance/{id}` | Admin | Sửa bản ghi, bắt buộc `editReason` |
| POST | `/api/locations/share` | Employee | Gửi vị trí `{latitude, longitude, accuracy, isSharing}` (app gọi mỗi 30 giây khi bật) |
| GET | `/api/locations/live` | Mgr/Admin | Vị trí trực tiếp trong 5 phút gần nhất (Manager: phòng mình) |
| GET | `/api/locations/history/{userId}` | Mọi user | Lịch sử vị trí trong ngày `date` (theo quyền) |
| GET | `/api/dashboard/admin` | Admin | Tổng quan toàn công ty |
| GET | `/api/dashboard/manager` | Manager | Tổng quan phòng + đơn chờ duyệt |
| GET | `/api/dashboard/me` | Mọi user | Giờ làm tháng này, số lần trễ, ngày nghỉ được duyệt, trạng thái hôm nay |

Trang tĩnh: `GET /kiosk.html` (công khai, xác thực bằng `key` khi gọi `/api/qr/kiosk`).

Dùng file `NguyenHoaiPhong_1681.http` để thử nhanh luồng đăng nhập → lấy QR → chấm công → dashboard.

---

## 7. Luật OTP

- Mã 6 số sinh bằng `RandomNumberGenerator`, lưu dạng hash SHA256 (không lưu mã), so sánh bằng `FixedTimeEquals`.
- Hạn **5 phút**. Gửi lại sau tối thiểu **60 giây**. Tối đa **5 lần gửi/phiên**. Tối đa **10 SMS/số điện thoại/ngày**.
- Gửi mã mới thì mã cũ của phiên hết hiệu lực.
- Nhập sai **5 lần** thì khóa đăng nhập **15 phút**. Lỗi xác thực luôn là "Mã không đúng hoặc đã hết hạn".
- Kênh Email luôn có; kênh SMS chỉ có khi số điện thoại đã xác minh (`/api/auth/phone/send-otp` rồi `/api/auth/phone/verify`).
- Nội dung SMS không dấu: "Ma xac thuc cham cong cua ban la {code}. Het han sau 5 phut. Khong chia se ma nay."
- Twilio lỗi trả 502 "Không gửi được SMS, vui lòng chọn Email". Mọi lần gửi được ghi vào lịch sử (Admin xem ở `/api/users/{id}/otp-logs`), không lưu mã.
- Cấu hình ở mục `Otp:*` (`DevMode`, `ExpireMinutes`, `ResendSeconds`, `MaxSendPerSession`, `MaxSmsPerPhonePerDay`, `MaxAttempts`, `LockMinutes`). `Otp:DevMode=true` luôn in mã ra console, hãy tắt khi triển khai thật.

---

## 8. Công thức Trust Score (chấm công)

Chấm công theo thứ tự: tìm ca hôm nay → kiểm tra khoảng cách tới địa điểm (vượt bán kính thì từ chối) → xác minh Face hoặc QR → gắn thiết bị → tính điểm.

Điểm khởi đầu **100**, trừ theo bảng (kẹp trong khoảng 0–100). **Dưới 60 thì `isSuspicious = true`.**

| Điều kiện | Trừ |
|---|---|
| Vị trí giả lập (`isMockLocation`) | −40 |
| Thiết bị khác với thiết bị đã gắn (`BoundDeviceId`) | −25 |
| Địa điểm có `WifiBssid` mà Wi-Fi gửi lên không khớp | −10 |
| Face: không vượt qua liveness (`livenessPassed=false`) | −30 |
| Face: điểm khớp < `Face:MatchThreshold` + 0.1 | −10 |
| Qr (không xác minh danh tính bằng khuôn mặt) | −10 |

Thiết bị đầu tiên chấm công thành công sẽ được gắn cho tài khoản. Admin gỡ bằng `PATCH /api/users/{id}/reset-device`. Lý do bị trừ điểm được trả về trong `reasons` và lưu vào ghi chú bản ghi.

Quy tắc khác:

- **Trạng thái:** check-in sau giờ bắt đầu + thời gian trễ cho phép là `Late`, ngược lại `OnTime`; check-out trước giờ kết thúc là `EarlyLeave`.
- **Chặn trùng:** không check-in hai lần trong cùng ca, không check-out khi chưa check-in.
- **Khuôn mặt:** `matched` khi điểm cosine ≥ `Face:MatchThreshold` (0.45). Điểm trong vùng xám (`Face:GrayZone` = 0.05, tức [0.40, 0.45)) trả `matched=false, retry=true` để app cho quét lại. Mỗi challenge chỉ dùng một lần, hạn 30 giây, ảnh tối đa 2 MB (JPEG/PNG). Backend không lưu ảnh, chỉ lưu vector 512 chiều.
- **Liveness:** chạy ở app (ví dụ ML Kit); backend kiểm tra challenge và cờ `livenessPassed` do app gửi.
- **QR động:** token gồm `locationId`, số thứ tự khung 30 giây và chữ ký HMAC-SHA256 bằng khóa bí mật riêng của địa điểm. Server chấp nhận khung hiện tại và khung liền trước. Mỗi nhân viên chỉ dùng một token một lần.

---

## 9. Hướng dẫn lấy khóa dịch vụ

### 9.1. Google Web Client ID

1. Vào [Google Cloud Console](https://console.cloud.google.com/), tạo project (hoặc chọn project có sẵn).
2. **APIs & Services → OAuth consent screen**: chọn loại External, điền tên ứng dụng và email, thêm tài khoản thử nghiệm ở mục Test users.
3. **APIs & Services → Credentials → Create Credentials → OAuth client ID**:
   - Tạo một client loại **Web application**, sao chép **Client ID** (dạng `xxxx.apps.googleusercontent.com`). Đặt vào `Google:WebClientId` của backend.
   - Tạo thêm một client loại **Android** với package name của app (app mẫu trong `mobile/` là `vn.edu.hutech.cham_cong_app`) và SHA-1 của keystore debug (lấy bằng `cd mobile/android && ./gradlew signingReport`).
4. Trong app Flutter, dùng Web Client ID làm `serverClientId` khi gọi Google Sign-In để `idToken` có audience khớp với backend. App gửi `idToken` và `deviceId` tới `POST /api/auth/google`.

### 9.2. Gmail App Password (gửi OTP qua Email)

1. Bật **Xác minh 2 bước** cho tài khoản Gmail tại [myaccount.google.com/security](https://myaccount.google.com/security).
2. Vào [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords), tạo mật khẩu ứng dụng (đặt tên bất kỳ). Google hiển thị chuỗi 16 ký tự.
3. Đặt `Smtp:User` là địa chỉ Gmail và `Smtp:AppPassword` là chuỗi 16 ký tự (bỏ khoảng trắng). Không dùng mật khẩu đăng nhập Gmail thường.
4. Cấu hình mặc định: `smtp.gmail.com`, cổng 587, StartTls.

### 9.3. Twilio (gửi OTP qua SMS)

Có thể dùng **tài khoản trial, không cần nâng cấp**.

1. Đăng ký tại [twilio.com](https://www.twilio.com/), xác minh email và số điện thoại.
2. Trên Console lấy **Account SID** và **Auth Token** (trang chủ Console, mục Account Info).
3. Nhận số điện thoại trial (Get a trial phone number) và dùng làm `Twilio:FromNumber` (dạng `+1...`).
4. **Giới hạn của trial:** chỉ gửi được tới các số đã thêm ở **Verified Caller IDs** (Console → Phone Numbers → Manage → Verified Caller IDs → Add a new Caller ID, nhập số dạng `+84...` và xác minh bằng mã). Tin nhắn trial có thể kèm dòng "Sent from your Twilio trial account".
5. Nếu cần gửi tới số Việt Nam, bật quyền gửi tới Việt Nam ở **Messaging → Settings → Geo permissions**.
6. Đặt `Twilio:Enabled=true`, `Twilio:AccountSid`, `Twilio:AuthToken`, `Twilio:FromNumber` bằng user-secrets.
7. Muốn xác minh số điện thoại cho một tài khoản, số đó phải nằm trong Verified Caller IDs (khi dùng trial).

Khi `Twilio:Enabled=false` (mặc định), SMS không gửi thật: nội dung và mã OTP chỉ được in ra console và coi như gửi thành công.

---

## 10. Cấu trúc thư mục

```
Controllers/   API controller (mỏng, gọi Service)
Services/      Logic nghiệp vụ (interface + implementation)
Data/          AppDbContext, DbSeeder, Migrations/
Entities/      Entity EF Core
Enums/         Enum (lưu dạng chuỗi trong DB)
DTOs/          Request/Response
Middleware/    ExceptionMiddleware, AppException
Options/       Lớp cấu hình (Options pattern)
Helpers/       Claims, Phone, Mask, Hash, Geo, VnTime...
wwwroot/       kiosk.html
mobile/        App Flutter mẫu
```

Công nghệ: .NET 8, EF Core 8 (SQL Server), JWT Bearer, BCrypt, MailKit, Twilio, Google.Apis.Auth, QRCoder, FaceAiSharp (SCRFD + ArcFace) trên ONNX Runtime.

---

## 11. Lưu ý giấy phép model khuôn mặt

Backend nhận diện khuôn mặt bằng thư viện FaceAiSharp cùng các model đi kèm: bộ phát hiện **SCRFD** và bộ nhúng **ArcFace** (trọng số từ dự án **InsightFace**). Trọng số model InsightFace chỉ được phép dùng cho **mục đích học tập, nghiên cứu phi thương mại**. Nếu đưa sản phẩm vào sử dụng thương mại, cần thay bằng model có giấy phép phù hợp hoặc xin phép tác giả. Dự án này dùng cho môn học nên nằm trong phạm vi phi thương mại.

---

## 12. Hướng dẫn Git cho nhóm

Các lệnh dưới đây **bạn tự chạy** trong terminal (Git Bash hoặc PowerShell). Thư mục gốc của repo là thư mục cha **`D:\KiemTraMon`** (chứa `NguyenHoaiPhong_1681`). File `.gitignore` nằm trong `NguyenHoaiPhong_1681` đã loại `bin/`, `obj/`, `.vs/`, `*.user`, `appsettings.*.local.json`, build của Flutter...

### 12.1. Cấu hình lần đầu (mỗi máy một lần)

```bash
git config --global user.name "Họ và Tên"
git config --global user.email "email@example.com"
```

### 12.2. Tạo repo trên GitHub

1. Vào GitHub → **New repository**.
2. Đặt tên, chọn **Private**.
3. **Không** tick "Add a README file", "Add .gitignore", "Choose a license" (repo phải trống để đẩy code có sẵn lên).
4. Bấm **Create repository** và sao chép địa chỉ repo (dạng `https://github.com/<user>/<repo>.git`).

### 12.3. Đẩy code lần đầu

```bash
cd D:\KiemTraMon
git init
git branch -M main
git add .
git status
```

Kiểm tra kết quả `git status`: **không** được thấy `bin/`, `obj/`, `.vs/`, file `*.user`. Nếu thấy, dừng lại và kiểm tra `.gitignore`. Nếu mọi thứ ổn:

```bash
git commit -m "Khoi tao backend cham cong"
git remote add origin https://github.com/<user>/<repo>.git
git push -u origin main
```

### 12.4. Tạo nhánh dev

```bash
git checkout -b dev
git push -u origin dev
```

### 12.5. Mời thành viên

GitHub → repo → **Settings → Collaborators → Add people**, nhập tên tài khoản hoặc email GitHub của thành viên.

Thành viên mới clone repo:

```bash
git clone https://github.com/<user>/<repo>.git
cd <repo>
git checkout dev
```

Sau đó đặt user-secrets riêng theo mục 2.2.

### 12.6. Quy trình làm việc hằng ngày

```bash
git checkout dev
git pull
git checkout -b feature/<ten-viec>
# ... sửa code ...
git add .
git commit -m "Mo ta ngan gon viec da lam"
git push -u origin feature/<ten-viec>
```

Sau đó vào GitHub tạo **Pull Request** từ `feature/<ten-viec>` vào **`dev`**, nhờ bạn cùng nhóm xem rồi merge. Không commit trực tiếp vào `main`.

### 12.7. Về user-secrets

User-secrets nằm **ngoài repo**, tại `%APPDATA%\Microsoft\UserSecrets\40e112c4-e0a0-4cbc-af8d-c38ec6e178f1\secrets.json`, nên **không** bị đẩy lên GitHub. Vì vậy mỗi thành viên phải tự chạy các lệnh `dotnet user-secrets set` ở mục 2.2 trên máy của mình. Không dán Auth Token, App Password, `Jwt:Key` vào file trong repo hoặc tin nhắn nhóm.
