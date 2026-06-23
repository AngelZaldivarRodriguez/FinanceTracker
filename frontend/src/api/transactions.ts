import { api } from './client'

export type TransactionType = 'Income' | 'Expense'

export interface Transaction {
  id: string
  amount: number
  type: TransactionType
  description: string
  date: string
  categoryId: string
  categoryName: string
  categoryIcon: string
  categoryColor: string
  isImported: boolean
}

export interface PagedTransactions {
  items: Transaction[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CreateTransactionDto {
  amount: number
  type: TransactionType
  description: string
  date: string
  categoryId: string
}

export interface TransactionFilters {
  from?: string
  to?: string
  categoryId?: string
  type?: string
  search?: string
  page?: number
  pageSize?: number
}

export const transactionsApi = {
  getAll: (params?: TransactionFilters) =>
    api.get<PagedTransactions>('/transactions', { params }).then((r) => r.data),

  create: (data: CreateTransactionDto) =>
    api.post<Transaction>('/transactions', data).then((r) => r.data),

  delete: (id: string) => api.delete(`/transactions/${id}`),

  deleteAll: () => api.delete('/transactions'),

  importBbva: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.post<{ imported: number; skipped: number; errors: string[] }>('/import/bbva', form).then((r) => r.data)
  },
}
