import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { LayoutDashboard, ShoppingCart, Store } from 'lucide-react'
import { useCart } from '@/context/cart-context'
import { useShopInfo } from '@/context/shop-info-context'
import { CartDrawer } from '@/components/shop/cart-drawer'
import { cn } from '@/lib/utils'

export function Header() {
  const { itemCount } = useCart()
  const { shopName } = useShopInfo()
  const [cartOpen, setCartOpen] = useState(false)
  const [bump, setBump] = useState(false)
  const previousCount = useRef(itemCount)

  useEffect(() => {
    if (itemCount > previousCount.current) {
      setBump(true)
      const timeout = setTimeout(() => setBump(false), 400)
      previousCount.current = itemCount
      return () => clearTimeout(timeout)
    }
    previousCount.current = itemCount
  }, [itemCount])

  return (
    <header className="sticky top-0 z-40 border-b border-neutral-200 bg-white/80 backdrop-blur dark:border-neutral-800 dark:bg-neutral-950/80">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
        <Link to="/" className="flex items-center gap-2 font-semibold">
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white dark:bg-brand-500">
            <Store className="h-5 w-5" />
          </span>
          <span className="text-lg">{shopName}</span>
        </Link>

        <div className="flex items-center gap-1">
          <Link
            to="/admin"
            className="flex h-10 w-10 items-center justify-center rounded-lg text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
            aria-label="Admin"
          >
            <LayoutDashboard className="h-5 w-5" />
          </Link>

          <button
            type="button"
            onClick={() => setCartOpen(true)}
            className="relative flex h-10 w-10 cursor-pointer items-center justify-center rounded-lg hover:bg-neutral-100 dark:hover:bg-neutral-800"
            aria-label="Open cart"
          >
            <ShoppingCart className={cn('h-5 w-5', bump && 'animate-cart-bump')} />
            {itemCount > 0 && (
              <span
                className={cn(
                  'absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-600 px-1 text-xs font-medium text-white dark:bg-brand-500',
                  bump && 'animate-cart-bump',
                )}
              >
                {itemCount}
              </span>
            )}
          </button>
        </div>
      </div>

      <CartDrawer open={cartOpen} onClose={() => setCartOpen(false)} />
    </header>
  )
}
