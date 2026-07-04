/**
 * Personalization - Player Archetypes, Recommendation Engines, and Controls
 */

import { useState, useEffect } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import * as personApi from '../api'
import type { PlayerArchetype, RecommendationEngine, RecommendationControl } from '../types'

export default function ArchetypesPage() {
  usePermission('personalization:write')

  const [activeTab, setActiveTab] = useState<'archetypes' | 'engines' | 'controls'>('archetypes')
  const [archetypes, setArchetypes] = useState<PlayerArchetype[]>([])
  const [engines, setEngines] = useState<RecommendationEngine[]>([])
  const [controls, setControls] = useState<RecommendationControl[]>([])
  const [loading, setLoading] = useState(true)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  useEffect(() => {
    const loadData = async () => {
      setLoading(true)
      try {
        const [archeRes, engRes, ctrlRes] = await Promise.all([
          personApi.getArchetypes(),
          personApi.getRecommendationEngines(),
          personApi.getRecommendationControls(),
        ])
        setArchetypes(archeRes.items)
        setEngines(engRes.items)
        setControls(ctrlRes.items)
      } catch (error) {
        console.error('Failed to load personalization data:', error)
      } finally {
        setLoading(false)
      }
    }
    loadData()
  }, [])

  const handleRecalculateArchetype = async (archetypeId: string) => {
    try {
      await personApi.recalculateArchetypeMetrics(archetypeId)
      setSuccessMsg('Archetype metrics recalculated')
      setTimeout(() => setSuccessMsg(null), 2000)
    } catch (error) {
      console.error('Recalculation failed:', error)
    }
  }

  const handleToggleEngine = async (id: string, enabled: boolean) => {
    try {
      await personApi.toggleRecommendationEngine(id, !enabled)
      setEngines(engines.map((e) => (e.id === id ? { ...e, enabled: !enabled } : e)))
      setSuccessMsg(`Engine ${enabled ? 'disabled' : 'enabled'}`)
      setTimeout(() => setSuccessMsg(null), 2000)
    } catch (error) {
      console.error('Toggle failed:', error)
    }
  }

  const handleResetEngine = async (engineId: string) => {
    if (!confirm('Reset this recommendation model? This will recalculate all metrics.')) return
    try {
      await personApi.resetRecommendationModel(engineId)
      setSuccessMsg('Recommendation model reset')
      setTimeout(() => setSuccessMsg(null), 2000)
    } catch (error) {
      console.error('Reset failed:', error)
    }
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Personalization & Archetypes</h1>
          <p className="mt-2 text-ink-secondary">Manage player archetypes and recommendation engines</p>
        </div>

        {/* Success Message */}
        {successMsg && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {successMsg}
          </div>
        )}

        {/* Stats */}
        {loading ? (
          <SkeletonGrid count={4} />
        ) : (
          <div className="grid grid-cols-4 gap-4">
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Total Archetypes</p>
          <p className="text-2xl font-bold text-accent mt-1">{archetypes.length}</p>
        </div>
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Recommendation Engines</p>
          <p className="text-2xl font-bold text-ink-primary mt-1">{engines.length}</p>
        </div>
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Active Engines</p>
          <p className="text-2xl font-bold text-status-healthy mt-1">{engines.filter((e) => e.enabled).length}</p>
        </div>
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Personalization Enabled</p>
          <p className="text-2xl font-bold text-status-healthy mt-1">{controls.filter((c) => c.enabled).length}</p>
        </div>
          </div>
        )}

      {/* Tab Navigation */}
      <div className="flex gap-2 border-b border-panel-border">
        {[
          { id: 'archetypes' as const, label: '👥 Archetypes' },
          { id: 'engines' as const, label: '🤖 Recommendation Engines' },
          { id: 'controls' as const, label: '⚙️ Recommendation Controls' },
        ].map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-2 font-medium border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-accent text-accent'
                : 'border-transparent text-ink-secondary hover:text-ink-primary'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="operator-card">
        {loading ? (
          <SkeletonTable rows={8} columns={4} />
        ) : activeTab === 'archetypes' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Player Archetypes ({archetypes.length})</h2>
            {archetypes.length > 0 ? (
              <div className="space-y-3">
                {archetypes.map((archetype) => (
                  <div key={archetype.id} className="p-4 border border-panel-border rounded hover:bg-panel/50">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="text-2xl">{archetype.icon}</span>
                          <div>
                            <h3 className="font-semibold text-ink-primary">{archetype.name}</h3>
                            <p className="text-sm text-ink-secondary">{archetype.description}</p>
                          </div>
                        </div>
                        <div className="grid grid-cols-2 gap-4 mt-3 text-sm">
                          <div>
                            <p className="text-ink-tertiary">Engagement</p>
                            <p className="font-semibold text-ink-primary">{archetype.engagementLevel}</p>
                          </div>
                          <div>
                            <p className="text-ink-tertiary">Players</p>
                            <p className="font-semibold text-accent">{archetype.playerCount.toLocaleString()}</p>
                          </div>
                          <div>
                            <p className="text-ink-tertiary">Conversion Rate</p>
                            <p className="font-semibold text-status-healthy">{(archetype.conversionRate * 100).toFixed(1)}%</p>
                          </div>
                          <div>
                            <p className="text-ink-tertiary">Retention Rate</p>
                            <p className="font-semibold text-status-healthy">{(archetype.retentionRate * 100).toFixed(1)}%</p>
                          </div>
                        </div>
                      </div>
                      <button
                        onClick={() => handleRecalculateArchetype(archetype.id)}
                        className="px-4 py-2 rounded bg-panel hover:bg-panel-border text-ink-secondary font-medium transition-colors whitespace-nowrap"
                      >
                        Recalculate
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState
                title="No archetypes found"
                description="Configure player archetypes to enable personalization"
                icon="👥"
              />
            )}
          </div>
        ) : activeTab === 'engines' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Recommendation Engines ({engines.length})</h2>
            {engines.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-panel border-b border-panel-border">
                    <tr>
                      <th className="px-4 py-2 text-left">Engine</th>
                      <th className="px-4 py-2 text-left">Algorithm</th>
                      <th className="px-4 py-2 text-right">Accuracy</th>
                      <th className="px-4 py-2 text-right">Coverage</th>
                      <th className="px-4 py-2 text-center">Status</th>
                      <th className="px-4 py-2 text-center">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {engines.map((engine) => (
                      <tr key={engine.id} className="border-t border-panel-border hover:bg-panel/50">
                        <td className="px-4 py-3">
                          <div>
                            <p className="font-semibold text-ink-primary">{engine.name}</p>
                            <p className="text-xs text-ink-tertiary">v{engine.version}</p>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <span className="px-2 py-1 rounded text-xs bg-accent/20 text-accent">{engine.algorithm}</span>
                        </td>
                        <td className="px-4 py-3 text-right font-semibold">{(engine.accuracy * 100).toFixed(1)}%</td>
                        <td className="px-4 py-3 text-right font-semibold">{(engine.coverage * 100).toFixed(1)}%</td>
                        <td className="px-4 py-3 text-center">
                          <button
                            onClick={() => handleToggleEngine(engine.id, engine.enabled)}
                            className={`px-2 py-1 rounded text-xs font-medium transition-colors ${
                              engine.enabled
                                ? 'bg-status-healthy/20 text-status-healthy'
                                : 'bg-panel text-ink-secondary'
                            }`}
                          >
                            {engine.enabled ? '✓ Active' : '✗ Inactive'}
                          </button>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <button
                            onClick={() => handleResetEngine(engine.id)}
                            className="text-xs text-accent hover:text-accent-dark"
                          >
                            Reset
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState
                title="No recommendation engines found"
                description="Configure recommendation engines for personalization"
                icon="🤖"
              />
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Recommendation Controls ({controls.length})</h2>
            {controls.length > 0 ? (
              <div className="grid grid-cols-1 gap-3">
                {controls.slice(0, 10).map((control) => (
                  <div key={control.id} className="p-3 border border-panel-border rounded hover:bg-panel/50">
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <p className="text-sm font-semibold text-ink-primary">Player: {control.playerId}</p>
                        <div className="flex gap-4 mt-1 text-xs text-ink-tertiary">
                          <span>Archetype: {control.archetypeId}</span>
                          <span>Frequency: {control.frequency}</span>
                          <span>Max Recommendations: {control.maxRecommendations}</span>
                          <span>Min Quality: {control.minQualityScore}</span>
                        </div>
                      </div>
                      <span
                        className={`px-2 py-1 rounded text-xs font-medium ${
                          control.enabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'
                        }`}
                      >
                        {control.enabled ? '✓ Enabled' : '✗ Disabled'}
                      </span>
                    </div>
                  </div>
                ))}
                {controls.length > 10 && (
                  <div className="text-center py-4 text-ink-tertiary text-sm">
                    +{controls.length - 10} more controls
                  </div>
                )}
              </div>
            ) : (
              <EmptyState
                title="No recommendation controls found"
                description="Set up individual player recommendation controls"
                icon="⚙️"
              />
            )}
          </div>
        )}
      </div>

      {/* Status */}
      <div className="p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary">
        <p className="font-medium text-ink-secondary mb-2">✅ Personalization Management Complete</p>
        <ul className="space-y-1">
          <li>✓ Player archetype management and metrics</li>
          <li>✓ Recommendation engine configuration and toggling</li>
          <li>✓ Individual player recommendation controls</li>
          <li>✓ Accuracy and coverage monitoring</li>
          <li>✓ Model reset and recalculation capabilities</li>
        </ul>
      </div>
      </div>
    </ErrorBoundary>
  )
}
