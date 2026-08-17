import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, Minus, Plus, ShoppingCart } from 'lucide-react'
import { getArticle } from '@/lib/api'
import type { Product } from '@/data/types'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Breadcrumb } from '@/components/ui/breadcrumb'
import { Skeleton } from '@/components/ui/skeleton'
import { ProductThumbnail } from '@/components/shop/product-thumbnail'
import { useCart } from '@/context/cart-context'
import { useToast } from '@/context/toast-context'
import { categoryBadgeClass } from '@/lib/category-style'

export function ProductPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { addItem } = useCart()
  const { toast } = useToast()
  const [quantity, setQuantity] = useState(1)

  const [product, setProduct] = useState<Product | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    setLoading(true)
    setNotFound(false)
    getArticle(id)
      .then((article) => {
        if (cancelled) return
        setProduct(article)
      })
      .catch(() => {
        if (cancelled) return
        setNotFound(true)
      })
      .finally(() => {
        if (cancelled) return
        setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id])

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-8">
        <div className="grid grid-cols-1 gap-8 sm:grid-cols-2">
          <Skeleton className="aspect-square w-full" />
          <div className="flex flex-col gap-4">
            <Skeleton className="h-6 w-24" />
            <Skeleton className="h-8 w-2/3" />
            <Skeleton className="h-20 w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (notFound || !product) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 text-center">
        <p className="text-lg font-medium">Product not found.</p>
        <Link to="/" className="mt-4 inline-block">
          <Button variant="outline">
            <ArrowLeft className="h-4 w-4" />
            Back to catalog
          </Button>
        </Link>
      </div>
    )
  }

  const outOfStock = product.stock === 0

  const handleAddToCart = () => {
    addItem(product, quantity)
    toast(`Added ${product.name} to cart`)
    navigate('/cart')
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 pb-28 sm:pb-8">
      <Breadcrumb
        items={[
          { label: 'Home', to: '/' },
          { label: product.categoryName, to: `/?category=${encodeURIComponent(product.categoryName)}` },
          { label: product.name },
        ]}
      />

      <div className="grid grid-cols-1 gap-8 sm:grid-cols-2">
        <ProductThumbnail category={product.categoryName} className="w-full" />

        <div className="flex flex-col gap-4">
          <div>
            <Badge className={categoryBadgeClass(product.categoryName)}>{product.categoryName}</Badge>
            <h1 className="mt-2 text-2xl font-semibold">{product.name}</h1>
          </div>

          <p className="text-neutral-600 dark:text-neutral-300">{product.description}</p>

          <div className="flex items-center gap-3">
            <span className="text-2xl font-semibold">{product.price} USDC</span>
            {outOfStock ? (
              <Badge variant="destructive">Out of stock</Badge>
            ) : (
              <Badge variant="success">{product.stock} in stock</Badge>
            )}
          </div>

          {!outOfStock && (
            <div className="flex items-center gap-3">
              <span className="text-sm text-neutral-500 dark:text-neutral-400">Quantity</span>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                  className="flex h-9 w-9 items-center justify-center cursor-pointer rounded-md border border-neutral-300 hover:bg-neutral-100 dark:border-neutral-700 dark:hover:bg-neutral-800"
                  aria-label="Decrease quantity"
                >
                  <Minus className="h-4 w-4" />
                </button>
                <span className="w-8 text-center">{quantity}</span>
                <button
                  type="button"
                  onClick={() => setQuantity((q) => Math.min(product.stock, q + 1))}
                  className="flex h-9 w-9 items-center justify-center cursor-pointer rounded-md border border-neutral-300 hover:bg-neutral-100 dark:border-neutral-700 dark:hover:bg-neutral-800"
                  aria-label="Increase quantity"
                >
                  <Plus className="h-4 w-4" />
                </button>
              </div>
            </div>
          )}

          <Button size="lg" disabled={outOfStock} onClick={handleAddToCart} className="hidden sm:inline-flex">
            <ShoppingCart className="h-4 w-4" />
            Add to cart
          </Button>
        </div>
      </div>

      <div className="fixed inset-x-0 bottom-0 z-30 flex items-center justify-between gap-4 border-t border-neutral-200 bg-white/95 p-4 backdrop-blur sm:hidden dark:border-neutral-800 dark:bg-neutral-950/95">
        <span className="text-lg font-semibold">{product.price} USDC</span>
        <Button size="lg" disabled={outOfStock} onClick={handleAddToCart} className="flex-1">
          <ShoppingCart className="h-4 w-4" />
          Add to cart
        </Button>
      </div>
    </div>
  )
}
