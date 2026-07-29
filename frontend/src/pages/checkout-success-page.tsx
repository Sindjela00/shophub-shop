import { Link, Navigate, useLocation } from 'react-router-dom'
import { CheckCircle2 } from 'lucide-react'
import type { CartItem } from '@/data/types'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'

interface SuccessState {
  items: CartItem[]
  total: number
  txHash: string
  wallet: string | null
}

export function CheckoutSuccessPage() {
  const location = useLocation()
  const state = location.state as SuccessState | null

  if (!state) {
    return <Navigate to="/" replace />
  }

  const { items, total, txHash, wallet } = state

  return (
    <div className="mx-auto max-w-2xl px-4 py-12">
      <Card className="flex flex-col items-center gap-4 p-8 text-center">
        <span className="flex h-16 w-16 items-center justify-center rounded-full bg-green-100 text-green-600 dark:bg-green-900/40 dark:text-green-400">
          <CheckCircle2 className="h-9 w-9" />
        </span>
        <div>
          <h1 className="text-xl font-semibold">Payment successful</h1>
          <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
            Your order has been confirmed on the blockchain.
          </p>
        </div>

        <div className="w-full rounded-lg bg-neutral-100 p-4 text-left text-sm dark:bg-neutral-800">
          <div className="flex justify-between gap-4">
            <span className="text-neutral-500 dark:text-neutral-400">Wallet</span>
            <span className="truncate font-mono">{wallet}</span>
          </div>
          <div className="mt-2 flex justify-between gap-4">
            <span className="text-neutral-500 dark:text-neutral-400">Tx hash</span>
            <span className="truncate font-mono">{txHash}</span>
          </div>
        </div>

        <div className="flex w-full flex-col gap-2 divide-y divide-neutral-200 text-left dark:divide-neutral-800">
          {items.map(({ product, quantity }) => (
            <div key={product.id} className="flex justify-between py-2 text-sm">
              <span>
                {product.name} × {quantity}
              </span>
              <span className="font-medium">{product.price * quantity} USDT</span>
            </div>
          ))}
          <div className="flex justify-between pt-2 text-base font-semibold">
            <span>Total paid</span>
            <span>{total} USDT</span>
          </div>
        </div>

        <Link to="/" className="w-full">
          <Button size="lg" className="w-full">
            Continue shopping
          </Button>
        </Link>
      </Card>
    </div>
  )
}
