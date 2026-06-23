import { api } from './client'

export interface DashboardData {
  totalIncome: number
  totalExpenses: number
  balance: number
  spendingByCategory: {
    categoryName: string
    categoryIcon: string
    categoryColor: string
    amount: number
    percentage: number
  }[]
  recentTransactions: {
    id: string
    description: string
    amount: number
    type: string
    date: string
    categoryName: string
    categoryIcon: string
    categoryColor: string
  }[]
  dailyFlow: {
    date: string
    income: number
    expenses: number
  }[]
}

export interface MonthlySummary {
  label: string
  income: number
  expenses: number
}

export const dashboardApi = {
  get: (month?: number, year?: number) =>
    api.get<DashboardData>('/dashboard', { params: { month, year } }).then((r) => r.data),

  getMonthly: (months = 6) =>
    api.get<MonthlySummary[]>('/dashboard/monthly', { params: { months } }).then((r) => r.data),
}
