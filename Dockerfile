FROM python:3.11-slim

WORKDIR /app

COPY app /app
COPY crontab.txt /etc/cron.d/scraper-cron

RUN pip install --no-cache-dir -r /app/requirements.txt && \
    chmod 0644 /etc/cron.d/scraper-cron && \
    crontab /etc/cron.d/scraper-cron

CMD ["sh", "-c", "cron && python /app/server.py"]
