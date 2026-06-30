/**
 * Notification templates tab
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { NotificationFormModal } from './notification-form-modal'
import { useNotificationTemplates, useCreateTemplate, useUpdateTemplate, useDeleteTemplate } from '../hooks/useNotifications'
import { formatDate } from '@/lib/utils'
import type { NotificationTemplate, CreateTemplatePayload } from '../types'

export function TemplatesTab() {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingTemplate, setEditingTemplate] = useState<NotificationTemplate | null>(null)

  const { data: templates = [], isLoading } = useNotificationTemplates()
  const createTemplate = useCreateTemplate()
  const updateTemplate = useUpdateTemplate()
  const deleteTemplate = useDeleteTemplate()

  const handleSubmit = async (payload: CreateTemplatePayload) => {
    if (editingTemplate) {
      await updateTemplate.mutateAsync({ templateId: editingTemplate.id, payload })
      setEditingTemplate(null)
    } else {
      await createTemplate.mutateAsync(payload)
    }
    setIsModalOpen(false)
  }

  const handleDelete = (templateId: string) => {
    if (confirm('Delete this template?')) {
      deleteTemplate.mutate(templateId)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-ink-primary">Templates</h3>
        <Button
          variant="default"
          size="sm"
          onClick={() => {
            setEditingTemplate(null)
            setIsModalOpen(true)
          }}
        >
          + New Template
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-16 bg-bg-secondary rounded animate-pulse" />
          ))}
        </div>
      ) : templates.length === 0 ? (
        <div className="text-center py-12 text-ink-secondary">
          <p>No templates yet</p>
        </div>
      ) : (
        <div className="space-y-2">
          {templates.map((template) => (
            <div
              key={template.id}
              className="p-4 bg-bg-secondary border border-panel-border rounded hover:bg-bg-tertiary transition-colors"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <h4 className="font-medium text-ink-primary">{template.name}</h4>
                  <p className="text-sm text-ink-secondary mt-1 line-clamp-2">{template.body}</p>
                  <div className="flex gap-2 mt-2">
                    {template.channels.map((channel) => (
                      <span
                        key={channel}
                        className="inline-block px-2 py-1 rounded text-xs bg-accent/10 text-accent"
                      >
                        {channel}
                      </span>
                    ))}
                  </div>
                  <p className="text-xs text-ink-tertiary mt-2">
                    Updated {formatDate(template.updatedAt)}
                  </p>
                </div>

                <div className="flex gap-2 ml-4">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                      setEditingTemplate(template)
                      setIsModalOpen(true)
                    }}
                  >
                    Edit
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleDelete(template.id)}
                    disabled={deleteTemplate.isPending}
                  >
                    Delete
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <NotificationFormModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false)
          setEditingTemplate(null)
        }}
        onSubmit={handleSubmit}
        initialData={editingTemplate || undefined}
        isLoading={createTemplate.isPending || updateTemplate.isPending}
      />
    </div>
  )
}
