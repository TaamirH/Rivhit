import React, { createContext, useContext, useMemo, useState } from 'react'
import { api } from '../lib/api'
import { clearAuthState, loadAuthState, saveAuthState } from '../lib/authStorage'
import type { AuthState } from '../lib/authStorage'

type AuthContextValue = {
  auth: AuthState | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

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

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

