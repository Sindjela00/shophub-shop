import { useState, type FormEvent } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { KeyRound, Store } from 'lucide-react'
import { cn } from '@/lib/utils'
import { AdminAuthProvider, useAdminAuth } from '@/context/admin-auth-context'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'

const NAV_ITEMS = [
  { to: '/admin/articles', label: 'Articles' },
  { to: '/admin/categories', label: 'Categories' },
  { to: '/admin/orders', label: 'Orders' },
]

function AdminKeyGate() {
  const { setAdminKey } = useAdminAuth()
  const [key, setKey] = useState('')

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (key.trim()) setAdminKey(key.trim())
  }

  return (
    <div className="flex flex-1 items-center justify-center px-4 py-16">
      <Card className="flex w-full max-w-sm flex-col items-center gap-4 p-8 text-center">
        <span className="flex h-12 w-12 items-center justify-center rounded-full bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300">
          <KeyRound className="h-6 w-6" />
        </span>
        <div>
          <h1 className="font-medium">Admin access</h1>
          <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
            Enter the admin key to manage articles and view orders.
          </p>
        </div>
        <form onSubmit={handleSubmit} className="flex w-full flex-col gap-3">
          <Input
            type="password"
            value={key}
            onChange={(e) => setKey(e.target.value)}
            placeholder="Admin key"
            autoFocus
          />
          <Button type="submit" size="lg" disabled={!key.trim()}>
            Continue
          </Button>
        </form>
      </Card>
    </div>
  )
}

function AdminShell() {
  const { adminKey } = useAdminAuth()

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-40 border-b border-neutral-200 bg-white/80 backdrop-blur dark:border-neutral-800 dark:bg-neutral-950/80">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2 font-semibold">
              <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white dark:bg-brand-500">
                <Store className="h-5 w-5" />
              </span>
              <span className="text-lg">Admin</span>
            </div>
            {adminKey && (
              <nav className="flex items-center gap-1">
                {NAV_ITEMS.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    className={({ isActive }) =>
                      cn(
                        'rounded-lg px-3 py-1.5 text-sm font-medium',
                        isActive
                          ? 'bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300'
                          : 'text-neutral-600 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800',
                      )
                    }
                  >
                    {item.label}
                  </NavLink>
                ))}
              </nav>
            )}
          </div>

          <NavLink to="/" className="text-sm text-neutral-500 hover:text-neutral-900 dark:hover:text-neutral-100">
            View storefront
          </NavLink>
        </div>
      </header>

      <main className="flex flex-1 flex-col">{adminKey ? <Outlet /> : <AdminKeyGate />}</main>
    </div>
  )
}

export function AdminLayout() {
  return (
    <AdminAuthProvider>
      <AdminShell />
    </AdminAuthProvider>
  )
}
