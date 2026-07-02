/**
 * Dashboard API client
 */
import { apiGet } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
export async function getDashboardStats() {
    if (getMockMode())
        return mockApi.mockGetDashboardStats();
    return apiGet('/admin/dashboard/stats');
}
export async function getServiceHistory(serviceId, hours = 24) {
    if (getMockMode())
        return mockApi.mockGetServiceHistory(serviceId, hours);
    return apiGet(`/admin/dashboard/services/${serviceId}/history?hours=${hours}`);
}
export async function getAllServiceHistory(hours = 24) {
    if (getMockMode())
        return mockApi.mockGetAllServiceHistory(hours);
    return apiGet(`/admin/dashboard/services/history?hours=${hours}`);
}
