from ics import Calendar, Event
from db import get_all_movies
from util import generate_uid

def create_ics(movies):
    cal = Calendar()
    for m in movies:
        e = Event()
        e.name = f"🍿 {m.title}"
        e.begin = m.release_date.isoformat()
        e.make_all_day()
        e.description = m.description
        e.url = m.url
        e.uid = generate_uid(m.title)
        cal.events.add(e)

    with open("/app/calendar.ics", "w") as f:
        f.writelines(cal)
