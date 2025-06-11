import requests
from bs4 import BeautifulSoup
from datetime import datetime
from db import init_db, upsert_movie, delete_removed_movies, get_all_movies
from calendar_builder import create_ics
from util import normalize_title

YEARS = [datetime.now().year - 1, datetime.now().year, datetime.now().year + 1]

def scrape():
    seen_titles = set()

    for year in YEARS:
        url = f"https://www.firstshowing.net/schedule{year}"
        response = requests.get(url)
        soup = BeautifulSoup(response.text, 'html.parser')

        current_month = None
        current_day = None
        full_date = None

        for tag in soup.find_all(['h2', 'h4', 'p']):
            if tag.name == 'h2':
                current_month = tag.get_text(strip=True)
            elif tag.name == 'h4':
                strong = tag.find('strong')
                if strong:
                    try:
                        current_day = strong.get_text(strip=True)
                        full_date = datetime.strptime(f"{current_month} {current_day}, {year}", "%B %d, %Y")
                    except Exception:
                        full_date = None
            elif tag.name == 'p' and 'sched' in tag.get('class', []) and full_date:
                for a in tag.find_all('a'):
                    if not a.find('strong'):
                        continue
                    full_title = a.get_text(strip=True)
                    link = a.get('href')
                    full_link = f"https:{link}" if link.startswith("//") else link
                    description = fetch_movie_description(full_link)
                    upsert_movie(full_title, full_date, description, full_link)
                    seen_titles.add(full_title)

    delete_removed_movies(seen_titles)
    create_ics(get_all_movies())

def fetch_movie_description(url):
    try:
        response = requests.get(url)
        soup = BeautifulSoup(response.text, 'html.parser')
        entry = soup.find('div', class_='entry')
        if entry:
            first_p = entry.find('p')
            return str(first_p) if first_p else ''
    except Exception as e:
        print(f"Error fetching description for {url}: {e}")
    return ''

if __name__ == "__main__":
    init_db()
    scrape()
