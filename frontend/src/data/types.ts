export type Category = string

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

export type Article = Product

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
