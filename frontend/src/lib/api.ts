import axios from 'axios'
import { loadAuthState } from './authStorage'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5150'

export const api = axios.create({
  baseURL,
})

api.interceptors.request.use((config) => {
  const auth = loadAuthState()
  if (auth?.accessToken) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${auth.accessToken}`
  }
  return config
})

