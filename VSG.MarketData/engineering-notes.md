# Engineering Notes

## Approach

The solution is split into four main application projects:

- **Core** — domain models and reusable calculation logic.
- **Infrastructure** — SQLite persistence and Binance WebSocket collection.
- **API** — HTTP endpoints, request validation, and JSON/XML responses.
- **Console** — a command-line client that consumes the API over HTTP.

The tests project uses xUnit.

## Technical Decisions

### Data Collection

The application consumes Binance Spot trade streams and persists individual trade prices with their symbols and timestamps.

The initial monitored symbols are:

- `BTCUSDT`
- `ADAUSDT`
- `ETHUSDT`

The admin API allows monitored symbols to be listed, added, activated, and deactivated.

The lifecycle of runtime symbol changes must remain consistent with the active WebSocket subscriptions.

### Persistence

SQLite was selected for a lightweight assessment setup. Entity Framework Core migrations initialize the database schema and seed the initial monitored symbols.

Docker Compose persists the database using a named volume, allowing the data to survive container recreation.

SQLite keeps the setup simple and reproducible for local evaluation. For higher write volumes or production workloads, a more suitable managed database should be considered.

### Price Analysis

The 24-hour average endpoint calculates the arithmetic mean of stored trade prices for the requested symbol over the preceding 24 hours.

For SMA, prices are grouped into fixed-duration time buckets. The average price is calculated for each populated bucket, then the average of the selected bucket prices is returned.

Missing buckets are not synthesized. The returned data point count can therefore be lower than the requested count.

If fewer than `n` populated buckets are available, the endpoint returns the available data with HTTP `200 OK` and a warning. The response includes both the requested and actual data point counts.

The implementation treats `s`, when provided, as the UTC reference/end time. If it is omitted, the current UTC time is used.

The SMA represents the arithmetic mean of the selected bucket averages. Each populated bucket has equal weight, regardless of the number of trades recorded in it.

### API and Console Client

The API exposes endpoints for:

- Listing and managing monitored symbols.
- Calculating the average price over the preceding 24 hours.
- Calculating a simple moving average over selected time buckets.

The API supports JSON and XML response formatting through ASP.NET Core content negotiation.

The console client uses `HttpClient` to call the API and display the returned data.

### Containerization

The API is packaged using a multi-stage Docker build.

Docker Compose exposes port `8080` and mounts a named volume for the SQLite database.

The containerized setup is intended for local evaluation and demonstration rather than production deployment.

## Validation Performed

The following checks were performed during development:

- Built the solution successfully in Release configuration.
- Ran the xUnit suite successfully: eight tests passed.
- Started the API using Docker Compose.
- Verified the monitored-symbol listing endpoint.
- Verified that the 24-hour average endpoint returned HTTP `200 OK` when data was available.
- Verified XML response formatting using `Accept: application/xml`.
- Verified that the SMA endpoint returned available data points for a fixed reference time.
- Verified the SMA endpoint's partial-result behavior when fewer data points are available than requested.
- Verified that the partial-result response includes a warning and the actual number of available data points.
- Ran the console client and verified the 24-hour average and SMA operations.
- Added a symbol and toggled its active state through the admin API.
- Verified that activating `XRPUSDT` caused the Binance WebSocket connection to reconnect with the updated active symbol list, without restarting the application.

These checks validate the scenarios exercised during development. They do not replace a comprehensive integration test suite or a production-readiness assessment.

## AI-Assisted Development Record

AI assistance was used to help with:

- Structuring the solution and identifying implementation steps.
- Reviewing API behavior and interpreting HTTP responses.
- Drafting unit tests for average calculation and time-bucket grouping.
- Preparing Docker build and Compose configuration.
- Reviewing controller behavior and drafting tests for partial SMA results.
- Drafting project documentation and a deployment proposal.

Representative prompt themes included:

- “Help me implement a .NET market-data API that collects Binance Spot WebSocket trade prices and stores them in a relational database.”
- “Review this SMA controller implementation and identify potential problems with time-bucket grouping and insufficient data.”
- “Write xUnit tests for the average calculator and time-bucket grouping.”
- “Help containerize the .NET API using Docker Compose and persistent SQLite storage.”
- “Help test the API behavior when fewer SMA data points are available than requested.”

AI-generated suggestions were reviewed and validated through compilation, automated tests, and HTTP requests for the scenarios exercised during development. The prompts above summarize the topics of assistance rather than a verbatim transcript.

## Assumptions

- The SMA period parameter represents fixed-duration buckets for `1m`, `5m`, `30m`, `1d`, and `1w`.
- The optional `s` parameter is treated as a UTC reference/end time.
- The average for each bucket is the arithmetic mean of stored trade prices in that bucket.
- The SMA is the arithmetic mean of the selected bucket averages, so buckets with fewer trades have the same weight as buckets with more trades.
- Empty buckets are omitted