import type { Order } from './types'

export const orders: Order[] = [
  {
    id: 'o1',
    walletAddress: '0x2a92c1b4f6a7e8d9c0b1a2f3e4d5c6b7a8908c1b',
    txHash: '0xcd543ecd89272e422f8220017875f8a11ca8f61e4246345377d1c542c75a1b2',
    total: 89,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 6).toISOString(),
    items: [{ articleId: 'a1', articleName: 'Aurora Windbreaker', unitPrice: 89, quantity: 1 }],
  },
  {
    id: 'o2',
    walletAddress: '0x7a3fbb1c2d3e4f5061728394a5b6c7d8e9f0a1b2',
    txHash: '0xab12cd34ef56ab78cd90ef12ab34cd56ef78ab90cd12ef34ab56cd78ef90ab12',
    total: 131,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 26).toISOString(),
    items: [
      { articleId: 'a2', articleName: 'Essential Tee', unitPrice: 19, quantity: 1 },
      { articleId: 'a3', articleName: 'Runner X Sneakers', unitPrice: 112, quantity: 1 },
    ],
  },
]
