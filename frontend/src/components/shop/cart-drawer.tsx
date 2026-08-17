import { Link } from 'react-router-dom'
import { Minus, Plus, ShoppingBag, Trash2 } from 'lucide-react'
import { Sheet } from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { ProductThumbnail } from './product-thumbnail'
import { useCart } from '@/context/cart-context'

interface CartDrawerProps {
  open: boolean
  onClose: () => void
}

export function CartDrawer({ open, onClose }: CartDrawerProps) {
  const { items, total, setQuantity, removeItem } = useCart()

  const footer = items.length > 0 && (
    <div className="border-t border-neutral-200 p-5 dark:border-neutral-800">
      <div className="mb-4 flex items-center justify-between text-base font-semibold">
        <span>Total</span>
        <span>{total} USDC</span>
      </div>
      <Link to="/checkout" onClick={onClose}>
        <Button className="w-full" size="lg">
          Proceed to payment
        </Button>
      </Link>
    </div>
  )

  return (
    <Sheet open={open} onClose={onClose} title="Cart" footer={footer}>
      {items.length === 0 ? (
        <div className="flex h-full flex-col items-center justify-center gap-3 p-8 text-center text-neutral-500 dark:text-neutral-400">
          <ShoppingBag className="h-10 w-10" strokeWidth={1.5} />
          <p>Your cart is empty</p>
        </div>
      ) : (
        <div className="flex flex-col gap-4 p-5">
          {items.map(({ product, quantity }) => (
            <div key={product.id} className="flex gap-3">
              <ProductThumbnail category={product.categoryName} className="w-16 flex-shrink-0" />
              <div className="flex flex-1 flex-col gap-1">
                <div className="flex items-start justify-between gap-2">
                  <span className="text-sm font-medium">{product.name}</span>
                  <button
                    type="button"
                    onClick={() => removeItem(product.id)}
                    className="cursor-pointer text-neutral-400 hover:text-red-600"
                    aria-label="Remove item"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
                <span className="text-sm text-neutral-500 dark:text-neutral-400">{product.price} USDC</span>
                <div className="mt-1 flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setQuantity(product.id, quantity - 1)}
                    className="flex h-7 w-7 cursor-pointer items-center justify-center rounded-md border border-neutral-300 hover:bg-neutral-100 dark:border-neutral-700 dark:hover:bg-neutral-800"
                    aria-label="Decrease quantity"
                  >
                    <Minus className="h-3.5 w-3.5" />
                  </button>
                  <span className="w-6 text-center text-sm">{quantity}</span>
                  <button
                    type="button"
                    onClick={() => setQuantity(product.id, quantity + 1)}
                    disabled={quantity >= product.stock}
                    className="flex h-7 w-7 cursor-pointer items-center justify-center rounded-md border border-neutral-300 hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-40 dark:border-neutral-700 dark:hover:bg-neutral-800"
                    aria-label="Increase quantity"
                  >
                    <Plus className="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </Sheet>
  )
}
