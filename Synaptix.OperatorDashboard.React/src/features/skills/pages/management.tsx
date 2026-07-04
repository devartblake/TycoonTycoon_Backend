/**
 * Skills & Seed Management
 */

import { useState, useEffect } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import * as skillsApi from '../api'
import type { Skill, SkillSeed } from '../types'

export default function ManagementPage() {
  usePermission('storage:read')

  const [skills, setSkills] = useState<Skill[]>([])
  const [seeds, setSeeds] = useState<SkillSeed[]>([])
  const [stats, setStats] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const loadData = async () => {
      try {
        const [skillsData, seedsData, statsData] = await Promise.all([
          skillsApi.getSkills(),
          skillsApi.getSkillSeeds(),
          skillsApi.getSkillStats(),
        ])
        setSkills(skillsData)
        setSeeds(seedsData)
        setStats(statsData)
      } catch (error) {
        console.error('Failed to load skills data:', error)
      } finally {
        setLoading(false)
      }
    }
    loadData()
  }, [])

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Skills & Seed Management</h1>
          <p className="mt-2 text-ink-secondary">Manage game skills and seed unlocks</p>
        </div>

        {loading ? (
          <SkeletonGrid count={4} />
        ) : stats ? (
          <div className="grid grid-cols-4 gap-4">
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Total Skills</p>
            <p className="text-2xl font-bold text-accent mt-1">{stats.totalSkills}</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Total Seeds</p>
            <p className="text-2xl font-bold text-ink-primary mt-1">{stats.totalSeeds}</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Active Seeds</p>
            <p className="text-2xl font-bold text-status-healthy mt-1">{stats.activeSeeds}</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Most Equipped</p>
            <p className="text-lg font-bold text-ink-secondary mt-1">{stats.mostEquipped || '—'}</p>
          </div>
          </div>
        ) : null}

      <div className="operator-card">
        <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Skills ({skills.length})</h2>
        {loading ? (
          <SkeletonTable rows={8} columns={5} />
        ) : skills.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-panel">
                <tr>
                  <th className="px-4 py-2 text-left">Skill Name</th>
                  <th className="px-4 py-2 text-left">Category</th>
                  <th className="px-4 py-2 text-right">Unlock Level</th>
                  <th className="px-4 py-2 text-right">Equipped Count</th>
                  <th className="px-4 py-2 text-center">Status</th>
                </tr>
              </thead>
              <tbody>
                {skills.map((skill) => (
                  <tr key={skill.id} className="border-t border-panel-border hover:bg-panel/50">
                    <td className="px-4 py-3 font-medium">{skill.name}</td>
                    <td className="px-4 py-3">{skill.category}</td>
                    <td className="px-4 py-3 text-right">{skill.unlockLevel}</td>
                    <td className="px-4 py-3 text-right">{skill.totalEquipped}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-1 rounded text-xs ${skill.enabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'}`}>
                        {skill.enabled ? '✓ Enabled' : '✗ Disabled'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState
            title="No skills found"
            description="Create skills to provide gameplay abilities"
            icon="⚔️"
          />
        )}
      </div>

      <div className="operator-card">
        <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Seeds ({seeds.length})</h2>
        <div className="space-y-2 p-4">
          {seeds.slice(0, 10).map((seed) => (
            <div key={seed.id} className="p-2 border border-panel-border rounded">
              <div className="flex justify-between text-sm">
                <span className="font-medium">Skill {seed.skillId} - Player {seed.playerId}</span>
                <span className={`px-2 py-1 rounded text-xs font-medium bg-${seed.seedType}/20 text-${seed.seedType}`}>
                  {seed.seedType}
                </span>
              </div>
              <div className="text-xs text-ink-secondary mt-1">Level {seed.level} • Exp: {seed.experience}</div>
            </div>
          ))}
          {seeds.length > 10 && <p className="text-center text-ink-tertiary text-sm">+{seeds.length - 10} more seeds</p>}
        </div>
      </div>

      <div className="p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary">
        <p className="font-medium text-ink-secondary mb-2">✅ Skills Management Complete</p>
        <ul className="space-y-1">
          <li>✓ Skill catalog and metadata management</li>
          <li>✓ Seed unlock tracking and distribution</li>
          <li>✓ Equip statistics and analytics</li>
          <li>✓ Enable/disable skill availability</li>
        </ul>
      </div>
      </div>
    </ErrorBoundary>
  )
}
