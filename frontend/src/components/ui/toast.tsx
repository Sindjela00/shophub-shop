import { createPortal } from 'react-dom'
import { AlertCircle, CheckCircle2 } from 'lucide-react'
import type { ToastItem } from '@/context/toast-context'
import { cn } from '@/lib/utils'

interface ToastViewportProps {
  toasts: ToastItem[]
  onDismiss: (id: number) => void
}

export function ToastViewport({ toasts, onDismiss }: ToastViewportProps) {
  if (toasts.length === 0) return null

  return createPortal(
    <div className="fixed bottom-4 right-4 z-[60] flex flex-col gap-2">
      {toasts.map((t) => {
        const isError = t.variant === 'error'
        return (
          <div
            key={t.id}
            className={cn(
              'animate-toast-in flex items-start gap-2 rounded-lg border px-4 py-3 text-sm shadow-lg',
              isError
                ? 'border-red-200 bg-red-50 text-red-800 dark:border-red-900/50 dark:bg-red-950/60 dark:text-red-200'
                : 'border-neutral-200 bg-white text-neutral-900 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-100',
            )}
          >
            {isError ? (
              <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0 text-red-600 dark:text-red-400" />
            ) : (
              <CheckCircle2 className="mt-0.5 h-4 w-4 flex-shrink-0 text-green-600 dark:text-green-400" />
            )}
            <span className="flex-1">{t.message}</span>
            <button
              type="button"
              onClick={() => onDismiss(t.id)}
              className={cn(
                'cursor-pointer',
                isError
                  ? 'text-red-400 hover:text-red-900 dark:hover:text-red-100'
                  : 'text-neutral-400 hover:text-neutral-900 dark:hover:text-neutral-100',
              )}
              aria-label="Dismiss"
            >
              ×
            </button>
          </div>
        )
      })}
    </div>,
    document.body,
  )
}
