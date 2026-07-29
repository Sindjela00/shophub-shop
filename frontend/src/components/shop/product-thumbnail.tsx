import { Glasses } from 'lucide-react'
import type { Category } from '@/data/types'
import { CATEGORY_STYLE } from '@/lib/category-style'
import { cn } from '@/lib/utils'

interface ProductThumbnailProps {
  category: Category
  className?: string
}

export function ProductThumbnail({ category, className }: ProductThumbnailProps) {
  const { gradient, Icon } = CATEGORY_STYLE[category] ?? { gradient: 'from-neutral-400 to-neutral-600', Icon: Glasses }

  return (
    <div
      className={cn(
        'flex aspect-square items-center justify-center rounded-lg bg-linear-to-br',
        gradient,
        className,
      )}
    >
      <Icon className="h-1/3 w-1/3 text-white/90" strokeWidth={1.5} />
    </div>
  )
}
