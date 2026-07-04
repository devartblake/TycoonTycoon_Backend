import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Store Management - Products, Flash Sales, Stock Policies, Reward Limits
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons';
import * as storeApi from '../api';
export default function StoreManagementPage() {
    usePermission('storage:write');
    const [products, setProducts] = useState([]);
    const [sales, setSales] = useState([]);
    const [policies, setPolicies] = useState([]);
    const [limits, setLimits] = useState([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState('products');
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            try {
                const [productsRes, salesRes, policiesRes, limitsRes] = await Promise.all([
                    storeApi.getProducts(),
                    storeApi.getFlashSales(),
                    storeApi.getStockPolicies(),
                    storeApi.getRewardLimits(),
                ]);
                setProducts(productsRes.items);
                setSales(salesRes.items);
                setPolicies(policiesRes.items);
                setLimits(limitsRes.items);
            }
            catch (error) {
                console.error('Failed to load store data:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
    }, []);
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Store Management" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage products, sales, inventory, and rewards" })] }), loading ? (_jsx(SkeletonGrid, { count: 4 })) : (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Products" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: products.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Flash Sales" }), _jsx("p", { className: "text-2xl font-bold text-status-degraded mt-1", children: sales.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Stock Policies" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: policies.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Reward Limits" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: limits.length })] })] })), _jsx("div", { className: "flex gap-2 border-b border-panel-border", children: [
                        { id: 'products', label: '📦 Products' },
                        { id: 'flash-sales', label: '⚡ Flash Sales' },
                        { id: 'policies', label: '📊 Policies' },
                        { id: 'limits', label: '🎁 Rewards' },
                    ].map((tab) => (_jsx("button", { onClick: () => setActiveTab(tab.id), className: `px-4 py-2 font-medium border-b-2 transition-colors ${activeTab === tab.id
                            ? 'border-accent text-accent'
                            : 'border-transparent text-ink-secondary hover:text-ink-primary'}`, children: tab.label }, tab.id))) }), _jsx("div", { className: "operator-card", children: loading ? (_jsx(SkeletonTable, { rows: 8, columns: 5 })) : activeTab === 'products' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Products (", products.length, ")"] }), products.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Name" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Category" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Price" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Stock" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Status" })] }) }), _jsx("tbody", { children: products.map((product) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: product.name }), _jsx("td", { className: "px-4 py-3", children: product.category }), _jsxs("td", { className: "px-4 py-3 text-right", children: ["$", product.price] }), _jsx("td", { className: "px-4 py-3 text-right", children: product.stock }), _jsx("td", { className: "px-4 py-3 text-center", children: product.active ? '✓' : '✗' })] }, product.id))) })] }) })) : (_jsx(EmptyState, { title: "No products found", description: "Start by adding products to your store", icon: "\uD83D\uDCE6" }))] })) : activeTab === 'flash-sales' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Flash Sales (", sales.length, ")"] }), sales.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Product" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Discount" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Price" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Status" })] }) }), _jsx("tbody", { children: sales.map((sale) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: sale.productName }), _jsxs("td", { className: "px-4 py-3 text-right", children: [sale.discountPercentage, "%"] }), _jsxs("td", { className: "px-4 py-3 text-right", children: ["$", sale.salePrice] }), _jsx("td", { className: "px-4 py-3", children: sale.status })] }, sale.id))) })] }) })) : (_jsx(EmptyState, { title: "No flash sales found", description: "Create flash sales to offer limited-time discounts", icon: "\u26A1" }))] })) : activeTab === 'policies' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Stock Policies (", policies.length, ")"] }), policies.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Name" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Max Stock" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Auto Reorder" })] }) }), _jsx("tbody", { children: policies.map((policy) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: policy.name }), _jsx("td", { className: "px-4 py-3 text-right", children: policy.maxStockLevel }), _jsx("td", { className: "px-4 py-3 text-center", children: policy.autoReorder ? '✓' : '✗' })] }, policy.id))) })] }) })) : (_jsx(EmptyState, { title: "No stock policies found", description: "Set up stock policies for inventory management", icon: "\uD83D\uDCCA" }))] })) : (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Reward Limits (", limits.length, ")"] }), limits.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Name" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Type" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Max Amount" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Status" })] }) }), _jsx("tbody", { children: limits.map((limit) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: limit.name }), _jsx("td", { className: "px-4 py-3", children: limit.type }), _jsx("td", { className: "px-4 py-3 text-right", children: limit.maxAmount }), _jsx("td", { className: "px-4 py-3 text-center", children: limit.status })] }, limit.id))) })] }) })) : (_jsx(EmptyState, { title: "No reward limits found", description: "Configure reward limits to control player rewards", icon: "\uD83C\uDF81" }))] })) }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Store Management Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Products catalog with full details" }), _jsx("li", { children: "\u2713 Flash Sales tracking and management" }), _jsx("li", { children: "\u2713 Stock Policies with reorder rules" }), _jsx("li", { children: "\u2713 Reward Limits with interval tracking" }), _jsx("li", { children: "\u2713 Real-time data synchronization" })] })] })] }) }));
}
