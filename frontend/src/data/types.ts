// A category name as displayed/filtered-by — still just the string a Product/Article denormalizes
// from its real Category (below), matching ArticleDto's categoryName field.
export type CategoryName = string

export interface Product {
  id: string
  name: string
  description: string
  price: number
  categoryId: string
  categoryName: CategoryName
  stock: number
}

// An admin-managed category record (Category management: backend CRUD API) — what a
// Product/Article's categoryId actually references.
export interface Category {
  id: string
  name: string
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
