import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { CartProvider } from '@/context/cart-context'
import { ToastProvider } from '@/context/toast-context'
import { Header } from '@/components/layout/header'
import { Footer } from '@/components/layout/footer'
import { AdminLayout } from '@/components/layout/admin-layout'
import { basePath } from '@/lib/base-path'
import { CatalogPage } from '@/pages/catalog-page'
import { ProductPage } from '@/pages/product-page'
import { CartPage } from '@/pages/cart-page'
import { CheckoutPage } from '@/pages/checkout-page'
import { CheckoutSuccessPage } from '@/pages/checkout-success-page'
import { ArticlesPage } from '@/pages/admin/articles-page'
import { CategoriesPage } from '@/pages/admin/categories-page'
import { OrdersPage } from '@/pages/admin/orders-page'

function StorefrontLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}

function App() {
  return (
    <BrowserRouter basename={basePath}>
      <ToastProvider>
        <CartProvider>
          <Routes>
            <Route element={<StorefrontLayout />}>
              <Route path="/" element={<CatalogPage />} />
              <Route path="/product/:id" element={<ProductPage />} />
              <Route path="/cart" element={<CartPage />} />
              <Route path="/checkout" element={<CheckoutPage />} />
              <Route path="/checkout/success" element={<CheckoutSuccessPage />} />
            </Route>

            <Route element={<AdminLayout />}>
              <Route path="/admin" element={<Navigate to="/admin/articles" replace />} />
              <Route path="/admin/articles" element={<ArticlesPage />} />
              <Route path="/admin/categories" element={<CategoriesPage />} />
              <Route path="/admin/orders" element={<OrdersPage />} />
            </Route>
          </Routes>
        </CartProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}

export default App
