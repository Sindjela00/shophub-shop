import { Link } from 'react-router-dom'
import { Compass } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'

export function NotFoundPage() {
  return (
    <div className="mx-auto max-w-2xl px-4 py-12">
      <Card className="flex flex-col items-center gap-4 p-8 text-center">
        <span className="flex h-16 w-16 items-center justify-center rounded-full bg-neutral-100 text-neutral-500 dark:bg-neutral-800 dark:text-neutral-400">
          <Compass className="h-9 w-9" />
        </span>
        <div>
          <h1 className="text-xl font-semibold">Page not found</h1>
          <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
            The page you're looking for doesn't exist or may have moved.
          </p>
        </div>

        <Link to="/" className="w-full">
          <Button size="lg" className="w-full">
            Back to storefront
          </Button>
        </Link>
      </Card>
    </div>
  )
}
