import { create } from 'zustand'

interface AuthState {
  token: string | null
  user: { name: string; email: string } | null
  login: (token: string, name: string, email: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  token: localStorage.getItem('token'),
  user: (() => {
    const name = localStorage.getItem('userName')
    const email = localStorage.getItem('userEmail')
    return name && email ? { name, email } : null
  })(),

  login: (token, name, email) => {
    localStorage.setItem('token', token)
    localStorage.setItem('userName', name)
    localStorage.setItem('userEmail', email)
    set({ token, user: { name, email } })
  },

  logout: () => {
    localStorage.removeItem('token')
    localStorage.removeItem('userName')
    localStorage.removeItem('userEmail')
    set({ token: null, user: null })
  },
}))
