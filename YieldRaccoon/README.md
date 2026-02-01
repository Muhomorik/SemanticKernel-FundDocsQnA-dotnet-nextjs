# YieldRaccoon

A massively over-engineered fund price crawler that sneaks around financial websites like a raccoon rummaging through garbage bins at 3 AM.

We've taken the simple task of "check if number went up or down" and wrapped it in layers of DDD, CQRS, event sourcing, reactive programming, and enough design patterns to make a senior architect weep with joy (or horror - it's hard to tell).

Because why scrape a website with a simple script when you can architect a *solution* with Aggregates, Value Objects, and Domain Events? 🦝

*Disclaimer: No actual banks were named in the making of this README. We use generic terms like "fund provider" because lawyers exist and we'd like to stay employed. The over-engineering, however, is 100% real, deeply unnecessary, and very entertaining for those who appreciate watching a simple HTTP request transform into a saga of bounded contexts and eventual consistency.*

## Preview

![YieldRaccoon Screenshot](YieldRaccoon_screenshot.png)

## ⚠️ CRITICAL SECURITY REQUIREMENT

**NEVER USE ACTUAL BANK/FINANCIAL INSTITUTION NAMES IN CODE OR DOCUMENTATION**

- ❌ **FORBIDDEN:** Never write specific financial institution names (e.g., "Avanza", "Nordnet") in code, comments, XML docs, logs, or any text
- ✅ **ALLOWED:** Use generic terms: "fund provider", "financial data source", "fund platform", "data provider"
- ✅ **ALLOWED:** Use placeholders in URLs: `https://<fund-provider>.com/funds/{isin}`
- **Reason:** Legal compliance, neutrality, and avoiding brand-specific dependencies

**This is non-negotiable. Code review will reject any mentions of specific bank names.**

## Key Technologies

| Technology | Version | Purpose |
| ------------ | --------- | --------- |
| .NET | 9.0 | Framework |
| WPF | - | Desktop UI |
| Entity Framework Core | 9.0 | SQLite persistence |
| DevExpressMvvm | 24.1.6 | MVVM framework |
| Autofac | 9.0.0 | Dependency injection |
| Rx.NET | 6.1.0 | Reactive programming |
| MahApps.Metro | 2.4.11 | Modern UI toolkit |
| NLog | 6.0.7 | Logging |

## Project Structure

```plaintext
YieldRaccoon.sln
├── YieldRaccoon.Domain/              # Core business logic (no dependencies)
│   ├── Entities/                     # FundProfile, FundHistoryRecord
│   └── ValueObjects/                 # FundId, FundHistoryRecordId
│
├── YieldRaccoon.Application/         # Use-case orchestration
│   ├── DTOs/                         # FundDataDto
│   ├── Repositories/                 # IFundProfileRepository, IFundHistoryRepository
│   └── Services/                     # IFundIngestionService, ICrawlEventStore
│
├── YieldRaccoon.Infrastructure/      # Technical concerns
│   ├── Data/                         # EF Core DbContext, configurations
│   │   └── Repositories/             # EfCore* and InMemory* repository implementations
│   └── EventStore/                   # InMemoryCrawlEventStore
│
└── YieldRaccoon.Wpf/                 # WPF UI
    ├── ViewModels/                   # DevExpress MVVM ViewModels
    ├── Views/                        # XAML views
    └── Configuration/                # DatabaseOptions, YieldRaccoonOptions
```

## Database Persistence

Fund data persists to SQLite via EF Core. Configure in `appsettings.json`:

```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=YieldRaccoon.db"
  }
}
```

| Provider | Description |
| ---------- | ------------- |
| `InMemory` | Session-scoped cache only (default) |
| `SQLite` | Persistent local database |

**Default SQLite file location:**

- File name: `YieldRaccoon.db`
- Location: Same folder as the executable
  - Development: `YieldRaccoon.Wpf/bin/Debug/net9.0-windows/YieldRaccoon.db`
  - Published: Application installation folder

**Database Tables:**

| Table | Purpose |
| ------- | --------- |
| `FundProfiles` | Static fund data (name, fees, ESG scores) - keyed by ISIN |
| `FundHistoryRecords` | Time-series data (NAV, owners, ratings) - FK to FundProfiles |

## Repository Architecture

The application supports swappable repository implementations based on configuration.

```mermaid
flowchart TB
    subgraph External["External Data"]
        API[Fund Provider API]
    end

    subgraph Application["Application Layer"]
        DTO[FundDataDto]
        SVC[FundIngestionService]
        IRepo["IFundProfileRepository\nIFundHistoryRepository"]
    end

    subgraph Domain["Domain Layer"]
        FP[FundProfile]
        FHR[FundHistoryRecord]
    end

    subgraph Infrastructure["Infrastructure Layer"]
        Config{DatabaseOptions.Provider}

        subgraph InMem["InMemory Provider"]
            IMRepo["InMemoryFundProfileRepository\nInMemoryFundHistoryRepository"]
            Dict[(ConcurrentDictionary)]
        end

        subgraph SQLite["SQLite Provider"]
            EFRepo["EfCoreFundProfileRepository\nEfCoreFundHistoryRepository"]
            DB[(YieldRaccoon.db)]
        end
    end

    API --> DTO
    DTO --> SVC
    SVC -->|"Maps DTO → Entities"| FP
    SVC -->|"Maps DTO → Entities"| FHR
    SVC --> IRepo

    Config -->|"InMemory"| IMRepo
    Config -->|"SQLite"| EFRepo

    IRepo -.->|"Resolved by DI"| IMRepo
    IRepo -.->|"Resolved by DI"| EFRepo

    IMRepo --> Dict
    EFRepo --> DB
```

**Key points:**

- Repositories accept **domain entities** (`FundProfile`, `FundHistoryRecord`), not DTOs
- `FundIngestionService` maps DTOs to entities before calling repositories
- DI container resolves the correct implementation based on `DatabaseOptions.Provider`
- InMemory repositories use `ConcurrentDictionary` for thread-safe, session-scoped storage

## Automatic Pagination

Crawl sessions automatically load all funds by clicking "Show more" buttons on paginated lists.

```mermaid
sequenceDiagram
    participant User
    participant VM as ViewModel
    participant Orchestrator
    participant Ingestion as FundIngestionService
    participant Repo as Repository
    participant WebView2
    participant API as Fund API

    User->>VM: StartSessionCommand
    VM->>Orchestrator: Start crawl session
    loop Until all funds loaded
        Orchestrator->>Orchestrator: Wait (randomized delay)
        Orchestrator->>VM: LoadBatchRequested
        VM->>WebView2: Execute JS (click "Show more")
        WebView2->>API: HTTP request
        API-->>WebView2: JSON response (intercepted)
        WebView2-->>VM: OnFundDataReceived
        VM->>VM: Map to FundDataDto[]
        VM->>Orchestrator: NotifyBatchLoaded(funds)
        Orchestrator->>Ingestion: IngestBatch(funds)
        Ingestion->>Repo: AddOrUpdate(FundProfile)
        Ingestion->>Repo: Add(FundHistoryRecord)
        Repo-->>Ingestion: Persisted
        Ingestion-->>Orchestrator: Count
    end
    Orchestrator->>VM: SessionCompleted
```

**Commands:**

- `StartSessionCommand` - Begins automated crawl with randomized delays
- `LoadNextBatchCommand` - Manual single batch load
- `StopSessionCommand` - Cancel running session

**Features:** ISIN deduplication, randomized delays (20-60s), progress tracking.

## Domain Events

Events track crawl session lifecycle and batch loading progress.

```mermaid
stateDiagram-v2
    [*] --> CrawlSessionStarted
    CrawlSessionStarted --> BatchLoadScheduled

    state "Batch Cycle" as BC {
        BatchLoadDelayStarted --> BatchLoadDelayCompleted
        BatchLoadDelayCompleted --> BatchLoadStarted
        BatchLoadStarted --> BatchLoadCompleted
    }

    BatchLoadScheduled --> BC
    BC --> BatchLoadScheduled: More funds
    BC --> CrawlSessionCompleted: All loaded
    CrawlSessionCompleted --> DailyCrawlScheduled
```

| Category | Events |
| ---------- | -------- |
| Session | `Started`, `Completed`, `Failed`, `Cancelled` |
| Batch | `Scheduled`, `DelayStarted`, `DelayCompleted`, `Started`, `Completed`, `Failed` |
| Daily | `DailyCrawlScheduled`, `DailyCrawlReady` |

## Layer Responsibilities

| Layer | Purpose | Key Patterns |
| ------- | --------- | -------------- |
| Domain | Business logic, entities, value objects | Strongly-typed IDs, aggregates |
| Application | Use-case orchestration, interfaces | Repository pattern, DTOs |
| Infrastructure | EF Core, web scraping, event publishing | Rx.NET, SQLite |
| Presentation | WPF UI, ViewModels | DevExpress MVVM, Autofac |

## Build and Run

```bash
cd YieldRaccoon
dotnet build
dotnet run --project YieldRaccoon.Wpf
```

## Development Skills

Use these skills for implementation guidance:

| Skill | Use For |
| ------- | --------- |
| `/dotnet-domain-driven-design` | Domain entities, aggregates, value objects |
| `/dotnet-wpf-mvvm` | ViewModels, data binding, commands |
| `/dotnet-unit-testing-nunit` | NUnit tests with AutoFixture |

**Key Principles:**

- Strongly-typed IDs using `readonly record struct`
- Intent signals with `IObservable<T>`
- Layer separation (no UI dependencies in Domain/Application)
- ILogger as first constructor parameter
