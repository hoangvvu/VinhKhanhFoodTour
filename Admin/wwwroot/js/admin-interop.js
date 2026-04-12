// ═══════════════════════════════════════════════════
//  VK Admin — JS Interop (Leaflet.js + OpenStreetMap)
//  Đặt tại: wwwroot/js/admin-interop.js
//
//  Thay thế hoàn toàn Google Maps
//  ✅ Miễn phí 100%, không cần API key
//  ✅ Nhẹ (~40KB), nhanh
//  ✅ Không bị deprecated
// ═══════════════════════════════════════════════════

// ── 1. Map Picker — Chọn vị trí kéo thả (ThongTinQuan.razor) ──

let pickerMap = null;
let pickerMarker = null;

window.initMapPicker = function(elementId, lat, lng, dotNetRef) {
    const el = document.getElementById(elementId);
    if (!el) { console.warn('[MAP] Element not found:', elementId); return; }

    // Xóa map cũ nếu đã khởi tạo trước đó (tránh lỗi re-render Blazor)
    if (pickerMap) {
        pickerMap.remove();
        pickerMap = null;
    }

    pickerMap = L.map(elementId).setView([lat, lng], 17);

    // Tile layer — OpenStreetMap (miễn phí, không key)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap',
        maxZoom: 19
    }).addTo(pickerMap);

    // Marker kéo thả được
    pickerMarker = L.marker([lat, lng], {
        draggable: true,
        autoPan: true
    }).addTo(pickerMap);

    pickerMarker.bindPopup('Kéo ghim để chọn vị trí').openPopup();

    // Khi kéo thả xong → gửi tọa độ về Blazor
    pickerMarker.on('dragend', function() {
        const pos = pickerMarker.getLatLng();
        pickerMarker.setPopupContent(
            'Lat: ' + pos.lat.toFixed(8) + '<br>Lng: ' + pos.lng.toFixed(8)
        ).openPopup();
        dotNetRef.invokeMethodAsync('OnMapLocationChanged', pos.lat, pos.lng);
    });

    // Click vào bản đồ → di chuyển marker
    pickerMap.on('click', function(e) {
        pickerMarker.setLatLng(e.latlng);
        pickerMarker.setPopupContent(
            'Lat: ' + e.latlng.lat.toFixed(8) + '<br>Lng: ' + e.latlng.lng.toFixed(8)
        ).openPopup();
        dotNetRef.invokeMethodAsync('OnMapLocationChanged', e.latlng.lat, e.latlng.lng);
    });
};

// Cập nhật marker từ Blazor (khi load POI khác)
window.updateMapMarker = function(lat, lng) {
    if (pickerMap && pickerMarker) {
        pickerMarker.setLatLng([lat, lng]);
        pickerMap.panTo([lat, lng]);
    }
};

// ── 2. Multi-POI Overview Map (GianHang / Home) ─────

let overviewMap = null;

window.initPoiOverviewMap = function(elementId, pois) {
    const el = document.getElementById(elementId);
    if (!el) return;

    // Xóa map cũ
    if (overviewMap) {
        overviewMap.remove();
        overviewMap = null;
    }

    const vinhKhanhCenter = [10.7595, 106.7040];

    overviewMap = L.map(elementId).setView(vinhKhanhCenter, 16);

    // Dark tile theme (CartoDB Dark Matter — miễn phí)
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        maxZoom: 19
    }).addTo(overviewMap);

    const bounds = L.latLngBounds();

    pois.forEach(function(poi) {
        const pos = [poi.lat, poi.lng];
        bounds.extend(pos);

        // Tạo icon tùy chỉnh với màu theo priority
        const color = poi.priority === 1 ? '#C8372D' : '#E8A020';
        const icon = L.divIcon({
            className: 'custom-poi-marker',
            html: '<div style="' +
                'background:' + color + ';' +
                'color:#fff;' +
                'width:28px;height:28px;' +
                'border-radius:50%;' +
                'display:flex;align-items:center;justify-content:center;' +
                'font-size:12px;font-weight:bold;' +
                'border:2px solid #1a1a1a;' +
                'box-shadow:0 2px 6px rgba(0,0,0,0.4);' +
                '">' + poi.priority + '</div>',
            iconSize: [28, 28],
            iconAnchor: [14, 14]
        });

        const marker = L.marker(pos, { icon: icon }).addTo(overviewMap);

        marker.bindPopup(
            '<div style="min-width:160px;">' +
            '<strong>' + poi.name + '</strong><br/>' +
            '<small style="color:#666;">' + (poi.address || '') + '</small><br/>' +
            '<small>Bán kính: ' + poi.radius + 'm | Ưu tiên: ' + poi.priority + '</small>' +
            '</div>'
        );
    });

    if (pois.length > 0) {
        overviewMap.fitBounds(bounds, { padding: [30, 30] });
    }
};

// ── 3. Audio Player ──────────────────────────────────
window.playAudioFromUrl = function(url) {
    const audio = new Audio(url);
    audio.play();
};

// ── 4. Download helper ───────────────────────────────
window.downloadImage = function(url, filename) {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'qr-code.png';
    a.target = '_blank';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};