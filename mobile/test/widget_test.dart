import 'package:flutter_test/flutter_test.dart';

import 'package:cham_cong_app/main.dart';

void main() {
  testWidgets('hiển thị màn hình đăng nhập', (tester) async {
    await tester.pumpWidget(const ChamCongApp());
    expect(find.text('Đăng nhập'), findsWidgets);
  });
}
