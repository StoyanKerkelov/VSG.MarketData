# VSG Market Data

A .NET 10 application that collects cryptocurrency trade prices from Binance Spot WebSocket, stores them in SQLite, and exposes HTTP endpoints for price analysis.

## Features

- Collects live trade prices for configured Binance Spot symbols.
- Seeds the initial symbols: `BTCUSDT`, `ADAUSDT`, and `ETHUSDT`.
- Provides an admin API to list, add, activate, and deactivate monitored symbols.
- Calculates the average price over the last 24 hours.
- Calculates a simple moving average from time-bucketed price data.
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

The SQLite database is stored in a named Docker volume, so it persists when the container is recreated. To remove the database volume as well, use `docker compose down -v`.

## API Endpoints

### List monitored symbols

```http
GET /api/admin/symbols
```

### Add a monitored symbol

```http
POST /api/admin/symbols
Content-Type: application/json

"XRPUSDT"
```

### Activate or deactivate a symbol

```http
PATCH /api/admin/symbols/{symbol}/active?active=true
PATCH /api/admin/symbols/{symbol}/active?active=false
```

### 24-hour average price

```http
GET /api/{symbol}/24hAvgPrice
```

Example:

```bash
curl -H "Accept: application/json" \
  http://localhost:8080/api/BTCUSDT/24hAvgPrice
```

Request XML using the `Accept` header:

```bash
curl -H "Accept: application/xml" \
  http://localhost:8080/api/BTCUSDT/24hAvgPrice
```

### Simple moving average

```http
GET /api/{symbol}/SimpleMovingAverage?n={numberOfDataPoints}&p={timePeriod}&s={startDateTime}
```

Supported periods: `1m`, `5m`, `30m`, `1d`, `1w`.

The `s` parameter is optional and specifies the reference time in UTC.

Example:

```bash
curl "http://localhost:8080/api/BTCUSDT/SimpleMovingAverage?n=5&p=1m&s=2026-10-20T11:41:00Z"
```

The implementation groups collected trade prices into time buckets, averages the prices within each bucket, and calculates the average of the selected bucket averages. If fewer than the requested number of buckets are available, the API returns an error response.

## Console Client

Run the console client locally:

```bash
dotnet run --project VSG.MarketData.Console
```

By default, it connects to `http://localhost:8080`. A different API base URL can be passed as the first argument:

```bash
dotnet run --project VSG.MarketData.Console -- http://localhost:8080
```

The client supports the 24-hour average and simple moving average operations.

## Database

The application uses SQLite. Entity Framework Core migrations are applied by the API during startup.

The Docker Compose configuration uses:

`Data Source=/app/data/marketdata.db`

and persists the database in a named Docker volume.

For local development, run the API project using the configured connection string.

## Tests

Run the test suite from the solution root:

```bash
dotnet test
```

The current tests cover average calculation and time-bucket grouping. Extend the suite with API integration tests and additional interval-boundary cases.

## Deployment Proposal

For a production deployment, use a container hosting platform such as Azure Container Apps or Azure App Service for Containers.

Recommended production changes:

1. Use managed, durable storage appropriate for the expected write volume instead of a local SQLite file.
2. Store secrets and environment-specific configuration outside the container image.
3. Configure health checks, structured logs, monitoring, and alerting.
4. Configure resource limits, restart policies, and deployment health validation.
5. Consider retention policies or aggregation of raw trade data to control storage growth.
6. Validate Binance WebSocket reconnection and symbol-change behavior under production conditions.

The supplied Docker Compose setup is intended for local evaluation and demonstration. It is not a production deployment.

## Known Limitations and Assumptions

- Prices are collected from live Binance Spot trade streams; results depend on the availability of the stream and the duration of collection.
- The database stores individual trade updates, which can cause rapid database growth.
- SMA is calculated from the average price in each populated time bucket; missing buckets are not synthesized.
- Runtime symbol changes need to be validated against the WebSocket subscription lifecycle.
- The current implementation is intended as a technical assessment, not a fully hardened production trading system.
