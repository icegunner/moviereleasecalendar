#!/bin/sh
echo "🔄 Starting cron..."
cron

echo "🧲 Running initial movie scrape..."
python /app/main.py

echo "🌐 Starting web server..."
exec python /app/server.py
