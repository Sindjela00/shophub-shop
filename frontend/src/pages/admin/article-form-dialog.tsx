import { useState, type FormEvent } from 'react'
import { AlertCircle } from 'lucide-react'
import { Dialog } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import type { Article } from '@/data/types'
import { CATEGORIES } from '@/lib/category-style'

export interface ArticleFormValues {
  name: string
  description: string
  price: number
  category: string
  stock: number
}

interface ArticleFormDialogProps {
  open: boolean
  onClose: () => void
  onSave: (values: ArticleFormValues) => void
  article?: Article
}

export function ArticleFormDialog({ open, onClose, onSave, article }: ArticleFormDialogProps) {
  const isEdit = !!article

  const [name, setName] = useState(article?.name ?? '')
  const [description, setDescription] = useState(article?.description ?? '')
  const [price, setPrice] = useState(article ? String(article.price) : '')
  const [category, setCategory] = useState(article?.category ?? '')
  const [stock, setStock] = useState(article ? String(article.stock) : '')
  const [error, setError] = useState<string | null>(null)

  const resetAndClose = () => {
    setError(null)
    onClose()
  }

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    const priceValue = Number(price)
    const stockValue = Number(stock)

    if (!name.trim()) return setError('Name is required.')
    if (!category.trim()) return setError('Category is required.')
    if (!Number.isFinite(priceValue) || priceValue < 0) return setError('Price must be a non-negative number.')
    if (!Number.isInteger(stockValue) || stockValue < 0) return setError('Stock must be a non-negative whole number.')

    onSave({ name: name.trim(), description: description.trim(), price: priceValue, category: category.trim(), stock: stockValue })
    resetAndClose()
  }

  return (
    <Dialog
      open={open}
      onClose={resetAndClose}
      title={isEdit ? 'Edit article' : 'Add article'}
      footer={
        <div className="flex justify-end gap-2 border-t border-neutral-200 p-5 dark:border-neutral-800">
          <Button type="button" variant="outline" onClick={resetAndClose}>
            Cancel
          </Button>
          <Button type="submit" form="article-form">
            {isEdit ? 'Save changes' : 'Add article'}
          </Button>
        </div>
      }
    >
      <form id="article-form" onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && (
          <div className="flex items-start gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">
            <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="flex flex-col gap-1.5">
          <label htmlFor="article-name" className="text-sm font-medium">
            Name
          </label>
          <Input id="article-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Aurora Windbreaker" />
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="article-description" className="text-sm font-medium">
            Description
          </label>
          <Textarea
            id="article-description"
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Short product description"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="article-price" className="text-sm font-medium">
              Price (USDT)
            </label>
            <Input
              id="article-price"
              type="number"
              min="0"
              step="0.01"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
              placeholder="0.00"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="article-stock" className="text-sm font-medium">
              Stock
            </label>
            <Input
              id="article-stock"
              type="number"
              min="0"
              step="1"
              value={stock}
              onChange={(e) => setStock(e.target.value)}
              placeholder="0"
            />
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="article-category" className="text-sm font-medium">
            Category
          </label>
          <Input
            id="article-category"
            list="article-category-options"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            placeholder="e.g. Jackets"
          />
          <datalist id="article-category-options">
            {CATEGORIES.map((c) => (
              <option key={c} value={c} />
            ))}
          </datalist>
        </div>
      </form>
    </Dialog>
  )
}
