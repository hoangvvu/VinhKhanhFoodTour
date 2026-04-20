# Hướng dẫn Kiểm thử Tải (Load Testing) - VinhKhanhStreet

Tài liệu này hướng dẫn cách sử dụng công cụ **k6** để mô phỏng hàng trăm thiết bị di động truy cập vào hệ thống cùng một lúc.

## 1. Yêu cầu hệ thống
- Đã cài đặt **k6** (Lệnh cài: `winget install k6`).
- Dự án **VKFoodTour.API** phải đang chạy (nhấn F5 trong Visual Studio).
- URL API mặc định: `https://localhost:7105` (Đã được cấu hình sẵn trong các script).

---

## 2. Các kịch bản Test có sẵn

### Kịch bản 1: Load Test (100 thiết bị)
Dùng để kiểm tra tính ổn định với lượng người dùng cố định.
- **Mô tả:** Chạy 100 thiết bị ảo liên tục.
- **Lệnh chạy:**
  ```powershell
  & "C:\Program Files\k6\k6.exe" run load_test.js
  ```
- **Cách tắt:** Nhấn `Ctrl + C` trong cửa sổ lệnh.

### Kịch bản 2: Stress Test (Ép tải lên 1000 thiết bị)
Dùng để kiểm tra khả năng chịu tải tối đa của hệ thống.
- **Mô tả:** Tăng dần từ 0 lên 1000 người dùng trong 6 phút và duy trì ở mức đó.
- **Lệnh chạy:**
  ```powershell
  & "C:\Program Files\k6\k6.exe" run stress_test.js
  ```
- **Cách tắt:** Nhấn `Ctrl + C` khi bạn muốn dừng test.

---

## 3. Cách xem kết quả trên Admin Dashboard
1. Đăng nhập vào trang Admin của bạn.
2. Tại màn hình chính, quan sát Widget **"NGƯỜI DÙNG ĐANG ONLINE"**.
3. Khi bạn chạy script, con số này sẽ tăng lên (tương ứng với số VUs trong k6).
4. Khi bạn tắt script hoặc chạy bài Ramp-down, con số này sẽ giảm dần sau khoảng 2 phút (theo cấu hình của Admin).

---

## 4. Lưu ý quan trọng
- **Device ID:** Tất cả các kịch bản đã được đồng nhất sử dụng bộ ID `test-device-1` đến `test-device-1000`. Điều này giúp Admin đếm chính xác số lượng thiết bị thực tế đang tham gia test.
- **Hiệu năng:** Khi chạy Stress Test (1000 users), laptop của bạn có thể sẽ bị nóng hoặc lag do k6 tiêu tốn tài nguyên để bắn hàng ngàn request mỗi giây.

---
*Tài liệu được tạo bởi Antigravity AI trợ lý đồ án của bạn.*
