import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function NavBar() {
  const { auth, logout } = useAuth()
  const nav = useNavigate()
  const isAdmin = auth?.roles?.includes('Admin') ?? false

  return (
    <div
      style={{
        display: 'flex',
        gap: 12,
        padding: 12,
        borderBottom: '1px solid #ddd',
        alignItems: 'center',
      }}
    >
      <Link to="/dashboard">Dashboard</Link>
      <Link to="/history">History</Link>
      {isAdmin ? <Link to="/admin">Admin</Link> : null}

      <div style={{ flex: 1 }} />

      {auth ? (
        <>
          <span style={{ opacity: 0.8 }}>{auth.email}</span>
          <button
            onClick={() => {
              logout()
              nav('/login', { replace: true })
            }}
          >
            Logout
          </button>
        </>
      ) : (
        <Link to="/login">Login</Link>
      )}
    </div>
  )
}

