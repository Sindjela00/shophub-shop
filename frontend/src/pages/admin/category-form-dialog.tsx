import { useState, type FormEvent } from 'react'
import { AlertCircle } from 'lucide-react'
import { Dialog } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { Category } from '@/data/types'

interface CategoryFormDialogProps {
  open: boolean
  onClose: () => void
  onSave: (name: string) => void
  category?: Category
  saving?: boolean
}

export function CategoryFormDialog({ open, onClose, onSave, category, saving }: CategoryFormDialogProps) {
  const isEdit = !!category

  const [name, setName] = useState(category?.name ?? '')
  const [error, setError] = useState<string | null>(null)

  const resetAndClose = () => {
    setError(null)
    onClose()
  }

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    if (!name.trim()) return setError('Name is required.')

    onSave(name.trim())
  }

  return (
    <Dialog
      open={open}
      onClose={resetAndClose}
      title={isEdit ? 'Rename category' : 'Add category'}
      footer={
        <div className="flex justify-end gap-2 border-t border-neutral-200 p-5 dark:border-neutral-800">
          <Button type="button" variant="outline" onClick={resetAndClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" form="category-form" disabled={saving}>
            {saving ? 'Saving...' : isEdit ? 'Save changes' : 'Add category'}
          </Button>
        </div>
      }
    >
      <form id="category-form" onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && (
          <div className="flex items-start gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">
            <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="flex flex-col gap-1.5">
          <label htmlFor="category-name" className="text-sm font-medium">
            Name
          </label>
          <Input id="category-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Jackets" autoFocus />
        </div>
      </form>
    </Dialog>
  )
}
