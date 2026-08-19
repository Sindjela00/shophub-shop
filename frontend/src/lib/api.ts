import type { Article, CartItem as LocalCartItem, Category, Order } from '@/data/types'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5022'

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  })

  if (response.status === 204) {
    return undefined as T
  }

  const body = await response.json().catch(() => null)

  if (!response.ok) {
    const message =
      (body?.error as string | undefined) ??
      (body?.reason as string | undefined) ??
      `Request failed with status ${response.status}.`
    throw new ApiError(response.status, message)
  }

  return body as T
}

function adminHeaders(adminKey: string): HeadersInit {
  return { 'X-Admin-Key': adminKey }
}

// Articles

export interface ArticleListParams {
  search?: string
  category?: string
}

export function listArticles(params: ArticleListParams = {}) {
  const query = new URLSearchParams()
  if (params.search) query.set('search', params.search)
  if (params.category) query.set('category', params.category)
  const qs = query.toString()
  return request<Article[]>(`/api/articles${qs ? `?${qs}` : ''}`)
}

export function getArticle(id: string) {
  return request<Article>(`/api/articles/${id}`)
}

export interface UpsertArticleBody {
  name: string
  description: string
  price: number
  categoryId: string
  stock: number
}

export function createArticle(body: UpsertArticleBody, adminKey: string) {
  return request<Article>('/api/articles', {
    method: 'POST',
    headers: adminHeaders(adminKey),
    body: JSON.stringify(body),
  })
}

export function updateArticle(id: string, body: UpsertArticleBody, adminKey: string) {
  return request<Article>(`/api/articles/${id}`, {
    method: 'PUT',
    headers: adminHeaders(adminKey),
    body: JSON.stringify(body),
  })
}

export function deleteArticle(id: string, adminKey: string) {
  return request<void>(`/api/articles/${id}`, {
    method: 'DELETE',
    headers: adminHeaders(adminKey),
  })
}

// Shop

export interface ShopInfoDto {
  name: string
}

export function getShopInfo() {
  return request<ShopInfoDto>('/api/shop')
}

// Categories

export function listCategories() {
  return request<Category[]>('/api/categories')
}

export function createCategory(name: string, adminKey: string) {
  return request<Category>('/api/categories', {
    method: 'POST',
    headers: adminHeaders(adminKey),
    body: JSON.stringify({ name }),
  })
}

export function updateCategory(id: string, name: string, adminKey: string) {
  return request<Category>(`/api/categories/${id}`, {
    method: 'PUT',
    headers: adminHeaders(adminKey),
    body: JSON.stringify({ name }),
  })
}

export function deleteCategory(id: string, adminKey: string) {
  return request<void>(`/api/categories/${id}`, {
    method: 'DELETE',
    headers: adminHeaders(adminKey),
  })
}

// Carts

export interface CartItemDto {
  articleId: string
  articleName: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface CartDto {
  id: string
  items: CartItemDto[]
  total: number
}

export function createCart() {
  return request<CartDto>('/api/carts', { method: 'POST' })
}

export function getCart(cartId: string) {
  return request<CartDto>(`/api/carts/${cartId}`)
}

export function addCartItem(cartId: string, articleId: string, quantity: number) {
  return request<CartDto>(`/api/carts/${cartId}/items`, {
    method: 'POST',
    body: JSON.stringify({ articleId, quantity }),
  })
}

export function setCartItemQuantity(cartId: string, articleId: string, quantity: number) {
  return request<CartDto>(`/api/carts/${cartId}/items/${articleId}`, {
    method: 'PUT',
    body: JSON.stringify({ quantity }),
  })
}

export function removeCartItem(cartId: string, articleId: string) {
  return request<CartDto>(`/api/carts/${cartId}/items/${articleId}`, { method: 'DELETE' })
}

export interface PendingPaymentDto {
  to: string
  data: string
  value: string
  total: number
}

export function prepareCheckout(cartId: string, walletAddress: string) {
  return request<PendingPaymentDto>(`/api/carts/${cartId}/checkout/prepare`, {
    method: 'POST',
    body: JSON.stringify({ walletAddress }),
  })
}

// Distinguishes "payment not confirmed on-chain yet" (poll again) from a real order.
export type CheckoutResult = { status: 'confirmed'; order: Order } | { status: 'pending'; reason: string }

export async function checkout(cartId: string, walletAddress: string, txHash: string): Promise<CheckoutResult> {
  const response = await fetch(`${API_BASE_URL}/api/carts/${cartId}/checkout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ walletAddress, txHash }),
  })

  const body = await response.json().catch(() => null)

  if (response.status === 202) {
    return { status: 'pending', reason: body?.reason ?? 'Payment not confirmed yet.' }
  }

  if (!response.ok) {
    throw new ApiError(response.status, body?.error ?? `Request failed with status ${response.status}.`)
  }

  return { status: 'confirmed', order: body as Order }
}

// Orders

export function listOrders(adminKey: string) {
  return request<Order[]>('/api/orders', { headers: adminHeaders(adminKey) })
}

export async function syncCartWithLocalItems(items: LocalCartItem[]) {
  const cart = await createCart()
  for (const item of items) {
    await addCartItem(cart.id, item.product.id, item.quantity)
  }
  return cart.id
}
