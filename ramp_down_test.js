import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    stages: [
        { duration: '10s', target: 0 }, // Giảm dần từ số lượng hiện tại (100) về 0 trong 10 giây
    ],
    insecureSkipTLSVerify: true,
};

const BASE_URL = 'https://localhost:7105';

export default function () {
    const deviceId = `test-device-${__VU}`;
    const randomLat = 10.758;
    const randomLon = 106.705;

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

    sleep(1); // Gửi nốt các tín hiệu cuối trước khi thoát
}
