/**
 * useContent hook - manage question moderation
 */
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as api from '../api';
export function useQuestions(filters, offset = 0, limit = 50) {
    return useQuery({
        queryKey: ['questions', filters, offset, limit],
        queryFn: () => api.getQuestions(filters, offset, limit),
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useQuestionDetail(questionId) {
    return useQuery({
        queryKey: ['questionDetail', questionId],
        queryFn: () => api.getQuestionDetail(questionId),
        enabled: !!questionId,
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useReviewQuestion() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (review) => api.reviewQuestion(review),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['questions'] });
            queryClient.invalidateQueries({ queryKey: ['questionsStats'] });
        },
    });
}
export function useBulkReviewQuestions() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ questionIds, verdict, reason }) => api.bulkReviewQuestions(questionIds, verdict, reason),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['questions'] });
            queryClient.invalidateQueries({ queryKey: ['questionsStats'] });
        },
    });
}
export function useQuestionsStats() {
    return useQuery({
        queryKey: ['questionsStats'],
        queryFn: () => api.getQuestionsStats(),
        staleTime: 1000 * 60 * 5, // 5 minutes
    });
}
export function useCategories() {
    return useQuery({
        queryKey: ['questionCategories'],
        queryFn: () => api.getCategories(),
        staleTime: 1000 * 60 * 60, // 1 hour
    });
}
