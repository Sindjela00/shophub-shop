# Shop

Web application (front-end + back-end) representing a single storefront instance. Deployed
dynamically per-tenant by the Shop operator, on request from ShopHub.

## Structure

```
shophub-shop/
├── backend/                     # ASP.NET Core Web API solution
│   ├── Shop.sln
│   ├── src/Shop.Api/
│   └── tests/
│       ├── Shop.Api.Tests/              # unit tests
│       └── Shop.Api.IntegrationTests/   # integration tests (Testcontainers)
├── frontend/                    # React (Vite + TypeScript) app
├── docker-compose.yml           # local dev / integration test infrastructure
└── .github/workflows/ci.yml     # CI pipeline (build, test, image publish)
```

## Responsibilities

- **Admin**: create, update, delete articles (name, stock count, price); list orders
- **Customer**: browse articles, manage cart, checkout
- **Payments**: crypto checkout on an EVM testnet (Sepolia) via a Web3 wallet (MetaMask), e.g. USDT

## Related repositories

- `shophub-app` — management app used to provision Shop instances
- `shophub-shop-operator` — Kubernetes operator providing the `Shop`, `DiscordChannel`, and `Wallet` CRDs
- `shophub-helm-charts` — Helm charts for ShopHub and the Shop operator
- `shophub-kube-state` — desired-state configuration for the Kubernetes cluster
