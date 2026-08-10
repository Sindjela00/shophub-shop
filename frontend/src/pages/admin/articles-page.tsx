import { useState } from 'react'
import { Package, Pencil, Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'
import { useToast } from '@/context/toast-context'
import { initialArticles } from '@/data/articles'
import type { Article } from '@/data/types'
import { ArticleFormDialog, type ArticleFormValues } from './article-form-dialog'

export function ArticlesPage() {
  const { toast } = useToast()
  const [articles, setArticles] = useState<Article[]>(initialArticles)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Article | undefined>(undefined)
  const [deleting, setDeleting] = useState<Article | undefined>(undefined)

  const openCreate = () => {
    setEditing(undefined)
    setFormOpen(true)
  }

  const openEdit = (article: Article) => {
    setEditing(article)
    setFormOpen(true)
  }

  const handleSave = (values: ArticleFormValues) => {
    if (editing) {
      setArticles((current) => current.map((a) => (a.id === editing.id ? { ...a, ...values } : a)))
      toast(`Updated ${values.name}`)
    } else {
      const article: Article = { id: crypto.randomUUID(), ...values }
      setArticles((current) => [...current, article])
      toast(`Added ${values.name}`)
    }
  }

  const handleDelete = () => {
    if (!deleting) return
    setArticles((current) => current.filter((a) => a.id !== deleting.id))
    toast(`Deleted ${deleting.name}`)
    setDeleting(undefined)
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

      {articles.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16 text-center text-neutral-500 dark:text-neutral-400">
          <Package className="h-10 w-10" strokeWidth={1.5} />
          <p>No articles yet.</p>
          <Button variant="outline" size="sm" onClick={openCreate}>
            Add your first article
          </Button>
        </div>
      ) : (
        <Card className="overflow-x-auto">
          <table className="w-full text-left text-sm">
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
                  <td className="px-4 py-3">{article.category}</td>
                  <td className="px-4 py-3">{article.price} USDT</td>
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

      <ArticleFormDialog open={formOpen} onClose={() => setFormOpen(false)} onSave={handleSave} article={editing} />

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
