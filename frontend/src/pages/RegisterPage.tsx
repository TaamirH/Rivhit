import { type FormEvent, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import axios from 'axios'

type BackendError =
  | {
      message?: string
      errors?: Array<{ code?: string; description?: string }>
    }

function extractRegisterError(err: unknown): string {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5150'

  if (axios.isAxiosError(err)) {
    // Network/CORS/wrong URL: no response at all.
    if (!err.response) {
      const code = err.code ? ` (${err.code})` : ''
      return `Could not reach the API at ${apiBaseUrl}.${code} ${err.message}. Make sure the backend is running, the URL is correct, and CORS allows your frontend origin.`
    }

    const status = err.response.status
    const data = err.response.data as unknown

    if (typeof data === 'string') {
      return `Registration failed (HTTP ${status}). ${data}`
    }

    if (data && typeof data === 'object') {
      const typed = data as BackendError
      if (typed.message) return typed.message

      const descriptions = typed.errors
        ?.map((e: { description?: string }) => e.description)
        .filter(Boolean) as string[] | undefined
      if (descriptions?.length) return descriptions.join(' ')

      return `Registration failed (HTTP ${status}). ${JSON.stringify(data)}`
    }

    return `Registration failed (HTTP ${status}).`
  }

  if (err instanceof Error) {
    return `Registration failed. ${err.message}`
  }

  return 'Registration failed for an unknown reason.'
}

function validateEmail(email: string): string | null {
  const e = email.trim()
  if (!e) return 'Email is required.'
  // Simple check only; server is the source of truth.
  if (!/^\S+@\S+\.\S+$/.test(e)) return 'Email must look like a valid email address (name@example.com).'
  return null
}

function validatePassword(pw: string): string | null {
  if (!pw) return 'Password is required.'
  if (pw.length < 8) return 'Password must be at least 8 characters.'
  if (!/[a-z]/.test(pw)) return 'Password must include at least one lowercase letter (a-z).'
  if (!/[A-Z]/.test(pw)) return 'Password must include at least one uppercase letter (A-Z).'
  if (!/[0-9]/.test(pw)) return 'Password must include at least one number (0-9).'
  return null
}

export function RegisterPage() {
  const { register } = useAuth()
  const nav = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const emailError = validateEmail(email)
  const passwordError = validatePassword(password)
  const canSubmit = !busy && !emailError && !passwordError

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await register(email, password)
      nav('/dashboard', { replace: true })
    } catch (err: unknown) {
      setError(extractRegisterError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="container" style={{ maxWidth: 420 }}>
      <h1>Register</h1>
      <div className="card">
        <form onSubmit={onSubmit}>
        <label>
          Email
          <input
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
            inputMode="email"
            required
          />
        </label>
        {emailError ? <p style={{ color: 'crimson', marginTop: 0 }}>{emailError}</p> : null}
        <label>
          Password
          <input
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
          />
        </label>
        {passwordError ? <p style={{ color: 'crimson', marginTop: 0 }}>{passwordError}</p> : null}
        <button disabled={busy} type="submit">
          {busy ? 'Creating...' : 'Create account'}
        </button>
        {!canSubmit && !busy ? <p style={{ marginBottom: 0 }}>Fix the errors above to continue.</p> : null}
        </form>
        {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}
        <p style={{ marginBottom: 0 }}>
          Have an account? <Link to="/login">Login</Link>
        </p>
        <div style={{ marginTop: 12 }}>
          <p style={{ margin: '0 0 8px 0' }}>
            <b>Password requirements</b>
          </p>
          <p style={{ margin: 0 }}>- At least 8 characters</p>
          <p style={{ margin: 0 }}>- At least one uppercase letter (A-Z)</p>
          <p style={{ margin: 0 }}>- At least one lowercase letter (a-z)</p>
          <p style={{ margin: 0 }}>- At least one number (0-9)</p>
        </div>
      </div>
    </div>
  )
}

