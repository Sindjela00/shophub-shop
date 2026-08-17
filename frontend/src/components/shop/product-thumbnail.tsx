import type { CategoryName } from '@/data/types'
import { categoryStyle } from '@/lib/category-style'
import { cn } from '@/lib/utils'

interface ProductThumbnailProps {
  category: CategoryName
  className?: string
}

export function ProductThumbnail({ category, className }: ProductThumbnailProps) {
  const { gradient, Icon } = categoryStyle(category)

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
