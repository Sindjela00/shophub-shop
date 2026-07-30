import {
  AlertTriangle,
  ArrowLeft,
  Check,
  ExternalLink,
  Loader2,
  ShieldCheck,
  Wallet,
} from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { SEPOLIA_CHAIN, formatAddress } from "@/lib/ethereum";
import { delay, generateTxHash } from "@/lib/mock-web3";

import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { useCart } from "@/context/cart-context";
import { useMetaMask } from "@/hooks/use-metamask";
import { useState } from "react";

type Step = "review" | "wallet" | "pay";
type PaymentPhase = "idle" | "pending" | "confirming";

const STEPS: { key: Step; label: string }[] = [
  { key: "review", label: "Review" },
  { key: "wallet", label: "Wallet" },
  { key: "pay", label: "Payment" },
];

const STEP_STORAGE_KEY = "shophub-shop-checkout-step";

function loadInitialStep(): Step {
  const raw = sessionStorage.getItem(STEP_STORAGE_KEY);
  return raw === "wallet" || raw === "pay" ? raw : "review";
}

export function CheckoutPage() {
  const { items, total, clear } = useCart();
  const navigate = useNavigate();
  const wallet = useMetaMask();

  const [order] = useState({ items, total });

  const [step, setStepState] = useState<Step>(loadInitialStep);
  const [paymentPhase, setPaymentPhase] = useState<PaymentPhase>("idle");

  const setStep = (next: Step) => {
    setStepState(next);
    sessionStorage.setItem(STEP_STORAGE_KEY, next);
  };

  if (order.items.length === 0) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-20 text-center">
        <p className="text-lg font-medium">You have no items to pay for.</p>
        <Link to="/" className="mt-4 inline-block">
          <Button variant="outline">
            <ArrowLeft className="h-4 w-4" />
            Back to catalog
          </Button>
        </Link>
      </div>
    );
  }

  const currentIndex = STEPS.findIndex((s) => s.key === step);
  // Mid-transaction, don't let the user leave or jump between steps.
  const canNavigate = paymentPhase === "idle";

  const handleConfirmPayment = async () => {
    setPaymentPhase("pending");
    await delay(1000);
    setPaymentPhase("confirming");
    await delay(1500);

    const txHash = generateTxHash();
    clear();
    sessionStorage.removeItem(STEP_STORAGE_KEY);
    navigate("/checkout/success", {
      state: {
        items: order.items,
        total: order.total,
        txHash,
        wallet: wallet.address,
      },
    });
  };

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      {canNavigate && (
        <Link to="/" className="-ml-2 mb-3 inline-block">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4" />
            Back
          </Button>
        </Link>
      )}
      <h1 className="mb-6 text-2xl font-semibold">Pay with crypto</h1>

      <ol className="mb-8 flex items-center">
        {STEPS.map((s, i) => {
          const reachable = i < currentIndex && canNavigate;
          return (
            <li key={s.key} className="flex flex-1 items-center last:flex-none">
              <button
                type="button"
                onClick={reachable ? () => setStep(s.key) : undefined}
                disabled={!reachable}
                className={cn(
                  "group flex flex-col items-center gap-1.5 disabled:pointer-events-none",
                  reachable && "cursor-pointer",
                )}
              >
                <span
                  className={cn(
                    "flex h-8 w-8 items-center justify-center rounded-full border text-sm font-medium transition-colors",
                    i < currentIndex &&
                      "border-brand-600 bg-brand-600 text-white",
                    i === currentIndex &&
                      "border-brand-600 text-brand-600 dark:text-brand-400",
                    i > currentIndex &&
                      "border-neutral-300 text-neutral-400 dark:border-neutral-700",
                    reachable && "group-hover:opacity-80",
                  )}
                >
                  {i < currentIndex ? <Check className="h-4 w-4" /> : i + 1}
                </span>
                <span className="text-xs text-neutral-500 dark:text-neutral-400">
                  {s.label}
                </span>
              </button>
              {i < STEPS.length - 1 && (
                <div
                  className={cn(
                    "mx-2 h-px flex-1",
                    i < currentIndex
                      ? "bg-brand-600"
                      : "bg-neutral-200 dark:bg-neutral-800",
                  )}
                />
              )}
            </li>
          );
        })}
      </ol>

      {step === "review" && (
        <Card className="flex flex-col gap-4 p-5">
          <h2 className="font-medium">Order items</h2>
          <div className="flex flex-col gap-3">
            {order.items.map(({ product, quantity }) => (
              <div
                key={product.id}
                className="flex items-center justify-between text-sm"
              >
                <span>
                  {product.name}{" "}
                  <span className="text-neutral-400">× {quantity}</span>
                </span>
                <span className="font-medium">
                  {product.price * quantity} USDT
                </span>
              </div>
            ))}
          </div>
          <div className="flex items-center justify-between border-t border-neutral-200 pt-3 text-base font-semibold dark:border-neutral-800">
            <span>Total</span>
            <span>{order.total} USDT</span>
          </div>
          <Button size="lg" onClick={() => setStep("wallet")}>
            Continue
          </Button>
        </Card>
      )}

      {step === "wallet" && (
        <Card className="flex flex-col items-center gap-4 p-8 text-center">
          <span className="flex h-14 w-14 items-center justify-center rounded-full bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300">
            <Wallet className="h-7 w-7" />
          </span>

          {!wallet.isInstalled && (
            <>
              <div>
                <h2 className="font-medium">MetaMask not detected</h2>
                <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
                  Install the MetaMask browser extension to pay with crypto.
                </p>
              </div>
              <a
                href="https://metamask.io/download/"
                target="_blank"
                rel="noreferrer"
              >
                <Button size="lg" variant="outline">
                  <ExternalLink className="h-4 w-4" />
                  Install MetaMask
                </Button>
              </a>
              <p className="flex items-center gap-1.5 text-xs text-neutral-400 dark:text-neutral-500">
                <Loader2 className="h-3 w-3 animate-spin" />
                Waiting for MetaMask to become available...
              </p>
            </>
          )}

          {wallet.isInstalled &&
            (wallet.status === "disconnected" ||
              wallet.status === "connecting") && (
              <>
                <div>
                  <h2 className="font-medium">Connect your Web3 wallet</h2>
                  <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
                    Connect MetaMask on the {SEPOLIA_CHAIN.chainName} testnet
                  </p>
                </div>
                <Button
                  size="lg"
                  onClick={wallet.connect}
                  disabled={wallet.status === "connecting"}
                >
                  {wallet.status === "connecting" && (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  )}
                  {wallet.status === "connecting"
                    ? "Connecting..."
                    : "Connect wallet"}
                </Button>
                {wallet.error && (
                  <p className="text-sm text-red-600 dark:text-red-400">
                    {wallet.error}
                  </p>
                )}
              </>
            )}

          {(wallet.status === "wrong-network" ||
            wallet.status === "switching-network") && (
            <>
              <AlertTriangle className="h-6 w-6 text-amber-500" />
              <div>
                <h2 className="font-medium">Wrong network</h2>
                <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
                  Switch MetaMask to the {SEPOLIA_CHAIN.chainName} testnet to
                  continue.
                </p>
              </div>
              <Button
                size="lg"
                onClick={wallet.switchNetwork}
                disabled={wallet.status === "switching-network"}
              >
                {wallet.status === "switching-network" && (
                  <Loader2 className="h-4 w-4 animate-spin" />
                )}
                {wallet.status === "switching-network"
                  ? "Switching..."
                  : `Switch to ${SEPOLIA_CHAIN.chainName}`}
              </Button>
              {wallet.error && (
                <p className="text-sm text-red-600 dark:text-red-400">
                  {wallet.error}
                </p>
              )}
            </>
          )}

          {wallet.status === "connected" && wallet.address && (
            <>
              <div>
                <h2 className="font-medium">Wallet connected</h2>
                <p className="mt-1 font-mono text-sm text-neutral-500 dark:text-neutral-400">
                  {formatAddress(wallet.address)}
                </p>
              </div>
              <Button size="lg" onClick={() => setStep("pay")}>
                Pay
              </Button>
              <Button variant="ghost" size="sm" onClick={wallet.changeWallet}>
                Change wallet
              </Button>
              {wallet.error && (
                <p className="text-sm text-red-600 dark:text-red-400">
                  {wallet.error}
                </p>
              )}
            </>
          )}
        </Card>
      )}

      {step === "pay" && (
        <Card className="flex flex-col items-center gap-4 p-8 text-center">
          <span className="flex h-14 w-14 items-center justify-center rounded-full bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300">
            <ShieldCheck className="h-7 w-7" />
          </span>

          {paymentPhase === "idle" && (
            <>
              <div>
                <h2 className="font-medium">Confirm payment</h2>
                <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
                  You are sending{" "}
                  <span className="font-semibold">{order.total} USDT</span> from
                  wallet{" "}
                  <span className="font-mono">
                    {wallet.address && formatAddress(wallet.address)}
                  </span>
                </p>
              </div>
              <Button size="lg" onClick={handleConfirmPayment}>
                Confirm payment
              </Button>
            </>
          )}

          {paymentPhase === "pending" && (
            <div className="flex flex-col items-center gap-2 py-4">
              <Loader2 className="h-6 w-6 animate-spin text-brand-600 dark:text-brand-400" />
              <p className="text-sm text-neutral-500 dark:text-neutral-400">
                Transaction sent, awaiting confirmation...
              </p>
            </div>
          )}

          {paymentPhase === "confirming" && (
            <div className="flex flex-col items-center gap-2 py-4">
              <Loader2 className="h-6 w-6 animate-spin text-brand-600 dark:text-brand-400" />
              <p className="text-sm text-neutral-500 dark:text-neutral-400">
                Confirming on the blockchain...
              </p>
            </div>
          )}
        </Card>
      )}
    </div>
  );
}
