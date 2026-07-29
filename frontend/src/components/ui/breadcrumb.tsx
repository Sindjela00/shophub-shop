import { Fragment } from 'react'
import { Link } from 'react-router-dom'
import { ChevronRight } from 'lucide-react'

export interface BreadcrumbItem {
  label: string
  to?: string
}

export function Breadcrumb({ items }: { items: BreadcrumbItem[] }) {
  return (
    <nav aria-label="Breadcrumb" className="mb-6 flex items-center gap-1.5 text-sm text-neutral-500 dark:text-neutral-400">
      {items.map((item, i) => (
        <Fragment key={i}>
          {i > 0 && <ChevronRight className="h-3.5 w-3.5 flex-shrink-0" />}
          {item.to ? (
            <Link to={item.to} className="hover:text-neutral-900 dark:hover:text-neutral-100">
              {item.label}
            </Link>
          ) : (
            <span className="truncate text-neutral-900 dark:text-neutral-100">{item.label}</span>
          )}
        </Fragment>
      ))}
    </nav>
  )
}
