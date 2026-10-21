import { createBrowserRouter, Navigate } from 'react-router-dom';
import { App } from './App';
import { LoginPage } from './features/auth/LoginPage';
import { RequireAuth } from './features/auth/RequireAuth';
import { NotFoundPage } from './features/NotFoundPage';
import { CustomerDetailPage } from './features/customers/CustomerDetailPage';
import { CustomerListPage } from './features/customers/CustomerListPage';
import { CreateTicketPage } from './features/tickets/CreateTicketPage';
import { TicketDetailPage } from './features/tickets/TicketDetailPage';
import { TicketListPage } from './features/tickets/TicketListPage';

/**
 * Route tree:
 *   /login                — public, no App shell
 *   RequireAuth           — AU-12: redirects to /login when unauthenticated
 *     App                 — header + nav shell
 *       /               → /tickets
 *       /tickets, /customers, …
 */
export const router = createBrowserRouter([
  // ── Public ─────────────────────────────────────────────────────────────────
  { path: '/login', element: <LoginPage /> },

  // ── Protected (AU-12) ───────────────────────────────────────────────────────
  {
    element: <RequireAuth />,
    children: [
      {
        element: <App />,
        children: [
          { path: '/', element: <Navigate to="/tickets" replace /> },
          { path: '/tickets', element: <TicketListPage /> },
          { path: '/tickets/new', element: <CreateTicketPage /> },
          { path: '/tickets/:id', element: <TicketDetailPage /> },
          { path: '/customers', element: <CustomerListPage /> },
          { path: '/customers/:id', element: <CustomerDetailPage /> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);
