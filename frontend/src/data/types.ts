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

export interface Article {
  id: string
  name: string
  description: string
  price: number
  category: string
  stock: number
}

export interface OrderItem {
  articleId: string
  articleName: string
  unitPrice: number
  quantity: number
}

export interface Order {
  id: string
  walletAddress: string
  txHash: string
  total: number
  createdAt: string
  items: OrderItem[]
}
