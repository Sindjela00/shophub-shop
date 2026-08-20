import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PackageSearch, Search } from 'lucide-react'
import { listArticles, listCategories } from '@/lib/api'
import { useShopInfo } from '@/context/shop-info-context'
import type { Category, Product } from '@/data/types'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { ProductCard } from '@/components/shop/product-card'
import { categoryStyle } from '@/lib/category-style'

type Sort = 'featured' | 'price-asc' | 'price-desc' | 'name-asc'

export function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const query = searchParams.get('q') ?? ''
  const category = searchParams.get('category') ?? ''
  const sort = (searchParams.get('sort') as Sort | null) ?? 'featured'

  const { shopName } = useShopInfo()
  const [products, setProducts] = useState<Product[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    listCategories().then((data) => {
      if (!cancelled) setCategories(data)
    })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    const timeout = setTimeout(() => {
      listArticles({ search: query || undefined, category: category || undefined })
        .then((articles) => {
          if (cancelled) return
          setProducts(articles)
        })
        .catch(() => {
          if (cancelled) return
          setError('Could not load products. Please try again.')
        })
        .finally(() => {
          if (cancelled) return
          setLoading(false)
        })
    }, 250)
    return () => {
      cancelled = true
      clearTimeout(timeout)
    }
  }, [query, category])

  const filtered = useMemo(() => {
    switch (sort) {
      case 'price-asc':
        return [...products].sort((a, b) => a.price - b.price)
      case 'price-desc':
        return [...products].sort((a, b) => b.price - a.price)
      case 'name-asc':
        return [...products].sort((a, b) => a.name.localeCompare(b.name))
      default:
        return products
    }
  }, [products, sort])

  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    setSearchParams(next)
  }

  const clearFilters = () => setSearchParams(new URLSearchParams())

  return (
    <div>
      <div className="border-b border-neutral-200 bg-linear-to-br from-brand-600 to-brand-800 dark:border-neutral-800">
        <div className="mx-auto max-w-6xl px-4 py-14 text-center">
          <h1 className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">{shopName}</h1>
          <p className="mx-auto mt-3 max-w-md text-brand-100">
            Clothing and gear for every season. Pay instantly with crypto — testnet, no hassle.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-6xl px-4 py-8">
        <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="relative w-full sm:max-w-xs">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-neutral-400" />
            <Input
              placeholder="Search products..."
              className="pl-9"
              value={query}
              onChange={(e) => setParam('q', e.target.value)}
            />
          </div>

          <select
            value={sort}
            onChange={(e) => setParam('sort', e.target.value)}
            className="h-10 cursor-pointer rounded-lg border border-neutral-300 bg-white px-3 text-sm text-neutral-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
          >
            <option value="featured">Sort: Featured</option>
            <option value="price-asc">Price: Low to High</option>
            <option value="price-desc">Price: High to Low</option>
            <option value="name-asc">Name: A–Z</option>
          </select>
        </div>

        <div className="mb-6 flex flex-wrap gap-2">
          <Button size="sm" variant={category === '' ? 'default' : 'outline'} onClick={() => setParam('category', '')}>
            All
          </Button>
          {categories.map((c) => {
            const { Icon } = categoryStyle(c.name)
            return (
              <Button
                key={c.id}
                size="sm"
                variant={category === c.name ? 'default' : 'outline'}
                onClick={() => setParam('category', c.name)}
              >
                <Icon className="h-3.5 w-3.5" />
                {c.name}
              </Button>
            )
          })}
        </div>

        {error && (
          <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">
            {error}
          </div>
        )}

        {loading ? (
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="aspect-4/5 w-full" />
            ))}
          </div>
        ) : (
          <>
            <p className="mb-4 text-sm text-neutral-500 dark:text-neutral-400">
              Showing {filtered.length} product{filtered.length === 1 ? '' : 's'}
            </p>

            {filtered.length === 0 ? (
              <div className="flex flex-col items-center gap-3 py-16 text-center text-neutral-500 dark:text-neutral-400">
                <PackageSearch className="h-10 w-10" strokeWidth={1.5} />
                <p>No products match your search.</p>
                <Button variant="outline" size="sm" onClick={clearFilters}>
                  Clear filters
                </Button>
              </div>
            ) : (
              <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {filtered.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
