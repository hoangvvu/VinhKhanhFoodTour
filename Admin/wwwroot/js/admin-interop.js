// ═══════════════════════════════════════════════════
//  VK Admin — JS Interop (Google Maps + Audio)
//  Đặt file này tại: wwwroot/js/admin-interop.js
// ═══════════════════════════════════════════════════

// ── Google Maps Picker ──────────────────────────────
let map, marker;

window.initMapPicker = function(elementId, lat, lng, dotNetRef) {
    const center = { lat: lat, lng: lng };

    map = new google.maps.Map(document.getElementById(elementId), {
        center: center,
        zoom: 17,
        mapTypeId: 'roadmap',
        disableDefaultUI: false,
        zoomControl: true,
        streetViewControl: false,
        mapTypeControl: false,
        fullscreenControl: true
    });

    marker = new google.maps.Marker({
        position: center,
        map: map,
        draggable: true,
        title: 'Kéo để chọn vị trí'
    });

    // Khi kéo thả marker xong → gửi tọa độ về Blazor
    marker.addListener('dragend', function() {
        const pos = marker.getPosition();
        dotNetRef.invokeMethodAsync('OnMapLocationChanged', pos.lat(), pos.lng());
    });

    // Khi click vào bản đồ → di chuyển marker tới đó
    map.addListener('click', function(e) {
        marker.setPosition(e.latLng);
        dotNetRef.invokeMethodAsync('OnMapLocationChanged', e.latLng.lat(), e.latLng.lng());
    });
};

// Cập nhật vị trí marker từ Blazor (khi load POI khác)
window.updateMapMarker = function(lat, lng) {
    if (map && marker) {
        const pos = { lat: lat, lng: lng };
        marker.setPosition(pos);
        map.panTo(pos);
    }
};

// ── Audio Player ────────────────────────────────────
window.playAudioFromUrl = function(url) {
    const audio = new Audio(url);
    audio.play();
};