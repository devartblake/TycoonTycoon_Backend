import { jsx as _jsx } from "react/jsx-runtime";
/**
 * Toaster component (shadcn/ui - Sonner)
 * This is a minimal stub. Run `npx shadcn-ui@latest add sonner` to scaffold the full component.
 */
import { Toaster as Sonner } from 'sonner';
export function Toaster() {
    return (_jsx(Sonner, { theme: "light", className: "toaster group", position: "bottom-right" }));
}
