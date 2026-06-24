import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { budgetsApi } from '../api/budgets'
import { categoriesApi } from '../api/categories'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Trash2, Plus } from 'lucide-react'
import { useState } from 'react'

const fmt = (n: number) =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(n)

const schema = z.object({
  categoryId: z.string().uuid('Selecciona una categoría'),
  limitAmount: z.coerce.number().positive('Debe ser mayor a 0'),
  month: z.coerce.number().min(1).max(12),
  year: z.coerce.number().min(2020).max(2100),
})

type FormData = z.infer<typeof schema>

const inputCls = 'mt-1 w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white'

export function BudgetsPage() {
  const qc = useQueryClient()
  const now = new Date()
  const [showForm, setShowForm] = useState(false)

  const { data: budgets = [], isLoading } = useQuery({
    queryKey: ['budgets', now.getMonth() + 1, now.getFullYear()],
    queryFn: () => budgetsApi.getAll(now.getMonth() + 1, now.getFullYear()),
  })

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  })

  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: { month: now.getMonth() + 1, year: now.getFullYear() },
  })

  const createMutation = useMutation({
    mutationFn: budgetsApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['budgets'] }); reset(); setShowForm(false) },
  })

  const deleteMutation = useMutation({
    mutationFn: budgetsApi.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['budgets'] }),
  })

  const barColor = (pct: number) => {
    if (pct >= 100) return 'bg-red-500'
    if (pct >= 80) return 'bg-yellow-400'
    return 'bg-green-500'
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Presupuestos</h2>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          <Plus size={16} />
          Nuevo
        </button>
      </div>

      {showForm && (
        <form
          onSubmit={handleSubmit((d) => createMutation.mutate(d as any))}
          className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 p-6 grid grid-cols-2 gap-4"
        >
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Categoría</label>
            <select {...register('categoryId')} className={inputCls}>
              <option value="">Selecciona...</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>{c.icon} {c.name}</option>
              ))}
            </select>
            {errors.categoryId && <p className="text-red-500 text-xs mt-1">{errors.categoryId.message}</p>}
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Límite (MXN)</label>
            <input {...register('limitAmount')} type="number" step="0.01" className={inputCls} />
            {errors.limitAmount && <p className="text-red-500 text-xs mt-1">{errors.limitAmount.message}</p>}
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Mes</label>
            <input {...register('month')} type="number" min={1} max={12} className={inputCls} />
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Año</label>
            <input {...register('year')} type="number" className={inputCls} />
          </div>
          <div className="col-span-2 flex justify-end gap-2">
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 text-sm border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700">
              Cancelar
            </button>
            <button type="submit" className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700">
              Guardar
            </button>
          </div>
        </form>
      )}

      {isLoading ? (
        <p className="text-gray-400 text-sm">Cargando...</p>
      ) : budgets.length === 0 ? (
        <p className="text-gray-400 text-sm">Sin presupuestos este mes.</p>
      ) : (
        <div className="grid grid-cols-2 gap-4">
          {budgets.map((b) => (
            <div key={b.id} className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 p-5">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <span className="text-xl">{b.categoryIcon}</span>
                  <span className="font-medium text-gray-900 dark:text-white">{b.categoryName}</span>
                </div>
                <div className="flex items-center gap-2">
                  {b.percentage >= 80 && (
                    <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-yellow-50 dark:bg-yellow-900/20 text-yellow-700 dark:text-yellow-400">
                      {b.percentage >= 100 ? '⚠ Excedido' : '⚡ Cerca del límite'}
                    </span>
                  )}
                  <button onClick={() => deleteMutation.mutate(b.id)} className="text-gray-400 hover:text-red-500">
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
              <div className="w-full bg-gray-100 dark:bg-gray-700 rounded-full h-2 mb-2">
                <div className={`h-2 rounded-full transition-all ${barColor(b.percentage)}`} style={{ width: `${Math.min(b.percentage, 100)}%` }} />
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-500 dark:text-gray-400">{fmt(b.spentAmount)} gastado</span>
                <span className="font-medium text-gray-900 dark:text-white">{fmt(b.limitAmount)} límite</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
