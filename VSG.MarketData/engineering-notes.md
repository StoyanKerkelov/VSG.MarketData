# Engineering Notes

## Approach

The solution is split into four main application projects:

- **Core** — domain models and reusable calculation logic.
- **Infrastructure** — SQLite persistence and Binance WebSocket collection.
- **API** — HTTP endpoints, request validation, and JSON/XML responses.
- **Console** — a command-line client that consumes the API over HTTP.

The tests project uses xUnit.

## Technical Decisions

### Data collection

The application consumes Binance Spot trade streams and persists individual trade prices with their symbols and timestamps. The initial monitored symbols are `BTCUSDT`, `ADAUSDT`, and `ETHUSDT`.

### Persistence

SQLite was selected for a lightweight assessment setup. Entity Framework Core migrations initialize the schema and seed the initial monitored symbols. Docker Compose persists the database using a named volume.

### Price analysis

The 24-hour average is calculated from the stored prices within the preceding 24 hours.

For SMA, prices are grouped into fixed-duration time buckets. The average price is calculated for each populated bucket, then the average of the selected bucket prices is returned. Missing buckets are not synthesized. If fewer than `n` buckets are available, the endpoint returns an error rather than silently calculating an average from fewer points.

### API and console client

The API supports JSON and XML response formatting through ASP.NET Core content negotiation. The console client uses `HttpClient` to call the API and displays the returned JSON.

### Containerization

The API is packaged using a multi-stage Docker build. Docker Compose exposes port 8080 and mounts a named volume for the SQLite database.

## Validation Performed

The following checks were performed during development:

- Built the API project successfully with no compiler warnings or errors.
- Ran the xUnit suite successfully: five tests passed.
- Started the API with Docker Compose.
- Verified the monitored-symbol listing endpoint.
- Verified the 24-hour average endpoint returned HTTP 200.
- Verified XML response formatting using `Accept: application/xml`.
- Verified the SMA endpoint returned five one-minute points for a fixed reference time.
- Ran the console client and verified both the 24-hour average and SMA operations.
- Added a symbol and toggled its active state through the admin API.

These checks validate the tested scenarios only; they do not replace a comprehensive integration test suite.

## AI-Assisted Development Record

AI assistance was used to help with:

- Structuring the solution and identifying implementation steps.
- Reviewing API behavior and interpreting HTTP responses.
- Drafting unit tests for average calculation and time-bucket grouping.
- Preparing Docker build and Compose configuration.
- Drafting project documentation and a deployment proposal.

Representative prompt themes included:

- “Help me implement a .NET market-data API that collects Binance Spot WebSocket trade prices and stores them in a relational database.”
- “Review this SMA controller implementation and identify potential problems with time-bucket grouping and insufficient data.”
- “Write xUnit tests for the average calculator and price interval grouping.”
- “Help containerize the .NET API using Docker Compose and persistent SQLite storage.”

AI-generated suggestions were reviewed and validated by compiling the application, running tests, and exercising endpoints through HTTP requests. The prompts above summarize the topics of assistance rather than a verbatim transcript.

## Assumptions

- The SMA period parameter represents fixed-duration buckets for `1m`, `5m`, `30m`, `1d`, and `1w`.
- The optional `s` parameter is treated as a UTC reference/end time.
- The average for each bucket is the arithmetic mean of stored trade prices in that bucket.
- The SMA is the arithmetic mean of the selected bucket averages, so buckets with fewer trades have the same weight as buckets with more trades.

## Known Issues and Potential Improvements

- **Runtime symbol changes:** verify that adding or activating a symbol updates the active WebSocket subscriptions without requiring an application restart.
- **Time-bucket semantics:** review calendar alignment for daily and weekly periods and add tests for bucket boundaries.
- **Missing intervals:** decide whether empty buckets should be omitted or represented explicitly.
- **Database growth:** storing every trade can lead to high write volume and database growth. Consider aggregation and retention.
- **Logging volume:** reduce per-trade logging and use structured operational logging.
- **Automated coverage:** add API integration tests, XML response tests, admin endpoint tests, and tests for insufficient data.
- **Production hardening:** use durable managed storage, health checks, monitoring, resource limits, and externalized configuration.
- **Admin security:** protect administrative endpoints before exposing the application outside a trusted development environment.
