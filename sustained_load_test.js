import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    vus: 100, // 100 thiết bị giả lập
    duration: '10m', // Chạy duy trì trong 10 phút (Bạn có thể tắt sớm bằng Ctrl+C)
    insecureSkipTLSVerify: true,
};

const BASE_URL = 'https://localhost:7105';

export default function () {
    const deviceId = `test-device-${__VU}`;
    const randomLat = 10.758 + (Math.random() - 0.5) * 0.005;
    const randomLon = 106.705 + (Math.random() - 0.5) * 0.005;

    const payload = JSON.stringify({
        deviceId: deviceId,
        latitude: randomLat,
        longitude: randomLon,
        eventType: 'move'
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'X-Tunnel-Authorization': 'tunnel'
        },
    };

    http.post(`${BASE_URL}/api/Tracking/log`, payload, params);

    sleep(3); // Gửi heartbeat mỗi 3 giây
}
