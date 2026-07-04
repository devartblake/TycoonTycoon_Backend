import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useProducts, useCreateProduct, useUpdateProduct, useDeleteProduct } from '../hooks/useStore';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
export function ProductsTab() {
    const { data: products, isLoading, refetch } = useProducts();
    const createMutation = useCreateProduct();
    const updateMutation = useUpdateProduct();
    const deleteMutation = useDeleteProduct();
    const [selectedProduct, setSelectedProduct] = useState(null);
    const [formOpen, setFormOpen] = useState(false);
    const [formData, setFormData] = useState({
        name: '',
        price: 0,
        category: '',
        stock: null,
    });
    const handleSave = async () => {
        if (!formData.name || formData.price === undefined)
            return;
        try {
            if (selectedProduct) {
                await updateMutation.mutateAsync({ id: selectedProduct.id, ...formData });
            }
            else {
                await createMutation.mutateAsync(formData);
            }
            setFormOpen(false);
            setFormData({ name: '', price: 0, category: '', stock: null });
            setSelectedProduct(null);
            refetch();
        }
        catch (error) {
            console.error('Save failed:', error);
        }
    };
    const handleDelete = async (id) => {
        if (confirm('Delete this product?')) {
            try {
                await deleteMutation.mutateAsync(id);
                refetch();
            }
            catch (error) {
                console.error('Delete failed:', error);
            }
        }
    };
    const handleEdit = (product) => {
        setSelectedProduct(product);
        setFormData(product);
        setFormOpen(true);
    };
    const handleNew = () => {
        setSelectedProduct(null);
        setFormData({ name: '', price: 0, category: '', stock: null });
        setFormOpen(true);
    };
    if (isLoading) {
        return _jsx("div", { className: "text-center py-8 text-ink-secondary", children: "Loading products..." });
    }
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex justify-between items-center", children: [_jsxs("span", { className: "text-sm text-ink-secondary", children: [products?.length || 0, " products"] }), _jsxs(Dialog, { open: formOpen, onOpenChange: setFormOpen, children: [_jsx(DialogTrigger, { asChild: true, children: _jsx(Button, { onClick: handleNew, size: "sm", children: "+ Add Product" }) }), _jsxs(DialogContent, { className: "max-w-md", children: [_jsx(DialogHeader, { children: _jsxs(DialogTitle, { children: [selectedProduct ? 'Edit' : 'Add', " Product"] }) }), _jsxs("div", { className: "space-y-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Product Name *" }), _jsx("input", { type: "text", placeholder: "e.g., Premium Pass", value: formData.name || '', onChange: (e) => setFormData({ ...formData, name: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { className: "grid grid-cols-2 gap-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Price *" }), _jsx("input", { type: "number", placeholder: "9.99", value: formData.price || '', onChange: (e) => setFormData({ ...formData, price: Number(e.target.value) }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Category" }), _jsx("input", { type: "text", placeholder: "e.g., pass", value: formData.category || '', onChange: (e) => setFormData({ ...formData, category: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Stock (optional)" }), _jsx("input", { type: "number", placeholder: "Leave empty for unlimited", value: formData.stock || '', onChange: (e) => setFormData({ ...formData, stock: e.target.value ? Number(e.target.value) : null }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { className: "flex gap-2 justify-end pt-2", children: [_jsx(Button, { variant: "outline", onClick: () => setFormOpen(false), children: "Cancel" }), _jsx(Button, { onClick: handleSave, disabled: createMutation.isPending || updateMutation.isPending, children: selectedProduct ? 'Update' : 'Create' })] })] })] })] })] }), products && products.length > 0 ? (_jsx("div", { className: "overflow-x-auto border border-panel-border rounded", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Product" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Category" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Price" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Stock" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsx("tbody", { children: products.map((product) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: product.name }), _jsx("td", { className: "px-4 py-3", children: product.category || '-' }), _jsxs("td", { className: "px-4 py-3 text-right", children: ["$", product.price] }), _jsx("td", { className: "px-4 py-3 text-right", children: product.stock ?? '∞' }), _jsxs("td", { className: "px-4 py-3 text-center space-x-2", children: [_jsx(Button, { size: "sm", variant: "outline", onClick: () => handleEdit(product), children: "Edit" }), _jsx(Button, { size: "sm", variant: "destructive", onClick: () => handleDelete(product.id), children: "Delete" })] })] }, product.id))) })] }) })) : (_jsxs("div", { className: "text-center py-12 text-ink-secondary border border-panel-border rounded", children: [_jsx("p", { children: "No products yet" }), _jsx("p", { className: "text-xs mt-1", children: "Create your first product to get started" })] }))] }));
}
export default ProductsTab;
