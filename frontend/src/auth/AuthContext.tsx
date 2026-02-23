import React, { useMemo, useState } from 'react'
import { AuthContext, type AuthContextValue } from './AuthContextCore'
import { api } from '../lib/api'
import { clearAuthState, loadAuthState, saveAuthState } from '../lib/authStorage'
import type { AuthState } from '../lib/authStorage'

type AuthResponse = {
  accessToken: string
  expiresAtUtc: string
  userId: string
  email: string
  roles: string[]
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(() => loadAuthState())

  async function login(email: string, password: string) {
    const res = await api.post<AuthResponse>('/auth/login', { email, password })
    setAuth(res.data)
    saveAuthState(res.data)
  }

  async function register(email: string, password: string) {
    const res = await api.post<AuthResponse>('/auth/register', { email, password })
    setAuth(res.data)
    saveAuthState(res.data)
  }

  function logout() {
    clearAuthState()
    setAuth(null)
  }

  const value = useMemo<AuthContextValue>(
    () => ({ auth, login, register, logout }),
    [auth],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

