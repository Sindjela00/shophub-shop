import { Fragment, useState } from 'react'
import { ChevronRight, ReceiptText } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { orders } from '@/data/orders'
import { SEPOLIA_CHAIN, formatAddress } from '@/lib/ethereum'
import { cn } from '@/lib/utils'

export function OrdersPage() {
  const [expanded, setExpanded] = useState<string | null>(null)

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <h1 className="mb-6 text-2xl font-semibold">Orders</h1>

      {orders.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16 text-center text-neutral-500 dark:text-neutral-400">
          <ReceiptText className="h-10 w-10" strokeWidth={1.5} />
          <p>No orders yet.</p>
        </div>
      ) : (
        <Card className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-neutral-200 text-neutral-500 dark:border-neutral-800 dark:text-neutral-400">
              <tr>
                <th className="w-8 px-4 py-3" />
                <th className="px-4 py-3 font-medium">Date</th>
                <th className="px-4 py-3 font-medium">Wallet</th>
                <th className="px-4 py-3 font-medium">Tx hash</th>
                <th className="px-4 py-3 font-medium">Items</th>
                <th className="px-4 py-3 font-medium text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => {
                const isOpen = expanded === order.id
                return (
                  <Fragment key={order.id}>
                    <tr
                      className="cursor-pointer border-b border-neutral-100 last:border-0 hover:bg-neutral-50 dark:border-neutral-900 dark:hover:bg-neutral-900/50"
                      onClick={() => setExpanded(isOpen ? null : order.id)}
                    >
                      <td className="px-4 py-3 text-neutral-400">
                        <ChevronRight className={cn('h-4 w-4 transition-transform duration-200', isOpen && 'rotate-90')} />
                      </td>
                      <td className="px-4 py-3">{new Date(order.createdAt).toLocaleString()}</td>
                      <td className="px-4 py-3 font-mono">{formatAddress(order.walletAddress)}</td>
                      <td className="px-4 py-3">
                        <a
                          href={`${SEPOLIA_CHAIN.blockExplorerUrls[0]}/tx/${order.txHash}`}
                          target="_blank"
                          rel="noreferrer"
                          onClick={(e) => e.stopPropagation()}
                          className="font-mono text-brand-600 hover:underline dark:text-brand-400"
                        >
                          {formatAddress(order.txHash)}
                        </a>
                      </td>
                      <td className="px-4 py-3">{order.items.reduce((sum, i) => sum + i.quantity, 0)}</td>
                      <td className="px-4 py-3 text-right font-medium">{order.total} USDT</td>
                    </tr>
                    <tr className="bg-neutral-50 dark:bg-neutral-900/50">
                      <td />
                      <td colSpan={5} className="p-0">
                        <div
                          className={cn(
                            'grid transition-[grid-template-rows] duration-200 ease-out',
                            isOpen ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]',
                          )}
                        >
                          <div className="overflow-hidden">
                            <div className="flex flex-col gap-1.5 px-4 py-3">
                              {order.items.map((item) => (
                                <div key={item.articleId} className="flex items-center justify-between text-neutral-600 dark:text-neutral-300">
                                  <span>
                                    {item.articleName} <span className="text-neutral-400">× {item.quantity}</span>
                                  </span>
                                  <span>{item.unitPrice * item.quantity} USDT</span>
                                </div>
                              ))}
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  </Fragment>
                )
              })}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}
