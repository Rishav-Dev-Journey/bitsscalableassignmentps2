# Payment API - System Architecture & Database Schema

## System Architecture Diagram

```mermaid
graph TB
    subgraph "External Access"
        Client[Client/Browser]
        Internet[Internet]
    end

    subgraph "Azure Load Balancer"
        LB[Azure LoadBalancer<br/>IP: 4.213.208.156]
        DNS[DNS: payment-api.4.187.130.156.nip.io]
    end

    subgraph "Azure Kubernetes Service - payment-aks-cluster"
        subgraph "NGINX Ingress Controller"
            Ingress[Ingress Controller<br/>nginx v1.9.4]
        end

        subgraph "Payment API - Deployment"
            API1[payment-api Pod 1<br/>Image: v1.3]
            API2[payment-api Pod 2<br/>Image: v1.3]
            APIN[payment-api Pod N<br/>Auto-scaled 2-10]
        end

        subgraph "HPA - Auto Scaling"
            HPA[Horizontal Pod Autoscaler<br/>CPU: 70% | Memory: 80%]
        end

        subgraph "SQL Server 2022"
            DB[(SQL Server Pod<br/>mssql-service:1433)]
            PVC[Persistent Volume<br/>8GB Premium SSD]
        end

        subgraph "Kubernetes Services"
            APISvc[payment-api-service<br/>Type: LoadBalancer<br/>Port: 80 → 8080]
            DBSvc[mssql-service<br/>Type: ClusterIP<br/>Port: 1433]
        end
    end

    subgraph "Azure Container Registry"
        ACR[demoimagecontainer.azurecr.io<br/>payment-api:latest, v1.0-v1.3]
    end

    Client -->|HTTP Request| Internet
    Internet -->|http://4.213.208.156| LB
    Internet -->|http://payment-api...nip.io| DNS
    DNS -->|Route| LB
    LB -->|Port 80| APISvc

    Ingress -.->|Optional Route| APISvc

    APISvc -->|Load Balance| API1
    APISvc -->|Load Balance| API2
    APISvc -->|Load Balance| APIN

    HPA -->|Scale Up/Down| API1
    HPA -->|Scale Up/Down| API2
    HPA -->|Scale Up/Down| APIN

    API1 -->|SQL Connection| DBSvc
    API2 -->|SQL Connection| DBSvc
    APIN -->|SQL Connection| DBSvc

    DBSvc -->|Internal| DB
    DB -->|Persist Data| PVC

    ACR -.->|Pull Images| API1
    ACR -.->|Pull Images| API2
    ACR -.->|Pull Images| APIN

    style Client fill:#e1f5ff
    style LB fill:#ff9800
    style DNS fill:#ff9800
    style API1 fill:#4caf50
    style API2 fill:#4caf50
    style APIN fill:#4caf50
    style DB fill:#2196f3
    style PVC fill:#9c27b0
    style HPA fill:#ffeb3b
    style ACR fill:#607d8b
```

## Application Component Architecture

```mermaid
graph LR
    subgraph "Client Layer"
        HTTP[HTTP Requests<br/>REST API]
    end

    subgraph "API Layer - ASP.NET Core"
        Controller[PaymentsController<br/>Route: /v1/payments]
        Health[Health Endpoint<br/>Route: /]
        Swagger[Swagger UI<br/>Route: /swagger]
    end

    subgraph "Business Logic Layer"
        PaymentSvc[PaymentService<br/>Process, Capture, Cancel]
        IdempotencySvc[IdempotencyService<br/>Duplicate Prevention]
    end

    subgraph "Data Access Layer"
        EF[Entity Framework Core<br/>DbContext]
        Models[Models<br/>Charge, IdempotencyRecord]
    end

    subgraph "Database"
        SQLServer[(SQL Server 2022<br/>PaymentDB)]
    end

    HTTP -->|POST /charge| Controller
    HTTP -->|GET /{id}| Controller
    HTTP -->|PATCH /capture| Controller
    HTTP -->|PATCH /cancel| Controller
    HTTP -->|GET /| Health
    HTTP -->|GET /swagger| Swagger

    Controller -->|Business Logic| PaymentSvc
    Controller -->|Check Idempotency| IdempotencySvc

    PaymentSvc -->|CRUD Operations| EF
    IdempotencySvc -->|Cache Check| EF

    EF -->|LINQ Queries| Models
    Models -->|SQL Commands| SQLServer

    style HTTP fill:#e1f5ff
    style Controller fill:#4caf50
    style Health fill:#8bc34a
    style Swagger fill:#8bc34a
    style PaymentSvc fill:#ff9800
    style IdempotencySvc fill:#ff9800
    style EF fill:#2196f3
    style Models fill:#9c27b0
    style SQLServer fill:#f44336
```

## Database Schema - Entity Relationship Diagram

```mermaid
erDiagram
    Charges ||--o{ IdempotencyRecords : "referenced by"

    Charges {
        UNIQUEIDENTIFIER Id PK "Primary Key, Auto-generated GUID"
        NVARCHAR_50 Status "pending, succeeded, captured, canceled"
        BIGINT Amount "Amount in cents (e.g., 2500 = $25.00)"
        NVARCHAR_10 Currency "USD, EUR, etc."
        NVARCHAR_MAX Description "Payment description"
        NVARCHAR_100 CustomerId "Customer identifier"
        DATETIME2 CreatedAt "Timestamp, default GETUTCDATE()"
        NVARCHAR_50 PaymentMethodType "card, bank_account, etc."
        NVARCHAR_4 CardLast4 "Last 4 digits of card"
    }

    IdempotencyRecords {
        INT Id PK "Primary Key, Identity(1,1)"
        NVARCHAR_255 IdempotencyKey UK "Unique index for duplicate detection"
        NVARCHAR_MAX Response "Cached JSON response"
        DATETIME2 CreatedAt "Timestamp, default GETUTCDATE()"
    }
```

## Payment Flow Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant LoadBalancer
    participant PaymentAPI
    participant IdempotencyService
    participant PaymentService
    participant Database

    Client->>LoadBalancer: POST /v1/payments/charge<br/>Idempotency-Key: xyz123
    LoadBalancer->>PaymentAPI: Forward Request

    PaymentAPI->>IdempotencyService: Check Idempotency Key
    IdempotencyService->>Database: SELECT * FROM IdempotencyRecords<br/>WHERE IdempotencyKey = 'xyz123'

    alt Key Exists (Duplicate Request)
        Database-->>IdempotencyService: Return Cached Response
        IdempotencyService-->>PaymentAPI: Return Existing Charge
        PaymentAPI-->>LoadBalancer: 200 OK (isIdempotent: true)
        LoadBalancer-->>Client: Same Charge Response
    else Key Not Found (New Request)
        Database-->>IdempotencyService: No Record Found
        IdempotencyService-->>PaymentAPI: Proceed with New Charge

        PaymentAPI->>PaymentService: ProcessChargeAsync(request)
        PaymentService->>Database: INSERT INTO Charges VALUES (...)
        Database-->>PaymentService: Charge Created

        PaymentService->>Database: INSERT INTO IdempotencyRecords<br/>VALUES (key, response, timestamp)
        Database-->>PaymentService: Idempotency Record Saved

        PaymentService-->>PaymentAPI: Return ChargeResponse
        PaymentAPI-->>LoadBalancer: 200 OK (isIdempotent: false)
        LoadBalancer-->>Client: New Charge Response
    end
```

## Payment Status State Machine

```mermaid
stateDiagram-v2
    [*] --> pending: POST /charge<br/>(capture: false)
    [*] --> succeeded: POST /charge<br/>(capture: true)

    pending --> captured: PATCH /{id}/capture<br/>(Success)
    pending --> canceled: PATCH /{id}/cancel

    succeeded --> [*]: Payment Complete
    captured --> [*]: Payment Complete
    canceled --> [*]: Payment Voided

    note right of pending
        Two-step authorization
        Awaiting manual capture
    end note

    note right of succeeded
        One-step payment
        Immediately captured
    end note

    note right of captured
        Previously pending
        Now captured
    end note

    note right of canceled
        Cannot be captured
        after cancellation
    end note
```

## Deployment Architecture - Kubernetes Resources

```mermaid
graph TB
    subgraph "Kubernetes Cluster"
        subgraph "Namespace: default"
            Deploy[Deployment<br/>payment-api<br/>replicas: 2]
            RS[ReplicaSet<br/>payment-api-5f5d546c56]
            Pod1[Pod: payment-api-xxx-thbz8]
            Pod2[Pod: payment-api-xxx-zch4d]

            Svc[Service<br/>payment-api-service<br/>Type: LoadBalancer]

            HPA2[HPA<br/>Min: 2, Max: 10<br/>CPU: 70%, Mem: 80%]

            DBDeploy[Deployment<br/>mssql-deployment<br/>replicas: 1]
            DBPod[Pod: mssql-xxx-fhpvs]
            DBSvc2[Service<br/>mssql-service<br/>Type: ClusterIP]

            PVC2[PersistentVolumeClaim<br/>mssql-data-claim<br/>8GB]
            PV[PersistentVolume<br/>Azure Disk - Premium SSD]

            Secret1[Secret<br/>payment-api-secret<br/>ConnectionStrings]
            Secret2[Secret<br/>mssql-secret<br/>SA_PASSWORD]

            Ingress2[Ingress<br/>payment-api-ingress<br/>nginx class]
        end
    end

    Deploy -->|Manages| RS
    RS -->|Creates| Pod1
    RS -->|Creates| Pod2

    Svc -->|Routes to| Pod1
    Svc -->|Routes to| Pod2

    HPA2 -->|Scales| Deploy

    Pod1 -.->|Reads| Secret1
    Pod2 -.->|Reads| Secret1

    DBDeploy -->|Creates| DBPod
    DBSvc2 -->|Routes to| DBPod
    DBPod -.->|Reads| Secret2
    DBPod -->|Mounts| PVC2
    PVC2 -->|Bound to| PV

    Ingress2 -.->|Routes| Svc

    Pod1 -->|Connects| DBSvc2
    Pod2 -->|Connects| DBSvc2

    style Deploy fill:#4caf50
    style Pod1 fill:#8bc34a
    style Pod2 fill:#8bc34a
    style Svc fill:#2196f3
    style HPA2 fill:#ffeb3b
    style DBPod fill:#ff9800
    style DBSvc2 fill:#ff5722
    style PVC2 fill:#9c27b0
    style PV fill:#673ab7
    style Secret1 fill:#f44336
    style Secret2 fill:#f44336
```

## Technology Stack Overview

```mermaid
mindmap
  root((Payment API))
    Backend
      .NET 8.0.121
      ASP.NET Core Web API
      Entity Framework Core 9.0.10
      C# 12
    Database
      SQL Server 2022
      Persistent Storage
      8GB Premium SSD
      In-Cluster Deployment
    Containerization
      Docker
      Multi-stage Build
      linux/amd64 Platform
      ACR Registry
    Orchestration
      Azure Kubernetes Service
      Horizontal Pod Autoscaler
      LoadBalancer Service
      NGINX Ingress
    Cloud Platform
      Microsoft Azure
      Central India Region
      Resource Group: Dev-Demo-App-Rg
      2 Nodes: Standard_B2s
    API Features
      REST Endpoints
      Idempotency Support
      Health Monitoring
      Auto-scaling
      Swagger Documentation
```

## Resource Allocation & Scaling

```mermaid
graph TD
    subgraph "Payment API Pods"
        A[Pod Resources]
        A1[Memory: 256Mi request]
        A2[Memory: 512Mi limit]
        A3[CPU: 250m request]
        A4[CPU: 500m limit]

        A --> A1
        A --> A2
        A --> A3
        A --> A4
    end

    subgraph "SQL Server Pod"
        B[Pod Resources]
        B1[Memory: 1Gi request]
        B2[Memory: 2Gi limit]
        B3[CPU: 500m request]
        B4[CPU: 1000m limit]

        B --> B1
        B --> B2
        B --> B3
        B --> B4
    end

    subgraph "Auto-scaling Rules"
        C[HPA Configuration]
        C1[Min Replicas: 2]
        C2[Max Replicas: 10]
        C3[CPU Target: 70%]
        C4[Memory Target: 80%]

        C --> C1
        C --> C2
        C --> C3
        C --> C4
    end

    C -.->|Monitors & Scales| A

    style A fill:#4caf50
    style B fill:#2196f3
    style C fill:#ffeb3b
```

## Data Flow - Create Payment

```mermaid
flowchart TD
    Start([Client Request]) --> CheckHeader{Idempotency-Key<br/>Present?}

    CheckHeader -->|No| ValidateReq[Validate Request Body]
    CheckHeader -->|Yes| CheckDB{Key Exists<br/>in Database?}

    CheckDB -->|Yes| ReturnCached[Return Cached Response<br/>isIdempotent: true]
    CheckDB -->|No| ValidateReq

    ValidateReq --> ValidReq{Valid?}
    ValidReq -->|No| Return400[Return 400 Bad Request]
    ValidReq -->|Yes| GenerateID[Generate GUID<br/>for Payment]

    GenerateID --> ProcessCard[Process Card Details<br/>Mask Card Number]
    ProcessCard --> CreateCharge[Create Charge Record<br/>in Database]

    CreateCharge --> CheckCapture{Capture Flag<br/>True?}
    CheckCapture -->|Yes| SetSucceeded[Status: succeeded]
    CheckCapture -->|No| SetPending[Status: pending]

    SetSucceeded --> SaveDB[Save to Charges Table]
    SetPending --> SaveDB

    SaveDB --> SaveIdempotency[Save Idempotency Record]
    SaveIdempotency --> Return200[Return 200 OK<br/>ChargeResponse]

    ReturnCached --> End([Response Sent])
    Return400 --> End
    Return200 --> End

    style Start fill:#e1f5ff
    style CheckHeader fill:#ffeb3b
    style CheckDB fill:#ffeb3b
    style ValidReq fill:#ffeb3b
    style CheckCapture fill:#ffeb3b
    style Return200 fill:#4caf50
    style Return400 fill:#f44336
    style End fill:#e1f5ff
```

---

## Summary

This architecture provides:

✅ **High Availability** - Multiple API pods with load balancing  
✅ **Auto-scaling** - Dynamic scaling based on load (2-10 pods)  
✅ **Persistent Storage** - 8GB Premium SSD for SQL Server  
✅ **Idempotency** - Duplicate request prevention  
✅ **Health Monitoring** - Liveness and readiness probes  
✅ **External Access** - LoadBalancer with public IP  
✅ **Security** - Kubernetes secrets for credentials  
✅ **Data Integrity** - ACID compliant SQL Server database

**Production URL:** `http://4.213.208.156`  
**Status:** ✅ Operational
