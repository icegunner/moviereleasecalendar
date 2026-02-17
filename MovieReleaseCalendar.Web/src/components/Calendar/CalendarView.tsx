import { useRef, useCallback, useState, useEffect } from 'react';
import { createRoot } from 'react-dom/client';
import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import type { EventContentArg, DatesSetArg } from '@fullcalendar/core';
import { fetchCalendarEvents } from '../../api/calendarApi';
import type { MovieCalendarEvent, CalendarViewType } from '../../types';
import MovieEvent from './MovieEvent';
import MovieTooltip from './MovieTooltip';
import YearView from './YearView';
import WeekView from './WeekView';
import { usePreferences } from '../../hooks/usePreferences';

interface CalendarViewProps {
  view: CalendarViewType;
  onMovieClick: (movie: MovieCalendarEvent) => void;
}

/**
 * Manages an imperative tooltip that does NOT cause CalendarView to re-render.
 * A hidden container div is appended to document.body; we render/unmount
 * MovieTooltip into it without touching any state in CalendarView.
 */
function useImperativeTooltip() {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const rootRef = useRef<ReturnType<typeof createRoot> | null>(null);

  useEffect(() => {
    const div = document.createElement('div');
    div.id = 'movie-tooltip-container';
    document.body.appendChild(div);
    containerRef.current = div;
    rootRef.current = createRoot(div);
    return () => {
      rootRef.current?.unmount();
      div.remove();
    };
  }, []);

  const show = useCallback((movie: MovieCalendarEvent, x: number, y: number) => {
    rootRef.current?.render(<MovieTooltip movie={movie} x={x} y={y} />);
  }, []);

  const hide = useCallback(() => {
    rootRef.current?.render(null);
  }, []);

  return { show, hide };
}

export default function CalendarView({ view, onMovieClick }: CalendarViewProps) {
  const calendarRef = useRef<FullCalendar>(null);
  const [events, setEvents] = useState<MovieCalendarEvent[]>([]);
  const { prefs } = usePreferences();

  // Imperative tooltip — no state changes, no re-renders
  const tooltip = useImperativeTooltip();

  // Track the previous view so we know when the user switches
  const prevViewRef = useRef<CalendarViewType>(view);

  const loadEvents = useCallback(async (start?: string, end?: string) => {
    try {
      const data = await fetchCalendarEvents(start, end);
      setEvents(data);
    } catch (err) {
      console.error('Failed to load events', err);
    }
  }, []);

  // When the view changes, reload events with the appropriate range for the new view
  useEffect(() => {
    const viewChanged = prevViewRef.current !== view;
    prevViewRef.current = view;

    if (view === 'yearGrid') {
      const year = new Date().getFullYear();
      loadEvents(`${year}-01-01`, `${year + 1}-01-01`);
    } else if (view === 'dayGridWeek') {
      const now = new Date();
      const sun = new Date(now);
      sun.setDate(sun.getDate() - sun.getDay());
      const sat = new Date(sun);
      sat.setDate(sat.getDate() + 7);
      loadEvents(sun.toISOString().slice(0, 10), sat.toISOString().slice(0, 10));
    } else if (view === 'dayGridMonth') {
      if (viewChanged) {
        const now = new Date();
        const start = new Date(now.getFullYear(), now.getMonth(), 1);
        const end = new Date(now.getFullYear(), now.getMonth() + 1, 1);
        loadEvents(start.toISOString().slice(0, 10), end.toISOString().slice(0, 10));
      }
    }
  }, [view, loadEvents]);

  const handleDatesSet = useCallback(
    (arg: DatesSetArg) => {
      if (prevViewRef.current === 'dayGridMonth') {
        loadEvents(arg.startStr, arg.endStr);
      }
    },
    [loadEvents]
  );

  // Change FullCalendar view when prop changes
  useEffect(() => {
    const api = calendarRef.current?.getApi();
    if (api && view === 'dayGridMonth') {
      api.changeView(view);
    }
  }, [view]);

  // Stable refs for values used in renderEventContent so it doesn't change
  const showRatingsRef = useRef(prefs?.showRatings ?? true);
  showRatingsRef.current = prefs?.showRatings ?? true;
  const onMovieClickRef = useRef(onMovieClick);
  onMovieClickRef.current = onMovieClick;

  const renderEventContent = useCallback(
    (arg: EventContentArg) => {
      const movie = arg.event.extendedProps as MovieCalendarEvent;
      return (
        <MovieEvent
          movie={movie}
          showRatings={showRatingsRef.current}
          onMouseEnter={(e) => tooltip.show(movie, e.clientX, e.clientY)}
          onMouseLeave={tooltip.hide}
          onClick={() => { tooltip.hide(); onMovieClickRef.current(movie); }}
        />
      );
    },
    [tooltip]
  );

  if (view === 'yearGrid') {
    return <YearView events={events} showRatings={prefs?.showRatings ?? true} onMovieClick={onMovieClick} />;
  }

  if (view === 'dayGridWeek') {
    return <WeekView events={events} showRatings={prefs?.showRatings ?? true} onMovieClick={onMovieClick} />;
  }

  const fcEvents = events.map((ev) => ({
    title: ev.title,
    date: ev.date,
    allDay: ev.allDay,
    extendedProps: ev,
  }));

  return (
    <div className="relative">
      <FullCalendar
        ref={calendarRef}
        plugins={[dayGridPlugin]}
        initialView="dayGridMonth"
        events={fcEvents}
        eventContent={renderEventContent}
        datesSet={handleDatesSet}
        headerToolbar={{
          left: 'prev,next today',
          center: 'title',
          right: '',
        }}
        height="auto"
        dayMaxEvents={6}
        eventDisplay="block"
      />
    </div>
  );
}
