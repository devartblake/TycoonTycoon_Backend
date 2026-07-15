/**
 * IP Geolocation map for audit events
 */

import { useEffect, useMemo } from 'react'
import { MapContainer, TileLayer, CircleMarker, Popup, useMap } from 'react-leaflet'
import type { IPLocationData } from '../types'
import 'leaflet/dist/leaflet.css'

interface IPMapProps {
  locations: IPLocationData[]
  isLoading: boolean
}

/** Leaflet needs an invalidateSize after the container is laid out (hidden tabs / flex). */
function MapResizeFix() {
  const map = useMap()
  useEffect(() => {
    const t = window.setTimeout(() => map.invalidateSize(), 80)
    return () => window.clearTimeout(t)
  }, [map])
  return null
}

function isValidCoord(lat: number, lon: number) {
  return (
    Number.isFinite(lat) &&
    Number.isFinite(lon) &&
    Math.abs(lat) <= 90 &&
    Math.abs(lon) <= 180
  )
}

export function IPMap({ locations, isLoading }: IPMapProps) {
  const validLocations = useMemo(
    () =>
      (locations ?? []).filter(
        (l) => l && isValidCoord(Number(l.latitude), Number(l.longitude))
      ),
    [locations]
  )

  if (isLoading) {
    return (
      <div className="operator-card h-96 bg-bg-secondary rounded flex items-center justify-center">
        <p className="text-ink-secondary">Loading map...</p>
      </div>
    )
  }

  if (validLocations.length === 0) {
    return (
      <div className="operator-card h-96 bg-bg-secondary rounded flex flex-col items-center justify-center gap-2 px-6 text-center">
        <p className="text-ink-secondary font-medium">No location data available</p>
        <p className="text-xs text-ink-tertiary">
          Audit events need latitude/longitude (or a geo IP lookup) to plot on the map.
        </p>
      </div>
    )
  }

  const latitudes = validLocations.map((l) => Number(l.latitude))
  const longitudes = validLocations.map((l) => Number(l.longitude))
  const centerLat = (Math.min(...latitudes) + Math.max(...latitudes)) / 2
  const centerLon = (Math.min(...longitudes) + Math.max(...longitudes)) / 2

  const getColor = (eventCount: number, maxCount: number) => {
    const intensity = maxCount > 0 ? eventCount / maxCount : 0
    const hue = 10 + intensity * 40
    return `hsl(${hue}, 100%, 50%)`
  }

  const maxEventCount = Math.max(...validLocations.map((l) => l.eventCount), 1)

  return (
    <div className="operator-card overflow-hidden rounded">
      <div className="px-4 py-2 border-b border-panel-border flex items-center justify-between">
        <h3 className="text-sm font-semibold text-ink-primary">Admin access by location</h3>
        <span className="text-xs text-ink-tertiary">{validLocations.length} locations</span>
      </div>
      {/* Fixed height + relative: Leaflet requires explicit pixel height */}
      <div className="relative w-full" style={{ height: 400 }}>
        <MapContainer
          center={[centerLat, centerLon]}
          zoom={2}
          scrollWheelZoom={false}
          className="absolute inset-0 z-0 w-full h-full"
          style={{ height: '100%', width: '100%', background: '#e8e4dd' }}
        >
          <MapResizeFix />
          <TileLayer
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          />

          {validLocations.map((location, idx) => (
            <CircleMarker
              key={`${location.ip}-${location.city}-${idx}`}
              center={[Number(location.latitude), Number(location.longitude)]}
              radius={Math.max(4, Math.sqrt(location.eventCount) * 2.5)}
              pathOptions={{
                fillColor: getColor(location.eventCount, maxEventCount),
                color: '#333',
                weight: 1,
                opacity: 0.85,
                fillOpacity: 0.65,
              }}
            >
              <Popup>
                <div className="text-xs">
                  <p className="font-semibold">
                    {location.city}, {location.country}
                  </p>
                  <p>IP: {location.ip}</p>
                  <p>Events: {location.eventCount}</p>
                </div>
              </Popup>
            </CircleMarker>
          ))}
        </MapContainer>
      </div>

      <div className="p-3 border-t border-panel-border">
        <p className="text-xs text-ink-tertiary mb-2">Marker size = event frequency</p>
        <div className="flex gap-4 text-xs">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full" style={{ backgroundColor: getColor(1, maxEventCount) }} />
            <span className="text-ink-secondary">Few events</span>
          </div>
          <div className="flex items-center gap-2">
            <div
              className="w-4 h-4 rounded-full"
              style={{ backgroundColor: getColor(maxEventCount * 0.5, maxEventCount) }}
            />
            <span className="text-ink-secondary">Medium</span>
          </div>
          <div className="flex items-center gap-2">
            <div
              className="w-5 h-5 rounded-full"
              style={{ backgroundColor: getColor(maxEventCount, maxEventCount) }}
            />
            <span className="text-ink-secondary">Many events</span>
          </div>
        </div>
      </div>
    </div>
  )
}
