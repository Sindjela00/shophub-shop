import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { getShop } from '@/lib/api'

interface ShopContextValue {
  name: string
  description: string
}

const ShopContext = createContext<ShopContextValue | null>(null)

// Generic placeholders shown only until the real values load from the backend — never
// specific to any shop, so nothing tenant-specific is baked into the frontend.
const FALLBACK: ShopContextValue = {
  name: 'Shop',
  description: '',
}

export function ShopProvider({ children }: { children: ReactNode }) {
  const [shop, setShop] = useState<ShopContextValue>(FALLBACK)

  useEffect(() => {
    let cancelled = false
    getShop()
      .then((result) => {
        if (!cancelled) {
          setShop({ name: result.name, description: result.description })
          document.title = `${result.name} — ShopHub`
        }
      })
      .catch(() => {
        // Keep the fallback values; the rest of the storefront still works without them.
      })
    return () => {
      cancelled = true
    }
  }, [])

  return <ShopContext.Provider value={shop}>{children}</ShopContext.Provider>
}

export function useShop() {
  const ctx = useContext(ShopContext)
  if (!ctx) throw new Error('useShop must be used within a ShopProvider')
  return ctx
}
