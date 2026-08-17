import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Store } from 'lucide-react'
import { listCategories } from '@/lib/api'
import type { Category } from '@/data/types'

export function Footer() {
  const [categories, setCategories] = useState<Category[]>([])

  useEffect(() => {
    let cancelled = false
    listCategories().then((data) => {
      if (!cancelled) setCategories(data)
    })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <footer className="border-t border-neutral-200 dark:border-neutral-800">
      <div className="mx-auto flex max-w-6xl flex-col gap-8 px-4 py-10 sm:flex-row sm:justify-between">
        <div className="flex max-w-xs flex-col gap-2">
          <div className="flex items-center gap-2 font-semibold">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-white dark:bg-brand-500">
              <Store className="h-4 w-4" />
            </span>
            <span>Nordic Wear</span>
          </div>
          <p className="text-sm text-neutral-500 dark:text-neutral-400">
            Clothing and gear for every season, powered by ShopHub — crypto payments on testnet.
          </p>
        </div>

        <div className="flex flex-col gap-2">
          <span className="text-sm font-medium text-neutral-900 dark:text-neutral-100">Categories</span>
          <ul className="flex flex-col gap-1.5">
            {categories.map((category) => (
              <li key={category.id}>
                <Link
                  to={`/?category=${encodeURIComponent(category.name)}`}
                  className="text-sm text-neutral-500 hover:text-brand-600 dark:text-neutral-400 dark:hover:text-brand-400"
                >
                  {category.name}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </footer>
  )
}
