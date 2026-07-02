import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * IP Geolocation map for audit events
 */
import { MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
export function IPMap({ locations, isLoading }) {
    if (isLoading) {
        return (_jsx("div", { className: "operator-card h-96 bg-bg-secondary rounded flex items-center justify-center", children: _jsx("p", { className: "text-ink-secondary", children: "Loading map..." }) }));
    }
    if (!locations || locations.length === 0) {
        return (_jsx("div", { className: "operator-card h-96 bg-bg-secondary rounded flex items-center justify-center", children: _jsx("p", { className: "text-ink-secondary", children: "No location data available" }) }));
    }
    // Calculate bounds to fit all markers
    const latitudes = locations.map((l) => l.latitude);
    const longitudes = locations.map((l) => l.longitude);
    const minLat = Math.min(...latitudes);
    const maxLat = Math.max(...latitudes);
    const minLon = Math.min(...longitudes);
    const maxLon = Math.max(...longitudes);
    const centerLat = (minLat + maxLat) / 2;
    const centerLon = (minLon + maxLon) / 2;
    // Color intensity based on event count
    const getColor = (eventCount, maxCount) => {
        const intensity = eventCount / maxCount;
        const hue = 10 + intensity * 40; // From red (10) to orange (50)
        return `hsl(${hue}, 100%, 50%)`;
    };
    const maxEventCount = Math.max(...locations.map((l) => l.eventCount), 1);
    return (_jsxs("div", { className: "operator-card overflow-hidden rounded", children: [_jsxs(MapContainer, { center: [centerLat, centerLon], zoom: 2, className: "w-full h-96", style: { height: '400px' }, children: [_jsx(TileLayer, { url: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", attribution: '\u00A9 OpenStreetMap contributors', opacity: 0.8 }), locations.map((location) => (_jsx(CircleMarker, { center: [location.latitude, location.longitude], radius: Math.sqrt(location.eventCount) * 2, pathOptions: {
                            fillColor: getColor(location.eventCount, maxEventCount),
                            color: '#333',
                            weight: 1,
                            opacity: 0.8,
                            fillOpacity: 0.6,
                        }, children: _jsx(Popup, { children: _jsxs("div", { className: "text-xs", children: [_jsxs("p", { className: "font-semibold", children: [location.city, ", ", location.country] }), _jsxs("p", { className: "text-ink-secondary", children: ["IP: ", location.ip] }), _jsxs("p", { className: "text-status-offline", children: ["Events: ", location.eventCount] })] }) }) }, `${location.country}-${location.city}`)))] }), _jsxs("div", { className: "p-3 border-t border-panel-border", children: [_jsx("p", { className: "text-xs text-ink-tertiary mb-2", children: "Marker size = event frequency" }), _jsxs("div", { className: "flex gap-4 text-xs", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("div", { className: "w-3 h-3 rounded-full", style: { backgroundColor: getColor(1, maxEventCount) } }), _jsx("span", { className: "text-ink-secondary", children: "Few events" })] }), _jsxs("div", { className: "flex items-center gap-2", children: [_jsx("div", { className: "w-4 h-4 rounded-full", style: { backgroundColor: getColor(maxEventCount * 0.5, maxEventCount) } }), _jsx("span", { className: "text-ink-secondary", children: "Medium" })] }), _jsxs("div", { className: "flex items-center gap-2", children: [_jsx("div", { className: "w-5 h-5 rounded-full", style: { backgroundColor: getColor(maxEventCount, maxEventCount) } }), _jsx("span", { className: "text-ink-secondary", children: "Many events" })] })] })] })] }));
}
