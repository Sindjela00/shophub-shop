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

## Observability

- **Metrics** — Prometheus-format, scraped at `GET /metrics`: standard ASP.NET Core HTTP metrics
  (request counts by route/method/status code, duration histograms — covers 2xx/3xx/4xx/5xx and
  per-endpoint 404 breakdowns) plus custom traffic metrics (request/response byte volume,
  approximate daily unique visitors).
- **Tracing** — OpenTelemetry spans across incoming requests, outgoing HTTP calls (e.g. the
  Sepolia RPC calls in payment verification), and EF Core queries. Exported via OTLP if
  `Observability:OtlpEndpoint` is configured, otherwise printed to the console so tracing is
  visible without running a collector locally.
- **Logging** — structured JSON on stdout, correlated with the active trace/span id.

## Related repositories

- `shophub-app` — management app used to provision Shop instances
- `shophub-shop-operator` — Kubernetes operator providing the `Shop`, `DiscordChannel`, and `Wallet` CRDs
- `shophub-helm-charts` — Helm charts for ShopHub and the Shop operator
- `shophub-kube-state` — desired-state configuration for the Kubernetes cluster
