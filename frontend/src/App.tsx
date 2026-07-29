import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { CartProvider } from '@/context/cart-context'
import { ToastProvider } from '@/context/toast-context'
import { Header } from '@/components/layout/header'
import { Footer } from '@/components/layout/footer'
import { CatalogPage } from '@/pages/catalog-page'
import { ProductPage } from '@/pages/product-page'
import { CartPage } from '@/pages/cart-page'
import { CheckoutPage } from '@/pages/checkout-page'
import { CheckoutSuccessPage } from '@/pages/checkout-success-page'

function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <CartProvider>
          <div className="flex min-h-screen flex-col">
            <Header />
            <main className="flex-1">
              <Routes>
                <Route path="/" element={<CatalogPage />} />
                <Route path="/product/:id" element={<ProductPage />} />
                <Route path="/cart" element={<CartPage />} />
                <Route path="/checkout" element={<CheckoutPage />} />
                <Route path="/checkout/success" element={<CheckoutSuccessPage />} />
              </Routes>
            </main>
            <Footer />
          </div>
        </CartProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}

export default App
