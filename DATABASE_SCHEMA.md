# PaymentDB - Database Schema Diagram

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         Charges                             │
├─────────────────────────────────────────────────────────────┤
│ PK  Id                    nvarchar(450)                     │
│     Status                nvarchar(50)                      │
│     Amount                decimal(18,2)                     │
│     Currency              nvarchar(3)                       │
│     Description           nvarchar(max)                     │
│     CreatedAt             datetime2                         │
│     CustomerId            nvarchar(450)       [Indexed]     │
│     PaymentMethodType     nvarchar(max)                     │
│     CardLast4             nvarchar(4)                       │
└─────────────────────────────────────────────────────────────┘
                                │
                                │ 1
                                │
                                │ Referenced by
                                │
                                │ *
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                   IdempotencyRecords                        │
├─────────────────────────────────────────────────────────────┤
│ PK  IdempotencyKey        nvarchar(450)                     │
│ FK  ChargeId              nvarchar(450)                     │
│     ResponseData          nvarchar(max)                     │
│     CreatedAt             datetime2                         │
└─────────────────────────────────────────────────────────────┘
```

## Table Details

### **Charges Table**

Stores all payment charge transactions

| Column Name         | Data Type     | Constraints       | Description                                            |
| ------------------- | ------------- | ----------------- | ------------------------------------------------------ |
| `Id`                | nvarchar(450) | PRIMARY KEY       | Unique charge identifier (format: ch\_{guid})          |
| `Status`            | nvarchar(50)  | NOT NULL          | Payment status: pending, succeeded, captured, canceled |
| `Amount`            | decimal(18,2) | NOT NULL          | Payment amount                                         |
| `Currency`          | nvarchar(3)   | NOT NULL          | Currency code (e.g., USD, EUR)                         |
| `Description`       | nvarchar(max) | NULL              | Payment description                                    |
| `CreatedAt`         | datetime2     | NOT NULL, INDEXED | Timestamp when charge was created                      |
| `CustomerId`        | nvarchar(450) | NULL, INDEXED     | Customer identifier                                    |
| `PaymentMethodType` | nvarchar(max) | NULL              | Payment method type (e.g., card)                       |
| `CardLast4`         | nvarchar(4)   | NULL              | Last 4 digits of card number                           |

**Indexes:**

- `IX_Charges_CreatedAt` - For date-based queries
- `IX_Charges_CustomerId` - For customer lookup queries

---

### **IdempotencyRecords Table**

Stores cached responses to prevent duplicate charges

| Column Name      | Data Type     | Constraints | Description                                  |
| ---------------- | ------------- | ----------- | -------------------------------------------- |
| `IdempotencyKey` | nvarchar(450) | PRIMARY KEY | Unique idempotency key from request header   |
| `ChargeId`       | nvarchar(450) | NULL        | Associated charge ID (references Charges.Id) |
| `ResponseData`   | nvarchar(max) | NULL        | Cached JSON response data                    |
| `CreatedAt`      | datetime2     | NOT NULL    | Timestamp when record was created            |

**Note:** Currently using in-memory cache for idempotency, so this table may be empty.

---

## Status Flow Diagram

```
┌─────────────┐
│   pending   │ ◄── Created with capture: false
└──────┬──────┘
       │
       │ PATCH /capture
       ▼
┌─────────────┐
│  captured   │ ◄── Payment successfully captured
└─────────────┘

       OR

┌─────────────┐
│  succeeded  │ ◄── Created with capture: true (default)
└─────────────┘

       OR

┌─────────────┐
│  canceled   │ ◄── PATCH /cancel (only from pending/succeeded)
└─────────────┘
```

## Payment State Transitions

| From Status | To Status | Action         | Allowed                                 |
| ----------- | --------- | -------------- | --------------------------------------- |
| pending     | captured  | PATCH /capture | ✅ Yes                                  |
| pending     | canceled  | PATCH /cancel  | ✅ Yes                                  |
| succeeded   | captured  | PATCH /capture | ✅ Yes                                  |
| succeeded   | canceled  | PATCH /cancel  | ✅ Yes                                  |
| captured    | canceled  | PATCH /cancel  | ❌ No - Cannot cancel captured payment  |
| canceled    | captured  | PATCH /capture | ❌ No - Cannot capture canceled payment |
| canceled    | canceled  | PATCH /cancel  | ❌ No - Already canceled                |
| captured    | captured  | PATCH /capture | ❌ No - Already captured                |

---

## Sample Data Structure

### Charges Table Example:

```sql
Id: ch_54846754b9024dd294bbbbb5
Status: succeeded
Amount: 50.00
Currency: USD
Description: Coffee subscription - Monthly
CreatedAt: 2025-11-09 04:32:18
CustomerId: cus_001
PaymentMethodType: card
CardLast4: 4242
```

### IdempotencyRecords Table Example:

```sql
IdempotencyKey: unique-key-001
ChargeId: ch_54846754b9024dd294bbbbb5
ResponseData: {"id":"ch_54846754b9024dd294bbbbb5","status":"succeeded",...}
CreatedAt: 2025-11-09 04:32:18
```

---

## Connection String

```
Server=localhost,1433;
Database=PaymentDB;
User Id=sa;
Password=YourStrong@Passw0rd123;
TrustServerCertificate=True;
```

---

**Last Updated:** November 9, 2025  
**Database:** PaymentDB  
**Server:** SQL Server 2022 (Docker)
