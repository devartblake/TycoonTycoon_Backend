/**
 * Store API client
 */
import { apiGet, apiPost, apiPut, apiDelete } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
// Products
export async function getProducts(offset = 0, limit = 50) {
    if (getMockMode())
        return mockApi.mockGetProducts(offset, limit);
    return apiGet(`/admin/store/products?offset=${offset}&limit=${limit}`);
}
export async function createProduct(product) {
    if (getMockMode())
        return mockApi.mockCreateProduct(product);
    return apiPost('/admin/store/products', product);
}
export async function updateProduct(id, product) {
    if (getMockMode())
        return mockApi.mockUpdateProduct(id, product);
    return apiPut(`/admin/store/products/${id}`, product);
}
export async function deleteProduct(id) {
    if (getMockMode())
        return mockApi.mockDeleteProduct(id);
    return apiDelete(`/admin/store/products/${id}`);
}
// Flash Sales
export async function getFlashSales(offset = 0, limit = 50) {
    if (getMockMode())
        return mockApi.mockGetFlashSales(offset, limit);
    return apiGet(`/admin/store/flash-sales?offset=${offset}&limit=${limit}`);
}
export async function createFlashSale(sale) {
    if (getMockMode())
        return mockApi.mockCreateFlashSale(sale);
    return apiPost('/admin/store/flash-sales', sale);
}
export async function updateFlashSale(id, sale) {
    if (getMockMode())
        return mockApi.mockUpdateFlashSale(id, sale);
    return apiPut(`/admin/store/flash-sales/${id}`, sale);
}
export async function deleteFlashSale(id) {
    if (getMockMode())
        return mockApi.mockDeleteFlashSale(id);
    return apiDelete(`/admin/store/flash-sales/${id}`);
}
// Stock Policies
export async function getStockPolicies(offset = 0, limit = 50) {
    if (getMockMode())
        return mockApi.mockGetStockPolicies(offset, limit);
    return apiGet(`/admin/store/stock-policies?offset=${offset}&limit=${limit}`);
}
export async function createStockPolicy(policy) {
    if (getMockMode())
        return mockApi.mockCreateStockPolicy(policy);
    return apiPost('/admin/store/stock-policies', policy);
}
export async function updateStockPolicy(id, policy) {
    if (getMockMode())
        return mockApi.mockUpdateStockPolicy(id, policy);
    return apiPut(`/admin/store/stock-policies/${id}`, policy);
}
export async function deleteStockPolicy(id) {
    if (getMockMode())
        return mockApi.mockDeleteStockPolicy(id);
    return apiDelete(`/admin/store/stock-policies/${id}`);
}
// Reward Limits
export async function getRewardLimits(offset = 0, limit = 50) {
    if (getMockMode())
        return mockApi.mockGetRewardLimits(offset, limit);
    return apiGet(`/admin/store/reward-limits?offset=${offset}&limit=${limit}`);
}
export async function createRewardLimit(limit) {
    if (getMockMode())
        return mockApi.mockCreateRewardLimit(limit);
    return apiPost('/admin/store/reward-limits', limit);
}
export async function updateRewardLimit(id, limit) {
    if (getMockMode())
        return mockApi.mockUpdateRewardLimit(id, limit);
    return apiPut(`/admin/store/reward-limits/${id}`, limit);
}
export async function deleteRewardLimit(id) {
    if (getMockMode())
        return mockApi.mockDeleteRewardLimit(id);
    return apiDelete(`/admin/store/reward-limits/${id}`);
}
// Stats
export async function getStoreStats() {
    if (getMockMode())
        return mockApi.mockGetStoreStats();
    return apiGet('/admin/store/stats');
}
