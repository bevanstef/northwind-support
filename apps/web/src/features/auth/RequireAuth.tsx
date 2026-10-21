import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

/**
 * Route guard (AU-12). Wraps every protected route in the router tree.
 * If the user is not authenticated it redirects to /login, preserving the
 * intended destination so the login page can redirect back after success.
 */
export function RequireAuth() {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
