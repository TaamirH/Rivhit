import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function NavBar() {
  const { auth, logout } = useAuth()
  const nav = useNavigate()
  const isAdmin = auth?.roles?.includes('Admin') ?? false

  return (
    <div className="nav">
      <div className="navInner">
        <Link to="/dashboard">Dashboard</Link>
        <Link to="/history">History</Link>
        {isAdmin ? <Link to="/admin">Admin</Link> : null}

        <div className="navSpacer" />

        {auth ? (
          <>
            <span className="navEmail">{auth.email}</span>
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
    </div>
  )
}

