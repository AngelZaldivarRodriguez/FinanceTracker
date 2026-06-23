import { api } from './client'

export interface Category {
  id: string
  name: string
  icon: string
  color: string
  isDefault: boolean
}

export const categoriesApi = {
  getAll: () => api.get<Category[]>('/categories').then((r) => r.data),

  create: (data: { name: string; icon: string; color: string }) =>
    api.post<Category>('/categories', data).then((r) => r.data),

  delete: (id: string) => api.delete(`/categories/${id}`),
}
