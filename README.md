# 🎬 Movie Release Calendar

A self-hosted, Dockerized scraper and calendar that aggregates movie release dates from [FirstShowing.net](https://www.firstshowing.net).

## 📦 Features
- Scrapes theatrical wide release titles
- Stores data in PostgreSQL
- Outputs `.ics` and a full-screen calendar UI
- Auto-updates weekly with cron
- Supports light/dark mode UI
- Exposes JSON + ICS feeds
- Automatically builds Docker images via GitHub Actions

## 🛠 Getting Started

```bash
git clone https://github.com/youruser/moviecalendar.git
cd moviecalendar
docker compose up -d
```

Access:

- Web calendar: `http://localhost:8080`
- ICS feed: `http://localhost:8080/calendar.ics`
- JSON feed: `http://localhost:8080/calendar.json`

## 🐳 Tags

- `ghcr.io/youruser/moviecalendar:latest`
- `ghcr.io/youruser/moviecalendar:v1.0.0`

## 📅 Versioning

Tag your release and push:

```bash
git tag v1.0.0
git push origin v1.0.0
```