export default function Footer() {
  return (
    <footer className="border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 py-3 px-4 text-center text-xs text-gray-500 dark:text-gray-400">
      <p>
        Movie Release Calendar &copy; {new Date().getFullYear()} &mdash; Data from{' '}
        <a
          href="https://www.firstshowing.net"
          target="_blank"
          rel="noopener noreferrer"
          className="text-primary-500 hover:underline"
        >
          firstshowing.net
        </a>{' '}
        &amp;{' '}
        <a
          href="https://www.themoviedb.org"
          target="_blank"
          rel="noopener noreferrer"
          className="text-primary-500 hover:underline"
        >
          TMDb
        </a>
      </p>
    </footer>
  );
}
