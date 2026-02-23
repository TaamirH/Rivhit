import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function RequireAuth() {
  const { auth } = useAuth()
  const loc = useLocation()

  if (!auth?.accessToken) {
    return <Navigate to="/login" replace state={{ from: loc.pathname }} />
  }

  return <Outlet />
}

