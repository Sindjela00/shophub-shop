import { createContext, useContext, useState, type ReactNode } from 'react'

const STORAGE_KEY = 'shophub-shop-admin-key'

interface AdminAuthContextValue {
  adminKey: string | null
  setAdminKey: (key: string) => void
  clearAdminKey: () => void
}

const AdminAuthContext = createContext<AdminAuthContextValue | null>(null)

export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [adminKey, setAdminKeyState] = useState<string | null>(() =>
    localStorage.getItem(STORAGE_KEY),
  )

  const setAdminKey = (key: string) => {
    localStorage.setItem(STORAGE_KEY, key)
    setAdminKeyState(key)
  }

  const clearAdminKey = () => {
    localStorage.removeItem(STORAGE_KEY)
    setAdminKeyState(null)
  }

  return (
    <AdminAuthContext.Provider value={{ adminKey, setAdminKey, clearAdminKey }}>
      {children}
    </AdminAuthContext.Provider>
  )
}

export function useAdminAuth() {
  const ctx = useContext(AdminAuthContext)
  if (!ctx) throw new Error('useAdminAuth must be used within an AdminAuthProvider')
  return ctx
}
