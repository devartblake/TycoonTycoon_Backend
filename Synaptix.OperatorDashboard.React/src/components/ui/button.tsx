/**
 * Button component (shadcn/ui)
 * This is a minimal stub. Run `npx shadcn-ui@latest add button` to scaffold the full component.
 */

import React from 'react'
import { cn } from '@/lib/utils'

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link'
  size?: 'default' | 'sm' | 'lg' | 'icon'
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'default', size = 'default', ...props }, ref) => {
    const baseStyles = 'inline-flex items-center justify-center whitespace-nowrap rounded-md font-medium ring-offset-2 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50'

    const variantStyles = {
      default: 'bg-accent text-white hover:bg-accent-dark',
      destructive: 'bg-status-offline text-white hover:bg-red-700',
      outline: 'border border-panel-border bg-transparent hover:bg-bg-secondary',
      secondary: 'bg-bg-secondary text-ink-primary hover:bg-bg-tertiary',
      ghost: 'hover:bg-bg-secondary',
      link: 'text-accent underline-offset-4 hover:underline',
    }

    const sizeStyles = {
      default: 'h-10 px-4 py-2 text-sm',
      sm: 'h-9 rounded-md px-3 text-xs',
      lg: 'h-11 rounded-md px-8 text-base',
      icon: 'h-10 w-10',
    }

    return (
      <button
        className={cn(baseStyles, variantStyles[variant], sizeStyles[size], className)}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = 'Button'

export { Button }
