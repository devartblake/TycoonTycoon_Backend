/**
 * IP Geolocation map for audit events
 */

import { MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet'
import type { IPLocationData } from '../types'

interface IPMapProps {
  locations: IPLocationData[]
  isLoading: boolean
}

export function IPMap({ locations, isLoading }: IPMapProps) {
  if (isLoading) {
    return (
      <div className="operator-card h-96 bg-bg-secondary rounded flex items-center justify-center">
        <p className="text-ink-secondary">Loading map...</p>
      </div>
    )
  }

  if (!locations || locations.length === 0) {
    return (
      <div className="operator-card h-96 bg-bg-secondary rounded flex items-center justify-center">
        <p className="text-ink-secondary">No location data available</p>
      </div>
    )
  }

  // Calculate bounds to fit all markers
  const latitudes = locations.map((l) => l.latitude)
  const longitudes = locations.map((l) => l.longitude)
  const minLat = Math.min(...latitudes)
  const maxLat = Math.max(...latitudes)
  const minLon = Math.min(...longitudes)
  const maxLon = Math.max(...longitudes)
  const centerLat = (minLat + maxLat) / 2
  const centerLon = (minLon + maxLon) / 2

  // Color intensity based on event count
  const getColor = (eventCount: number, maxCount: number) => {
    const intensity = eventCount / maxCount
    const hue = 10 + intensity * 40 // From red (10) to orange (50)
    return `hsl(${hue}, 100%, 50%)`
  }

  const maxEventCount = Math.max(...locations.map((l) => l.eventCount), 1)

  return (
    <div className="operator-card overflow-hidden rounded">
      <MapContainer
        center={[centerLat, centerLon]}
        zoom={2}
        className="w-full h-96"
        style={{ height: '400px' }}
      >
        <TileLayer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution='&copy; OpenStreetMap contributors'
          opacity={0.8}
        />

        {locations.map((location) => (
          <CircleMarker
            key={`${location.country}-${location.city}`}
            center={[location.latitude, location.longitude]}
            radius={Math.sqrt(location.eventCount) * 2}
            pathOptions={{
              fillColor: getColor(location.eventCount, maxEventCount),
              color: '#333',
              weight: 1,
              opacity: 0.8,
              fillOpacity: 0.6,
            }}
          >
            <Popup>
              <div className="text-xs">
                <p className="font-semibold">{location.city}, {location.country}</p>
                <p className="text-ink-secondary">IP: {location.ip}</p>
                <p className="text-status-offline">Events: {location.eventCount}</p>
              </div>
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>

      {/* Legend */}
      <div className="p-3 border-t border-panel-border">
        <p className="text-xs text-ink-tertiary mb-2">Marker size = event frequency</p>
        <div className="flex gap-4 text-xs">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full" style={{ backgroundColor: getColor(1, maxEventCount) }} />
            <span className="text-ink-secondary">Few events</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 rounded-full" style={{ backgroundColor: getColor(maxEventCount * 0.5, maxEventCount) }} />
            <span className="text-ink-secondary">Medium</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-5 h-5 rounded-full" style={{ backgroundColor: getColor(maxEventCount, maxEventCount) }} />
            <span className="text-ink-secondary">Many events</span>
          </div>
        </div>
      </div>
    </div>
  )
}
