/// Địa chỉ backend.
/// - Máy ảo Android: http://10.0.2.2:5280
/// - Điện thoại thật: `http://IP-LAN-may-tinh:5280` (chạy backend bằng profile "lan")
/// Có thể ghi đè khi chạy: flutter run --dart-define=API_BASE=http://192.168.1.10:5280
const String apiBase = String.fromEnvironment('API_BASE', defaultValue: 'http://10.0.2.2:5280');
