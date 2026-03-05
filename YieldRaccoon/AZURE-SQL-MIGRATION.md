# Azure SQL Database Migration

Plan for hosting YieldRaccoon fund data on Azure SQL Database so the Backend API can serve it.

## Why Azure SQL Database

- **Free tier** — 100,000 vCore seconds/month, 32 GB storage, lifetime of subscription
- **Native vector search** — `VECTOR(1536)` type + `VECTOR_DISTANCE` for RAG embeddings (future Cosmos DB replacement)
- **Single database** — fund data now, RAG embeddings later
- **Managed Identity** — passwordless auth from App Service via `Active Directory Default`
- **Microsoft Entra-only auth** — zero SQL passwords, contained database users

## Importing Existing SQLite Data

Three options for the initial migration of existing data from `YieldRaccoon.db` to Azure SQL Database.

### Option 1: CSV + bcp (Quick and Dirty)

Export from SQLite to CSV, bulk import to Azure SQL. Good for a one-time migration.

```bash
# Export from SQLite
sqlite3 YieldRaccoon.db ".mode csv" ".headers on" \
  ".output FundProfiles.csv" "SELECT * FROM FundProfiles;"

sqlite3 YieldRaccoon.db ".mode csv" ".headers on" \
  ".output FundHistoryRecords.csv" "SELECT * FROM FundHistoryRecords;"

# Import to Azure SQL
bcp FundProfiles in FundProfiles.csv -S <server>.database.windows.net \
  -d <database> -U <user> -P <password> -c -t "," -F 2

bcp FundHistoryRecords in FundHistoryRecords.csv -S <server>.database.windows.net \
  -d <database> -U <user> -P <password> -c -t "," -F 2
```

**Pros:** Simple, no code changes.
**Cons:** Manual, need to handle type mappings, not repeatable.

### Option 2: EF Core Migration Tool (Recommended for Initial Import)

A small console app/command that reads from SQLite and writes to Azure SQL using existing entity models. Both `DbContext` configurations already exist (or will exist after DualWrite implementation).

```csharp
// Read all data from SQLite
using var sqliteContext = new YieldRaccoonDbContext(sqliteOptions);
var profiles = await sqliteContext.FundProfiles.AsNoTracking().ToListAsync();
var history = await sqliteContext.FundHistoryRecords.AsNoTracking().ToListAsync();

// Write to Azure SQL
using var azureSqlContext = new YieldRaccoonDbContext(azureSqlOptions);
await azureSqlContext.FundProfiles.AddRangeAsync(profiles);
await azureSqlContext.FundHistoryRecords.AddRangeAsync(history);
await azureSqlContext.SaveChangesAsync();
```

**Pros:** Type-safe, uses existing EF Core models, handles schema differences automatically.
**Cons:** Requires the AzureSql DbContext to be implemented first.

### Option 3: SqlPackage BACPAC (Overkill)

Export SQLite to an intermediate SQL Server LocalDB, then export as `.bacpac` and import to Azure SQL via portal.

**Pros:** Official Microsoft migration path.
**Cons:** Requires intermediate SQL Server instance, complex for a simple schema.

**Recommendation:** Use Option 2 after DualWrite is implemented. For a quick test before that, Option 1 works.

## Architecture

**Key principle:** YieldRaccoon does NOT connect to Azure SQL directly. All cloud data flows through the Backend API.

```plaintext
YieldRaccoon (WPF) ──[HTTP/API]──► Backend API ──[Managed Identity]──► Azure SQL Database
       │                                                                      │
       └── SQLite (local)                                     Fund data tables (cloud)
```

### Why This Architecture?

- **Single point of access** — Backend API is the only service with database credentials
- **Simpler security** — only one managed identity to configure, one firewall rule
- **API reuse** — same endpoints serve both YieldRaccoon sync and future frontends
- **No SQL driver in WPF** — YieldRaccoon stays lightweight, no `Microsoft.Data.SqlClient` dependency

### Data Flow

```plaintext
YieldRaccoon crawl session
  │
  ├── Crawl fund data → SQLite (local, always available)
  │
  └── Sync to cloud (new feature, planned)
        └── HTTP POST/PUT → Backend API → Azure SQL
```

### What Changes (Planned)

| Component | Change |
| --- | --- |
| **Backend API** | Add fund data endpoints (CRUD for FundProfiles, FundHistoryRecords) |
| **Backend API** | Add EF Core + Azure SQL provider (`Microsoft.EntityFrameworkCore.SqlServer`) |
| **Backend API** | Add `AzureSqlDbContext` with fund data entities |
| **YieldRaccoon** | Add HTTP sync service to push data to Backend API after crawl |
| **YieldRaccoon** | Keep SQLite as local-first storage (no changes to existing DB layer) |
| **Domain/Application** | No changes to existing interfaces |

### Provider Options Summary

| Provider | SQLite | Azure SQL (via API) | Use Case |
| --- | --- | --- | --- |
| `InMemory` | No | No | Session-scoped testing |
| `SQLite` | Yes | No | Local development (default) |
| `DualWrite` | Yes | Yes (via Backend API) | Production: local + cloud sync |

## Azure Infrastructure

### Resources (Provisioned)

| Resource | Name | SKU | Region | Cost |
| --- | --- | --- | --- | --- |
| SQL Server | `<your-sql-server>` | — | Sweden Central | — |
| SQL Database | `<your-sql-database>` | Free tier (General Purpose, serverless, 2 vCores) | Sweden Central | $0/month |
| Key Vault | `<your-keyvault>` (existing) | Standard | Sweden Central | ~$0.03/month |

### Free Tier Details

- **100,000 vCore seconds/month** (≈28 hours of active compute)
- **32 GB storage** (lifetime)
- **Auto-pause** when free limits hit (resumes next month)
- **Upgrade path:** flip to Serverless pay-per-use (~$3-5/mo) on same DB, no migration needed

### Authentication & Access Control

**Microsoft Entra-only authentication** — no SQL passwords exist.

| Principal | Type | Roles | Purpose |
| --- | --- | --- | --- |
| Your Entra account | Entra Admin (server-level) | Full admin | Portal Query Editor, schema management |
| `<your-app-service>` | Contained DB user (managed identity) | `db_datareader`, `db_datawriter`, `db_ddladmin` | Read/write fund data, EF Core migrations |

SQL commands used to grant access (via Portal Query Editor):

```sql
CREATE USER [<your-app-service>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<your-app-service>];
ALTER ROLE db_datawriter ADD MEMBER [<your-app-service>];
ALTER ROLE db_ddladmin ADD MEMBER [<your-app-service>];
```

### Networking

- **Public endpoint** with firewall
- **Allow Azure services and resources:** Yes (Backend API access)
- **Developer IPs:** Not configured (use Portal Query Editor for admin tasks)

### Connection String (Key Vault)

Stored in Key Vault as `BackendOptions--AzureSqlConnectionString`:

```text
Server=tcp:<your-sql-server>.database.windows.net,1433;Database=<your-sql-database>;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;
```

No passwords — `Active Directory Default` uses `DefaultAzureCredential` (Managed Identity in Azure, VS/CLI login locally).

### Secrets Inventory

| Secret | Location | Purpose |
| --- | --- | --- |
| OpenAI API key | Key Vault | Embeddings + LLM chat |
| Groq API key | Key Vault | Optional LLM provider |
| Azure SQL connection string | Key Vault | Passwordless (`Active Directory Default`) |

## Future: Vector Search Migration (Cosmos DB → Azure SQL)

Azure SQL Database supports native vector search — we can consolidate fund data + RAG embeddings in one database:

- **`VECTOR(1536)`** data type (matches `text-embedding-3-small` dimensions)
- **`VECTOR_DISTANCE('cosine', ...)`** for similarity search
- **`Microsoft.Data.SqlClient` 6.1.0+** has native `SqlVector` support
- Max 1998 dimensions per vector (our 1536 fits)

This migration is planned for later and will eliminate the need for Cosmos DB.
