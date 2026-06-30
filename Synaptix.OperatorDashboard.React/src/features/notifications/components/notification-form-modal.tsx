/**
 * Notification template form modal
 */

import React, { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { NotificationTemplate, CreateTemplatePayload } from '../types'

interface NotificationFormModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (payload: CreateTemplatePayload) => Promise<void>
  initialData?: NotificationTemplate
  isLoading?: boolean
}

const CHANNELS = ['email', 'push', 'sms'] as const

export function NotificationFormModal({
  isOpen,
  onClose,
  onSubmit,
  initialData,
  isLoading = false,
}: NotificationFormModalProps) {
  const [form, setForm] = useState<CreateTemplatePayload>(
    initialData
      ? {
          name: initialData.name,
          subject: initialData.subject,
          body: initialData.body,
          channels: initialData.channels,
        }
      : {
          name: '',
          subject: '',
          body: '',
          channels: [],
        }
  )

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    await onSubmit(form)
    setForm({ name: '', subject: '', body: '', channels: [] })
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
      <div className="bg-panel-bg border border-panel-border rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto">
        <div className="sticky top-0 bg-panel-bg border-b border-panel-border p-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-ink-primary">
            {initialData ? 'Edit Template' : 'Create Template'}
          </h2>
          <button
            onClick={onClose}
            className="text-ink-tertiary hover:text-ink-primary text-2xl leading-none"
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6 p-6">
          {/* Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-ink-primary mb-2">
              Template Name
            </label>
            <input
              id="name"
              type="text"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
              placeholder="e.g., Welcome Email"
              required
            />
          </div>

          {/* Subject (email only) */}
          <div>
            <label htmlFor="subject" className="block text-sm font-medium text-ink-primary mb-2">
              Subject (for email templates)
            </label>
            <input
              id="subject"
              type="text"
              value={form.subject || ''}
              onChange={(e) => setForm({ ...form, subject: e.target.value })}
              className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
              placeholder="e.g., Welcome to Synaptix!"
            />
          </div>

          {/* Body */}
          <div>
            <label htmlFor="body" className="block text-sm font-medium text-ink-primary mb-2">
              Message Body
            </label>
            <textarea
              id="body"
              value={form.body}
              onChange={(e) => setForm({ ...form, body: e.target.value })}
              className="w-full px-3 py-2 border border-panel-border rounded focus-ring h-32"
              placeholder="e.g., Hi {{playerName}}, welcome to Synaptix!"
              required
            />
            <p className="text-xs text-ink-tertiary mt-1">
              Use {'{{variable}}'} for dynamic content
            </p>
          </div>

          {/* Channels */}
          <div>
            <label className="block text-sm font-medium text-ink-primary mb-2">Channels</label>
            <div className="space-y-2">
              {CHANNELS.map((channel) => (
                <label key={channel} className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.channels.includes(channel)}
                    onChange={(e) => {
                      if (e.target.checked) {
                        setForm({ ...form, channels: [...form.channels, channel] })
                      } else {
                        setForm({
                          ...form,
                          channels: form.channels.filter((c) => c !== channel),
                        })
                      }
                    }}
                    className="cursor-pointer"
                  />
                  <span className="text-sm text-ink-primary capitalize">{channel}</span>
                </label>
              ))}
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-2 justify-end pt-6 border-t border-panel-border">
            <Button variant="ghost" onClick={onClose} disabled={isLoading}>
              Cancel
            </Button>
            <Button
              variant="default"
              type="submit"
              disabled={isLoading || !form.name || !form.body || form.channels.length === 0}
            >
              {isLoading ? 'Saving...' : 'Save Template'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
