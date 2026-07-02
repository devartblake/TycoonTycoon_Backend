import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Users table with TanStack Table
 */
import React, { useState } from 'react';
import { useReactTable, getCoreRowModel, flexRender } from '@tanstack/react-table';
import { formatDate } from '@/lib/utils';
const columns = [
    {
        id: 'select',
        header: ({ table }) => (_jsx("input", { type: "checkbox", checked: table.getIsAllRowsSelected(), ref: (el) => {
                if (el)
                    el.indeterminate = table.getIsSomeRowsSelected();
            }, onChange: table.getToggleAllRowsSelectedHandler(), className: "cursor-pointer" })),
        cell: ({ row }) => (_jsx("input", { type: "checkbox", checked: row.getIsSelected(), onChange: row.getToggleSelectedHandler(), className: "cursor-pointer" })),
        size: 50,
    },
    {
        accessorKey: 'email',
        header: 'Email',
        cell: (info) => _jsx("span", { className: "text-accent hover:underline cursor-pointer", children: info.getValue() }),
    },
    {
        accessorKey: 'status',
        header: 'Status',
        cell: (info) => {
            const status = info.getValue();
            const statusColor = {
                active: 'bg-status-healthy/10 text-status-healthy',
                suspended: 'bg-status-degraded/10 text-status-degraded',
                banned: 'bg-status-offline/10 text-status-offline',
                inactive: 'bg-status-unknown/10 text-status-unknown',
            };
            return (_jsx("span", { className: `inline-block px-2 py-1 rounded text-xs font-medium ${statusColor[status]}`, children: status }));
        },
    },
    {
        accessorKey: 'createdAt',
        header: 'Created',
        cell: (info) => formatDate(info.getValue()),
    },
    {
        accessorKey: 'lastActiveAt',
        header: 'Last Active',
        cell: (info) => {
            const date = info.getValue();
            return date ? formatDate(date) : '—';
        },
    },
    {
        accessorKey: 'flaggedCount',
        header: 'Flags',
        cell: (info) => {
            const count = info.getValue();
            return count > 0 ? (_jsx("span", { className: "inline-block px-2 py-1 rounded bg-status-offline/10 text-status-offline text-xs font-medium", children: count })) : ('—');
        },
        size: 60,
    },
];
export function UsersTable({ users, isLoading, onSelectionChange }) {
    const [rowSelection, setRowSelection] = useState({});
    const table = useReactTable({
        data: users,
        columns,
        state: { rowSelection },
        onRowSelectionChange: setRowSelection,
        getCoreRowModel: getCoreRowModel(),
        manualPagination: true,
    });
    // Notify parent of selection changes
    React.useEffect(() => {
        const selectedIds = table.getSelectedRowModel().rows.map((row) => row.original.id);
        onSelectionChange(selectedIds);
    }, [rowSelection, table]);
    if (isLoading) {
        return (_jsx("div", { className: "space-y-2", children: [...Array(5)].map((_, i) => (_jsx("div", { className: "h-12 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (users.length === 0) {
        return (_jsxs("div", { className: "text-center py-12 text-ink-secondary", children: [_jsx("p", { className: "text-lg", children: "No users found" }), _jsx("p", { className: "text-sm", children: "Try adjusting your filters" })] }));
    }
    return (_jsx("div", { className: "overflow-x-auto border border-panel-border rounded", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-bg-secondary border-b border-panel-border", children: table.getHeaderGroups().map((headerGroup) => (_jsx("tr", { children: headerGroup.headers.map((header) => (_jsx("th", { className: "px-4 py-3 text-left font-medium text-ink-primary", style: { width: header.getSize() }, children: header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext()) }, header.id))) }, headerGroup.id))) }), _jsx("tbody", { children: table.getRowModel().rows.map((row) => (_jsx("tr", { className: "border-b border-panel-border hover:bg-bg-secondary transition-colors", children: row.getVisibleCells().map((cell) => (_jsx("td", { className: "px-4 py-3", style: { width: cell.column.getSize() }, children: flexRender(cell.column.columnDef.cell, cell.getContext()) }, cell.id))) }, row.id))) })] }) }));
}
