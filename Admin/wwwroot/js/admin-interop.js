// ── Google Maps Picker (Cập nhật chuẩn AdvancedMarkerElement 2024) ──
let map, marker;

window.initMapPicker = function(elementId, lat, lng, dotNetRef) {
    const checkGoogleAndInit = () => {
        if (typeof google !== 'undefined' && google.maps) {
            const el = document.getElementById(elementId);
            if (!el) return;

            const center = { lat: lat, lng: lng };
            map = new google.maps.Map(el, {
                center: center,
                zoom: 17,
                mapTypeId: 'roadmap',
                mapId: 'DEMO_MAP_ID' // Thêm mapId để tránh cảnh báo chuẩn mới
            });

            marker = new google.maps.Marker({
                position: center,
                map: map,
                draggable: true,
                title: 'Kéo để chọn vị trí'
            });

            marker.addListener('dragend', function() {
                const pos = marker.getPosition();
                dotNetRef.invokeMethodAsync('OnMapLocationChanged', pos.lat(), pos.lng());
            });

            map.addListener('click', function(e) {
                marker.setPosition(e.latLng);
                dotNetRef.invokeMethodAsync('OnMapLocationChanged', e.latLng.lat(), e.latLng.lng());
            });
        } else {
            // Nếu chưa thấy google maps, đợi 200ms rồi thử lại
            setTimeout(checkGoogleAndInit, 200);
        }
    };
    checkGoogleAndInit();
};

// Cập nhật vị trí marker từ Blazor (khi load POI khác)
window.updateMapMarker = function(lat, lng) {
    if (marker) {
        marker.position = { lat: lat, lng: lng };
    }
    if (map) {
        map.setCenter({ lat: lat, lng: lng });
    }
};