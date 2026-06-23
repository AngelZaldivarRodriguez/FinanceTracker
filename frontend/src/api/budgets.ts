import { api } from './client'

export interface Budget {
  id: string
  categoryId: string
  categoryName: string
  categoryIcon: string
  categoryColor: string
  limitAmount: number
  spentAmount: number
  percentage: number
  month: number
  year: number
}

export const budgetsApi = {
  getAll: (month?: number, year?: number) =>
    api.get<Budget[]>('/budgets', { params: { month, year } }).then((r) => r.data),

  create: (data: { categoryId: string; limitAmount: number; month: number; year: number }) =>
    api.post<Budget>('/budgets', data).then((r) => r.data),

  delete: (id: string) => api.delete(`/budgets/${id}`),
}
