import { useEffect, useState } from 'react'
import { Package, Pencil, Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { useToast } from '@/context/toast-context'
import { useAdminAuth } from '@/context/admin-auth-context'
import { ApiError, createArticle, deleteArticle, listArticles, listCategories, updateArticle } from '@/lib/api'
import type { Article, Category } from '@/data/types'
import { ArticleFormDialog, type ArticleFormValues } from './article-form-dialog'

export function ArticlesPage() {
  const { toast } = useToast()
  const { adminKey, clearAdminKey } = useAdminAuth()
  const [articles, setArticles] = useState<Article[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Article | undefined>(undefined)
  const [deleting, setDeleting] = useState<Article | undefined>(undefined)
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
    Promise.all([listArticles(), listCategories()])
      .then(([articleData, categoryData]) => {
        if (cancelled) return
        setArticles(articleData)
        setCategories(categoryData)
      })
      .catch((err) => {
        if (cancelled) return
        if (!handleAuthError(err)) setError('Could not load articles.')
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

  const openEdit = (article: Article) => {
    setEditing(article)
    setFormOpen(true)
  }

  const handleSave = async (values: ArticleFormValues) => {
    if (!adminKey) return
    setSaving(true)
    try {
      if (editing) {
        const updated = await updateArticle(editing.id, values, adminKey)
        setArticles((current) => current.map((a) => (a.id === editing.id ? updated : a)))
        toast(`Updated ${values.name}`)
      } else {
        const created = await createArticle(values, adminKey)
        setArticles((current) => [...current, created])
        toast(`Added ${values.name}`)
      }
      setFormOpen(false)
    } catch (err) {
      if (!handleAuthError(err)) {
        toast(err instanceof ApiError ? err.message : 'Could not save article.', 'error')
      }
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleting || !adminKey) return
    try {
      await deleteArticle(deleting.id, adminKey)
      setArticles((current) => current.filter((a) => a.id !== deleting.id))
      toast(`Deleted ${deleting.name}`)
    } catch (err) {
      if (!handleAuthError(err)) {
        toast(err instanceof ApiError ? err.message : 'Could not delete article.', 'error')
      }
    } finally {
      setDeleting(undefined)
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Articles</h1>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4" />
          Add article
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
      ) : articles.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16 text-center text-neutral-500 dark:text-neutral-400">
          <Package className="h-10 w-10" strokeWidth={1.5} />
          <p>No articles yet.</p>
          <Button variant="outline" size="sm" onClick={openCreate}>
            Add your first article
          </Button>
        </div>
      ) : (
        <Card className="w-full overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-neutral-200 text-neutral-500 dark:border-neutral-800 dark:text-neutral-400">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Category</th>
                <th className="px-4 py-3 font-medium">Price</th>
                <th className="px-4 py-3 font-medium">Stock</th>
                <th className="px-4 py-3 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {articles.map((article) => (
                <tr key={article.id} className="border-b border-neutral-100 last:border-0 dark:border-neutral-900">
                  <td className="px-4 py-3">
                    <div className="font-medium">{article.name}</div>
                    <div className="line-clamp-1 text-neutral-500 dark:text-neutral-400">{article.description}</div>
                  </td>
                  <td className="px-4 py-3">{article.categoryName}</td>
                  <td className="px-4 py-3">{article.price} USDC</td>
                  <td className="px-4 py-3">{article.stock}</td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" size="icon" onClick={() => openEdit(article)} aria-label={`Edit ${article.name}`}>
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeleting(article)}
                        aria-label={`Delete ${article.name}`}
                      >
                        <Trash2 className="h-4 w-4 text-red-600 dark:text-red-400" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      <ArticleFormDialog
        // The dialog stays mounted while closed (its open/close transition depends on that),
        // so its internal useState(article?.x ?? '') initializers only ever run once — without
        // a key forcing a remount per article, editing a different article after the first
        // would keep showing whatever was left over from before instead of that article's
        // actual values.
        key={editing?.id ?? 'new'}
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSave={handleSave}
        article={editing}
        categories={categories}
        saving={saving}
      />

      <Dialog
        open={!!deleting}
        onClose={() => setDeleting(undefined)}
        title="Delete article"
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
          Are you sure you want to delete <span className="font-medium">{deleting?.name}</span>? This can't be undone.
        </p>
      </Dialog>
    </div>
  )
}
