/**
 * Notifications API client
 *
 * Reconciled to the real backend route surface under /admin/notifications
 * (see Synaptix.Backend.Api/Features/AdminNotifications). The backend and this
 * dashboard were designed against different data models, so the functions below
 * keep their existing return types (used by the components) and adapt the
 * backend shapes internally.
 *
 * Known fidelity gaps (backend does not expose these fields today; see the
 * notifications reconciliation sub-issue of #409):
 *   - ScheduledNotification.templateId / targetCount, DeadLetterMessage.recipient
 *     / error / attemptCount are best-effort placeholders.
 *   - A channel maps to a single backend channelKey; the email/push/sms "type"
 *     is inferred from the key.
 */
import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
// ── Mapping helpers ──────────────────────────────────────────────────────────
function inferChannelType(channelKey) {
    const key = channelKey.toLowerCase();
    if (key.includes('email') || key.includes('mail'))
        return 'email';
    if (key.includes('sms') || key.includes('text'))
        return 'sms';
    return 'push';
}
function toTemplate(dto) {
    return {
        id: dto.templateId,
        name: dto.name,
        subject: dto.title,
        body: dto.body,
        channels: [inferChannelType(dto.channelKey)],
        variables: dto.variables ?? [],
        createdAt: dto.updatedAt,
        updatedAt: dto.updatedAt,
    };
}
function toChannel(dto) {
    return {
        id: dto.key,
        type: inferChannelType(dto.key),
        name: dto.name,
        enabled: dto.enabled,
        config: { description: dto.description, importance: dto.importance },
        createdAt: '',
    };
}
function mapScheduleStatus(status) {
    switch (status) {
        case 'scheduled':
        case 'retry_pending':
            return 'pending';
        case 'processing':
            return 'in_progress';
        case 'sent':
        case 'completed':
            return 'completed';
        default:
            return 'failed';
    }
}
function toScheduled(dto) {
    return {
        id: dto.scheduleId,
        templateId: '',
        templateName: dto.title,
        scheduledFor: dto.scheduledAt,
        targetCount: 0,
        status: mapScheduleStatus(dto.status),
        createdAt: '',
    };
}
function toDeadLetter(dto) {
    return {
        id: dto.scheduleId,
        templateId: '',
        templateName: dto.title,
        channel: inferChannelType(dto.channelKey),
        recipient: '',
        error: '',
        attemptCount: 0,
        createdAt: dto.scheduledAt,
        lastAttemptAt: dto.scheduledAt,
    };
}
async function fetchBackendTemplate(templateId) {
    const templates = await apiGet('/admin/notifications/templates');
    return templates.find((t) => t.templateId === templateId);
}
// ── Templates ────────────────────────────────────────────────────────────────
export async function getTemplates() {
    if (getMockMode())
        return mockApi.mockGetTemplates();
    const dtos = await apiGet('/admin/notifications/templates');
    return dtos.map(toTemplate);
}
export async function getTemplate(templateId) {
    if (getMockMode())
        return mockApi.mockGetTemplates().then(t => t.find(x => x.id === templateId));
    // Backend has no single-template GET; resolve from the list.
    const dto = await fetchBackendTemplate(templateId);
    if (!dto)
        throw new Error(`Template ${templateId} not found`);
    return toTemplate(dto);
}
export async function createTemplate(payload) {
    if (getMockMode())
        return mockApi.mockCreateTemplate(payload);
    const dto = await apiPost('/admin/notifications/templates', {
        name: payload.name,
        title: payload.subject ?? payload.name,
        body: payload.body,
        channelKey: payload.channels[0] ?? 'push',
        variables: [],
    });
    return toTemplate(dto);
}
export async function updateTemplate(templateId, payload) {
    if (getMockMode())
        return mockApi.mockUpdateTemplate(templateId, payload);
    // Backend PATCH requires the full template; merge with current values.
    const current = await fetchBackendTemplate(templateId);
    if (!current)
        throw new Error(`Template ${templateId} not found`);
    const dto = await apiPatch(`/admin/notifications/templates/${templateId}`, {
        name: payload.name ?? current.name,
        title: payload.subject ?? current.title,
        body: payload.body ?? current.body,
        channelKey: payload.channels?.[0] ?? current.channelKey,
        variables: current.variables ?? [],
    });
    return toTemplate(dto);
}
export async function deleteTemplate(templateId) {
    if (getMockMode())
        return mockApi.mockDeleteTemplate(templateId);
    await apiDelete(`/admin/notifications/templates/${templateId}`);
    return { success: true };
}
// ── Channels ─────────────────────────────────────────────────────────────────
export async function getChannels() {
    if (getMockMode())
        return mockApi.mockGetChannels();
    const dtos = await apiGet('/admin/notifications/channels');
    return dtos.map(toChannel);
}
export async function updateChannel(channelId, enabled, config) {
    if (getMockMode())
        return mockApi.mockUpdateChannel(channelId, enabled, config);
    // Backend PUT upserts the full channel; preserve name/description/importance.
    const channels = await apiGet('/admin/notifications/channels');
    const current = channels.find((c) => c.key === channelId);
    const dto = await apiPut(`/admin/notifications/channels/${channelId}`, {
        name: current?.name ?? channelId,
        description: config?.description ?? current?.description ?? '',
        importance: config?.importance ?? current?.importance ?? 'normal',
        enabled,
    });
    return toChannel(dto);
}
// ── Schedules ──────────────────────────────────────────────────────────────────
export async function getSchedules() {
    if (getMockMode())
        return mockApi.mockGetSchedules();
    const res = await apiGet('/admin/notifications/scheduled?page=1&pageSize=200');
    return res.items.map(toScheduled);
}
export async function createSchedule(payload) {
    if (getMockMode())
        return mockApi.mockCreateSchedule(payload);
    // Backend schedules by explicit title/body/channel; resolve them from the template.
    const template = await fetchBackendTemplate(payload.templateId);
    if (!template)
        throw new Error(`Template ${payload.templateId} not found`);
    const res = await apiPost('/admin/notifications/schedule', {
        title: template.title,
        body: template.body,
        channelKey: template.channelKey,
        scheduledAt: payload.scheduledFor,
        audience: payload.targetFilter ?? {},
    });
    return {
        id: res.scheduleId,
        templateId: payload.templateId,
        templateName: template.name,
        scheduledFor: payload.scheduledFor,
        targetCount: 0,
        status: 'pending',
        createdAt: new Date().toISOString(),
    };
}
export async function cancelSchedule(scheduleId) {
    if (getMockMode())
        return mockApi.mockCancelSchedule(scheduleId);
    await apiDelete(`/admin/notifications/scheduled/${scheduleId}`);
    return { success: true };
}
// ── Dead-letter ──────────────────────────────────────────────────────────────────
export async function getDeadLetterMessages() {
    if (getMockMode())
        return mockApi.mockGetDeadLetterMessages();
    const res = await apiGet('/admin/notifications/dead-letter?page=1&pageSize=200');
    return res.items.map(toDeadLetter);
}
export async function retryDeadLetterMessage(messageId) {
    if (getMockMode())
        return mockApi.mockRetryDeadLetter(messageId);
    await apiPost(`/admin/notifications/dead-letter/${messageId}/replay`, {});
    return { success: true };
}
// ── Test send ──────────────────────────────────────────────────────────────────
export async function sendTestNotification(payload) {
    if (getMockMode())
        return { success: true, messageId: `msg_${Date.now()}` };
    // Backend has no dedicated test-send; resolve the template and use /send with a
    // single-recipient audience.
    const template = await fetchBackendTemplate(payload.templateId);
    if (!template)
        throw new Error(`Template ${payload.templateId} not found`);
    const res = await apiPost('/admin/notifications/send', {
        title: template.title,
        body: template.body,
        channelKey: template.channelKey,
        audience: { recipient: payload.recipient },
        payload: payload.variables ?? {},
    });
    return { success: true, messageId: res.jobId };
}
