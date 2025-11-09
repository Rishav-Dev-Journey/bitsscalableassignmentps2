# Payment API

A .NET 8 Web API for payment processing with idempotency support, SQL Server persistence, and MVC controller-based architecture.

## Features

- ✅ Create, retrieve, capture, and cancel payment charges
- ✅ Idempotency key support to prevent duplicate transactions
- ✅ SQL Server database with Entity Framework Core
- ✅ Clean MVC architecture (Controllers → Services → Data Layer)
- ✅ Swagger/OpenAPI documentation

## Quick Start

### Prerequisites

- .NET 8 SDK
- Docker (for SQL Server)

### Setup & Run

1. **Start SQL Server:**

   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd123" \
      -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. **Apply Database Migrations:**

   ```bash
   cd WebApiProject
   dotnet ef database update
   ```

3. **Run the API:**
   ```bash
   dotnet run
   ```
   API: `http://localhost:5281` | Swagger: `http://localhost:5281/swagger`

## API Endpoints (Base: `/v1/payments`)

| Method | Endpoint                | Description                                                 |
| ------ | ----------------------- | ----------------------------------------------------------- |
| POST   | `/charge`               | Create a payment charge (requires `Idempotency-Key` header) |
| GET    | `/{payment_id}`         | Retrieve payment details                                    |
| PATCH  | `/{payment_id}/capture` | Capture a pending payment                                   |
| PATCH  | `/{payment_id}/cancel`  | Cancel an uncaptured payment                                |

### Example: Create Charge

```bash
curl -X POST http://localhost:5281/v1/payments/charge \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-123" \
  -d '{
    "amount": 100.00,
    "currency": "USD",
    "description": "Test payment",
    "customerId": "cust_123",
    "capture": false
  }'
```

**Response:**

```json
{
  "id": "ch_abc123...",
  "status": "pending",
  "amount": 100.0,
  "currency": "USD",
  "createdAt": "2025-11-09T04:30:00.000Z"
}
```

## Architecture

```
Controllers/         # HTTP request handling (PaymentsController.cs)
    ↓
Services/           # Business logic (PaymentService, IdempotencyService)
    ↓
Data/               # EF Core DbContext + Database persistence
    ↓
Models/             # Entities (Charge, IdempotencyRecord) + DTOs
```

## Database

**Tables:**

- `Charges` - Payment records (Id, Status, Amount, Currency, CustomerId, CreatedAt)
- `IdempotencyRecords` - Cached responses for duplicate prevention

**Connection String** (`appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PaymentDB;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True;"
  }
}
```

## Payment Status Flow

```
pending → captured
  ↓
canceled
```

- **pending**: Created with `capture: false`, awaiting manual capture
- **succeeded**: Auto-captured (default behavior)
- **captured**: Manually captured from pending state
- **canceled**: Payment canceled (cannot capture after cancellation)

---

**Tech Stack:** .NET 8 | ASP.NET Core | Entity Framework Core | SQL Server | Swagger
