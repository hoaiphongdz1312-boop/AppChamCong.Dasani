import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import 'config.dart';

class ApiException implements Exception {
  final int status;
  final String message;
  ApiException(this.status, this.message);
  @override
  String toString() => message;
}

/// Client gọi backend: đăng nhập 2 bước (mật khẩu → OTP), lưu token, tự refresh khi 401.
class Api {
  Api._();
  static final Api instance = Api._();

  String? accessToken;
  String? refreshToken;
  Map<String, dynamic>? user;

  Future<void> load() async {
    final p = await SharedPreferences.getInstance();
    accessToken = p.getString('access');
    refreshToken = p.getString('refresh');
    final u = p.getString('user');
    user = u == null ? null : jsonDecode(u) as Map<String, dynamic>;
  }

  Future<void> _saveSession(Map<String, dynamic> r) async {
    accessToken = r['accessToken'] as String;
    refreshToken = r['refreshToken'] as String;
    user = r['user'] as Map<String, dynamic>;
    final p = await SharedPreferences.getInstance();
    await p.setString('access', accessToken!);
    await p.setString('refresh', refreshToken!);
    await p.setString('user', jsonEncode(user));
  }

  Future<void> logout() async {
    try {
      if (refreshToken != null) {
        await post('/api/auth/logout', {'refreshToken': refreshToken});
      }
    } catch (_) {}
    accessToken = refreshToken = user = null;
    final p = await SharedPreferences.getInstance();
    await p.clear();
  }

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (accessToken != null) 'Authorization': 'Bearer $accessToken',
      };

  dynamic _decode(http.Response r) {
    final body = r.body.isEmpty ? null : jsonDecode(utf8.decode(r.bodyBytes));
    if (r.statusCode >= 200 && r.statusCode < 300) return body;
    final msg = body is Map && body['message'] != null ? body['message'].toString() : 'Lỗi ${r.statusCode}';
    throw ApiException(r.statusCode, msg);
  }

  Future<dynamic> get(String path) => _send(() => http.get(Uri.parse('$apiBase$path'), headers: _headers));

  Future<dynamic> post(String path, Object body) =>
      _send(() => http.post(Uri.parse('$apiBase$path'), headers: _headers, body: jsonEncode(body)));

  Future<dynamic> _send(Future<http.Response> Function() call) async {
    try {
      var r = await call().timeout(const Duration(seconds: 15));
      if (r.statusCode == 401 && refreshToken != null && await _tryRefresh()) {
        r = await call().timeout(const Duration(seconds: 15));
      }
      return _decode(r);
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException(0, 'Không kết nối được máy chủ ($apiBase). Kiểm tra backend và địa chỉ API.');
    }
  }

  Future<bool> _tryRefresh() async {
    try {
      final r = await http
          .post(Uri.parse('$apiBase/api/auth/refresh'),
              headers: {'Content-Type': 'application/json'}, body: jsonEncode({'refreshToken': refreshToken}))
          .timeout(const Duration(seconds: 15));
      if (r.statusCode != 200) return false;
      await _saveSession(jsonDecode(r.body) as Map<String, dynamic>);
      return true;
    } catch (_) {
      return false;
    }
  }

  // ---- Đăng nhập 2 bước ----
  Future<Map<String, dynamic>> login(String username, String password) async =>
      Map<String, dynamic>.from(await post('/api/auth/login', {'username': username, 'password': password}));

  Future<void> sendOtp(String sessionId, String channel) =>
      post('/api/auth/login/send-otp', {'otpSessionId': sessionId, 'channel': channel});

  Future<void> verifyOtp(String sessionId, String code, String deviceId) async {
    final r = await post('/api/auth/login/verify-otp', {'otpSessionId': sessionId, 'code': code, 'deviceId': deviceId});
    await _saveSession(Map<String, dynamic>.from(r));
  }
}
