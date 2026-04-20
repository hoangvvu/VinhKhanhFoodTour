# Hướng dẫn Kiểm thử Tải (Load Testing) - VinhKhanhStreet

Tài liệu này hướng dẫn cách sử dụng công cụ **k6** để mô phỏng hàng trăm thiết bị di động truy cập vào hệ thống cùng một lúc.

## 1. Yêu cầu hệ thống
- Đã cài đặt **k6** (Lệnh cài: `winget install k6`).
- Dự án **VKFoodTour.API** phải đang chạy (nhấn F5 trong Visual Studio).
- URL API mặc định: `https://localhost:7105` (Đã được cấu hình sẵn trong các script).

---

## 2. Các kịch bản Test có sẵn

### Kịch bản 1: Load Test (100 thiết bị)
Dùng để kiểm tra tính ổn định cơ bản.
- **Mô tả:** Chạy 100 thiết bị ảo trong 30 giây.
- **Lệnh chạy:**
  ```powershell
  & "C:\Program Files\k6\k6.exe" run load_test.js
  ```

### Kịch bản 2: Stress Test (Tìm điểm sập)
Dùng để tìm ngưỡng giới hạn chịu tải của Server.
- **Mô tả:** Tăng dần từ 0 lên **1000 người dùng** trong vòng vài phút.
- **Lệnh chạy:**
  ```powershell
  & "C:\Program Files\k6\k6.exe" run stress_test.js
  ```

### Kịch bản 3: Duy trì tải (Sustained Load)
Dùng khi bạn muốn "treo" 100 thiết bị để thao tác trên giao diện Admin.
- **Mô tả:** Chạy liên tục 100 thiết bị trong 10 phút.
- **Lệnh chạy:**
  ```powershell
  & "C:\Program Files\k6\k6.exe" run sustained_load_test.js
  ```
- **Cách tắt:** Nhấn `Ctrl + C` trong cửa sổ lệnh.

### Kịch bản 4: Thoát ứng dụng (Ramp-down)
Dùng để mô phỏng người dùng thoát app dần dần.
- **Mô tả:** 100 thiết bị lần lượt thoát ra trong vòng 10 giây.
- **Lệnh chạy:**
  ```powershell
  & "C:\Program Files\k6\k6.exe" run ramp_down_test.js --vus 100
  ```

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
