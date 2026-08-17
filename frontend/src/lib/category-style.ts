import { Footprints, Shirt, Tag, Watch } from 'lucide-react'
import type { CategoryName } from '@/data/types'

interface Style {
  gradient: string
  Icon: typeof Shirt
}

// Bespoke styling for the categories the storefront originally shipped with — kept for their
// nicer, hand-picked look. Anything else (any admin-created category) falls through to the
// generated fallback below instead of needing an entry added here.
const KNOWN_STYLE: Record<string, Style> = {
  Jackets: { gradient: 'from-indigo-400 to-brand-600', Icon: Shirt },
  'T-Shirts': { gradient: 'from-sky-400 to-brand-500', Icon: Shirt },
  Shoes: { gradient: 'from-emerald-400 to-teal-600', Icon: Footprints },
  Pants: { gradient: 'from-amber-400 to-orange-600', Icon: Shirt },
  Accessories: { gradient: 'from-rose-400 to-fuchsia-600', Icon: Watch },
}

const KNOWN_BADGE_CLASS: Record<string, string> = {
  Jackets: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300',
  'T-Shirts': 'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300',
  Shoes: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  Pants: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  Accessories: 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300',
}

// Rotated by name so different unknown categories still look visually distinct from each
// other, rather than every one of them collapsing onto one identical grey fallback.
const FALLBACK_GRADIENTS = [
  'from-violet-400 to-purple-600',
  'from-cyan-400 to-blue-600',
  'from-lime-400 to-green-600',
  'from-pink-400 to-red-600',
  'from-yellow-400 to-amber-600',
]

const FALLBACK_BADGE_CLASSES = [
  'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300',
  'bg-lime-100 text-lime-700 dark:bg-lime-900/40 dark:text-lime-300',
  'bg-pink-100 text-pink-700 dark:bg-pink-900/40 dark:text-pink-300',
  'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300',
]

function fallbackIndex(name: string, bucketCount: number): number {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) | 0
  }
  return Math.abs(hash) % bucketCount
}

// Categories are admin-defined free text on the backend (Category management: backend CRUD
// API) — every category needs *some* reasonable styling, not just the handful curated above,
// so both of these fall back to a deterministic (same name -> same look every time) generated
// style instead of requiring bespoke gradient/icon/badge entries per new category.

export function categoryStyle(category: CategoryName): Style {
  return KNOWN_STYLE[category] ?? { gradient: FALLBACK_GRADIENTS[fallbackIndex(category, FALLBACK_GRADIENTS.length)], Icon: Tag }
}

export function categoryBadgeClass(category: CategoryName): string {
  return KNOWN_BADGE_CLASS[category] ?? FALLBACK_BADGE_CLASSES[fallbackIndex(category, FALLBACK_BADGE_CLASSES.length)]
}
