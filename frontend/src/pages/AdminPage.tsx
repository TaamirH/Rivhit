import { useAuth } from '../auth/AuthContext'

export function AdminPage() {
  const { auth } = useAuth()
  const isAdmin = auth?.roles?.includes('Admin') ?? false

  return (
    <div style={{ maxWidth: 720, margin: '20px auto' }}>
      <h1>Admin</h1>
      {!isAdmin ? <p>You are not an admin.</p> : <p>Admin tools will be added in the hardening step.</p>}
    </div>
  )
}

