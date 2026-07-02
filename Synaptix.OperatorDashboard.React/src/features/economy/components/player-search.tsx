/**
 * Player search dropdown
 */

import { useState } from 'react'
import { useSearchPlayers } from '../hooks/useEconomy'

interface PlayerSearchProps {
  onSelectPlayer: (playerId: string) => void
}

export function PlayerSearch({ onSelectPlayer }: PlayerSearchProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [showResults, setShowResults] = useState(false)
  const searchResults = useSearchPlayers(searchQuery, 20)

  return (
    <div className="operator-card space-y-3 relative">
      <label htmlFor="search" className="block text-sm font-medium text-ink-primary">
        Search Player
      </label>
      <div className="relative">
        <input
          id="search"
          type="text"
          value={searchQuery}
          onChange={(e) => {
            setSearchQuery(e.target.value)
            setShowResults(true)
          }}
          onFocus={() => setShowResults(true)}
          placeholder="Email, handle, or player ID..."
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
        />

        {/* Results Dropdown */}
        {showResults && searchQuery && (
          <div className="absolute top-full left-0 right-0 mt-1 bg-bg-primary border border-panel-border rounded shadow-lg z-10 max-h-60 overflow-y-auto">
            {searchResults.isLoading ? (
              <div className="p-3 text-sm text-ink-secondary">Searching...</div>
            ) : searchResults.data && searchResults.data.length > 0 ? (
              <div>
                {searchResults.data.map((player) => (
                  <button
                    key={player.playerId}
                    onClick={() => {
                      onSelectPlayer(player.playerId)
                      setSearchQuery('')
                      setShowResults(false)
                    }}
                    className="w-full text-left px-3 py-2 hover:bg-bg-secondary transition-colors border-b border-panel-border last:border-b-0"
                  >
                    <p className="font-medium text-ink-primary text-sm">{player.handle}</p>
                    <p className="text-xs text-ink-secondary">{player.email}</p>
                    <p className="text-xs text-ink-tertiary">Balance: {player.currentBalance.toLocaleString()}</p>
                  </button>
                ))}
              </div>
            ) : (
              <div className="p-3 text-sm text-ink-secondary">No players found</div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
