export type AuthState = {
  accessToken: string
  expiresAtUtc: string
  userId: string
  email: string
  roles: string[]
}

const KEY = 'rivhit.auth'

export function loadAuthState(): AuthState | null {
  try {
    const raw = localStorage.getItem(KEY)
    if (!raw) return null
    return JSON.parse(raw) as AuthState
  } catch {
    return null
  }
}

export function saveAuthState(state: AuthState) {
  localStorage.setItem(KEY, JSON.stringify(state))
}

export function clearAuthState() {
  localStorage.removeItem(KEY)
}

