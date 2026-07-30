import { useCallback, useEffect, useState } from "react";
import {
  SEPOLIA_CHAIN,
  getAccounts,
  getChainId,
  isUserRejectionError,
  requestAccounts,
  requestAccountsPermission,
  switchToChain,
  type EthereumProvider,
} from "@/lib/ethereum";

type WalletStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "wrong-network"
  | "switching-network";

interface WalletState {
  status: WalletStatus;
  address: string | null;
  chainId: string | null;
  error: string | null;
}

function getProvider(): EthereumProvider | undefined {
  return typeof window !== "undefined" ? window.ethereum : undefined;
}

function statusFor(
  address: string | null,
  chainId: string | null,
): WalletStatus {
  if (!address) return "disconnected";
  return chainId === SEPOLIA_CHAIN.chainId ? "connected" : "wrong-network";
}

const initialState: WalletState = {
  status: "disconnected",
  address: null,
  chainId: null,
  error: null,
};

const NEXT_RELOAD_AT_KEY = "shophub-shop-mm-next-auto-reload";
const RELOAD_COOLDOWN_MS = 8000;

export function useMetaMask() {
  const [state, setState] = useState<WalletState>(initialState);
  const [isInstalled, setIsInstalled] = useState(() => Boolean(getProvider()));

  useEffect(() => {
    if (isInstalled) return;

    const pollInterval = setInterval(() => {
      if (getProvider()) setIsInstalled(true);
    }, 500);

    const tryAutoReload = () => {
      if (getProvider()) return;
      const now = Date.now();
      const nextAllowed = Number(
        sessionStorage.getItem(NEXT_RELOAD_AT_KEY) ?? 0,
      );
      if (now < nextAllowed) return;
      sessionStorage.setItem(
        NEXT_RELOAD_AT_KEY,
        String(now + RELOAD_COOLDOWN_MS),
      );
      window.location.reload();
    };

    const firstAttempt = setTimeout(tryAutoReload, 1500);
    const retryInterval = setInterval(tryAutoReload, RELOAD_COOLDOWN_MS);

    return () => {
      clearInterval(pollInterval);
      clearTimeout(firstAttempt);
      clearInterval(retryInterval);
    };
  }, [isInstalled]);

  useEffect(() => {
    const provider = getProvider();
    if (!provider) return;

    let cancelled = false;
    Promise.all([getAccounts(provider), getChainId(provider)]).then(
      ([accounts, chainId]) => {
        if (cancelled || accounts.length === 0) return;
        setState({
          status: statusFor(accounts[0], chainId),
          address: accounts[0],
          chainId,
          error: null,
        });
      },
    );

    const handleAccountsChanged = (...args: unknown[]) => {
      const accounts = args[0] as string[];
      const address = accounts[0] ?? null;
      setState((prev) => ({
        ...prev,
        address,
        status: statusFor(address, prev.chainId),
        error: null,
      }));
    };

    const handleChainChanged = (...args: unknown[]) => {
      const chainId = args[0] as string;
      setState((prev) => ({
        ...prev,
        chainId,
        status: statusFor(prev.address, chainId),
      }));
    };

    provider.on("accountsChanged", handleAccountsChanged);
    provider.on("chainChanged", handleChainChanged);

    return () => {
      cancelled = true;
      provider.removeListener("accountsChanged", handleAccountsChanged);
      provider.removeListener("chainChanged", handleChainChanged);
    };
  }, [isInstalled]);

  const connect = useCallback(async () => {
    const provider = getProvider();
    if (!provider) {
      setState((prev) => ({ ...prev, error: "MetaMask is not installed." }));
      return;
    }

    setState((prev) => ({ ...prev, status: "connecting", error: null }));

    try {
      const accounts = await requestAccounts(provider);
      let chainId = await getChainId(provider);

      if (chainId !== SEPOLIA_CHAIN.chainId) {
        await switchToChain(provider, SEPOLIA_CHAIN);
        chainId = await getChainId(provider);
      }

      const address = accounts[0] ?? null;
      setState({
        status: statusFor(address, chainId),
        address,
        chainId,
        error: null,
      });
    } catch (err) {
      setState((prev) => ({
        ...prev,
        status: prev.address ? prev.status : "disconnected",
        error: isUserRejectionError(err)
          ? "Connection request was rejected."
          : "Could not connect to MetaMask.",
      }));
    }
  }, []);

  const switchNetwork = useCallback(async () => {
    const provider = getProvider();
    if (!provider) return;

    setState((prev) => ({ ...prev, status: "switching-network", error: null }));

    try {
      await switchToChain(provider, SEPOLIA_CHAIN);
      const chainId = await getChainId(provider);
      setState((prev) => ({
        ...prev,
        chainId,
        status: statusFor(prev.address, chainId),
      }));
    } catch (err) {
      setState((prev) => ({
        ...prev,
        status: "wrong-network",
        error: isUserRejectionError(err)
          ? "Network switch was rejected."
          : "Could not switch network.",
      }));
    }
  }, []);

  const changeWallet = useCallback(async () => {
    const provider = getProvider();
    if (!provider) return;

    try {
      await requestAccountsPermission(provider);
      const accounts = await getAccounts(provider);
      const chainId = await getChainId(provider);
      const address = accounts[0] ?? null;
      setState({
        status: statusFor(address, chainId),
        address,
        chainId,
        error: null,
      });
    } catch (err) {
      setState((prev) => ({
        ...prev,
        error: isUserRejectionError(err)
          ? "Wallet switch was rejected."
          : "Could not switch wallet.",
      }));
    }
  }, []);

  return { ...state, isInstalled, connect, switchNetwork, changeWallet };
}
