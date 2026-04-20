// ═══════════════════════════════════════════════════
//  VK Admin — JS Interop (Leaflet.js / OpenStreetMap)
//  Đặt tại: wwwroot/js/admin-interop.js
// ═══════════════════════════════════════════════════

// ── 1. Map Picker (Leaflet) ──

let pickerMap = null;
let pickerMarker = null;
let searchDebounceTimer = null;
let currentDotNetRef = null; // Lưu lại để dùng cho các hàm trigger bên ngoài

window.initMapPicker = function(elementId, lat, lng, searchInputId, searchBtnId, suggestionContainerId, dotNetRef) {
    console.log('[MAP] Initializing Map Picker...', { elementId, lat, lng });
    const el = document.getElementById(elementId);
    if (!el) return;

    currentDotNetRef = dotNetRef;

    if (pickerMap) {
        pickerMap.remove();
        pickerMap = null;
    }

    pickerMap = L.map(el, {
        zoomControl: true,
        attributionControl: false
    }).setView([lat, lng], 17);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19
    }).addTo(pickerMap);

    pickerMarker = L.marker([lat, lng], { draggable: true }).addTo(pickerMap);

    pickerMarker.on('dragend', function (e) {
        const pos = pickerMarker.getLatLng();
        dotNetRef.invokeMethodAsync('OnMapLocationChanged', pos.lat, pos.lng);
    });

    pickerMap.on('click', function (e) {
        const pos = e.latlng;
        pickerMarker.setLatLng(pos);
        dotNetRef.invokeMethodAsync('OnMapLocationChanged', pos.lat, pos.lng);
        hideSuggestions(suggestionContainerId);
    });

    // ── Xử lý Autocomplete ──
    const searchInput = document.getElementById(searchInputId);
    const suggestionContainer = document.getElementById(suggestionContainerId);

    if (searchInput && suggestionContainer) {
        searchInput.addEventListener('input', () => {
            clearTimeout(searchDebounceTimer);
            const query = searchInput.value.trim();

            if (query.length < 2) {
                hideSuggestions(suggestionContainerId);
                return;
            }

            searchDebounceTimer = setTimeout(() => {
                fetchAutocomplete(query, suggestionContainerId, dotNetRef);
            }, 400);
        });

        // Đóng gợi ý khi click ra ngoài
        document.addEventListener('click', (e) => {
            if (e.target !== searchInput && e.target !== suggestionContainer && !suggestionContainer.contains(e.target)) {
                hideSuggestions(suggestionContainerId);
            }
        });
        
        // Xử lý Paste
        searchInput.addEventListener('paste', (e) => {
            const pasteData = (e.clipboardData || window.clipboardData).getData('text');
            setTimeout(() => {
                const result = parseGoogleLocation(pasteData);
                if (result) {
                    pickerMap.setView([result.lat, result.lng], 18);
                    pickerMarker.setLatLng([result.lat, result.lng]);
                    dotNetRef.invokeMethodAsync('OnMapLocationChanged', result.lat, result.lng);
                }
            }, 100);
        });
    }
};

/**
 * Hàm trigger tìm kiếm từ Blazor
 */
window.triggerMapSearch = function(query, suggestionContainerId) {
    if (!query) return;
    performExplicitSearch(query, suggestionContainerId, currentDotNetRef);
};

/**
 * Bóc tách tọa độ từ URL Google Maps hoặc chuỗi tọa độ
 */
function parseGoogleLocation(input) {
    if (!input) return null;

    // 1. Ưu TIÊN CAO: Regex tìm !3dLAT!4dLNG
    const regexData = /!3d(-?\d+\.\d+)!4d(-?\d+\.\d+)/;
    const matchData = input.match(regexData);
    if (matchData) return { lat: parseFloat(matchData[1]), lng: parseFloat(matchData[2]) };

    // 2. Regex tìm @lat,lng
    const regexAt = /@(-?\d+\.\d+),(-?\d+\.\d+)/;
    const matchAt = input.match(regexAt);
    if (matchAt) return { lat: parseFloat(matchAt[1]), lng: parseFloat(matchAt[2]) };

    // 3. Regex tìm q=lat,lng
    const regexQuery = /[?&](query|q|ll)=(-?\d+\.\d+),(-?\d+\.\d+)/;
    const matchQuery = input.match(regexQuery);
    if (matchQuery) return { lat: parseFloat(matchQuery[2]), lng: parseFloat(matchQuery[3]) };

    // 4. Regex tìm tọa độ thuần túy
    const regexRaw = /(-?\d+\.\d+)\s*[,\s]\s*(-?\d+\.\d+)/;
    const matchRaw = input.match(regexRaw);
    if (matchRaw) return { lat: parseFloat(matchRaw[1]), lng: parseFloat(matchRaw[2]) };

    return null;
}

async function performExplicitSearch(query, containerId, dotNetRef) {
    if (!query || query.trim().length < 2) return;
    console.log('[MAP] Explicit search trigger:', query);
    
    // Thử parse trước trường hợp copy paste link Google Maps
    const parsed = parseGoogleLocation(query);
    if (parsed) {
        pickerMap.setView([parsed.lat, parsed.lng], 18);
        pickerMarker.setLatLng([parsed.lat, parsed.lng]);
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnMapLocationChanged', parsed.lat, parsed.lng);
            dotNetRef.invokeMethodAsync('OnAddressSearchSelected', query.length > 60 ? "Vị trí từ liên kết" : query);
        }
        hideSuggestions(containerId);
        return;
    }

    // Nếu không parse được thì dùng Photon API
    const url = `https://photon.komoot.io/api/?q=${encodeURIComponent(query)}&lang=vi&lat=10.7578&lon=106.7095&limit=1`;
    try {
        const response = await fetch(url);
        const data = await response.json();
        if (data.features && data.features.length > 0) {
            const first = data.features[0];
            const coords = first.geometry.coordinates;
            const lat = coords[1];
            const lng = coords[0];
            
            pickerMap.setView([lat, lng], 18);
            pickerMarker.setLatLng([lat, lng]);
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnMapLocationChanged', lat, lng);
                dotNetRef.invokeMethodAsync('OnAddressSearchSelected', (first.properties.name || first.properties.street || query));
            }
            hideSuggestions(containerId);
        }
    } catch (err) {
        console.error("[OSM] Search error:", err);
    }
}

async function fetchAutocomplete(query, containerId, dotNetRef) {
    const url = `https://photon.komoot.io/api/?q=${encodeURIComponent(query)}&lang=vi&lat=10.7578&lon=106.7095&limit=10`;
    try {
        const response = await fetch(url);
        const data = await response.json();
        renderSuggestions(data.features, containerId, dotNetRef);
    } catch (err) {
        console.error("[OSM] Autocomplete error:", err);
    }
}

function renderSuggestions(features, containerId, dotNetRef) {
    const container = document.getElementById(containerId);
    if (!container) return;

    if (!features || features.length === 0) {
        hideSuggestions(containerId);
        return;
    }

    container.innerHTML = '';
    container.style.display = 'block';

    features.forEach(f => {
        const props = f.properties;
        const coords = f.geometry.coordinates; 
        
        const name = props.name || "";
        const city = props.city || props.state || "";
        const street = props.street || "";
        const houseNumber = props.housenumber || "";
        
        let displayTitle = name;
        if (!displayTitle && street) displayTitle = (houseNumber ? houseNumber + " " : "") + street;
        if (!displayTitle) displayTitle = "Địa điểm không tên";
        
        let displaySub = [props.district, city, props.country].filter(x => x).join(', ');

        const li = document.createElement('li');
        li.className = 'suggestion-item';
        li.innerHTML = `
            <div class="s-icon"><i class="bi bi-geo-alt"></i></div>
            <div class="s-content">
                <div class="s-title">${displayTitle}</div>
                <div class="s-sub">${displaySub}</div>
            </div>
        `;

        li.onclick = (e) => {
            e.stopPropagation();
            const lat = coords[1];
            const lng = coords[0];
            const fullName = (displayTitle + (displaySub ? ", " + displaySub : "")).trim();

            pickerMap.setView([lat, lng], 18);
            pickerMarker.setLatLng([lat, lng]);

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnMapLocationChanged', lat, lng);
                dotNetRef.invokeMethodAsync('OnAddressSearchSelected', fullName);
            }

            hideSuggestions(containerId);
            
            const input = document.getElementById('map-search-input');
            if (input) input.value = fullName;
        };

        container.appendChild(li);
    });
}

function hideSuggestions(containerId) {
    const container = document.getElementById(containerId);
    if (container) {
        container.style.display = 'none';
        container.innerHTML = '';
    }
}

window.updateMapMarker = function(lat, lng) {
    if (pickerMap && pickerMarker) {
        pickerMarker.setLatLng([lat, lng]);
        pickerMap.setView([lat, lng]);
    }
};

// ── 2. Multi-POI Overview Map (Leaflet) ──

let overviewMap = null;

window.initPoiOverviewMap = function(elementId, pois) {
    const el = document.getElementById(elementId);
    if (!el) {
        console.warn("[MAP] Element not found for init", elementId);
        return;
    }

    if (overviewMap) {
        try {
            overviewMap.remove();
        } catch(err) {
            console.warn("[MAP] Handled error during old map removal", err);
        }
        overviewMap = null;
    }
    
    // Đảm bảo element hoàn toàn sạch (không có "_leaflet_id")
    el._leaflet_id = null;
    window.__vkOverviewHeatLayer = null; // Reset heatmap layer

    try {
        overviewMap = L.map(el, {
            attributionControl: false,
            zoomControl: false
        }).setView([10.7595, 106.7040], 16);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            maxZoom: 20
        }).addTo(overviewMap);

        const group = L.featureGroup();

        pois.forEach(poi => {
            // Priority logic: 5 is VIP, 1 is Low
            let color = '#E8A020'; // Default (3-4)
            let radius = 10;
            
            if (poi.priority >= 5) {
                color = '#C8372D'; // VIP (Red)
                radius = 14;
            } else if (poi.priority >= 4) {
                color = '#ff4d4d'; // High (Lighter red)
                radius = 12;
            } else if (poi.priority <= 1) {
                color = '#8A8078'; // Low (Grey)
                radius = 7;
            } else if (poi.priority === 2) {
                color = '#b38600'; // Medium-Low (Darker yellow)
                radius = 8;
            }
            
            const marker = L.circleMarker([poi.lat, poi.lng], {
                radius: radius,
                fillColor: color,
                color: "#fff",
                weight: 2,
                opacity: 1,
                fillOpacity: 0.9
            }).addTo(group);

            marker.bindPopup(`
                <div style="color: #000; padding: 5px; min-width: 150px;">
                    <b style="font-size: 14px; display: block; margin-bottom: 4px;">${poi.name}</b>
                    <div style="font-size: 11px; color: #666; line-height: 1.4;">${poi.address || ''}</div>
                    <div style="margin-top: 8px; font-size: 10px; color: #999; border-top: 1px solid #eee; padding-top: 4px;">
                        Radius: ${poi.radius}m | Priority: ${poi.priority}
                    </div>
                </div>
            `);
        });

        group.addTo(overviewMap);
        if (pois.length > 0) {
            overviewMap.fitBounds(group.getBounds(), { padding: [30, 30] });
        }
        
        window.__vkOverviewMap = overviewMap; // Store globally for heatmap updates

        // Sửa lỗi mất Map: Bắt buộc Leaflet cập nhật lại kích thước vùng Canvas
        setTimeout(() => {
            if (overviewMap) {
                overviewMap.invalidateSize();
            }
        }, 300);

    } catch (err) {
        console.error("[MAP] Critical error initializing overview map:", err);
    }
};

/**
 * ── Heatmap Overlay for Overview Map ──
 */
window.updateOverviewHeatmap = function(rawData) {
    let data;
    if (typeof rawData === 'string') {
        try {
            data = JSON.parse(rawData);
        } catch(e) {
            console.error('[HEATMAP] Invalid JSON string provided');
            return;
        }
    } else {
        data = rawData;
    }

    console.log('[HEATMAP] Updating with data points:', data ? data.length : 0);
    if (!window.__vkOverviewMap) {
        console.warn('[HEATMAP] Map instance not found.');
        return;
    }

    const mapContainer = window.__vkOverviewMap.getContainer();
    if (!mapContainer || mapContainer.clientWidth === 0 || mapContainer.clientHeight === 0) {
        console.warn('[HEATMAP] Map container has zero size (possibly hidden). Skipping update.');
        return;
    }

    try {
        if (window.__vkOverviewHeatLayer) {
            window.__vkOverviewMap.removeLayer(window.__vkOverviewHeatLayer);
        }

        if (!data || data.length === 0) return;

        // In ra phần tử đầu tiên để debug định dạng mảng từ Blazor
        console.log('[HEATMAP] Format of first point:', data[0]);

        // Lọc và chuyển đổi dữ liệu thông minh hơn
        let cleanData = [];
        for (let i = 0; i < data.length; i++) {
            let p = data[i];
            if (Array.isArray(p) && p.length >= 2 && p[0] != null && p[1] != null) {
                cleanData.push([parseFloat(p[0]), parseFloat(p[1]), parseFloat(p[2] || 1.0)]);
            } else if (p && typeof p === 'object') {
                // Đề phòng Blazor serialize double[] thành object, hoặc DTO
                let lat = p.lat !== undefined ? p.lat : (p[0] !== undefined ? p[0] : null);
                let lng = p.lng !== undefined ? p.lng : (p.lon !== undefined ? p.lon : (p[1] !== undefined ? p[1] : null));
                let intensity = p.intensity !== undefined ? p.intensity : (p[2] !== undefined ? p[2] : 1.0);
                
                if (lat != null && lng != null) {
                    cleanData.push([parseFloat(lat), parseFloat(lng), parseFloat(intensity)]);
                }
            }
        }
        
        if (cleanData.length === 0) {
            console.warn('[HEATMAP] No valid points found after filtering. Sample:', data[0]);
            return;
        }

        window.__vkOverviewHeatLayer = L.heatLayer(cleanData, {
            radius: 45,
            blur: 15,
            maxZoom: 18,
            gradient: {
                0.1: '#fee0d2', // Hồng rất nhạt (thưa)
                0.3: '#fc9272', // Hồng cam
                0.5: '#fb6a4a', // Đỏ cam
                0.8: '#de2d26', // Đỏ đậm
                1.0: '#67000d'  // Đỏ đen (đông nhất)
            }
        }).addTo(window.__vkOverviewMap);
        
        console.log('[HEATMAP] Layers updated successfully.');
    } catch (err) {
        console.error('[HEATMAP] Error updating heatmap layer:', err);
    }
};

window.toggleOverviewHeatmap = function(visible) {
    if (!window.__vkOverviewMap || !window.__vkOverviewHeatLayer) return;
    
    if (visible) {
        window.__vkOverviewHeatLayer.addTo(window.__vkOverviewMap);
    } else {
        window.__vkOverviewMap.removeLayer(window.__vkOverviewHeatLayer);
    }
};

// ── 3. Helpers ────────────────────────────────────────

window.playAudioFromUrl = function(url) {
    const audio = new Audio(url);
    audio.play();
};

window.downloadImage = function(url, filename) {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'qr-code.png';
    a.target = '_blank';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};