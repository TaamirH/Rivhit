import { type FormEvent, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function LoginPage() {
  const { login } = useAuth()
  const nav = useNavigate()
  const loc = useLocation()
  const from = (loc.state as { from?: string } | null)?.from ?? '/dashboard'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await login(email, password)
      nav(from, { replace: true })
    } catch (err: unknown) {
      setError('Login failed. Check your email and password and try again.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="container" style={{ maxWidth: 420 }}>
      <h1>Login</h1>
      <div className="card">
        <form onSubmit={onSubmit}>
        <label>
          Email
          <input value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
        </label>
        <label>
          Password
          <input
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            type="password"
            autoComplete="current-password"
          />
        </label>
        <button disabled={busy} type="submit">
          {busy ? 'Signing in...' : 'Sign in'}
        </button>
        </form>
        {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}
        <p style={{ marginBottom: 0 }}>
          No account? <Link to="/register">Register</Link>
        </p>
      </div>
    </div>
  )
}

