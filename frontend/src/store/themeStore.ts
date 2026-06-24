import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface ThemeState {
  dark: boolean
  toggle: () => void
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      dark: false,
      toggle: () => {
        const next = !get().dark
        document.documentElement.classList.toggle('dark', next)
        set({ dark: next })
      },
    }),
    { name: 'theme' }
  )
)

export function initTheme() {
  const stored = localStorage.getItem('theme')
  if (stored) {
    const { state } = JSON.parse(stored)
    if (state?.dark) document.documentElement.classList.add('dark')
  }
}
