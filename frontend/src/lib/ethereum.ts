export interface EthereumProvider {
  isMetaMask?: boolean
  request<T = unknown>(args: { method: string; params?: unknown[] | Record<string, unknown> }): Promise<T>
  on(event: string, handler: (...args: unknown[]) => void): void
  removeListener(event: string, handler: (...args: unknown[]) => void): void
}

declare global {
  interface Window {
    ethereum?: EthereumProvider
  }
}

export interface ChainConfig {
  chainId: string
  chainName: string
  nativeCurrency: { name: string; symbol: string; decimals: number }
  rpcUrls: string[]
  blockExplorerUrls: string[]
}

// Sepolia testnet — payments only need to work on a testnet per the project spec.
export const SEPOLIA_CHAIN: ChainConfig = {
  chainId: '0xaa36a7',
  chainName: 'Sepolia',
  nativeCurrency: { name: 'Sepolia ETH', symbol: 'ETH', decimals: 18 },
  rpcUrls: ['https://rpc.sepolia.org'],
  blockExplorerUrls: ['https://sepolia.etherscan.io'],
}

export function formatAddress(address: string) {
  return `${address.slice(0, 6)}...${address.slice(-4)}`
}

function errorCode(err: unknown): number | undefined {
  if (typeof err === 'object' && err !== null && 'code' in err) {
    return (err as { code: unknown }).code as number
  }
  return undefined
}

export function isUserRejectionError(err: unknown) {
  return errorCode(err) === 4001
}

function isChainNotFoundError(err: unknown) {
  return errorCode(err) === 4902
}

export function getAccounts(provider: EthereumProvider) {
  return provider.request<string[]>({ method: 'eth_accounts' })
}

export function requestAccounts(provider: EthereumProvider) {
  return provider.request<string[]>({ method: 'eth_requestAccounts' })
}

export function getChainId(provider: EthereumProvider) {
  return provider.request<string>({ method: 'eth_chainId' })
}

export async function switchToChain(provider: EthereumProvider, chain: ChainConfig) {
  try {
    await provider.request({
      method: 'wallet_switchEthereumChain',
      params: [{ chainId: chain.chainId }],
    })
  } catch (err) {
    // MetaMask doesn't know this chain yet — register it, then the switch above has already prompted once.
    if (isChainNotFoundError(err)) {
      await provider.request({ method: 'wallet_addEthereumChain', params: [chain] })
    } else {
      throw err
    }
  }
}
