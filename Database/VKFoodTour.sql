-- ============================================================
--  VĨNH KHÁNH FOOD TOUR — Database Schema v2.0
--  SQL Server | Thiết kế lại hoàn chỉnh
-- ============================================================

CREATE DATABASE foodtour
GO
USE foodtour
GO

-- ============================================================
--  1. LANGUAGES — Danh sách ngôn ngữ hỗ trợ
-- ============================================================
CREATE TABLE LANGUAGES (
    language_id   INT           IDENTITY(1,1) PRIMARY KEY,
    code          NVARCHAR(10)  NOT NULL UNIQUE,   -- 'vi', 'en', 'zh', 'ko', 'ja'
    name          NVARCHAR(50)  NOT NULL,           -- 'Tiếng Việt', 'English'…
    tts_voice     NVARCHAR(100) NULL,               -- VD: 'vi-VN-Standard-A'
    is_active     BIT           NOT NULL DEFAULT 1
)
GO

-- ============================================================
--  2. USERS — Tài khoản Admin và Chủ gian hàng
-- ============================================================
CREATE TABLE USERS (
    user_id       INT           IDENTITY(1,1) PRIMARY KEY,
    name          NVARCHAR(100) NOT NULL,
    email         NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    role          NVARCHAR(20)  NOT NULL DEFAULT 'Vendor',
        CONSTRAINT chk_users_role CHECK (role IN ('Admin', 'Vendor', 'User')),
    is_active     BIT           NOT NULL DEFAULT 1,
    created_at    DATETIME      NOT NULL DEFAULT GETDATE(),
    updated_at    DATETIME      NULL
)
GO

-- ============================================================
--  3. POIS — Gian hàng / Điểm tham quan (tích hợp Geofence)
-- ============================================================
CREATE TABLE POIS (
    poi_id        INT            IDENTITY(1,1) PRIMARY KEY,
    owner_id      INT            NULL,
    name          NVARCHAR(200)  NOT NULL,
    address       NVARCHAR(255)  NULL,
    phone         NVARCHAR(20)   NULL,
    latitude      DECIMAL(10, 8) NOT NULL,
    longitude     DECIMAL(11, 8) NOT NULL,
    radius        INT            NOT NULL DEFAULT 20,  -- Bán kính Geofence (mét)
        CONSTRAINT chk_pois_radius CHECK (radius BETWEEN 5 AND 200),
    priority      INT            NOT NULL DEFAULT 1,   -- Mức ưu tiên kích hoạt (1=cao)
        CONSTRAINT chk_pois_priority CHECK (priority BETWEEN 1 AND 5),
    is_active     BIT            NOT NULL DEFAULT 1,
    created_at    DATETIME       NOT NULL DEFAULT GETDATE(),
    updated_at    DATETIME       NULL,
    CONSTRAINT fk_pois_owner FOREIGN KEY (owner_id)
        REFERENCES USERS(user_id) ON DELETE SET NULL
)
GO

-- ============================================================
--  4. NARRATIONS — Nội dung thuyết minh đa ngôn ngữ (TTS)
-- ============================================================
CREATE TABLE NARRATIONS (
    narration_id  INT            IDENTITY(1,1) PRIMARY KEY,
    poi_id        INT            NOT NULL,
    language_id   INT            NOT NULL,
    title         NVARCHAR(200)  NOT NULL,
    content       NVARCHAR(MAX)  NOT NULL,   -- Văn bản đưa vào TTS
    tts_voice     NVARCHAR(100)  NULL,       -- Override giọng đọc riêng (nếu cần)
    audio_url     NVARCHAR(500)  NULL,       -- File MP3 đã lưu (/uploads/narration/...) — đồng bộ với EF migration AddNarrationAudioUrl
    is_active     BIT            NOT NULL DEFAULT 1,
    updated_at    DATETIME       NULL,
    CONSTRAINT uq_narration_poi_lang UNIQUE (poi_id, language_id),
    CONSTRAINT fk_narrations_poi FOREIGN KEY (poi_id)
        REFERENCES POIS(poi_id) ON DELETE CASCADE,
    CONSTRAINT fk_narrations_lang FOREIGN KEY (language_id)
        REFERENCES LANGUAGES(language_id)
)
GO

-- ============================================================
--  5. FOODS — Món ăn của từng gian hàng
-- ============================================================
CREATE TABLE FOODS (
    food_id       INT            IDENTITY(1,1) PRIMARY KEY,
    poi_id        INT            NOT NULL,
    name          NVARCHAR(200)  NOT NULL,
    price         DECIMAL(10, 2) NULL,
        CONSTRAINT chk_foods_price CHECK (price IS NULL OR price >= 0),
    is_available  BIT            NOT NULL DEFAULT 1,
    sort_order    INT            NOT NULL DEFAULT 0,
    CONSTRAINT fk_foods_poi FOREIGN KEY (poi_id)
        REFERENCES POIS(poi_id) ON DELETE CASCADE
)
GO

-- ============================================================
--  6. FOOD_TRANSLATIONS — Tên & mô tả món ăn đa ngôn ngữ
-- ============================================================
CREATE TABLE FOOD_TRANSLATIONS (
    translation_id INT           IDENTITY(1,1) PRIMARY KEY,
    food_id        INT           NOT NULL,
    language_id    INT           NOT NULL,
    name           NVARCHAR(200) NOT NULL,
    description    NVARCHAR(MAX) NULL,
    CONSTRAINT uq_food_translation UNIQUE (food_id, language_id),
    CONSTRAINT fk_ft_food FOREIGN KEY (food_id)
        REFERENCES FOODS(food_id) ON DELETE CASCADE,
    CONSTRAINT fk_ft_lang FOREIGN KEY (language_id)
        REFERENCES LANGUAGES(language_id)
)
GO

-- ============================================================
--  7. IMAGES — Ảnh của gian hàng hoặc món ăn
-- ============================================================
CREATE TABLE IMAGES (
    image_id      INT            IDENTITY(1,1) PRIMARY KEY,
    poi_id        INT            NULL,
    food_id       INT            NULL,
    image_url     NVARCHAR(500)  NOT NULL,
    alt_text      NVARCHAR(200)  NULL,
    is_cover      BIT            NOT NULL DEFAULT 0,  -- Ảnh bìa chính
    sort_order    INT            NOT NULL DEFAULT 0,
    created_at    DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT chk_images_owner CHECK (
        (poi_id IS NOT NULL AND food_id IS NULL) OR
        (poi_id IS NULL  AND food_id IS NOT NULL)
    ),
    CONSTRAINT fk_images_poi  FOREIGN KEY (poi_id)
        REFERENCES POIS(poi_id)  ON DELETE CASCADE,
    CONSTRAINT fk_images_food FOREIGN KEY (food_id)
        REFERENCES FOODS(food_id)
)
GO

-- ============================================================
--  8. QRCODES — Mã QR kích hoạt thuyết minh
--     (tại gian hàng hoặc điểm xe buýt)
-- ============================================================
CREATE TABLE QRCODES (
    qr_id         INT            IDENTITY(1,1) PRIMARY KEY,
    poi_id        INT            NOT NULL,
    qr_token      NVARCHAR(100)  NOT NULL UNIQUE,  -- Token duy nhất nhúng vào QR
    location_note NVARCHAR(200)  NULL,  -- VD: 'Điểm xe buýt Khánh Hội'
    is_active     BIT            NOT NULL DEFAULT 1,
    created_at    DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT fk_qrcodes_poi FOREIGN KEY (poi_id)
        REFERENCES POIS(poi_id) ON DELETE CASCADE
)
GO

-- ============================================================
--  9. REVIEWS — Đánh giá của du khách
-- ============================================================
CREATE TABLE REVIEWS (
    review_id     INT            IDENTITY(1,1) PRIMARY KEY,
    device_id     NVARCHAR(255)  NOT NULL,   -- ID ẩn danh của thiết bị
    poi_id        INT            NOT NULL,
    rating        TINYINT        NOT NULL,
        CONSTRAINT chk_reviews_rating CHECK (rating BETWEEN 1 AND 5),
    comment       NVARCHAR(1000) NULL,
    language_code NVARCHAR(10)   NULL,       -- Ngôn ngữ du khách dùng khi viết review
    created_at    DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT fk_reviews_poi FOREIGN KEY (poi_id)
        REFERENCES POIS(poi_id) ON DELETE CASCADE
)
GO

-- ============================================================
--  10. TRACKING_LOGS — Lịch sử vị trí để vẽ Heatmap & Analytics
-- ============================================================
CREATE TABLE TRACKING_LOGS (
    log_id                BIGINT         IDENTITY(1,1) PRIMARY KEY,
    device_id             NVARCHAR(255)  NOT NULL,
    poi_id                INT            NULL,
    latitude              DECIMAL(10, 8) NOT NULL,
    longitude             DECIMAL(11, 8) NOT NULL,
    event_type            NVARCHAR(20)   NOT NULL DEFAULT 'move',
        CONSTRAINT chk_tracking_event CHECK (
            event_type IN ('move', 'enter', 'exit', 'qr_scan', 'listen_start', 'listen_end')
        ),
    listened_duration_sec INT            NULL,  -- Số giây nghe (khi event_type = 'listen_end')
    language_code         NVARCHAR(10)   NULL,  -- Ngôn ngữ đang dùng lúc đó
    created_at            DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT fk_tracking_poi FOREIGN KEY (poi_id)
        REFERENCES POIS(poi_id) ON DELETE SET NULL
)
GO

USE foodtour
GO

-- ============================================================
--  1. BẢNG POI_IMAGES (bản mở rộng / dự phòng)
--  Lưu ý: Ứng dụng hiện tại (EF) dùng bảng IMAGES ở trên cho ảnh POI + món.
--  Chỉ tạo POI_IMAGES nếu bạn thật sự cần tách schema; tránh trùng dữ liệu với IMAGES.
-- ============================================================
CREATE TABLE POI_IMAGES (
    image_id      INT           IDENTITY(1,1) PRIMARY KEY,
    poi_id        INT           NOT NULL,
    image_url     NVARCHAR(255) NOT NULL,     -- Đường dẫn tới file ảnh lưu trên server
    is_cover      BIT           DEFAULT 0,    -- Đánh dấu ảnh nào là Ảnh bìa chính
    created_at    DATETIME      DEFAULT GETDATE(),
    
    CONSTRAINT fk_poi_images_poi FOREIGN KEY (poi_id) 
        REFERENCES POIS(poi_id) ON DELETE CASCADE
)
GO

-- ============================================================
--  2. BẢNG MENU_ITEMS (Quản lý Thực đơn / Món ăn)
--  Phục vụ cho trang: Quản lý Thực đơn (ThucDon.razor)
-- ============================================================
CREATE TABLE MENU_ITEMS (
    item_id       INT           IDENTITY(1,1) PRIMARY KEY,
    poi_id        INT           NOT NULL,
    name          NVARCHAR(100) NOT NULL,      -- Tên món (VD: Ốc hương xào bơ tỏi)
    category      NVARCHAR(50)  NULL,          -- Danh mục (VD: Các món ốc, Đồ uống)
    price         DECIMAL(18,2) NOT NULL,      -- Giá tiền (VD: 120000)
    status        NVARCHAR(20)  DEFAULT 'AVAILABLE', -- Trạng thái: 'AVAILABLE' (Đang bán), 'OUT_OF_STOCK' (Hết hàng)
    created_at    DATETIME      DEFAULT GETDATE(),

    CONSTRAINT fk_menu_items_poi FOREIGN KEY (poi_id) 
        REFERENCES POIS(poi_id) ON DELETE CASCADE
)
GO

-- ============================================================
--  3. BẢNG TRANSACTIONS (Quản lý Giao dịch / Doanh thu)
--  Phục vụ cho trang: Doanh thu (ThuNhap.razor)
-- ============================================================
CREATE TABLE TRANSACTIONS (
    transaction_id INT           IDENTITY(1,1) PRIMARY KEY,
    poi_id         INT           NOT NULL,
    table_number   NVARCHAR(20)  NULL,         -- Bàn số mấy (VD: Bàn 04)
    amount         DECIMAL(18,2) NOT NULL,     -- Số tiền thanh toán
    payment_method NVARCHAR(50)  NOT NULL,     -- Phương thức: 'Tiền mặt', 'Momo', 'VNPay'
    created_at     DATETIME      DEFAULT GETDATE(),

    CONSTRAINT fk_transactions_poi FOREIGN KEY (poi_id) 
        REFERENCES POIS(poi_id) ON DELETE CASCADE
)
GO 
-- ============================================================
--  INDEXES — Tối ưu truy vấn
-- ============================================================

-- Geofence: tìm POI theo tọa độ gần nhất
CREATE INDEX idx_pois_geo ON POIS (latitude, longitude) WHERE is_active = 1
GO

-- Narrations: lấy nội dung theo poi + ngôn ngữ
CREATE INDEX idx_narrations_poi_lang ON NARRATIONS (poi_id, language_id)
GO

-- Tracking: lọc log theo thiết bị và thời gian
CREATE INDEX idx_tracking_device ON TRACKING_LOGS (device_id, created_at DESC)
GO

-- Tracking: lấy log theo POI để thống kê
CREATE INDEX idx_tracking_poi ON TRACKING_LOGS (poi_id, event_type, created_at DESC)
GO

-- Tracking: lọc theo vị trí để vẽ heatmap
CREATE INDEX idx_tracking_geo ON TRACKING_LOGS (latitude, longitude)
GO

-- QR: tra cứu token nhanh
CREATE INDEX idx_qr_token ON QRCODES (qr_token) WHERE is_active = 1
GO

-- ============================================================
--  SEED DATA — Ngôn ngữ
-- ============================================================
INSERT INTO LANGUAGES (code, name, tts_voice) VALUES
('vi', N'Tiếng Việt',  'vi-VN-Standard-A'),
('en', N'English',     'en-US-Neural2-C'),
('zh', N'中文',         'zh-CN-Standard-A'),
('ko', N'한국어',        'ko-KR-Standard-A'),
('ja', N'日本語',        'ja-JP-Standard-A')
GO

-- ============================================================
--  SEED DATA — POIs (6 gian hàng Vĩnh Khánh)
-- ============================================================
INSERT INTO POIS (name, address, phone, latitude, longitude, radius, priority) VALUES
(N'Ốc Oanh',           N'534 Vĩnh Khánh, P.8, Q.4', '0901234567', 10.76012345, 106.70298765, 20, 1),
(N'Ốc Vũ',             N'37 Vĩnh Khánh, P.8, Q.4',  '0987654321', 10.76054321, 106.70312345, 15, 2),
(N'Sushi Viên Vĩnh Khánh', N'Đường Vĩnh Khánh, P.8, Q.4', '0912345678', 10.76100000, 106.70400000, 15, 2),
(N'Lãng Quán',         N'531 Vĩnh Khánh, P.10, Q.4', '0888833111', 10.75921500, 106.70251200, 20, 1),
(N'Ớt Xiêm Quán',      N'568 Vĩnh Khánh, P.10, Q.4', '0983434926', 10.75884200, 106.70213000, 15, 2),
(N'Quán Ốc Sáu Nở',   N'128 Vĩnh Khánh, P.8, Q.4',  '0908355999', 10.76211000, 106.70452200, 15, 2)
GO

-- ============================================================
--  SEED DATA — Nội dung thuyết minh mẫu (Tiếng Việt)
-- ============================================================
INSERT INTO NARRATIONS (poi_id, language_id, title, content) VALUES
(1, 1, N'Ốc Oanh', N'Quán ốc Oanh là một trong những địa điểm nổi tiếng nhất tại phố ẩm thực Vĩnh Khánh, được yêu thích bởi món ốc hương rang muối ớt thơm lừng và hải sản tươi sống chế biến tại chỗ.'),
(2, 1, N'Ốc Vũ',   N'Ốc Vũ thu hút thực khách bởi không gian thoáng mát, hải sản đa dạng và giá cả bình dân. Đây là điểm dừng chân lý tưởng cho những ai muốn thưởng thức ẩm thực dân dã của người Sài Gòn.'),
(3, 1, N'Sushi Viên Vĩnh Khánh', N'Điểm khác biệt thú vị của phố Vĩnh Khánh, quán sushi viên phục vụ các viên sushi giá rẻ với đa dạng topping, là lựa chọn ăn vặt được giới trẻ yêu thích.'),
(4, 1, N'Lãng Quán', N'Lãng Quán chuyên phục vụ các món lẩu và nướng với thực đơn đa dạng gồm giò heo muối chiên giòn, dồi vịt, sụn gà, và cà kèo nướng. Quán được đặc biệt đánh giá cao về nước chấm đậm đà.'),
(5, 1, N'Ớt Xiêm Quán', N'Với không gian ấm cúng và thực đơn phong phú từ thịt, hải sản đến cơm mì, Ớt Xiêm Quán là địa điểm được các bạn trẻ yêu thích khi ghé thăm phố ẩm thực Vĩnh Khánh.'),
(6, 1, N'Quán Ốc Sáu Nở', N'Sáu Nở nổi tiếng với hải sản tươi ngon mỗi ngày và các loại nước chấm đặc trưng đậm đà, tạo nên hương vị khó quên cho từng món ăn trên phố Vĩnh Khánh.')
GO

-- ============================================================
--  SEED DATA — QR Codes
--  Không seed QR mặc định; vendor tự tạo từ trang Mô tả & hình ảnh.
-- ============================================================

INSERT INTO USERS (name, email, password_hash, role, is_active)
VALUES (
    N'Vũ Việt Hoàng', 
    'vuviethoan123@gmail.com', 
    -- Đây là mã hash mẫu của mật khẩu '123456789'
    '$2a$11$mC8.H1fG.6D9O6f.f.f.f.OqOqOqOqOqOqOqOqOqOqOqOqOqOqOqOq', 
    'Admin', 
    1
);
GO

ALTER TABLE MENU_ITEMS
ADD 
    image_url NVARCHAR(255) NULL,    -- Lưu link ảnh món ăn
    description NVARCHAR(500) NULL,  -- Đoạn mô tả món ăn (dùng để tạo giọng đọc)
    audio_url NVARCHAR(255) NULL;    -- Lưu link file mp3 sau khi AI đọc xong
GO

SELECT * FROM USERS WHERE email = 'vuviethoan123@gmail.com';
SELECT * FROM USERS;

ALTER TABLE POIS ADD description NVARCHAR(MAX) NULL;
ALTER TABLE POIS ADD image_url NVARCHAR(255) NULL;
GO

-- ============================================================
--  Bổ sung CSDL đã tồn tại (không chạy CREATE từ đầu)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'NARRATIONS') AND name = N'audio_url'
)
BEGIN
    ALTER TABLE NARRATIONS ADD audio_url NVARCHAR(500) NULL;
END
GO

ALTER TABLE USERS DROP CONSTRAINT chk_users_role;
ALTER TABLE USERS ADD CONSTRAINT chk_users_role CHECK (role IN ('Admin', 'Vendor', 'User'));
GO

IF COL_LENGTH('NARRATIONS', 'audio_url_auto') IS NULL
    ALTER TABLE NARRATIONS ADD audio_url_auto NVARCHAR(500) NULL;
GO
IF COL_LENGTH('NARRATIONS', 'audio_url_qr') IS NULL
    ALTER TABLE NARRATIONS ADD audio_url_qr NVARCHAR(500) NULL;
GO

SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'NARRATIONS'
  AND COLUMN_NAME IN ('audio_url_auto', 'audio_url_qr');

-- ============================================================
--  TOUR_SETTINGS — Cấu hình toàn cục tour (TTS intro phố...)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TOUR_SETTINGS')
BEGIN
    CREATE TABLE TOUR_SETTINGS (
        setting_id    INT            IDENTITY(1,1) PRIMARY KEY,
        setting_key   NVARCHAR(100)  NOT NULL,
            CONSTRAINT uq_tour_setting_key UNIQUE (setting_key),
        setting_value NVARCHAR(MAX)  NULL,
        updated_at    DATETIME       NULL
    );
END
GO