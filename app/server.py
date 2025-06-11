from flask import Flask, send_file, jsonify, render_template_string
from db import get_all_movies

app = Flask(__name__)

@app.route("/calendar.ics")
def serve_ics():
    return send_file("/app/calendar.ics", mimetype="text/calendar")

@app.route("/calendar.json")
def serve_json():
    movies = get_all_movies()
    return jsonify([
        {
            "title": f"🍿 {m.title}",
            "start": m.release_date.isoformat(),
            "allDay": True,
            "url": m.url,
            "description": m.description
        } for m in movies
    ])

@app.route("/")
def calendar_page():
    return render_template_string(open("/app/calendar.html").read())

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8080)
