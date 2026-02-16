# 🎬 Movie Release Calendar

A self-hosted, Dockerized scraper and calendar that aggregates theatrical movie release dates from [FirstShowing.net](https://www.firstshowing.net), inspired by their old Google Calendar feed.

## 📦 Features

- Scrapes theatrical wide-release titles from FirstShowing.net
- Enriches movies with metadata from TMDb (genres, descriptions, posters)
- Stores data in RavenDB (default), MongoDB, PostgreSQL, or MariaDB/MySQL
- Produces an ICS feed compatible with iPhone, Google Calendar, and Outlook subscriptions
- ICS events are all-day, transparent (won't block your schedule), with stable UIDs
- Full-screen web calendar UI with light/dark mode
- Background scraping on a configurable cron schedule
- JSON and ICS feed endpoints
- Docker images built and published via GitHub Actions

## 🛠 Getting Started

### Prerequisites

- Docker & Docker Compose
- A [TMDb API key](https://www.themoviedb.org/settings/api) (free)

### Run

```bash
git clone https://github.com/icegunner/MovieReleaseCalendar.git
cd MovieReleaseCalendar
```

Create a `.env` file in the project root with your TMDb API key:

```
TMDB_APIKEY=your_key_here
```

Then start the stack:

```bash
docker compose up -d
```

### Access

| Endpoint | URL |
|---|---|
| Web calendar | `http://localhost:8080` |
| ICS feed | `http://localhost:8080/calendar.ics` |
| JSON feed | `http://localhost:8080/api/calendar/events.json` |
| Trigger scrape | `POST http://localhost:8080/api/scraper/run` |

## ⚙️ Configuration

### Environment Variables

| Variable | Default | Description |
|---|---|---|
| `MOVIECALENDAR_DB_PROVIDER` | `ravendb` | Database provider (see table below) |
| `RAVENDB_URL` | `http://localhost:8080` | RavenDB server URL |
| `RAVENDB_DATABASE` | `MovieReleaseCalendar` | RavenDB database name |
| `MONGODB_CONNECTIONSTRING` | `mongodb://localhost:27017` | MongoDB connection string |
| `MONGODB_DATABASE` | `MovieReleaseCalendar` | MongoDB database name |
| `POSTGRESQL_CONNECTIONSTRING` | `Host=localhost;Database=MovieReleaseCalendar;Username=postgres;Password=postgres` | PostgreSQL connection string |
| `MARIADB_CONNECTIONSTRING` | `Server=localhost;Database=MovieReleaseCalendar;User=root;Password=root` | MariaDB/MySQL connection string |
| `TMDB_APIKEY` | — | TMDb API key for movie metadata |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening URL |

### Database Providers

Set `MOVIECALENDAR_DB_PROVIDER` to one of the following values:

| Value | Backend | Notes |
|---|---|---|
| `ravendb` (default) | RavenDB | Document database |
| `mongo` or `mongodb` | MongoDB | Document database |
| `postgres` or `postgresql` | PostgreSQL | Relational, via EF Core + Npgsql |
| `maria`, `mariadb`, or `mysql` | MariaDB / MySQL | Relational, via EF Core + Pomelo |

Connection strings can be set via environment variables (see above) or in `appsettings.json` under `RavenDb`, `MongoDb`, `PostgreSql`, or `MariaDb` sections. The PostgreSQL and MariaDB backends automatically create the database schema on first startup.

## 🐳 Docker

```
ghcr.io/icegunner/moviereleasecalendar:latest
```

## 📅 ICS Feed Details

The generated `.ics` feed is designed to match the look and feel of the original FirstShowing.net Google Calendar subscription:

- **All-day events** using date-only values (`DTSTART;VALUE=DATE:...`)
- **Transparent** (`TRANSP:TRANSPARENT`) — events don't block your schedule
- **Stable UIDs** — derived from movie title + release date, so refreshing the feed won't create duplicates
- **Calendar metadata** — includes `X-WR-CALNAME`, `X-WR-TIMEZONE`, and `X-WR-CALDESC` for proper display on iOS and other clients

## 📅 Versioning

Tag your release and push:

```bash
git tag v1.0.0
git push origin v1.0.0
```