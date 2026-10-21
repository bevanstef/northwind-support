import { type FormEvent, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { login } from '../../api/auth';
import { ApiError, toErrorMessage } from '../../api/client';
import { useAuth } from './AuthContext';

/**
 * AU-9: Login page — renders a branded email + password form, calls
 * POST /api/auth/login, and on success redirects to the originally
 * requested page (or / by default).
 */
export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: Location } | null)?.from?.pathname ?? '/';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await login({ email: email.trim(), password });
      signIn(response.token, response.user);
      navigate(from, { replace: true });
    } catch (err) {
      // A 401 from the login endpoint means wrong credentials.
      // Show the same generic message regardless to prevent enumeration (AU-4).
      if (err instanceof ApiError && err.status === 401) {
        setError('Invalid email or password.');
      } else {
        setError(toErrorMessage(err, 'Could not sign in. Please try again.'));
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-card__brand">
          <span className="login-card__logo" aria-hidden="true">🎫</span>
          <h1 className="login-card__title">Northwind Support</h1>
          <p className="login-card__subtitle">Sign in to your agent account</p>
        </div>

        <form
          id="login-form"
          className="login-card__form"
          onSubmit={handleSubmit}
          noValidate
        >
          {error && (
            <p id="login-error" role="alert" className="login-card__error">
              {error}
            </p>
          )}

          <div className="field">
            <label htmlFor="login-email" className="field__label">
              Email address
            </label>
            <input
              id="login-email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={e => setEmail(e.target.value)}
              aria-describedby={error ? 'login-error' : undefined}
              aria-invalid={!!error || undefined}
            />
          </div>

          <div className="field">
            <label htmlFor="login-password" className="field__label">
              Password
            </label>
            <input
              id="login-password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={e => setPassword(e.target.value)}
              aria-invalid={!!error || undefined}
            />
          </div>

          <button
            id="login-submit"
            type="submit"
            className="button button--primary login-card__submit"
            disabled={loading}
          >
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <p className="login-card__hint">
          Only Northwind Support agents can access this application.
        </p>
      </div>
    </div>
  );
}
