# VSG Market Data

A .NET 10 application that collects cryptocurrency trade prices from Binance Spot WebSocket, stores them in SQLite, and exposes HTTP endpoints for price analysis.

## Features

- Collects live trade prices for configured Binance Spot symbols.
- Seeds the initial symbols: `BTCUSDT`, `ADAUSDT`, and `ETHUSDT`.
- Provides an admin API to list, add, activate, and deactivate monitored symbols.
- Calculates the average price over the last 24 hours.
- Calculates a simple moving average from time-bucketed price data.
- Returns available SMA data with a warning when fewer than the requested number of data points are available.
- Supports JSON and XML responses through HTTP content negotiation.
- Includes a console client that consumes the HTTP API.
- Uses SQLite with Entity Framework Core migrations.
- Supports containerized execution with Docker Compose.

## Technology Stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Binance Spot WebSocket
- xUnit
- Docker and Docker Compose

## Prerequisites

Choose one of the following options:

- Docker Desktop with Docker Compose, or
- .NET 10 SDK for local development

## Run with Docker Compose

From the repository root:

```bash
docker compose up --build -d
```

Check the service:

```bash
docker compose ps
docker compose logs -f api
```

The API listens on:

`http://localhost:8080`

Stop the service:

```bash
docker compose down
```

The SQLite database is stored in a named Docker volume, so it persists when the container is recreated.

To remove the database volume as well:

```bash
docker compose down -v
```

**Warning:** `docker compose down -v` removes the persisted database volume and its data.

## API Endpoints

### List monitored symbols

```http
GET /api/admin/symbols
```

Returns the monitored symbols and their active status.

### Add a monitored symbol

```http
POST /api/admin/symbols
Content-Type: application/json

"XRPUSDT"
```

The request body is a JSON string containing the symbol.

### Activate or deactivate a symbol

```http
PATCH /api/admin/symbols/{symbol}/active?active=true
PATCH /api/admin/symbols/{symbol}/active?active=false
```

The `active` parameter determines whether the symbol should be active.

### 24-hour average price

```http
GET /api/{symbol}/24hAvgPrice
```

Example:

```bash
curl -H "Accept: application/json" \
  http://localhost:8080/api/BTCUSDT/24hAvgPrice
```

Request an XML response using the `Accept` header:

```bash
curl -H "Accept: application/xml" \
  http://localhost:8080/api/BTCUSDT/24hAvgPrice
```

The endpoint calculates the arithmetic mean of stored trade prices for the requested symbol over the preceding 24 hours. If no matching data is available, it returns `404 Not Found`.

### Simple moving average

```http
GET /api/{symbol}/SimpleMovingAverage?n={numberOfDataPoints}&p={timePeriod}&s={referenceTimeUtc}
```

Supported periods:

- `1m` — one minute
- `5m` — five minutes
- `30m` — thirty minutes
- `1d` — one day
- `1w` — seven days

The `n` parameter specifies the maximum number of populated time buckets to include. It must be between `1` and `1000`.

The optional `s` parameter specifies the reference/end time in UTC. If omitted, the current UTC time is used.

Example:

```bash
curl "http://localhost:8080/api/BTCUSDT/SimpleMovingAverage?n=5&p=1m&s=2026-10-20T11:41:00Z"
```

The implementation groups collected trade prices into fixed-duration time buckets, calculates the arithmetic mean within each populated bucket, and then calculates the arithmetic mean of the selected bucket averages.

Missing buckets are not synthesized. Consequently, the number of returned data points can be lower than the requested number.

When fewer than `n` data points are available, the endpoint returns `200 OK` with the available data and a warning in the response. The response distinguishes the requested number of points from the actual number returned.

Invalid parameters produce a `400 Bad Request` response. If no price data is available for the requested symbol and time window, the endpoint returns `404 Not Found`.

## Console Client

Run the console client locally:

```bash
dotnet run --project VSG.MarketData.Console
```

By default, it connects to:

`http://localhost:8080`

A different API base URL can be passed as the first argument:

```bash
dotnet run --project VSG.MarketData.Console -- http://localhost:8080
```

The client supports the 24-hour average and simple moving average operations.

## Database

The application uses SQLite. Entity Framework Core migrations are applied by the API during startup.

The Docker Compose configuration uses the connection string:

`Data Source=/app/data/marketdata.db`

The database is persisted using a named Docker volume.

For local development, run the API project using the configured connection string.

## Tests

Run the test suite from the solution root:

```bash
dotnet test VSG.MarketData.slnx --configuration Release
```

The current test suite contains eight tests covering:

- Simple moving average calculation.
- Price interval grouping.
- SMA controller behavior when fewer data points are available than requested.
- SMA controller behavior when the requested number of data points is available.
- SMA controller behavior when no data exists for the requested time window.

The controller tests use an isolated SQLite in-memory database. They do not require a running Binance connection or a persistent application database.

## Deployment Considerations

For a production deployment, consider a container hosting platform such as Azure Container Apps or Azure App Service for Containers.

Recommended production changes:

1. Use managed, durable storage appropriate for the expected write volume instead of a local SQLite file.
2. Store secrets and environment-specific configuration outside the container image.
3. Configure health checks, structured logs, monitoring, and alerting.
4. Configure resource limits, restart policies, and deployment health validation.
5. Consider retention policies or aggregation of raw trade data to control storage growth.
6. Validate Binance WebSocket reconnection and symbol-change behavior under production conditions.
7. Protect administrative endpoints before exposing the application outside a trusted development environment.

The supplied Docker Compose setup is intended for local evaluation and demonstration. It is not a production deployment.

## Known Limitations and Assumptions

- Prices are collected from live Binance Spot trade streams. Results depend on stream availability and the duration of data collection.
- The database stores individual trade updates, which can cause rapid database growth.
- SMA is calculated from the average price in each populated time bucket; missing buckets are not synthesized.
- The average of bucket averages gives each populated bucket equal weight, regardless of the number of trades within that bucket.
- Daily and weekly periods currently use fixed-duration intervals rather than explicitly defined calendar boundaries.
- Runtime symbol changes and their effect on active WebSocket subscriptions should be verified against the subscription lifecycle.
- The current implementation is intended as a technical assessment and demonstration, not a fully hardened production trading system.
