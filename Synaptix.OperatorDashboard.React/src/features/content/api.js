/**
 * Content API client
 *
 * Reconciled to the real backend route surface under /admin/questions
 * (Synaptix.Backend.Api/Features/AdminQuestions). The backend has no /content
 * group and no bulk-review/stats/categories routes, so those are derived from
 * the list + per-item approve/reject. Functions keep their existing return types.
 *
 * Known fidelity gaps (backend does not expose these; see #420):
 *   - The list DTO omits status/options/explanation, so list items carry
 *     best-effort values (status inferred from the requested filter). Detail
 *     (GET /{id}) is fully populated.
 *   - Stats are derived from three filtered counts; avgReviewTime is not exposed
 *     and defaults to 0. Categories are derived from the first page of results.
 */
import { apiGet, apiPost } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
function offsetToPage(offset, limit) {
    return Math.floor(offset / Math.max(1, limit)) + 1;
}
function mapDifficulty(difficulty) {
    const d = typeof difficulty === 'string' ? difficulty.toLowerCase() : difficulty;
    if (d === 1 || d === 'easy')
        return 'easy';
    if (d === 3 || d === 4 || d === 'hard' || d === 'expert')
        return 'hard';
    return 'medium';
}
function mapStatus(status) {
    switch ((status ?? '').toLowerCase()) {
        case 'approved':
            return 'approved';
        case 'rejected':
            return 'rejected';
        default:
            return 'pending';
    }
}
function detailToQuestion(dto) {
    const answers = (dto.options ?? []).map((o) => ({
        id: o.id,
        text: o.text,
        isCorrect: o.id === dto.correctOptionId,
    }));
    return {
        id: dto.id,
        text: dto.text,
        category: dto.category,
        difficulty: mapDifficulty(dto.difficulty),
        answers,
        correctAnswerId: dto.correctOptionId,
        source: '',
        status: mapStatus(dto.status),
        submittedBy: '',
        submittedAt: dto.createdAtUtc,
        tags: dto.tags,
    };
}
function listItemToQuestion(item, status) {
    return {
        id: item.id,
        text: item.textPreview,
        category: item.category,
        difficulty: mapDifficulty(item.difficulty),
        answers: [],
        correctAnswerId: '',
        source: '',
        status,
        submittedBy: '',
        submittedAt: item.updatedAtUtc,
        tags: item.tags,
    };
}
export async function getQuestions(filters, offset = 0, limit = 50) {
    if (getMockMode())
        return mockApi.mockGetQuestions(filters, offset, limit);
    const params = new URLSearchParams({
        page: offsetToPage(offset, limit).toString(),
        pageSize: limit.toString(),
    });
    if (filters?.status)
        params.set('status', filters.status);
    if (filters?.category)
        params.set('category', filters.category);
    if (filters?.searchText)
        params.set('q', filters.searchText);
    const res = await apiGet(`/admin/questions?${params}`);
    // The list DTO omits status; if a status filter was applied, every item matches it.
    const inferredStatus = filters?.status ?? 'pending';
    return {
        items: res.items.map((i) => listItemToQuestion(i, inferredStatus)),
        total: res.total,
        offset,
        limit,
    };
}
export async function getQuestionDetail(questionId) {
    if (getMockMode())
        return mockApi.mockGetQuestionDetail(questionId);
    const dto = await apiGet(`/admin/questions/${questionId}`);
    return detailToQuestion(dto);
}
export async function reviewQuestion(review) {
    if (getMockMode())
        return mockApi.mockReviewQuestion(review);
    const verb = review.verdict === 'approve' ? 'approve' : 'reject';
    await apiPost(`/admin/questions/${review.questionId}/${verb}`, {});
    return { success: true };
}
export async function bulkReviewQuestions(questionIds, verdict, _reason) {
    if (getMockMode())
        return mockApi.mockBulkReviewQuestions(questionIds, verdict, _reason);
    // Backend has no bulk approve/reject; apply per question.
    void _reason;
    const results = await Promise.allSettled(questionIds.map((id) => apiPost(`/admin/questions/${id}/${verdict}`, {})));
    return { success: true, reviewed: results.filter((r) => r.status === 'fulfilled').length };
}
export async function getQuestionsStats() {
    if (getMockMode())
        return mockApi.mockGetQuestionsStats();
    // Backend has no /stats; derive counts from filtered totals.
    const countFor = async (status) => {
        const res = await apiGet(`/admin/questions?status=${status}&page=1&pageSize=1`);
        return res.total;
    };
    const [totalPending, totalApproved, totalRejected] = await Promise.all([
        countFor('pending'),
        countFor('approved'),
        countFor('rejected'),
    ]);
    const reviewed = totalApproved + totalRejected;
    return {
        totalPending,
        totalApproved,
        totalRejected,
        approvalRate: reviewed === 0 ? 0 : totalApproved / reviewed,
        avgReviewTime: 0,
    };
}
export async function getCategories() {
    if (getMockMode())
        return mockApi.mockGetCategories();
    // Backend has no categories endpoint; derive distinct categories from the list.
    const res = await apiGet('/admin/questions?page=1&pageSize=100');
    return Array.from(new Set(res.items.map((i) => i.category).filter(Boolean))).sort();
}
