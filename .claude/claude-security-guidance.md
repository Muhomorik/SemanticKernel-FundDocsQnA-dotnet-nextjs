# Security guidance for this repo

Public GitHub repo. Hobby RAG app: ASP.NET Core 9 backend (Azure App Service), Next.js static frontend (Azure Static Web Apps), plus local desktop/CLI tools (YieldRaccoon WPF crawler, PdfTextExtractor, Preprocessor).

## Threat model

- `POST /api/ask` is anonymous by design. Every request spends paid LLM and embedding tokens, so abuse = cost, not just data.
- `/api/embeddings` and `/api/funds` are write/sync endpoints behind `ApiKeyAuthenticationMiddleware`. Anything that bypasses or widens that middleware is high severity.
- PDF text, `embeddings.json` chunks, Cosmos DB / Azure SQL records, and intercepted WebView2 responses are untrusted input.
- The repo history is public: anything ever committed is leaked.

## Secrets

- API keys and connection strings come only from user-secrets (local), Key Vault or App Service configuration (production), or environment variables. Never in `appsettings*.json`, `launchSettings.json`, `.env*` files, workflow YAML, tests or docs.
- Never log secrets, API keys, full connection strings or `Authorization` / `ApiKey` header values.
- API key comparison must stay constant-time (`CryptographicOperations.FixedTimeEquals`).
- A key that was ever committed is compromised: flag it and require rotation, not just deletion.

## Backend API

- New controllers must carry `[EnableRateLimiting("ApiRateLimit")]`. Do not remove or loosen rate limiting, request size limits, or `[MaxLength]` / `[SafeQuestion]` validation on `AskQuestionRequest`.
- New write, sync or admin endpoints must be covered by `ApiKeyAuthenticationMiddleware`. Check the path list in the middleware when adding routes.
- CORS stays an explicit allowlist from `BackendOptions.AllowedOrigins`. No `AllowAnyOrigin`, no wildcard origins, no `AllowCredentials` with broad origins.
- EF Core only through LINQ or parameterized APIs. No `FromSqlRaw` / `ExecuteSqlRaw` with interpolated or concatenated input.
- Error responses must not leak stack traces, exception messages, connection details or internal paths outside Development.

## LLM / RAG

- Use the sanitized question everywhere once `IUserQuestionSanitizer.Sanitize` has run: search **and** prompt building. Passing `request.Question` to `RagPromptBuilder.BuildUserPrompt` after sanitizing is a bug.
- Retrieved chunks and user questions are data, never instructions. Keep them inside their delimiters in `RagPromptBuilder`, and do not let user input close or forge those delimiters.
- Semantic Kernel plugins exposed to the LLM (`FundDataPlugin`) must stay read-only, clamp numeric arguments (`limit`, `top`, date ranges) to sane maxima, and never expose write operations, per-user data or raw SQL.
- Never let model output drive file paths, URLs fetched server-side, shell commands or SQL.

## Frontend

- Render LLM answers and source text as React text or sanitized markdown. No `dangerouslySetInnerHTML` with model or document content, and no `javascript:` links from model output.
- Only `NEXT_PUBLIC_*` values reach the browser, so never put secrets in them.

## Desktop tools (YieldRaccoon, PdfTextExtractor)

- WebView2 interceptors should match by host and path, not URL substring, when adding new ones.
- Backend sync clients must use https for non-localhost endpoints before sending the `ApiKey` header.
- File writes stay inside configured output directories. Build paths with `Path.Combine` and reject `..` from external data (fund names, ISINs, PDF metadata).

## CI (`.github/workflows`)

- Pin third-party actions to a full commit SHA or a trusted major version. Use least-privilege `permissions:`.
- Never echo secrets, never run untrusted PR code with secrets (`pull_request_target` + checkout of the PR head).
