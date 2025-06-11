from sqlalchemy import create_engine, Column, String, DateTime, Text, Date
from sqlalchemy.orm import declarative_base, sessionmaker
from datetime import datetime
import os

Base = declarative_base()
Session = sessionmaker()

class Movie(Base):
    __tablename__ = "movies"
    title = Column(String, primary_key=True)
    release_date = Column(Date, nullable=False)
    description = Column(Text, nullable=True)
    url = Column(String, nullable=True)
    added_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

def init_db():
    db_type = os.getenv("DB_TYPE", "postgres")
    user = os.getenv("DB_USER")
    pw = os.getenv("DB_PASS")
    host = os.getenv("DB_HOST")
    port = os.getenv("DB_PORT")
    name = os.getenv("DB_NAME")

    if db_type == "postgres":
        engine_str = f"postgresql://{user}:{pw}@{host}:{port}/{name}"
    elif db_type == "mariadb":
        engine_str = f"mysql+pymysql://{user}:{pw}@{host}:{port}/{name}"
    else:
        raise Exception("Unsupported DB_TYPE")

    engine = create_engine(engine_str, echo=False, future=True)
    Base.metadata.create_all(engine)
    Session.configure(bind=engine)

def upsert_movie(title, release_date, description, url):
    session = Session()
    try:
        movie = session.get(Movie, title)
        now = datetime.utcnow()
        if movie:
            updated = False
            if (movie.release_date != release_date or
                movie.url != url or
                movie.description != description):
                movie.release_date = release_date
                movie.url = url
                movie.description = description
                movie.updated_at = now
                updated = True
            if updated:
                print(f"Updated: {title}")
        else:
            movie = Movie(
                title=title,
                release_date=release_date,
                description=description,
                url=url,
                added_at=now,
                updated_at=now
            )
            session.add(movie)
            print(f"Added: {title}")
        session.commit()
    except Exception as e:
        print(f"DB error: {e}")
        session.rollback()
    finally:
        session.close()

def delete_removed_movies(seen_titles):
    session = Session()
    try:
        all_titles = {m.title for m in session.query(Movie.title).all()}
        to_delete = all_titles - seen_titles
        if to_delete:
            session.query(Movie).filter(Movie.title.in_(to_delete)).delete(synchronize_session=False)
            session.commit()
            print(f"Removed: {to_delete}")
    except Exception as e:
        print(f"Error deleting stale entries: {e}")
        session.rollback()
    finally:
        session.close()

def get_all_movies():
    session = Session()
    try:
        return session.query(Movie).all()
    finally:
        session.close()
