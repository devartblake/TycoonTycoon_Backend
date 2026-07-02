import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Questions filter bar
 */
import { useCategories } from '../hooks/useContent';
import { Button } from '@/components/ui/button';
const STATUS_OPTIONS = ['pending', 'approved', 'rejected'];
const DIFFICULTY_OPTIONS = ['easy', 'medium', 'hard'];
export function FilterBar({ filters, onFiltersChange }) {
    const categoriesQuery = useCategories();
    const categories = categoriesQuery.data || [];
    const handleStatusChange = (status) => {
        onFiltersChange({
            ...filters,
            status: filters.status === status ? undefined : status,
        });
    };
    const handleDifficultyChange = (difficulty) => {
        onFiltersChange({
            ...filters,
            difficulty: filters.difficulty === difficulty ? undefined : difficulty,
        });
    };
    const handleCategoryChange = (category) => {
        onFiltersChange({
            ...filters,
            category: filters.category === category ? undefined : category,
        });
    };
    const handleClearFilters = () => {
        onFiltersChange({});
    };
    const hasActiveFilters = Object.values(filters).some((v) => v != null);
    return (_jsxs("div", { className: "operator-card space-y-4", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Filters" }), hasActiveFilters && (_jsx(Button, { variant: "ghost", size: "sm", onClick: handleClearFilters, className: "text-xs", children: "Clear Filters" }))] }), _jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Status" }), _jsx("div", { className: "space-y-1", children: STATUS_OPTIONS.map((status) => (_jsxs("label", { className: "flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer", children: [_jsx("input", { type: "checkbox", checked: filters.status === status, onChange: () => handleStatusChange(status), className: "cursor-pointer" }), _jsx("span", { className: "text-xs text-ink-secondary capitalize", children: status })] }, status))) })] }), _jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Difficulty" }), _jsx("div", { className: "space-y-1", children: DIFFICULTY_OPTIONS.map((difficulty) => (_jsxs("label", { className: "flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer", children: [_jsx("input", { type: "checkbox", checked: filters.difficulty === difficulty, onChange: () => handleDifficultyChange(difficulty), className: "cursor-pointer" }), _jsx("span", { className: "text-xs text-ink-secondary capitalize", children: difficulty })] }, difficulty))) })] }), categories.length > 0 && (_jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Category" }), _jsx("div", { className: "space-y-1 max-h-40 overflow-y-auto", children: categories.map((category) => (_jsxs("label", { className: "flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer", children: [_jsx("input", { type: "checkbox", checked: filters.category === category, onChange: () => handleCategoryChange(category), className: "cursor-pointer" }), _jsx("span", { className: "text-xs text-ink-secondary", children: category })] }, category))) })] }))] }));
}
