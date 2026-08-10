import { Link } from 'react-router-dom'
import { ShoppingCart } from 'lucide-react'
import type { Product } from '@/data/types'
import { Card, CardContent, CardFooter } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ProductThumbnail } from './product-thumbnail'
import { useCart } from '@/context/cart-context'
import { useToast } from '@/context/toast-context'
import { categoryBadgeClass } from '@/lib/category-style'

export function ProductCard({ product }: { product: Product }) {
  const { addItem } = useCart()
  const { toast } = useToast()
  const outOfStock = product.stock === 0

  return (
    <Card className="flex flex-col overflow-hidden transition-all duration-200 hover:-translate-y-1 hover:shadow-lg">
      <Link to={`/product/${product.id}`} className="p-4 pb-0">
        <ProductThumbnail category={product.category} />
      </Link>
      <CardContent className="flex flex-1 flex-col gap-2 pt-4">
        <div className="flex items-start justify-between gap-2">
          <Link to={`/product/${product.id}`} className="font-medium hover:text-brand-600 dark:hover:text-brand-400">
            {product.name}
          </Link>
          <Badge className={categoryBadgeClass(product.category)}>{product.category}</Badge>
        </div>
        <p className="line-clamp-2 text-sm text-neutral-500 dark:text-neutral-400">{product.description}</p>
        <div className="mt-auto flex items-center justify-between pt-2">
          <span className="text-lg font-semibold">{product.price} USDC</span>
          {outOfStock ? (
            <Badge variant="destructive">Out of stock</Badge>
          ) : (
            <Badge variant="success">{product.stock} in stock</Badge>
          )}
        </div>
      </CardContent>
      <CardFooter>
        <Button
          className="w-full"
          disabled={outOfStock}
          onClick={() => {
            addItem(product, 1)
            toast(`Added ${product.name} to cart`)
          }}
        >
          <ShoppingCart className="h-4 w-4" />
          Add to cart
        </Button>
      </CardFooter>
    </Card>
  )
}
