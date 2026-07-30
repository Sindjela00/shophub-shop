import { createPortal } from 'react-dom'
import { CheckCircle2 } from 'lucide-react'
import type { ToastItem } from '@/context/toast-context'

interface ToastViewportProps {
  toasts: ToastItem[]
  onDismiss: (id: number) => void
}

export function ToastViewport({ toasts, onDismiss }: ToastViewportProps) {
  if (toasts.length === 0) return null

  return createPortal(
    <div className="fixed bottom-4 right-4 z-[60] flex flex-col gap-2">
      {toasts.map((t) => (
        <div
          key={t.id}
          className="animate-toast-in flex items-center gap-2 rounded-lg border border-neutral-200 bg-white px-4 py-3 text-sm shadow-lg dark:border-neutral-800 dark:bg-neutral-900"
        >
          <CheckCircle2 className="h-4 w-4 flex-shrink-0 text-green-600 dark:text-green-400" />
          <span>{t.message}</span>
          <button
            type="button"
            onClick={() => onDismiss(t.id)}
            className="ml-2 cursor-pointer text-neutral-400 hover:text-neutral-900 dark:hover:text-neutral-100"
            aria-label="Dismiss"
          >
            ×
          </button>
        </div>
      ))}
    </div>,
    document.body,
  )
}
