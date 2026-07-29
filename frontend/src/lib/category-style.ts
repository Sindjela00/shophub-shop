import { Footprints, Shirt, Watch } from 'lucide-react'
import type { Category } from '@/data/types'

export const CATEGORIES: Category[] = ['Jackets', 'T-Shirts', 'Shoes', 'Pants', 'Accessories']

export const CATEGORY_STYLE: Record<Category, { gradient: string; Icon: typeof Shirt }> = {
  Jackets: { gradient: 'from-indigo-400 to-brand-600', Icon: Shirt },
  'T-Shirts': { gradient: 'from-sky-400 to-brand-500', Icon: Shirt },
  Shoes: { gradient: 'from-emerald-400 to-teal-600', Icon: Footprints },
  Pants: { gradient: 'from-amber-400 to-orange-600', Icon: Shirt },
  Accessories: { gradient: 'from-rose-400 to-fuchsia-600', Icon: Watch },
}

export const CATEGORY_BADGE_CLASS: Record<Category, string> = {
  Jackets: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300',
  'T-Shirts': 'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300',
  Shoes: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  Pants: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  Accessories: 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300',
}
