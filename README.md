# MarketCore

> A production-grade full-stack e-commerce platform built with **.NET 8 Clean Architecture** on the backend and **Angular 21** on the frontend — featuring real-time order tracking via **SignalR**, role-based administration, a full shopping cart + checkout flow, and a dark crimson design system.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=flat-square&logo=angular)
![EF Core](https://img.shields.io/badge/EF_Core-8-512BD4?style=flat-square&logo=microsoftsqlserver)
![SignalR](https://img.shields.io/badge/SignalR-10-00ADEF?style=flat-square&logo=microsoftazure)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat-square&logo=redis)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

---



## Key Features

### Storefront
- **Product catalogue** — paginated grid with live search, category filtering, and real product images
- **Product detail** — image showcase, stock indicator, reviews, Add to Cart
- **Shopping cart** — persisted server-side per user, item thumbnails, line totals
- **Checkout** — transactional order creation with stock decrement, shipping address capture
- **Order history** — list and detail views; orders update in real time without a page reload

### Real-Time (SignalR)
- **Live order status** — `OrderStatusChanged` event pushed to the customer the moment an admin updates the order
- **New order notification** — `NewOrderPlaced` event sent to the user's personal group on checkout
- **LIVE badge** on order detail page shows WebSocket connection state
- Auto-reconnect with exponential backoff (0 → 2s → 5s → 10s → 20s)

### Admin Panel (`/admin`)
- **Stats dashboard** — Total Products, Total Users, Total Orders, Total Revenue cards
- **Product management** — full CRUD, stock danger highlight (red when < 5 units)
- **Order management** — paginated order list with per-row status dropdown; status changes trigger instant SignalR push to the customer
- Protected by `adminGuard` + `[Authorize(Roles = "Admin")]` server-side

### Security & Infrastructure
- JWT Bearer authentication (HS256, 7-day expiry, role claims)
- PBKDF2 password hashing via ASP.NET Core Identity
- Rate limiting — 60 req/min global, 10 req/5s on auth endpoints
- API versioning (`/api/v1/`)
- Health check endpoint (`/health`) — SQL Server + Redis
- Serilog structured logging (console + rolling file)
- Docker Compose with dependency health gates

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                         MarketCore.Api                           │
│   Controllers  ·  Middleware  ·  Program.cs  ·  OrderHub (WS)   │
└───────────────────────────┬──────────────────────────────────────┘
                            │  IMediator.Send / Publish
┌───────────────────────────▼──────────────────────────────────────┐
│                     MarketCore.Application                       │
│   Commands · Queries · Handlers · FluentValidation · Behaviors   │
│   IUnitOfWork · ITokenProvider · ICacheService · IOrderHubService│
└───────────┬────────────────────────────────┬─────────────────────┘
            │ defines                        │ implements
┌───────────▼────────────┐    ┌──────────────▼──────────────────── ┐
│   MarketCore.Domain    │    │     MarketCore.Infrastructure      │
│   Entities (DDD AggRoots│   │   EF Core 8 · SQL Server · Redis   │
│   Value Objects        │    │   JWT · PBKDF2 · SignalR Hub       │
│   Domain Events        │    │   Repositories · UnitOfWork        │
│   Repository interfaces│    │   OrderHubService · DataSeeder     │
└────────────────────────┘    └────────────────────────────────────┘

Angular 21 (market-core-client/)
┌──────────────────────────────────────────────────────────────────┐
│  Standalone components · OnPush · Angular Signals · RxJS         │
│  Lazy-loaded routes · authGuard · adminGuard                     │
│  OrderHubService (@microsoft/signalr) · ToastService             │
│  AdminDashboardComponent · ProductList · Cart · Checkout         │
└──────────────────────────────────────────────────────────────────┘
```

### Layer dependency rules

| Layer | May reference |
|---|---|
| Domain | Nothing (zero external dependencies) |
| Application | Domain only |
| Infrastructure | Application + Domain |
| Api | Application (Infrastructure only via DI registration) |

---

## Technology Stack

| Concern | Technology |
|---|---|
| **Backend runtime** | .NET 8 / ASP.NET Core 8 |
| **ORM** | Entity Framework Core 8 (SQL Server provider) |
| **CQRS / Mediator** | MediatR 12 |
| **Validation** | FluentValidation 11 (MediatR pipeline behavior) |
| **Real-time** | ASP.NET Core SignalR + `@microsoft/signalr` 10 |
| **Caching** | StackExchange.Redis 2.8 |
| **Authentication** | JWT Bearer HS256 |
| **Password hashing** | ASP.NET Identity PasswordHasher (PBKDF2) |
| **Logging** | Serilog (console + rolling file) |
| **API docs** | Swagger / Swashbuckle |
| **API versioning** | Asp.Versioning.Mvc 8 |
| **Rate limiting** | ASP.NET Core built-in RateLimiter |
| **Health checks** | Microsoft.Extensions.Diagnostics.HealthChecks |
| **Frontend** | Angular 21 standalone components |
| **State management** | Angular Signals + RxJS |
| **Change detection** | OnPush throughout |
| **Fonts** | Google Fonts — Playfair Display + Inter |
| **Containerisation** | Docker + Docker Compose (SQL Server + Redis + API) |
| **Testing** | xUnit · FluentAssertions · NSubstitute · Testcontainers |

---

## Domain Model

```
User ──────────────┐
  │                │ owns
  │ places         ▼
  ▼              Cart ─── CartItem (ProductId, Qty, UnitPrice snapshot)
Order ─── OrderItem (ProductId, ProductName, Qty, UnitPrice snapshot)
  │
  │  OrderStatus state machine:
  │  Pending ──► Confirmed ──► Shipped ──► Delivered  (terminal)
  └──────────────────────────────────────► Cancelled  (terminal)

Product ─── Review
  │
  └── Category (optional 2-level hierarchy: root → subcategory)

Value Objects:  Money(Amount, Currency)  ·  Email  ·  Address
Domain Events:  OrderPlacedEvent  ·  StockDepletedEvent
```

---

## Quick Start

### Option A — Docker (one command)

```bash
git clone https://github.com/your-username/MarketCore.git
cd MarketCore

# Copy and configure the JWT key before starting
cp .env.example .env   # then set JWT__KEY to any 32+ char string

docker-compose up --build
```

| Service | URL |
|---|---|
| API | `http://localhost:8080` |
| Swagger UI | `http://localhost:8080/swagger` |
| Health check | `http://localhost:8080/health` |

> The `api` container waits for SQL Server and Redis to pass health checks before starting.
> Dev seed data (users, categories, products) loads automatically on first run.

**Frontend (separate terminal):**
```bash
cd market-core-client
npm install
npm start          # → http://localhost:4200
```

---

### Option B — Manual (LocalDB)

**1. Start Redis:**
```bash
docker run -d -p 6379:6379 redis:7-alpine
```

**2. Apply migrations and seed:**
```bash
cd MarketCore

dotnet ef database update \
  --project src/MarketCore.Infrastructure \
  --startup-project src/MarketCore.Api
```

**3. Run the API:**
```bash
dotnet run --project src/MarketCore.Api
# HTTPS → https://localhost:5001  |  HTTP → http://localhost:5000
```

**4. Run the Angular dev server:**
```bash
cd market-core-client
npm install
npm start          # → http://localhost:4200
```

---

## Environment Variables

| Variable | Description | Default (Docker) |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | `sqlserver` container |
| `ConnectionStrings__Redis` | Redis connection string | `redis:6379` |
| `Jwt__Key` | HS256 signing key — **min 32 chars, keep secret** | ⚠️ Must be set |
| `Jwt__Issuer` | JWT issuer claim | `MarketCore` |
| `Jwt__Audience` | JWT audience claim | `MarketCoreClients` |
| `Jwt__ExpiryDays` | Token lifetime in days | `7` |
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger + seed data | `Production` |

---

## Seed Accounts (Development)

All accounts use password: **`Password123!`**

| Email | Role | Name |
|---|---|---|
| `admin@marketcore.dev` | **Admin** | Adam Admin |
| `seller1@marketcore.dev` | Customer | Sarah Seller |
| `seller2@marketcore.dev` | Customer | Mark Merchant |
| `buyer1@marketcore.dev` | Customer | Alice Buyer |
| `buyer2@marketcore.dev` | Customer | Bob Customer |
| `buyer3@marketcore.dev` | Customer | Carol Shopper |

> Log in as `admin@marketcore.dev` to access the Admin Dashboard at `/admin`.

---

## API Reference

All endpoints are prefixed with `/api/v1/`. The full interactive spec is at `/swagger` (Development only).

### Authentication

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | — | Register new Customer account, returns JWT |
| POST | `/auth/login` | — | Authenticate, returns JWT + expiry |

### Products

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/products` | — | Paginated list — `?search=&categoryId=&page=&pageSize=` |
| GET | `/products/{id}` | — | Full detail with category + reviews |
| POST | `/products` | Admin | Create product (name, price, stock, imageUrl, categoryId) |
| PUT | `/products/{id}` | Admin | Update product details |
| DELETE | `/products/{id}` | Admin | Delete product |

### Categories

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/categories` | — | All categories with product counts |
| POST | `/categories` | Admin | Create category (optional parentCategoryId) |
| PUT | `/categories/{id}` | Admin | Rename / re-describe |
| DELETE | `/categories/{id}` | Admin | Delete (blocked if products assigned) |

### Cart

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/cart` | User | Fetch cart (auto-created on first request) |
| POST | `/cart/items` | User | Add item `{ productId, quantity }` |
| DELETE | `/cart/items/{productId}` | User | Remove a line item |
| DELETE | `/cart` | User | Clear all items |

### Orders

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/orders/checkout` | User | Transactional checkout → creates order, decrements stock |
| GET | `/orders` | User | All user orders, newest first |
| GET | `/orders/{id}` | User | Full order with items + status |
| PUT | `/orders/{id}/cancel` | User | Cancel Pending/Confirmed order |
| PUT | `/orders/{id}/status` | Admin | Advance status (used by Admin panel) |

### Reviews

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/products/{productId}/reviews` | — | All reviews for a product |
| POST | `/products/{productId}/reviews` | User | Submit review `{ rating: 1-5, comment? }` |

### Admin

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/admin/stats` | Admin | `{ totalProducts, totalUsers, totalOrders, totalRevenue }` |
| GET | `/admin/products` | Admin | Full product list (up to 50, no cache) |
| GET | `/admin/orders` | Admin | Paginated recent orders |
| PATCH | `/admin/orders/{id}/status` | Admin | Update order status + push SignalR event |

---

## Real-Time (SignalR)

**Hub URL:** `wss://{host}/hubs/orders`
**Authentication:** JWT passed as `?access_token=<token>` query string on the WebSocket handshake.

### Groups

| Group key | Joined when | Receives |
|---|---|---|
| `user-{userId}` | On connect (automatic) | `NewOrderPlaced`, `OrderStatusChanged` for all user orders |
| `order-{orderId}` | Client calls `JoinOrderGroup(id)` | `OrderStatusChanged` for that specific order |

### Events (server → client)

```typescript
// OrderStatusChanged
{
  orderId: string;
  newStatus: "Pending" | "Confirmed" | "Shipped" | "Delivered" | "Cancelled";
  updatedAt: string;   // ISO 8601
}

// NewOrderPlaced
{
  orderId: string;
  totalAmount: number;
}
```

### Client example (Angular)

```typescript
// OrderHubService wraps the connection
this.orderHubService.startConnection();
this.orderHubService.joinOrderGroup(orderId);

this.orderHubService.onOrderStatusChanged()
  .pipe(filter(e => e.orderId === this.orderId))
  .subscribe(e => this.status.set(e.newStatus));
```

---

## Project Structure

```
MarketCore/
├── src/
│   ├── MarketCore.Api/
│   │   ├── Controllers/          # Auth, Products, Categories, Cart, Orders, Reviews, Admin
│   │   ├── Middleware/           # GlobalException, RequestLogging
│   │   └── Program.cs
│   ├── MarketCore.Application/
│   │   ├── Features/
│   │   │   ├── Admin/            # GetAdminStats, GetAdminOrders
│   │   │   ├── Auth/             # Register, Login
│   │   │   ├── Cart/             # AddToCart, RemoveFromCart, ClearCart, GetCart
│   │   │   ├── Categories/       # CRUD
│   │   │   ├── Orders/           # Checkout, CancelOrder, UpdateOrderStatus, GetUserOrders, GetOrderById
│   │   │   ├── Products/         # CRUD, GetProducts, GetProductById
│   │   │   └── Reviews/          # CreateReview, GetReviews
│   │   ├── Interfaces/           # IUnitOfWork, ITokenProvider, ICacheService, IOrderHubService
│   │   └── Behaviors/            # ValidationBehavior, CachingBehavior, LoggingBehavior
│   ├── MarketCore.Domain/
│   │   ├── Entities/             # Product, Category, User, Order, OrderItem, Cart, CartItem, Review
│   │   ├── ValueObjects/         # Money, Email, Address
│   │   ├── Events/               # OrderPlacedEvent, StockDepletedEvent
│   │   └── Repositories/         # IRepository<T>, IProductRepository, IOrderRepository, …
│   └── MarketCore.Infrastructure/
│       ├── Hubs/                 # OrderHub (SignalR)
│       ├── Persistence/          # ApplicationDbContext, DataSeeder, EF configs, Migrations
│       ├── Repositories/         # Repository<T>, ProductRepository, OrderRepository, …
│       └── Services/             # TokenService, OrderHubService, CacheService
├── market-core-client/           # Angular 21 frontend
│   └── src/app/
│       ├── core/
│       │   ├── guards/           # authGuard, adminGuard
│       │   ├── hubs/             # OrderHubService
│       │   ├── models/           # TypeScript interfaces
│       │   └── services/         # AuthService, ProductService, CartService, OrderService, AdminService
│       ├── features/
│       │   ├── admin/            # AdminDashboardComponent (stats + products + orders tabs)
│       │   ├── auth/             # Login, Register
│       │   ├── cart/             # CartComponent
│       │   ├── checkout/         # CheckoutComponent
│       │   ├── home/             # HomeComponent (hero, categories, products)
│       │   ├── not-found/        # 404 page
│       │   ├── orders/           # OrderList, OrderDetail (live status badge)
│       │   └── products/         # ProductList, ProductDetail, CreateProduct
│       └── shared/
│           └── components/       # Navbar, LoadingSpinner, Toast
├── tests/
│   └── MarketCore.Tests/         # xUnit unit + integration tests
├── Dockerfile                    # Multi-stage .NET 8 build
└── docker-compose.yml            # SQL Server 2022 + Redis 7 + API
```

---

## Development Notes

- **Adding a migration:**
  ```bash
  dotnet ef migrations add <Name> \
    --project src/MarketCore.Infrastructure \
    --startup-project src/MarketCore.Api
  ```
- **Seeding:** `DataSeeder` runs automatically in `Development`. It is idempotent — re-running is safe.
- **Multi-currency:** The `Money` value object supports any ISO 4217 currency code. Mixed-currency arithmetic is intentionally guarded at the domain level.
- **Rate limiting:** Global — 60 req/min per IP. Auth endpoints — 10 req/5s per IP (sliding window, queue depth 5).
- **Caching:** `GET /products` and `GET /categories` responses are Redis-cached. Admin reads bypass the cache.
- **Email:** `EmailService` logs instead of sending. Swap `IEmailService` in `AddInfrastructure` to integrate SendGrid, AWS SES, or SMTP.

---

## License

MIT © 2025
