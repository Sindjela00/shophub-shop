import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { getShopInfo } from '@/lib/api'

// Generic fallback used until the real name loads, and if the backend's SHOP_NAME env var
// is ever unset (e.g. local dev) — deliberately NOT a specific shop's name like "Nordic Wear".
export const DEFAULT_SHOP_NAME = 'ShopHub Store'

interface ShopInfoContextValue {
  shopName: string
}

const ShopInfoContext = createContext<ShopInfoContextValue>({ shopName: DEFAULT_SHOP_NAME })

export function ShopInfoProvider({ children }: { children: ReactNode }) {
  const [shopName, setShopName] = useState(DEFAULT_SHOP_NAME)

  useEffect(() => {
    let cancelled = false
    getShopInfo()
      .then((info) => {
        if (!cancelled && info.name) setShopName(info.name)
      })
      .catch(() => {
        // Keep the generic fallback — a shop name is cosmetic, not worth surfacing an error for.
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    document.title = `${shopName} — ShopHub`
  }, [shopName])

  return <ShopInfoContext.Provider value={{ shopName }}>{children}</ShopInfoContext.Provider>
}

export function useShopInfo() {
  return useContext(ShopInfoContext)
}
