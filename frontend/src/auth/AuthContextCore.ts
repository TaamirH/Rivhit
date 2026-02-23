import { createContext } from 'react'
import type { AuthState } from '../lib/authStorage'

export type AuthContextValue = {
  auth: AuthState | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

