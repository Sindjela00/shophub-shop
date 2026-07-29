export type Category = 'Jackets' | 'T-Shirts' | 'Shoes' | 'Pants' | 'Accessories'

export interface Product {
  id: string
  name: string
  description: string
  price: number
  category: Category
  stock: number
}

export interface CartItem {
  product: Product
  quantity: number
}
