import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from './features/auth/AuthContext';
import { router } from './routes';
import './index.css';

const container = document.getElementById('root');

if (!container) {
  throw new Error('Root element is missing from index.html.');
}

// AuthProvider sits above the router so auth context is available to every
// route (both public /login and protected pages) within the same React tree.
createRoot(container).render(
  <StrictMode>
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  </StrictMode>,
);
