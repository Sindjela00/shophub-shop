import { Link } from 'react-router-dom'
import { ArrowLeft, Minus, Plus, ShoppingBag, Trash2 } from 'lucide-react'
import { useCart } from '@/context/cart-context'
import { ProductThumbnail } from '@/components/shop/product-thumbnail'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Breadcrumb } from '@/components/ui/breadcrumb'

export function CartPage() {
  const { items, total, setQuantity, removeItem } = useCart()

  if (items.length === 0) {
    return (
      <div className="mx-auto flex max-w-3xl flex-col items-center gap-4 px-4 py-20 text-center">
        <ShoppingBag className="h-12 w-12 text-neutral-300 dark:text-neutral-700" strokeWidth={1.5} />
        <p className="text-lg font-medium">Your cart is empty</p>
        <Link to="/">
          <Button>
            <ArrowLeft className="h-4 w-4" />
            Continue shopping
          </Button>
        </Link>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <Breadcrumb items={[{ label: 'Home', to: '/' }, { label: 'Cart' }]} />
      <h1 className="mb-6 text-2xl font-semibold">Cart</h1>

      <div className="flex flex-col gap-4">
        {items.map(({ product, quantity }) => (
          <Card key={product.id} className="flex items-center gap-4 p-4">
            <ProductThumbnail category={product.categoryName} className="w-20 flex-shrink-0" />
            <div className="flex flex-1 flex-col gap-1">
              <Link to={`/product/${product.id}`} className="font-medium hover:text-brand-600 dark:hover:text-brand-400">
                {product.name}
              </Link>
              <span className="text-sm text-neutral-500 dark:text-neutral-400">{product.price} USDC / item</span>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setQuantity(product.id, quantity - 1)}
                className="flex h-8 w-8 cursor-pointer items-center justify-center rounded-md border border-neutral-300 hover:bg-neutral-100 dark:border-neutral-700 dark:hover:bg-neutral-800"
                aria-label="Decrease quantity"
              >
                <Minus className="h-3.5 w-3.5" />
              </button>
              <span className="w-6 text-center">{quantity}</span>
              <button
                type="button"
                onClick={() => setQuantity(product.id, quantity + 1)}
                disabled={quantity >= product.stock}
                className="flex h-8 w-8 cursor-pointer items-center justify-center rounded-md border border-neutral-300 hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-40 dark:border-neutral-700 dark:hover:bg-neutral-800"
                aria-label="Increase quantity"
              >
                <Plus className="h-3.5 w-3.5" />
              </button>
            </div>
            <span className="w-24 text-right font-medium">{(product.price * quantity).toFixed(0)} USDC</span>
            <button
              type="button"
              onClick={() => removeItem(product.id)}
              className="cursor-pointer text-neutral-400 hover:text-red-600"
              aria-label="Remove item"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </Card>
        ))}
      </div>

      <Card className="mt-6 flex items-center justify-between p-5">
        <span className="text-lg font-semibold">Total</span>
        <span className="text-lg font-semibold">{total} USDC</span>
      </Card>

      <div className="mt-6 flex justify-end">
        <Link to="/checkout">
          <Button size="lg">Proceed to payment</Button>
        </Link>
      </div>
    </div>
  )
}
