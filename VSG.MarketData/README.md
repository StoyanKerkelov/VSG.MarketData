# VSG Market Data

A .NET 10 application that collects cryptocurrency trade prices from Binance Spot WebSocket, stores them in SQLite, and exposes HTTP endpoints for price analysis.

## Features

- Collects live trade prices for configured Binance Spot symbols.
- Seeds the initial symbols: `BTCUSDT`, `ADAUSDT`, and `ETHUSDT`.
- Provides an admin API to list, add, activate, and deactivate monitored symbols.
- Automatically refreshes the Binance WebSocket connection when the active symbol list changes.
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

When the active symbol list changes, the Binance WebSocket service detects the change and reconnects using the updated list. This allows symbol changes to take effect without restarting the application.

This behavior was verified by activating `XRPUSDT` through the admin API and confirming that the service reconnected with `ADAUSDT`, `BTCUSDT`, `ETHUSDT`, and `XRPUSDT`.

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

If less than 24 hours of data is available, the average is calculated over the available data within the preceding 24-hour window.

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

The implementation groups collected trade prices into fixed-duration time buckets, calculates the arithmetic mean within