# 🎬 Movie Release Calendar

A self-hosted, Dockerized scraper and calendar that aggregates theatrical movie release dates from [FirstShowing.net](https://www.firstshowing.net), inspired by their old Google Calendar feed.

## 📦 Features

- Scrapes theatrical wide-release titles from FirstShowing.net
- Enriches movies with metadata from TMDb (genres, descriptions, posters)
- Stores data in RavenDB (default) or MongoDB
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
| `MOVIECALENDAR_DB_PROVIDER` | `ravendb` | Database provider (`ravendb` or `mongodb`) |
| `RAVENDB_URL` | `http://localhost:8080` | RavenDB server URL |
| `RAVENDB_DATABASE` | `MovieReleaseCalendar` | RavenDB database name |
| `MONGODB_CONNECTIONSTRING` | `mongodb://localhost:27017` | MongoDB connection string |
| `MONGODB_DATABASE` | `MovieReleaseCalendar` | MongoDB database name |
| `TMDB_APIKEY` | — | TMDb API key for movie metadata |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening URL |

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