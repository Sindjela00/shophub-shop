import { useEffect, useState } from 'react'
import { Pencil, Plus, Tag, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { useToast } from '@/context/toast-context'
import { useAdminAuth } from '@/context/admin-auth-context'
import { ApiError, createCategory, deleteCategory, listArticles, listCategories, updateCategory } from '@/lib/api'
import type { Category } from '@/data/types'
import { categoryStyle } from '@/lib/category-style'
import { cn } from '@/lib/utils'
import { CategoryFormDialog } from './category-form-dialog'

export function CategoriesPage() {
  const { toast } = useToast()
  const { adminKey, clearAdminKey } = useAdminAuth()
  const [categories, setCategories] = useState<Category[]>([])
  const [articleCounts, setArticleCounts] = useState<Record<string, number>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Category | undefined>(undefined)
  const [deleting, setDeleting] = useState<Category | undefined>(undefined)
  const [saving, setSaving] = useState(false)

  const handleAuthError = (err: unknown) => {
    if (err instanceof ApiError && err.status === 401) {
      clearAdminKey()
      toast('Invalid admin key', 'error')
      return true
    }
    return false
  }

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    // Article counts are used purely to explain, up front, why a category might refuse to
    // delete — not loaded on the critical path of showing the list, so a slow/failed count
    // wouldn't be worth blocking the page on.
    Promise.all([listCategories(), listArticles()])
      .then(([categoryData, articleData]) => {
        if (cancelled) return
        setCategories(categoryData)
        const counts: Record<string, number> = {}
        for (const article of articleData) {
          counts[article.categoryId] = (counts[article.categoryId] ?? 0) + 1
        }
        setArticleCounts(counts)
      })
      .catch((err) => {
        if (cancelled) return
        if (!handleAuthError(err)) setError('Could not load categories.')
      })
      .finally(() => {
        if (cancelled) return
        setLoading(false)
      })
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const openCreate = () => {
    setEditing(undefined)
    setFormOpen(true)
  }

  const openEdit = (category: Category) => {
    setEditing(category)
    setFormOpen(true)
  }

  const handleSave = async (name: string) => {
    if (!adminKey) return
    setSaving(true)
    try {
      if (editing) {
        const updated = await updateCategory(editing.id, name, adminKey)
        setCategories((current) => current.map((c) => (c.id === editing.id ? updated : c)))
        toast(`Renamed to ${name}`)
      } else {
        const created = await createCategory(name, adminKey)
        setCategories((current) => [...current, created])
        toast(`Added ${name}`)
      }
      setFormOpen(false)
    } catch (err) {
      if (!handleAuthError(err)) {
        toast(err instanceof ApiError ? err.message : 'Could not save category.', 'error')
      }
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleting || !adminKey) return
    try {
      await deleteCategory(deleting.id, adminKey)
      setCategories((current) => current.filter((c) => c.id !== deleting.id))
      toast(`Deleted ${deleting.name}`)
    } catch (err) {
      if (!handleAuthError(err)) {
        toast(err instanceof ApiError ? err.message : 'Could not delete category.', 'error')
      }
    } finally {
      setDeleting(undefined)
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Categories</h1>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4" />
          Add category
        </Button>
      </div>

      {loading ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-14 w-full" />
          ))}
        </div>
      ) : error ? (
        <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">
          {error}
        </div>
      ) : categories.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16 text-center text-neutral-500 dark:text-neutral-400">
          <Tag className="h-10 w-10" strokeWidth={1.5} />
          <p>No categories yet.</p>
          <Button variant="outline" size="sm" onClick={openCreate}>
            Add your first category
          </Button>
        </div>
      ) : (
        <Card className="w-full overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-neutral-200 text-neutral-500 dark:border-neutral-800 dark:text-neutral-400">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Articles</th>
                <th className="px-4 py-3 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((category) => {
                const { gradient, Icon } = categoryStyle(category.name)
                const count = articleCounts[category.id] ?? 0
                return (
                  <tr key={category.id} className="border-b border-neutral-100 last:border-0 dark:border-neutral-900">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <span
                          className={cn(
                            'flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg bg-linear-to-br',
                            gradient,
                          )}
                        >
                          <Icon className="h-4 w-4 text-white/90" strokeWidth={1.5} />
                        </span>
                        <span className="font-medium">{category.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={cn(
                          'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                          count > 0
                            ? 'bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300'
                            : 'bg-neutral-100 text-neutral-500 dark:bg-neutral-800 dark:text-neutral-400',
                        )}
                      >
                        {count} article{count === 1 ? '' : 's'}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => openEdit(category)}
                          aria-label={`Rename ${category.name}`}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setDeleting(category)}
                          aria-label={`Delete ${category.name}`}
                        >
                          <Trash2 className="h-4 w-4 text-red-600 dark:text-red-400" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </Card>
      )}

      <CategoryFormDialog
        // Same reasoning as ArticleFormDialog's key: it stays mounted while closed, so its
        // internal useState(category?.name ?? '') only initializes once per mount — without a
        // key, renaming a second category would start from whatever was left in the field
        // rather than that category's actual current name.
        key={editing?.id ?? 'new'}
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSave={handleSave}
        category={editing}
        saving={saving}
      />

      <Dialog
        open={!!deleting}
        onClose={() => setDeleting(undefined)}
        title="Delete category"
        footer={
          <div className="flex justify-end gap-2 border-t border-neutral-200 p-5 dark:border-neutral-800">
            <Button variant="outline" onClick={() => setDeleting(undefined)}>
              Cancel
            </Button>
            <Button variant="default" className="bg-red-600 hover:bg-red-700" onClick={handleDelete}>
              Delete
            </Button>
          </div>
        }
      >
        <p className="text-sm text-neutral-600 dark:text-neutral-300">
          Are you sure you want to delete <span className="font-medium">{deleting?.name}</span>?
          {deleting && (articleCounts[deleting.id] ?? 0) > 0 ? (
            <>
              {' '}
              It's used by {articleCounts[deleting.id]} article{articleCounts[deleting.id] === 1 ? '' : 's'} —
              reassign or delete {articleCounts[deleting.id] === 1 ? 'it' : 'them'} first.
            </>
          ) : (
            " This can't be undone."
          )}
        </p>
      </Dialog>
    </div>
  )
}
