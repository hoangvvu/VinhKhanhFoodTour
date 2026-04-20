import http from 'k6/http';
import { sleep, check } from 'k6';

// CẤU HÌNH TEST
export const options = {
    vus: 100, // 100 thiết bị giả lập cùng lúc
    duration: '999h', // Chạy liên tục (dùng Ctrl+C để tắt)
    insecureSkipTLSVerify: true, // Bỏ qua xác thực SSL cho localhost
};

// URL API Local
const BASE_URL = 'https://localhost:7105';

export default function () {
    // 1. Tạo Device ID giả lập cho từng Virtual User (VU)
    const deviceId = `test-device-${__VU}`;
    
    // 2. Giả lập tọa độ quanh khu vực Vĩnh Khánh (Quận 4)
    const randomLat = 10.758 + (Math.random() - 0.5) * 0.01;
    const randomLon = 106.705 + (Math.random() - 0.5) * 0.01;

    const payload = JSON.stringify({
        deviceId: deviceId,
        poiId: null,
        latitude: randomLat,
        longitude: randomLon,
        eventType: 'move',
        languageCode: 'vi'
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'ngrok-skip-browser-warning': 'true',
            'X-Tunnel-Authorization': 'tunnel'
        },
    };

    // 3. Gửi request POST tới API Tracking
    const res = http.post(`${BASE_URL}/api/Tracking/log`, payload, params);

    // 4. Kiểm tra phản hồi có phải 200 OK không
    check(res, {
        'status is 200/204': (r) => r.status === 200 || r.status === 204,
    });

    // 5. Mỗi thiết bị gửi tín hiệu sau mỗi 3-5 giây (giống thực tế)
    sleep(Math.random() * 2 + 3);
}
