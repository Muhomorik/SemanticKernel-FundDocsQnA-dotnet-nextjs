# Fund Data Export

Export filtered fund data to a standalone SQLite `.db` file — useful for sharing a subset of the database with others or for offline analysis. Available from the Export button in the title bar (SQLite provider only).

The original database is never modified. The pipeline copies first, then filters the copy.

## Export pipeline

1. `File.Copy` source → destination (+ WAL/SHM journal files if present)
2. `PRAGMA wal_checkpoint(TRUNCATE)` — merge WAL into main file
3. `DELETE FROM FundProfiles` where company name doesn't match (case-insensitive)
4. `DELETE FROM FundHistoryRecords` where fund no longer exists (orphan cleanup)
5. `DELETE FROM FundHistoryRecords` where NavDate is before cutoff
6. `PRAGMA journal_mode=DELETE` — switch from WAL to classic mode (checkpoints pending changes)
7. `VACUUM` — reclaim disk space
8. Clean up leftover `-wal` / `-shm` journal files

## Filter options

| Option | Values | Default |
| -------- | -------- | --------- |
| Time period | 1 week, 2 weeks, 1 month, 3 months | 1 week |
| Company name | Free text (case-insensitive match) | *(empty)* |
| Output file | Browse or auto-generated filename | `YieldRaccoon_{company}_{period}.db` in source directory |

## Architecture

| Layer | Component |
| ------- | ----------- |
| Application | `IFundDataExportService` — interface with `ExportAsync` |
| Infrastructure | `FundDataExportService` — raw SQLite operations via `Microsoft.Data.Sqlite` |
| Presentation | `ExportWindow` / `ExportWindowViewModel` — form with period, company, output path |
| Presentation | `ExportPeriod` record — selectable time period model |
| Presentation | `IExportWindowService` / `ExportWindowService` — modal dialog launcher |
