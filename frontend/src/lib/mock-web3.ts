const HEX = '0123456789abcdef'

function randomHex(length: number) {
  let out = ''
  for (let i = 0; i < length; i++) out += HEX[Math.floor(Math.random() * HEX.length)]
  return out
}

export function generateWalletAddress() {
  return `0x${randomHex(4)}...${randomHex(4)}`
}

export function generateTxHash() {
  return `0x${randomHex(64)}`
}

export function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}
