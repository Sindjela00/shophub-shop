import { type HTMLAttributes } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const badgeVariants = cva(
  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
  {
    variants: {
      variant: {
        default: 'bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300',
        success: 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
        warning: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
        destructive: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
        outline: 'border border-neutral-300 text-neutral-600 dark:border-neutral-700 dark:text-neutral-300',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
)

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement>, VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />
}
