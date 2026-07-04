import { apiGet, apiPost } from '@/lib/api-client';
export async function getQueuedEvents(status) {
    const url = status ? '/admin/event-queue?status=' + status : '/admin/event-queue';
    return apiGet(url);
}
export async function getEventStats() {
    return apiGet('/admin/event-queue/stats');
}
export async function retryEvent(eventId) {
    return apiPost('/admin/event-queue/' + eventId + '/retry', {});
}
export async function clearFailedEvents() {
    return apiPost('/admin/event-queue/clear-failed', {});
}
