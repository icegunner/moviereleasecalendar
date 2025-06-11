FROM python:3.11-slim

# Install system packages including cron
RUN apt-get update && apt-get install -y cron && apt-get clean

WORKDIR /app

COPY app /app
COPY crontab.txt /etc/cron.d/scraper-cron

RUN pip install --no-cache-dir -r /app/requirements.txt && \
    chmod 0644 /etc/cron.d/scraper-cron && \
    crontab /etc/cron.d/scraper-cron

CMD ["sh", "-c", "cron && python /app/server.py"]
