# PRD – Hệ thống VinhKhanhStreet (VKFoodTour)

> **Product Requirements Document**
> Dự án: Nền tảng Food Tour thông minh với audio guide đa ngôn ngữ cho phố ẩm thực
> Phiên bản: 1.0
> Ngày cập nhật: 21/04/2026

> **Phiên bản có ảnh sơ đồ đã render:** xem `docs/diagrams/PRD.Rendered.md` (đã thay tất cả khối Mermaid bằng PNG/SVG).
> **Render lại khi sửa file này:** chạy `pwsh -File docs/render-prd.ps1` từ thư mục gốc.

---

## 1. Tổng quan đồ án

### 1.1. Bối cảnh
Phố ẩm thực tại Việt Nam ngày càng phát triển và thu hút lượng lớn du khách trong và ngoài nước. Tuy nhiên, du khách thường gặp các vấn đề:
- Không hiểu được câu chuyện, văn hóa, lịch sử của từng quán.
- Rào cản ngôn ngữ khi đọc menu, biển hiệu, hỏi thông tin.
- Khó lựa chọn quán phù hợp do thiếu thông tin thống nhất.
- Trải nghiệm rời rạc, không có "sợi dây" dẫn dắt xuyên suốt tour.

Các giải pháp hiện có như Google Maps, TripAdvisor chỉ cung cấp thông tin tĩnh, không có trải nghiệm audio theo vị trí (location-based audio guide) và không hỗ trợ đa ngôn ngữ tự động cho từng quán nhỏ lẻ.

### 1.2. Mục tiêu sản phẩm
**VinhKhanhStreet (VKFoodTour)** là hệ thống trải nghiệm ẩm thực thông minh, bao gồm:
- **Web Admin**: nơi quản trị viên vận hành toàn bộ phố ẩm thực (POI, ngôn ngữ, audio, QR, thống kê).
- **Web Vendor**: nơi chủ quán tự quản lý gian hàng, thực đơn, media.
- **Mobile App**: ứng dụng dành cho du khách, quét QR đầu phố để bắt đầu tour audio đa ngôn ngữ, tự động thuyết minh theo vị trí GPS.

Mục tiêu cốt lõi:
1. Tạo trải nghiệm **audio tour tự động** theo geofence cho du khách.
2. Hỗ trợ **đa ngôn ngữ (i18n)** với dịch + TTS (Text-to-Speech) hàng loạt.
3. Hệ thống **QR-first**: một QR đầu phố khởi tạo toàn bộ tour, từng QR quán để xem chi tiết nhanh.
4. Cung cấp **dashboard thống kê realtime** phục vụ quản lý và quyết định kinh doanh.

### 1.3. Phạm vi & đối tượng sử dụng

| Đối tượng | Nền tảng | Vai trò chính |
|---|---|---|
| Admin | Web Admin (Blazor Server) | Quản trị toàn hệ thống, duyệt POI, cấu hình ngôn ngữ, tạo audio, quản lý QR, dịch nội dung |
| Vendor (chủ quán) | Web Vendor (cùng app Blazor, role `Vendor`) | Cập nhật thông tin quán, quản lý thực đơn, xem thống kê |
| Du khách (End-user) | Mobile App (.NET MAUI) | Quét QR, chọn ngôn ngữ, nghe audio theo vị trí, đánh giá |

### 1.4. Kiến trúc tổng thể

![diagram](./PRD.Rendered-1.svg)

> **Ghi chú kiến trúc thực tế**
> - Nghiệp vụ được đặt trực tiếp trong Controller của API và trong `Admin/Services/` (không dùng CQRS/MediatR).
> - Google Translate và Edge TTS được gọi từ Web Admin (khi Admin soạn nội dung), không phải từ API runtime.
> - Không có SignalR hub nghiệp vụ – dashboard realtime dựa trên polling tracking log.

---

## 2. Use-case tổng quan

**Mô tả:** Sơ đồ use-case gom toàn bộ chức năng của hệ thống theo **3 actor** chính: Admin, Vendor, Du khách. Đây là cái nhìn tổng quan về phạm vi đồ án trước khi đi vào chi tiết từng module.

![diagram](./PRD.Rendered-2.svg)

**Ghi chú:**
- Admin và Vendor **dùng chung hệ thống đăng nhập** (UA1) – phân quyền theo role.
- Chức năng "Quản lý POI" (UA3) **include** use-case "Duyệt POI" (Pending → Approved/Rejected) – chi tiết trạng thái xem ở sơ đồ State Lifecycle (STATE-01).
- Các use-case **dạng CRUD** (UA3, UA8, UA11, UV1, UV2) đều tuân theo pattern chung – xem sơ đồ SEQ-08 (CRUD generic).

---

## 3. Chức năng nổi bật

### 3.1. Web Admin
(Các trang Blazor tương ứng ghi chú trong cột **Route**)

| # | Chức năng | Route / File | Mô tả ngắn |
|---|---|---|---|
| A1 | **Đăng nhập / Đăng xuất** | `/login`, `/logout` | Xác thực bằng cookie, hỗ trợ Google OAuth. |
| A2 | **Dashboard thống kê** | `/` (`Home.razor`) | Tổng hợp QR scan, tương tác hôm nay, top POI, ngôn ngữ, đánh giá; realtime thiết bị qua `ActiveDevicesWidget` (poll EF). Component `OnlineUsersWidget` có trong codebase nhưng chưa gắn trang chủ. |
| A3 | **Quản lý POI / gian hàng** | `/admin/pois` (`PoiList.razor` + `PoiService`) | Tìm kiếm, lọc trạng thái, duyệt/ từ chối, chỉnh sửa thông tin, tọa độ, bán kính geofence. |
| A4 | **Bản đồ POI** | `/admin/ban-do` (`BanDoPoi.razor`) | Hiển thị toàn bộ POI trên bản đồ kèm vị trí & vùng geofence. |
| A5 | **Quản lý ngôn ngữ** | `/quan-ly-ngon-ngu` (`QuanLyNgonNgu.razor`) | Thêm/xóa/bật-tắt ngôn ngữ, cấu hình mã ngôn ngữ và TTS voice tương ứng. |
| A6 | **Dịch tự động nội dung POI** | `PoiList` + `GoogleTranslateService` | Dịch nội dung POI sang ngôn ngữ đích qua Google Cloud Translation API. |
| A7 | **Quản lý audio intro tour** | `/admin/intro-audio` (`IntroAudio.razor`) | Soạn nội dung intro phố ẩm thực, sinh audio theo từng ngôn ngữ bằng Edge TTS. |
| A8 | **Quản lý thuyết minh POI** | `/thuyet-minh` (`ThuyetMinh.razor`) | Soạn nội dung thuyết minh cho mỗi POI theo ngôn ngữ, sinh audio, theo dõi trạng thái đã có audio hay chưa. |
| A9 | **Quản lý audio tổng** | `/admin/audio-management` (`AudioManagement.razor`) | Danh sách tất cả audio (intro + POI), trạng thái, tái tạo / xóa. |
| A10 | **Quản lý QR** | `/ma-qr` (`MaQR.razor`) | Tạo & quản lý QR tour tổng (đầu phố) và QR từng POI; resolve qua API `Qr/resolve/{token}`. |
| A11 | **Quản lý nhân sự & Vendor** | `/nguoi-dung` (`NguoiDung.razor`) | Tạo/khóa tài khoản Admin, Vendor; gán Vendor với POI. |
| A12 | **Gian hàng (nội bộ)** | `/gian-hang` (`GianHang.razor`) | Trang thao tác gian hàng phía admin. |
| A13 | **Phản hồi từ app** | `/admin/phan-hoi-app` (`AppFeedback.razor`) | Xem feedback du khách gửi từ Mobile (API `Feedback/app`). |
| A14 | **Heatmap & Tracking log** | `/admin/ban-do` (`BanDoPoi.razor`) + Mobile → API `Tracking` | Heatmap: Blazor đọc `TrackingLogs` qua EF (bucket tọa độ), không qua HTTP nội bộ. Log vẫn do Mobile POST `/api/Tracking/log`. |

### 3.2. Web Vendor
(Cùng app Blazor `Admin/`, hiển thị theo role `Vendor`)

| # | Chức năng | Route / File | Mô tả ngắn |
|---|---|---|---|
| V1 | **Cập nhật thông tin quán** | `/vendor/thong-tin` (`Vendor/ThongTinQuan.razor`) | Sửa tên, mô tả, địa chỉ, tọa độ, ảnh đại diện / gallery. Sau khi lưu, trạng thái POI chuyển Pending chờ admin duyệt. |
| V2 | **Quản lý thực đơn** | `/vendor/thuc-don` (`Vendor/ThucDon.razor` + `MenuService`) | CRUD món ăn: tên, mô tả, giá, ảnh, danh mục (`MenuItem.Category`). |
| V3 | **Thống kê tương tác** | `/vendor/thu-nhap` (`Vendor/ThuNhap.razor`) | Xem lượt quét QR, lượt tương tác của quán (không có doanh thu do chưa tích hợp thanh toán). |

### 3.3. Mobile App (.NET MAUI)

| # | Chức năng | Trang / Service | Mô tả ngắn |
|---|---|---|---|
| M1 | **Onboarding & chọn ngôn ngữ** | `WelcomePage`, `LanguagePickerPage`, `SettingsService` | Màn chào + chọn ngôn ngữ giao diện/audio, lưu vào `SettingsService`. |
| M2 | **Đăng nhập / đăng ký du khách** | `LoginPage`, `AuthSessionService`, API `Auth/login`, `Auth/register` | Tài khoản `User` để gửi đánh giá/feedback. |
| M3 | **Quét QR đầu phố & QR quán** | `QrScanPage` (ZXing) → API `Qr/resolve/{token}` | Khởi động tour hoặc mở trực tiếp chi tiết quán. |
| M4 | **Bắt đầu tour & tải audio queue** | `TourPlayerPage` → API `Tour/start`, `Tour/audio-queue` | Tải intro + danh sách audio POI theo ngôn ngữ đã chọn. |
| M5 | **Phát audio** | `PlayerPage`, `AudioPlaybackService` (Plugin.Maui.Audio) | Play/Pause/Seek audio intro và thuyết minh POI. |
| M6 | **Geofence tự động (có dwell/exit debounce)** | `GeofenceMonitorService` | Poll GPS mỗi 3s, chỉ fire `PoiEntered` khi du khách **ở trong vùng liên tục 8s** (dwell), xác nhận `PoiExited` sau **10s ngoài vùng** (debounce). Bán kính tính từ `PoiRadiusMeters` + buffer 10m GPS. |
| M7 | **Audio queue thông minh với ngưỡng ưu tiên 60%** | `AudioQueueService` | Khi đang phát 1 POI mà đi vào vùng POI mới:<br>• track hiện tại **≥ 60%** → `InsertNext` (chèn POI mới phát kế tiếp).<br>• track hiện tại **< 60%** → `InterruptAndPlay` (ngắt ngay, phát POI mới, xếp track cũ phát lại sau).<br>Đảm bảo không phát đè, xử lý được cả trường hợp đứng giữa 2 geofence. |
| M8 | **Danh sách & chi tiết gian hàng** | Tab `Gian hàng`, `StallDetailPage` → API `Poi`, `Poi/{id}/detail` | Hiển thị thông tin quán (đã localize), menu, ảnh. |
| M9 | **Bản đồ toàn tour** | `FullMapPage` (Maui Maps) | Hiển thị vị trí người dùng + toàn bộ POI. |
| M10 | **Đánh giá quán** | `StallDetailPage` → API `Reviews` | Gửi rating + nội dung review cho POI. |
| M11 | **Yêu thích** | `FavoriteService` | Lưu danh sách POI yêu thích (lưu cục bộ). |
| M12 | **Tracking log** | `DataService.TrackEventAsync` → API `Tracking/log` | Gửi event `qr_scan`, `enter`, `exit`, `listen_start`, `listen_end`. |
| M13 | **Đa ngôn ngữ giao diện** | `LocalizationService`, `TranslationStrings` | Đổi ngôn ngữ UI tại runtime. |
| M14 | **Gửi phản hồi ứng dụng** | API `Feedback/app` | Người dùng gửi feedback tổng thể cho admin. |
| M15 | **Cache ảnh** | `HttpImageService` | Cache ảnh trong bộ nhớ với TTL, giảm tải mạng. |

### 3.4. Điểm nổi bật của đồ án
- **Location-based audio guide**: audio thuyết minh tự động phát theo GPS/geofence, khác biệt với các app du lịch bấm-mới-nghe.
- **Pipeline dịch + TTS khép kín trên Web Admin**: admin nhập tiếng Việt → Google Translate sang ngôn ngữ đích → Edge TTS sinh audio → lưu vào `UploadsData` và liên kết với POI.
- **Smart Audio Queue** (`AudioQueueService`): xử lý hàng đợi audio khi đi qua nhiều POI hoặc đứng giữa các vùng geofence chồng lấn – ưu tiên theo **tiến độ track (60%)** thay vì khoảng cách, kết hợp **dwell 8s + exit debounce 10s** ở `GeofenceMonitorService` để tránh "nháy" do GPS nhiễu.
- **QR-first onboarding**: một QR đầu phố đủ để khởi tạo toàn bộ tour, không cần đăng ký trước.
- **Tracking đầy đủ hành vi** (`Tracking/log`) phục vụ heatmap, đếm thiết bị online, thống kê tỉ lệ hoàn thành tour.

---

## 4. Sơ đồ Sequence (tuần tự)

### 4.1. SEQ-01 – Du khách quét QR và bắt đầu tour

**Mô tả:** Mô tả luồng từ khi du khách quét QR tại cổng phố ẩm thực đến khi audio intro được phát. Đây là luồng "onboarding" quan trọng nhất quyết định trải nghiệm đầu tiên.

![diagram](./PRD.Rendered-3.svg)

**Điểm quan trọng:**
- QR token luôn đi qua `Qr/resolve` để tránh lộ dữ liệu POI.
- Audio url là đường dẫn tĩnh `/uploads/...` (ASP.NET `UseStaticFiles`).
- `Tour/track-listen` ghi lại việc bắt đầu phát intro.

---

### 4.2. SEQ-02 – Tự động phát audio khi vào vùng geofence POI

**Mô tả:** Cơ chế geofence trigger tự động – mobile poll GPS mỗi 3s, phải ở trong vùng POI **liên tục 8 giây** (dwell) mới phát audio để tránh giật do GPS nhiễu. Khi kết thúc tự nhiên, audio kế tiếp được lấy từ queue.

![diagram](./PRD.Rendered-4.svg)

**Điểm quan trọng:**
- **Dwell 8s** ngăn false-positive khi du khách đi ngang qua quán mà không dừng.
- POI đã phát xong được đánh dấu `_playedPois` → không re-trigger dù du khách quay lại.
- Trường hợp **đứng giữa 2 geofence** được mô tả riêng ở SEQ-07.

---

### 4.3. SEQ-03 – Admin dịch nội dung POI + sinh audio bằng Edge TTS

**Mô tả:** Trên Web Admin, từ trang `ThuyetMinh.razor`, admin soạn nội dung tiếng Việt của 1 POI, dịch sang các ngôn ngữ đích (Google Translate), sau đó sinh audio (Edge TTS) và lưu file vào thư mục `UploadsData/` để API phục vụ qua `/uploads`.

![diagram](./PRD.Rendered-5.svg)

**Điểm quan trọng:**
- Hoạt động **trong Blazor Server** (không qua API runtime) – tận dụng server-side để gọi Google Translate và Edge TTS.
- Mỗi ngôn ngữ tương ứng một **voice** được cấu hình ở `QuanLyNgonNgu.razor`.
- File audio lưu vào `UploadsData/` (shared với API để phục vụ qua `/uploads`).

---

### 4.4. SEQ-04 – Vendor cập nhật gian hàng & Admin duyệt

**Mô tả:** Vendor đăng nhập cùng Blazor app, sửa thông tin quán và thực đơn. Admin duyệt trong `PoiList.razor`. Mobile app chỉ nhận POI ở trạng thái Approved qua `Poi` API.

![diagram](./PRD.Rendered-6.svg)

---

### 4.5. SEQ-05 – Mobile ghi log & Dashboard đọc cùng database

**Mô tả:** Du khách gửi sự kiện qua **VKFoodTour.API** (`TrackingController`). Trang Dashboard (`Home.razor`, Blazor Server) **không gọi HTTP nội bộ tới API** để lấy thống kê: nó và các widget con dùng **EF Core** (`ApplicationDbContext` / `IDbContextFactory`) truy vấn trực tiếp **cùng SQL Server** mà API ghi vào. Làm tươi “realtime” nhờ **Timer** trong widget (không dùng SignalR nghiệp vụ).

![diagram](./PRD.Rendered-7.svg)

**Ghi chú:**

- **Heatmap** trên bản đồ tổng quan nằm ở `/admin/ban-do` (`BanDoPoi.razor`): cũng đọc `TrackingLogs` qua EF + đẩy JSON sang JS (`updateOverviewHeatmap`), không đi qua `GET /api/Tracking/heatmap` từ Blazor.
- Component **`OnlineUsersWidget`** cùng pattern (Timer + `IDbContextFactory`) nhưng **chưa được nhúng** vào `Home.razor`; trang chủ admin hiện dùng **`ActiveDevicesWidget`** (`WindowSeconds=45`, `RefreshSeconds=3`).

#### 4.5.1. SEQ-05a – Admin Dashboard: phương thức trong `Home.razor` & `ActiveDevicesWidget`

**Mô tả:** Chuỗi gọi **theo code thực tế** khi user role **Admin** mở `/`. File: `Admin/Components/Pages/Home.razor`, widget: `Admin/Components/Pages/Admin/ActiveDevicesWidget.razor`.

**A) `Home.razor` — `OnInitializedAsync` → `LoadAdminStats` (inject `ApplicationDbContext Db`)**

| Thứ tự | Phương thức / truy vấn |
|--------|------------------------|
| 1 | `AuthenticationStateProvider.GetAuthenticationStateAsync()` |
| 2 | `LoadAdminStats()` |
| 3 | `Db.Pois.CountAsync()` |
| 4 | `Db.Pois.CountAsync(p => p.IsActive)` |
| 5 | `Db.Narrations.CountAsync()` |
| 6 | `Db.Languages.CountAsync(l => l.IsActive)` |
| 7 | `Db.Users.CountAsync()` |
| 8 | `Db.Users.CountAsync(u => u.Role == "Vendor")` |
| 9 | `Db.TrackingLogs.CountAsync(t => t.EventType == "qr_scan" && t.CreatedAt >= today)` |
| 10 | `Db.TrackingLogs.CountAsync(t => t.CreatedAt >= today && t.EventType != "move")` |
| 11 | `listenLogs.AnyAsync()` rồi `listenLogs.AverageAsync(t => t.ListenedDurationSec!.Value)` (`listenLogs` = `TrackingLogs` lọc `listen_end`) |
| 12 | `Db.TrackingLogs` … `GroupBy`/`OrderByDescending`/`Take(5)` → `ToListAsync()` (top POI) |
| 13 | `Db.Pois.Where(...).ToDictionaryAsync(p => p.PoiId, p => p.Name)` |
| 14 | Vòng `foreach` gán `TopPoiItem.PoiName` |
| 15 | `Db.TrackingLogs` … `GroupBy` POI `listen_end` → `ToListAsync()` (top audio) |
| 16 | `foreach` gán `TopAudioItem.PoiName` (dùng `poiNames`) |
| 17 | `Db.TrackingLogs` group theo `LanguageCode` → `ToListAsync()` |
| 18 | `Db.Languages.ToDictionaryAsync(l => l.Code, l => l.Name)` |
| 19 | Build `languageStats` (LINQ trên bộ nhớ) |
| 20 | `Db.Reviews.CountAsync()` |
| 21 | `Db.Reviews.GroupBy(r => (int)r.Rating).ToDictionaryAsync(...)` → `ratingDistribution` |

![diagram](./PRD.Rendered-8.svg)

**B) `ActiveDevicesWidget` — inject `IDbContextFactory<ApplicationDbContext> DbFactory`**

| Thứ tự | Phương thức |
|--------|-------------|
| 1 | `OnInitializedAsync()` |
| 2 | `Math.Clamp(WindowSeconds, 15, 300)` |
| 3 | `LoadAsync()` |
| 4 | `DbFactory.CreateDbContextAsync()` |
| 5 | `db.TrackingLogs.AsNoTracking()…GroupBy(DeviceId)…ToListAsync()` |
| 6 | `db.Pois.AsNoTracking()…ToDictionaryAsync(PoiId, Name)` (nếu có `poiIds`) |
| 7 | Gán `activeDevices` (LINQ trên bộ nhớ) |
| 8 | `new Timer(...)` — refresh mỗi `RefreshSeconds` (3s): gọi lại `LoadAsync()` + `InvokeAsync(StateHasChanged)` |
| 9 | `new Timer(...)` — mỗi 1s: cập nhật `secondsAgo` + `StateHasChanged` |
| 10 | `DisposeAsync()` — hủy hai timer |

![diagram](./PRD.Rendered-9.svg)

**Ghi chú thứ tự Blazor:** `Home.OnInitializedAsync` (gồm `LoadAdminStats`) chạy trước khi subtree render xong; `ActiveDevicesWidget` khởi tạo sau (lifecycle con), nên block **A** hoàn tất trước **B** trong cùng lần tải trang đầu.

---

### 4.6. SEQ-06 – Du khách đánh giá quán

**Mô tả:** Sau khi nghe audio và xem chi tiết quán, du khách có thể gửi rating + bình luận về quán.

![diagram](./PRD.Rendered-10.svg)

---

### 4.7. SEQ-07 – Ưu tiên audio khi đứng giữa 2 geofence chồng lấn

**Mô tả:** Kịch bản du khách đang nghe audio của POI A thì bước vào vùng geofence của POI B (2 vùng chồng lấn, hoặc 2 quán cạnh nhau). `AudioQueueService` quyết định xử lý dựa trên **tiến độ track A đang phát** so với ngưỡng **60%**.

![diagram](./PRD.Rendered-11.svg)

**Giải thích ý nghĩa ngưỡng 60%:**
- Với ngưỡng **≥ 60%**: track đang phát đã gần xong → cho nghe trọn vẹn để giữ trải nghiệm liền mạch, POI mới xếp kế tiếp.
- Với ngưỡng **< 60%**: du khách vừa mới bước vào quán cũ và chưa nghe được bao nhiêu, giờ đã ở gần quán mới – ưu tiên thông tin về **vị trí hiện tại**, ngắt track cũ, phát track mới, và phát lại track cũ sau khi track mới kết thúc.
- Hằng số `InterruptThreshold = 0.60` được định nghĩa trong `AudioQueueService.cs` – có thể tune để thay đổi hành vi.

**Các nhánh an toàn khác (code thực tế):**
- Nếu POI B đã có trong `_playedPois` → **bỏ qua** (không phát lại).
- Nếu POI B chính là track đang phát (`CurrentlyPlaying.PoiId == B`) → **bỏ qua** (tránh double-trigger do GPS jitter).
- Nếu `CurrentlyPlaying != null` nhưng `IsPlaying == false` (bootstrap/loading) → **InsertNext** để tránh race condition.

---

### 4.8. SEQ-08 – Pattern CRUD có duyệt (generic)

**Mô tả:** Sơ đồ **chung** cho tất cả chức năng CRUD có luồng duyệt trong hệ thống – áp dụng cho: quản lý POI, thực đơn, người dùng, ngôn ngữ, QR code, review, feedback, audio… Khi xem một chức năng CRUD trong bảng liệt kê (mục 3), tham chiếu sơ đồ này thay vì vẽ lại từng cái.

![diagram](./PRD.Rendered-12.svg)

**Áp dụng cho các chức năng:**

| Chức năng | Actor | Service/Controller | File? |
|---|---|---|---|
| Quản lý POI | Admin / Vendor | `PoiService` | Có ảnh |
| Quản lý thực đơn | Vendor | `MenuService` | Có ảnh |
| Quản lý ngôn ngữ | Admin | (Blazor page) | Không |
| Quản lý QR | Admin | (`QrController` resolve) | Không |
| Quản lý người dùng | Admin | `NguoiDung.razor` | Không |
| Gửi review | User | `ReviewsController` | Không |
| Gửi feedback app | User | `FeedbackController` | Không |
| Upload audio | Admin | `EdgeTtsService` + file write | Có file |

---

### 4.9. SEQ-09 – Hiển thị Heatmap tracking trên Web Admin

**Mô tả:** Luồng tải và hiển thị heatmap thực tế trên trang `BanDoPoi.razor`. Admin bật switch heatmap, chọn mốc thời gian, UI gọi API `Tracking/heatmap`, sau đó đẩy dữ liệu sang JS interop để cập nhật lớp heatmap trên bản đồ.

![diagram](./PRD.Rendered-13.svg)

**Điểm kỹ thuật chính (đúng code hiện tại):**
- API endpoint: `GET /api/Tracking/heatmap` trong `TrackingController`.
- UI xử lý ở `BanDoPoi.razor` với các hàm `OnToggleHeatmap()` và `ReloadHeatmapAsync()`.
- JS interop: `updateOverviewHeatmap()` và `toggleOverviewHeatmap()` trong `admin-interop.js`.

---

### 4.10. SEQ-10 – Quản lý ngôn ngữ và ánh xạ TTS voice

**Mô tả:** Luồng quản lý ngôn ngữ trên trang `QuanLyNgonNgu.razor`: Admin thêm ngôn ngữ mới, cấu hình mã ngôn ngữ + voice, bật/tắt trạng thái hoạt động. Cấu hình này được dùng lại khi dịch nội dung và sinh audio thuyết minh.

![diagram](./PRD.Rendered-14.svg)

**Điểm kỹ thuật chính:**
- Trang quản lý: `QuanLyNgonNgu.razor`.
- Ngôn ngữ bật (`isEnabled=true`) là nguồn dữ liệu cho luồng dịch/sinh audio ở `ThuyetMinh.razor`.
- TTS sử dụng `voice` đã ánh xạ theo từng ngôn ngữ để đảm bảo phát âm đúng.

---

## 5. Sơ đồ Activity & State

### 5.1. ACT-01 – Hành trình du khách end-to-end trên Mobile App

**Mô tả:** Toàn bộ luồng của Mobile App từ khi mở app (WelcomePage) đến khi kết thúc tour.

![diagram](./PRD.Rendered-15.svg)

---

### 5.2. ACT-02 – Duyệt POI của Admin trong `PoiList.razor`

![diagram](./PRD.Rendered-16.svg)

---

### 5.3. ACT-03 – Vendor cập nhật gian hàng

**Mô tả:** Vendor dùng chung Blazor app, chỉ thấy các trang `/vendor/*` theo role.

![diagram](./PRD.Rendered-17.svg)

---

### 5.4. ACT-04 – Dịch & sinh audio thuyết minh trong `ThuyetMinh.razor`

![diagram](./PRD.Rendered-18.svg)

---

### 5.5. ACT-05 – Logic Audio Queue với ngưỡng ưu tiên 60%

**Mô tả:** Logic thật của `HandlePoiEnteredAsync` trong `AudioQueueService.cs` khi nhận sự kiện `PoiEntered` từ `GeofenceMonitorService`.

![diagram](./PRD.Rendered-19.svg)

**Các hằng số tham chiếu trong code:**
- `GeofenceMonitorService.DwellThresholdSec = 8` – phải ở trong zone 8 giây mới trigger.
- `GeofenceMonitorService.ExitDebounceMs = 10_000` – 10 giây ngoài zone mới confirm exit.
- `GeofenceMonitorService.GpsBufferMeters = 10` – nới bán kính thêm 10m để bù GPS drift.
- `GeofenceMonitorService.PollIntervalMs = 3_000` – polling 3 giây.
- `AudioQueueService.InterruptThreshold = 0.60` – ngưỡng quyết định InsertNext vs Interrupt.

---

### 5.6. STATE-01 – Vòng đời (lifecycle) của POI

**Mô tả:** Sơ đồ state diagram thể hiện các trạng thái và chuyển đổi của một POI từ lúc Vendor tạo đến khi xuất hiện trên Mobile App.

![diagram](./PRD.Rendered-20.svg)

**Ràng buộc chuyển trạng thái:**
- Mọi thay đổi nội dung quan trọng của Vendor đều **reset về `Pending`** để Admin xem lại.
- POI chỉ **hiển thị công khai trên Mobile** khi ở trạng thái `Approved` hoặc `Published`.
- Chuyển sang `Archived` là **soft-delete** – vẫn giữ log tracking lịch sử.

---

## 6. Yêu cầu phi chức năng

| Hạng mục | Yêu cầu |
|---|---|
| Hiệu năng | API < 500ms cho các request chính; audio trigger theo geofence trong vài giây. Có sẵn `load_test.js` và `stress_test.js` để kiểm thử tải. |
| Bảo mật | Cookie auth + Google OAuth cho Admin/Vendor; JWT cho Mobile (`AuthController`); mật khẩu hash; static files phục vụ qua `/uploads`. |
| Khả dụng | App không crash khi API lỗi – `DataService` có `FallbackDemo()` để hiển thị dữ liệu tối thiểu. |
| Đa ngôn ngữ | Thêm ngôn ngữ trong `QuanLyNgonNgu.razor` → sinh audio là có thể sử dụng, không cần build lại app. |
| Tracking | Mọi hành vi chính (`qr_scan`, `enter`, `exit`, `listen_start`, `listen_end`) đều được ghi qua `Tracking/log` cho heatmap và thống kê. |

---

## 7. Phụ lục

### 7.1. Danh sách event tracking (Mobile → `Tracking/log`)
- `qr_scan` – quét QR đầu phố hoặc QR quán.
- `enter` / `exit` – vào / rời vùng geofence POI.
- `listen_start` / `listen_end` – bắt đầu / kết thúc phát audio.
- `move` – cập nhật vị trí di chuyển (phục vụ heatmap).

### 7.2. Danh sách API chính

| Controller | Endpoint | Mục đích |
|---|---|---|
| `AuthController` | `POST /api/Auth/login`, `/register` | Đăng nhập/đăng ký du khách (role User) |
| `PoiController` | `GET /api/Poi`, `GET /api/Poi/{id}`, `GET /api/Poi/{id}/detail` | Lấy danh sách / chi tiết POI đã localize |
| `LanguagesController` | `GET /api/Languages` | Danh sách ngôn ngữ có audio thuyết minh |
| `QrController` | `GET /api/Qr/resolve/{token}` | Resolve QR sang tour hoặc POI |
| `TourController` | `POST /api/Tour/start`, `GET /api/Tour/audio-queue`, `POST /api/Tour/track-listen` | Bắt đầu tour, tải audio queue, log nghe audio |
| `TrackingController` | `POST /api/Tracking/log`, `GET /api/Tracking/online-count`, `GET /api/Tracking/heatmap` | Ghi log + số thiết bị online + dữ liệu heatmap |
| `ReviewsController` | `GET /api/Reviews/recent`, `GET /api/Reviews/poi/{poiId}`, `POST /api/Reviews` | Danh sách review + gửi review |
| `FeedbackController` | `POST /api/Feedback/app` | Gửi feedback app từ du khách |

### 7.3. Thành phần dự án
- `Admin/` – **Web Admin + Vendor** (ASP.NET Core **Blazor Server**), phân quyền theo role `Admin` / `Vendor`.
- `VKFoodTour.API/` – ASP.NET Core Web API (JWT), phục vụ static files `UploadsData/` qua `/uploads`.
- `VKFoodTour.Application/` – project dự phòng cho layer Application (hiện tại gần như trống, logic đặt trực tiếp trong API controller và `Admin/Services`).
- `VKFoodTour.Infrastructure/` – `ApplicationDbContext`, Entities, Migrations (EF Core).
- `VKFoodTour.Shared/` – DTO dùng chung: `PoiDto`, `PoiDetailDto`, `TourDtos`, `AuthDtos`, `ReviewDtos`, `TrackingDtos`, `QrResolveDto`, `LanguageListItemDto`, `AppFeedbackDtos`.
- `VKFoodTour.Mobile/` – App **.NET MAUI** (ZXing QR, Maui Maps, Plugin.Maui.Audio) với `DataService`, `AuthSessionService`, `SettingsService`, `LocalizationService`, `FavoriteService`, `HttpImageService`, `AudioPlaybackService`, `AudioQueueService`, `GeofenceMonitorService`.
- `VKFoodTour.Mobile.Core/` – Core library dùng chung (chứa `PoiApiService` như bản thay thế tương lai).
- `Database/VKFoodTour.sql` – script DDL + seed.
- `UploadsData/` – chứa file ảnh và audio sinh ra từ Edge TTS.

---

_Tài liệu này là bản đặc tả yêu cầu sản phẩm (PRD) cho hệ thống VKFoodTour, phục vụ mục đích phát triển, nghiệm thu và báo cáo đồ án._
