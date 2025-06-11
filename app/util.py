import re
import hashlib
import unicodedata

def normalize_title(title: str) -> str:
    title = unicodedata.normalize("NFKD", title)
    title = title.lower().strip()
    title = title.replace("’", "'").replace("–", "-").replace("“", '"').replace("”", '"')
    title = re.sub(r"[^\w\s:-]", "", title)
    title = re.sub(r"\s+", " ", title)
    return title

def generate_uid(title: str) -> str:
    normalized = normalize_title(title)
    return hashlib.md5(normalized.encode("utf-8")).hexdigest() + "@firstshowing.net"
