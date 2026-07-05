import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { Menu, X } from 'lucide-react';
export function MobileNav({ children }) {
    const [isOpen, setIsOpen] = useState(false);
    return (_jsxs(_Fragment, { children: [_jsx("button", { onClick: () => setIsOpen(!isOpen), className: "md:hidden fixed top-4 left-4 z-50 p-2 hover:bg-panel rounded-lg", "aria-label": "Toggle menu", children: isOpen ? _jsx(X, { size: 24 }) : _jsx(Menu, { size: 24 }) }), isOpen && (_jsx("div", { className: "md:hidden fixed inset-0 bg-black/50 z-40", onClick: () => setIsOpen(false) })), _jsx("div", { className: `md:hidden fixed left-0 top-0 h-full w-64 bg-panel-bg border-r border-panel-border z-40 transform transition-transform ${isOpen ? 'translate-x-0' : '-translate-x-full'}`, children: _jsx("div", { className: "pt-16 px-4", children: children }) })] }));
}
