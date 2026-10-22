import 'package:flutter/material.dart';

import 'api.dart';
import 'config.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Api.instance.load();
  runApp(const ChamCongApp());
}

class ChamCongApp extends StatelessWidget {
  const ChamCongApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
        title: 'Chấm công',
        theme: ThemeData(colorSchemeSeed: Colors.indigo, useMaterial3: true),
        home: Api.instance.accessToken == null ? const LoginPage() : const HomePage(),
      );
}

void _toast(BuildContext c, String m) =>
    ScaffoldMessenger.of(c).showSnackBar(SnackBar(content: Text(m)));

/// Bước 1: tên đăng nhập + mật khẩu → backend trả otpSessionId và các kênh OTP.
class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _user = TextEditingController(text: 'nhanvien1');
  final _pass = TextEditingController(text: 'Employee@123');
  bool _busy = false;

  Future<void> _login() async {
    setState(() => _busy = true);
    try {
      final r = await Api.instance.login(_user.text.trim(), _pass.text);
      if (!mounted) return;
      Navigator.push(context, MaterialPageRoute(builder: (_) => OtpPage(login: r)));
    } on ApiException catch (e) {
      if (mounted) _toast(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Đăng nhập')),
        body: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(children: [
            TextField(controller: _user, decoration: const InputDecoration(labelText: 'Tên đăng nhập')),
            TextField(controller: _pass, obscureText: true, decoration: const InputDecoration(labelText: 'Mật khẩu')),
            const SizedBox(height: 16),
            FilledButton(onPressed: _busy ? null : _login, child: Text(_busy ? 'Đang xử lý...' : 'Tiếp tục')),
            const SizedBox(height: 16),
            Text('API: $apiBase', style: Theme.of(context).textTheme.bodySmall),
          ]),
        ),
      );
}

/// Bước 2: chọn kênh (Email/Sms), nhập mã OTP.
/// Khi backend chạy DevMode, mã OTP nằm trong console của backend (dòng "[DEV OTP]").
class OtpPage extends StatefulWidget {
  final Map<String, dynamic> login;
  const OtpPage({super.key, required this.login});
  @override
  State<OtpPage> createState() => _OtpPageState();
}

class _OtpPageState extends State<OtpPage> {
  final _code = TextEditingController();
  late String _channel = (widget.login['availableChannels'] as List).first as String;
  bool _sent = false, _busy = false;

  String get _session => widget.login['otpSessionId'] as String;

  Future<void> _send() async {
    setState(() => _busy = true);
    try {
      await Api.instance.sendOtp(_session, _channel);
      setState(() => _sent = true);
      if (mounted) _toast(context, 'Đã gửi mã qua $_channel');
    } on ApiException catch (e) {
      if (mounted) _toast(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _verify() async {
    setState(() => _busy = true);
    try {
      await Api.instance.verifyOtp(_session, _code.text.trim(), 'android-dev-device');
      if (!mounted) return;
      Navigator.pushAndRemoveUntil(context, MaterialPageRoute(builder: (_) => const HomePage()), (_) => false);
    } on ApiException catch (e) {
      if (mounted) _toast(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final channels = (widget.login['availableChannels'] as List).cast<String>();
    return Scaffold(
      appBar: AppBar(title: const Text('Xác thực OTP')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('Email: ${widget.login['maskedEmail']}'),
          if (channels.contains('Sms')) Text('SĐT: ${widget.login['maskedPhone']}'),
          const SizedBox(height: 8),
          SegmentedButton<String>(
            segments: [for (final c in channels) ButtonSegment(value: c, label: Text(c))],
            selected: {_channel},
            onSelectionChanged: (s) => setState(() => _channel = s.first),
          ),
          const SizedBox(height: 8),
          OutlinedButton(onPressed: _busy ? null : _send, child: Text(_sent ? 'Gửi lại mã' : 'Gửi mã')),
          TextField(
            controller: _code,
            keyboardType: TextInputType.number,
            maxLength: 6,
            decoration: const InputDecoration(labelText: 'Mã 6 số'),
          ),
          FilledButton(onPressed: _busy ? null : _verify, child: const Text('Xác nhận')),
        ]),
      ),
    );
  }
}

/// Màn hình chính: hiển thị thông tin tài khoản và lịch ca của tôi.
class HomePage extends StatefulWidget {
  const HomePage({super.key});
  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  late final Future<dynamic> _shifts = Api.instance.get('/api/shift-assignments/my');

  @override
  Widget build(BuildContext context) {
    final u = Api.instance.user ?? {};
    return Scaffold(
      appBar: AppBar(title: Text('Xin chào, ${u['fullName'] ?? ''}'), actions: [
        IconButton(
          icon: const Icon(Icons.logout),
          onPressed: () async {
            await Api.instance.logout();
            if (!context.mounted) return;
            Navigator.pushAndRemoveUntil(context, MaterialPageRoute(builder: (_) => const LoginPage()), (_) => false);
          },
        ),
      ]),
      body: Column(children: [
        ListTile(
          title: Text('Vai trò: ${u['role']}'),
          subtitle: Text('Đã đăng ký khuôn mặt: ${u['hasFaceEnrolled'] == true ? 'Có' : 'Chưa'}'),
        ),
        const Divider(),
        Expanded(
          child: FutureBuilder(
            future: _shifts,
            builder: (c, s) {
              if (s.connectionState != ConnectionState.done) return const Center(child: CircularProgressIndicator());
              if (s.hasError) return Center(child: Text('${s.error}'));
              final list = (s.data as List).cast<Map<String, dynamic>>();
              return ListView(children: [
                for (final a in list)
                  ListTile(
                    leading: const Icon(Icons.event),
                    title: Text('${a['workDate']} • ${a['shiftName']} (${a['startTime']}–${a['endTime']})'),
                    subtitle: Text(a['workLocationName'] as String),
                  ),
              ]);
            },
          ),
        ),
      ]),
    );
  }
}
