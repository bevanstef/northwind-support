import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from './features/auth/AuthContext';

/** Application shell: header (with auth info), navigation, and routed page content. */
export function App() {
  const { user, signOut } = useAuth();

  return (
    <div className="app">
      <header className="app__header">
        <Link className="app__brand" to="/tickets">
          Northwind Support
        </Link>

        <nav className="app__nav" aria-label="Main">
          <NavLink to="/tickets">Tickets</NavLink>
          <NavLink to="/customers">Customers</NavLink>
        </nav>

        {/* AU-13: Show the logged-in agent's name and a sign-out button. */}
        {user && (
          <div className="app__user">
            <span className="app__user-name" aria-label={`Signed in as ${user.fullName}`}>
              {user.fullName}
            </span>
            <button
              id="btn-logout"
              type="button"
              className="button button--ghost app__logout"
              onClick={signOut}
            >
              Sign out
            </button>
          </div>
        )}
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}
