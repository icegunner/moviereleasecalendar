import os
import requests

def fetch_tmdb_description_with_credits(title, year=None):
    TMDB_BEARER_TOKEN = os.environ.get("TMDB_BEARER_TOKEN")
    if not TMDB_BEARER_TOKEN:
        return ""

    # Step 1: Search movie
    headers = {"Authorization": f"Bearer {TMDB_BEARER_TOKEN}"}
    params = {"query": title, "include_adult": False}
    if year:
        params["year"] = year

    resp = requests.get("https://api.themoviedb.org/3/search/movie",
                        headers=headers, params=params)
    if resp.status_code != 200:
        print(f"TMDb search failed: {resp.status_code}")
        return ""
    results = resp.json().get("results", [])
    if not results:
        return ""

    movie = results[0]
    movie_id = movie["id"]
    overview = movie.get("overview", "")

    # Step 2: Fetch credits
    cred_resp = requests.get(
        f"https://api.themoviedb.org/3/movie/{movie_id}/credits",
        headers=headers
    )
    if cred_resp.status_code != 200:
        print(f"TMDb credits failed: {cred_resp.status_code}")
        return overview  # return overview alone

    data = cred_resp.json()
    cast_names = [c["name"] for c in data.get("cast", [])][:5]
    directors = [c["name"] for c in data.get("crew", []) if c.get("job") == "Director"]

    parts = [overview.strip()] if overview else []
    if cast_names:
        parts.append("Starring: " + ", ".join(cast_names))
    if directors:
        parts.append(". Directed by: " + ", ".join(directors))

    return " ".join(parts) + "."
