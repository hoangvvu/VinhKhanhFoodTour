import http from 'k6/http';
import { sleep, check } from 'k6';

// CẤU HÌNH STRESS TEST
export const options = {
    stages: [
        { duration: '1m', target: 200 },  // Tăng từ 0 lên 200 users trong 1 phút
        { duration: '2m', target: 500 },  // Tăng tiếp lên 500 users trong 2 phút
        { duration: '3m', target: 1000 }, // Tăng tiếp lên 1000 users trong 3 phút
        { duration: '999h', target: 1000 }, // Duy trì ở 1000 users (dùng Ctrl+C để tắt)
    ],
    thresholds: {
        http_req_failed: ['rate<0.1'], // Nếu tỉ lệ lỗi > 10%, coi như bài test thất bại
        http_req_duration: ['p(95)<2000'], // 95% request phải xong trong dưới 2s
    },
    insecureSkipTLSVerify: true, // Bỏ qua xác thực SSL cho localhost
};

const BASE_URL = 'https://localhost:7105';

export default function () {
    const deviceId = `test-device-${__VU}`;
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
            'X-Tunnel-Authorization': 'tunnel'
        },
    };

    const res = http.post(`${BASE_URL}/api/Tracking/log`, payload, params);

    check(res, {
        'status is 200/204': (r) => r.status === 200 || r.status === 204,
    });

    sleep(1); // Mỗi user bắn 1 request mỗi giây để ép tải cao
}
