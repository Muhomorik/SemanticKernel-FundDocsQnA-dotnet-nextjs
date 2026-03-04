# Cloud Sync

Bulk-sync local fund data (profiles + history records) to the Backend API on demand. Useful for initial population of a new backend database or catch-up syncing after a period of offline crawling.

## Prerequisites

- **Backend API URL** configured in Settings (e.g., `https://your-app.azurewebsites.net`)
- **Backend API key** configured in Settings (sent as `Authorization: ApiKey {key}` header)
- Fund data present in the local database (SQLite or InMemory)

## How to use

1. Click **Cloud sync** in the title bar (between Statistics and Settings)
2. Optionally enter a **Company name** to filter — leave empty to sync all funds
3. Set **Throttle (ms)** — delay between per-fund API calls (default: 500ms)
4. Click **Sync to cloud**
5. Watch the progress bar and status updates
6. Close the window to cancel an in-progress sync

If Backend API URL is not configured, the window shows a warning and the sync button is disabled.

## Sync phases

The sync operation runs in two phases:

```mermaid
sequenceDiagram
    participant VM as CloudSyncWindow
    participant SVC as CloudSyncService
    participant DB as FundProfileRepository
    participant API as Backend API

    VM->>SVC: SyncAsync(filter, throttle, progress, ct)
    SVC->>DB: GetByCompanyNameFilterAsync(filter)
    DB-->>SVC: List<FundProfile> with HistoryRecords

    Note over SVC,API: Phase 1 — Batch profile sync
    SVC->>API: POST /api/funds/list (all profiles)
    API-->>SVC: FundSyncResponse

    Note over SVC,API: Phase 2 — Per-fund history sync
    loop For each fund (with throttle delay)
        SVC->>API: POST /api/funds/about (profile + history)
        API-->>SVC: FundSyncResponse
        SVC-->>VM: Progress report
    end

    SVC-->>VM: CloudSyncResult
```

### Phase 1 — Batch profile sync

A single `POST /api/funds/list` sends all matched fund profiles. This ensures the backend knows about every fund before history records arrive.

### Phase 2 — Per-fund history sync

Iterates over each fund and sends `POST /api/funds/about` with the fund profile and all its history records. A configurable delay (`throttleMs`) is inserted between calls to avoid overwhelming the backend.

## Error handling

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
