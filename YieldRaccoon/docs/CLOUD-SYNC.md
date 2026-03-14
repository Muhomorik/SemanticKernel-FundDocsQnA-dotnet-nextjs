# Cloud Sync

Bulk-sync local fund data (profiles + history records) to the Backend API on demand. Useful for initial population of a new backend database or catch-up syncing after a period of offline crawling.

![Cloud sync window](IMG-CLOUD-SYNC.png)

## Prerequisites

- **Backend API URL** configured in Settings (e.g., `https://<your-app>.azurewebsites.net`)
- **Backend API key** configured in Settings (sent as `Authorization: ApiKey {key}` header)
- Fund data present in the local database (SQLite or InMemory)

## How to use

1. Click **Cloud sync** in the title bar (between Statistics and Settings)
2. Optionally enter a **Company name** to filter — leave empty to sync all funds
3. Set **Throttle (ms)** — delay between per-fund API calls (default: 1200ms, keeps requests under the backend's 60/min rate limit)
4. Click **Sync to cloud**
5. Watch the progress bar and status updates
6. Close the window to cancel an in-progress sync

If Backend API URL is not configured, the window shows a warning and the sync button is disabled.

## API endpoints

| Endpoint | Caller | Purpose |
| -------- | ------ | ------- |
| `POST /api/funds/full-sync` | `CloudSyncService` (on-demand) | Per-fund: insert-if-not-exists profile + sparse upsert full history (all 7 time-varying fields) |
| `POST /api/funds/list` | `DualWriteFundIngestionService` (crawl session) | Batch upsert of daily snapshots — profile metadata + Nav/NavDate from the fund listing page |
| `POST /api/funds/about` | `DualWriteChartIngestionService` (about-fund page) | Single fund: upsert profile + insert-only chart history across 7 time periods |

## Full sync flow

```mermaid
sequenceDiagram
    participant VM as CloudSyncWindow
    participant SVC as CloudSyncService
    participant DB as FundProfileRepository
    participant API as Backend API

    VM->>SVC: SyncAsync(filter, throttle, progress, ct)
    SVC->>DB: GetByCompanyNameFilterAsync(filter)
    DB-->>SVC: List<FundProfile> with HistoryRecords

    loop For each fund (with throttle delay)
        SVC->>SVC: Map FundProfile → ApiFundFullSyncProfileMetadataDto
        SVC->>SVC: Map FundHistoryRecord[] → ApiFundFullHistoryRecordDto[]
        SVC->>API: POST /api/funds/full-sync (profile metadata + full history)
        API-->>SVC: FundSyncResponse
        SVC-->>VM: Progress report
    end

    SVC-->>VM: CloudSyncResult
```

### Per-fund full history sync

Iterates over each fund and sends `POST /api/funds/full-sync` with:

- **`ApiFundFullSyncProfileMetadataDto`** — static profile metadata only (no time-varying history fields). The backend uses insert-if-not-exists semantics — existing profiles are never overwritten.
- **`ApiFundFullHistoryRecordDto[]`** — complete history records including all time-varying fields: `Nav`, `NavDate`, `Capital`, `NumberOfOwners`, `Risk`, `SharpeRatio`, `StandardDeviation`.

The backend first calls `InsertIfNotExistsAsync` on the profile — if the fund already exists in the database it is left untouched; if it is new it is inserted. This guarantees the FK constraint is satisfied before history records are inserted or updated.

Backend upsert semantics for history records (`UpsertSparseRangeAsync`):

- New `(ISIN, NavDate)` pair → **INSERT** with all fields
- Existing pair → update `Capital`, `NumberOfOwners`, `Risk`, `SharpeRatio`, `StandardDeviation` **only when incoming value is non-null**; `Nav` and `NavDate` are never modified

A configurable delay (`throttleMs`) is inserted between calls to avoid overwhelming the backend.

## DualWrite — Automatic sync during crawling

When the `DualWrite` provider is configured, fund data is automatically synced to the Backend API as it is crawled — no manual cloud sync needed. Two decorators handle the two data sources:

### CrawlSessionOrchestrator → DualWriteFundIngestionService

The crawl session (fund listing page) produces one daily snapshot per fund (Nav, NavDate, plus profile metadata). `DualWriteFundIngestionService` wraps `FundIngestionService` and fires a background sync after each batch is persisted locally.

```mermaid
sequenceDiagram
    participant CSO as CrawlSessionOrchestrator
    participant DW as DualWriteFundIngestionService
    participant SQLite as FundIngestionService
    participant Repo as FundProfileRepository
    participant API as Backend API

    CSO->>DW: IngestBatchAsync(funds)
    DW->>SQLite: IngestBatchAsync(funds)
    SQLite-->>DW: count (persisted locally)
    DW-->>CSO: return count

    par Backend sync (fire-and-forget)
        loop For each fund DTO
            DW->>Repo: GetByIsinAsync(isinId)
            Repo-->>DW: FundProfile (timestamps)
            DW->>DW: dto.ToApiFundDto() + overlay profile timestamps
        end
        DW->>API: POST /api/funds/list (ApiFundDto[])
        API-->>DW: FundSyncResponse
    end
```

The API DTO is always built from the `FundDataDto` (which carries the daily Nav/NavDate snapshot), enriched with authoritative timestamps (`FirstSeenAt`, `CrawlerLastUpdatedAt`, `AboutFundLastVisitedAt`) from the persisted profile when available.

### AboutFundOrchestrator → AboutFundChartIngestionService

The about-fund browser collects chart data across 7 time periods per fund. `AboutFundChartIngestionService` merges and deduplicates the data points, then persists as history records. A profile existence check guards against FK violations for funds not yet in the local database.

```mermaid
sequenceDiagram
    participant AFO as AboutFundOrchestrator
    participant ACI as AboutFundChartIngestionService
    participant PRepo as FundProfileRepository
    participant HRepo as FundHistoryRepository

    AFO->>ACI: IngestChartDataAsync(pageData, isinId)
    ACI->>PRepo: ExistsByIsinAsync(isinId)

    alt Profile not found
        PRepo-->>ACI: false
        ACI-->>AFO: 0 (skip — debug log)
    else Profile exists
        PRepo-->>ACI: true
        ACI->>ACI: Deserialize 7 chart slots → merge → deduplicate by NavDate
        ACI->>HRepo: AddRangeIfNotExistsAsync(records)
        ACI->>HRepo: SaveChangesAsync()
        ACI-->>AFO: inserted count
    end

    AFO->>PRepo: UpdateLastVisitedAtAsync(isinId)
```

When `DualWrite` is enabled, `DualWriteChartIngestionService` wraps this and additionally syncs the profile + history records to the Backend API via `POST /api/funds/about` (fire-and-forget).

## Error handling

- **Rate limiting (429):** `FundSyncApiClient` automatically retries with exponential backoff (2s, 4s, 8s), respecting the backend's `Retry-After` header. After 3 retries, the fund is skipped with a 10-second cooldown before continuing. The progress bar shows "Rate limited — waiting before next fund..." during cooldown.
- **Per-fund failures** are caught and counted — they don't stop the overall sync
- **Cancellation** (closing the window) stops the loop after the current fund completes
- The final result shows total funds, successful syncs, failures, and history records inserted

## Architecture

| Layer | Component |
| ------- | ----------- |
| Application | `ICloudSyncService` — interface with `SyncAsync` |
| Application | `CloudSyncProgress` / `CloudSyncResult` — progress and result DTOs |
| Infrastructure | `CloudSyncService` — orchestrates queries, mapping, and API calls |
| Presentation | `CloudSyncWindow` / `CloudSyncWindowViewModel` — form with company filter, throttle, progress |
| Presentation | `ICloudSyncWindowService` / `CloudSyncWindowService` — modal dialog launcher |

## Configuration

Backend API URL and API key are configured in the Settings window. See the [YieldRaccoon README](../README.md) for details on DualWrite provider configuration.
