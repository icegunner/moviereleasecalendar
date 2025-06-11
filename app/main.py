import requests
from bs4 import BeautifulSoup
from datetime import datetime
from db import init_db, upsert_movie, delete_removed_movies, get_all_movies
from calendar_builder import create_ics
import hashlib
import json
import os

YEARS = [datetime.now().year - 1, datetime.now().year, datetime.now().year + 1]

def hash_content(text):
    return hashlib.md5(text.encode("utf-8")).hexdigest()

def load_hashes():
    try:
        with open("/app/page_hashes.json", "r") as f:
            return json.load(f)
    except FileNotFoundError:
        return {}

def save_hashes(hashes):
    with open("/app/page_hashes.json", "w") as f:
        json.dump(hashes, f, indent=2)

def load_scrape_stats():
    try:
        with open("/app/scrape_stats.json", "r") as f:
            return json.load(f)
    except FileNotFoundError:
        return {}

def save_scrape_stats(stats):
    with open("/app/scrape_stats.json", "w") as f:
        json.dump(stats, f, indent=2)

def load_config():
    try:
        with open("/app/config.json", "r") as f:
            return json.load(f)
    except FileNotFoundError:
        return {"pushover": {"enabled": False}}

def send_pushover(config, message):
    if not config.get("pushover", {}).get("enabled"):
        return

    try:
        payload = {
            "token": config["pushover"]["app_token"],
            "user": config["pushover"]["user_key"],
            "message": message,
            "priority": config["pushover"].get("priority", 0),
            "title": config["pushover"].get("title", "Movie Calendar Warning")
        }
        endpoint = config["pushover"].get("endpoint", "https://api.pushover.net/1/messages.json")
        response = requests.post(endpoint, data=payload)
        if response.status_code != 200:
            print(f"⚠️ Pushover response: {response.status_code} {response.text}")
    except Exception as e:
        print(f"⚠️ Failed to send Pushover alert: {e}")

def fetch_movie_description(url):
    try:
        response = requests.get(url)
        soup = BeautifulSoup(response.text, "html.parser")
        entry = soup.find("div", class_="entry")
        if entry:
            first_p = entry.find("p")
            return str(first_p) if first_p else ""
    except Exception as e:
        print(f"Error fetching description for {url}: {e}")
    return ""

def scrape():
    seen_titles = set()
    page_hashes = load_hashes()
    new_hashes = {}

    scrape_stats = load_scrape_stats()
    new_stats = {}

    config = load_config()

    for year in YEARS:
        url = f"https://www.firstshowing.net/schedule{year}"
        response = requests.get(url)
        html = response.text
        html_hash = hash_content(html)
        new_hashes[str(year)] = html_hash

        if page_hashes.get(str(year)) == html_hash:
            print(f"✅ No change detected for {year}, skipping.")
            continue

        print(f"📝 Change detected for {year}, scraping...")
        soup = BeautifulSoup(html, "html.parser")

        current_month = None
        current_day = None
        full_date = None
        movie_count = 0

        for tag in soup.find_all(["h2", "h4", "p"]):
            if tag.name == "h2":
                current_month = tag.get_text(strip=True)
            elif tag.name == "h4":
                strong = tag.find("strong")
                if strong:
                    try:
                        current_day = strong.get_text(strip=True)
                        full_date = datetime.strptime(f"{current_month} {current_day}, {year}", "%B %d, %Y")
                    except Exception:
                        full_date = None
            elif tag.name == "p" and "sched" in tag.get("class", []) and full_date:
                for a in tag.find_all("a"):
                    if not a.find("strong"):
                        continue
                    full_title = a.get_text(strip=True)
                    link = a.get("href")
                    full_link = f"https:{link}" if link.startswith("//") else link
                    description = fetch_movie_description(full_link)
                    upsert_movie(full_title, full_date, description, full_link)
                    seen_titles.add(full_title)
                    movie_count += 1

        previous_count = scrape_stats.get(str(year), 0)
        new_stats[str(year)] = movie_count

        if previous_count > 0 and (movie_count == 0 or movie_count < previous_count / 2):
            warning = f"⚠️ WARNING: Only {movie_count} movies scraped for {year} (was {previous_count}). Page format may have changed."
            print(warning)
            send_pushover(config, warning)

    save_hashes(new_hashes)
    save_scrape_stats(new_stats)
    delete_removed_movies(seen_titles)
    create_ics(get_all_movies())

if __name__ == "__main__":
    init_db()
    scrape()
