FROM python:3.11-slim

# Install cron
RUN apt-get update && apt-get install -y cron && apt-get clean

# Set working directory
WORKDIR /app

# Copy files
COPY app /app
COPY crontab.txt /etc/cron.d/scraper-cron
COPY entrypoint.sh /app/entrypoint.sh

# Install Python requirements
RUN pip install --no-cache-dir -r /app/requirements.txt && \
    chmod 0644 /etc/cron.d/scraper-cron && \
    crontab /etc/cron.d/scraper-cron && \
    chmod +x /app/entrypoint.sh

# Run the entrypoint script
CMD ["/app/entrypoint.sh"]
