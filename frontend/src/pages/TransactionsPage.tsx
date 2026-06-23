import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { transactionsApi, TransactionFilters } from '../api/transactions'
import { categoriesApi } from '../api/categories'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Trash2, Plus, Upload, Eraser, Search, ChevronLeft, ChevronRight, X } from 'lucide-react'
import { useRef, useState } from 'react'

const fmt = (n: number) =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(n)

const schema = z.object({
  description: z.string().min(1, 'Requerido'),
  amount: z.coerce.number().positive('Debe ser mayor a 0'),
  type: z.enum(['Income', 'Expense']),
  categoryId: z.string().uuid('Selecciona una categoría'),
  date: z.string().min(1, 'Requerido'),
})

type FormData = z.infer<typeof schema>

const PAGE_SIZE = 20

export function TransactionsPage() {
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [importResult, setImportResult] = useState<{ imported: number; skipped: number } | null>(null)
  const [showForm, setShowForm] = useState(false)

  const [filters, setFilters] = useState<TransactionFilters>({ page: 1, pageSize: PAGE_SIZE })
  const [search, setSearch] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['transactions', filters],
    queryFn: () => transactionsApi.getAll(filters),
    placeholderData: (prev) => prev,
  })

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  })

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: { type: 'Expense', date: new Date().toISOString().split('T')[0] },
  })

  const createMutation = useMutation({
    mutationFn: transactionsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      reset({ type: 'Expense', date: new Date().toISOString().split('T')[0] })
      setShowForm(false)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: transactionsApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })

  const deleteAllMutation = useMutation({
    mutationFn: transactionsApi.deleteAll,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      setImportResult(null)
    },
  })

  const importMutation = useMutation({
    mutationFn: transactionsApi.importBbva,
    onSuccess: (res) => {
      setImportResult(res)
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })

  const setFilter = (key: keyof TransactionFilters, value: string | number | undefined) => {
    setFilters(f => ({ ...f, [key]: value || undefined, page: 1 }))
  }

  const clearFilters = () => {
    setSearch('')
    setFilters({ page: 1, pageSize: PAGE_SIZE })
  }

  const hasActiveFilters = !!(filters.search || filters.type || filters.categoryId || filters.from || filters.to)

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 1
  const total = data?.total ?? 0
  const page = filters.page ?? 1

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Transacciones</h2>
          {total > 0 && (
            <p className="text-sm text-gray-400 mt-0.5">{total.toLocaleString()} movimientos</p>
          )}
        </div>
        <div className="flex gap-2">
          {total > 0 && (
            <button
              onClick={() => {
                if (window.confirm('¿Borrar TODAS las transacciones? Esta acción no se puede deshacer.'))
                  deleteAllMutation.mutate()
              }}
              disabled={deleteAllMutation.isPending}
              className="flex items-center gap-2 px-4 py-2 text-sm border border-red-200 text-red-600 rounded-lg hover:bg-red-50 disabled:opacity-50"
            >
              <Eraser size={16} />
              Borrar todo
            </button>
          )}
          <button
            onClick={() => fileRef.current?.click()}
            disabled={importMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
          >
            <Upload size={16} />
            {importMutation.isPending ? 'Importando...' : 'Importar BBVA'}
          </button>
          <input ref={fileRef} type="file" accept=".pdf" className="hidden" onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) importMutation.mutate(file)
          }} />
          <button
            onClick={() => setShowForm(!showForm)}
            className="flex items-center gap-2 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            <Plus size={16} />
            Nueva
          </button>
        </div>
      </div>

      {importResult && (
        <div className="bg-green-50 border border-green-200 rounded-lg px-4 py-3 text-sm text-green-800">
          Importadas: {importResult.imported} | Duplicadas (omitidas): {importResult.skipped}
        </div>
      )}

      {/* Form */}
      {showForm && (
        <form
          onSubmit={handleSubmit((d) => createMutation.mutate(d as any))}
          className="bg-white rounded-2xl border border-gray-200 p-6 grid grid-cols-2 gap-4"
        >
          <div className="col-span-2">
            <label className="text-sm font-medium text-gray-700">Descripción</label>
            <input {...register('description')} className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            {errors.description && <p className="text-red-500 text-xs mt-1">{errors.description.message}</p>}
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700">Monto</label>
            <input {...register('amount')} type="number" step="0.01" className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            {errors.amount && <p className="text-red-500 text-xs mt-1">{errors.amount.message}</p>}
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700">Tipo</label>
            <select {...register('type')} className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm">
              <option value="Expense">Gasto</option>
              <option value="Income">Ingreso</option>
            </select>
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700">Categoría</label>
            <select {...register('categoryId')} className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm">
              <option value="">Selecciona...</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>{c.icon} {c.name}</option>
              ))}
            </select>
            {errors.categoryId && <p className="text-red-500 text-xs mt-1">{errors.categoryId.message}</p>}
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700">Fecha</label>
            <input {...register('date')} type="date" className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <div className="col-span-2 flex justify-end gap-2">
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 text-sm border border-gray-300 rounded-lg">Cancelar</button>
            <button type="submit" disabled={isSubmitting} className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg disabled:opacity-50">Guardar</button>
          </div>
        </form>
      )}

      {/* Filtros */}
      <div className="bg-white rounded-2xl border border-gray-200 p-4">
        <div className="grid grid-cols-5 gap-3">
          {/* Búsqueda */}
          <div className="col-span-2 relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Buscar descripción..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && setFilter('search', search)}
              onBlur={() => setFilter('search', search)}
              className="w-full pl-9 pr-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Tipo */}
          <select
            value={filters.type ?? ''}
            onChange={(e) => setFilter('type', e.target.value)}
            className="text-sm border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Todos los tipos</option>
            <option value="Income">Ingresos</option>
            <option value="Expense">Gastos</option>
          </select>

          {/* Categoría */}
          <select
            value={filters.categoryId ?? ''}
            onChange={(e) => setFilter('categoryId', e.target.value)}
            className="text-sm border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Todas las categorías</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.icon} {c.name}</option>
            ))}
          </select>

          {/* Limpiar */}
          <button
            onClick={clearFilters}
            disabled={!hasActiveFilters}
            className="flex items-center justify-center gap-1.5 text-sm border border-gray-300 rounded-lg px-3 py-2 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <X size={14} />
            Limpiar
          </button>
        </div>

        {/* Fechas */}
        <div className="flex gap-3 mt-3">
          <div className="flex items-center gap-2">
            <span className="text-xs text-gray-500 whitespace-nowrap">Desde</span>
            <input
              type="date"
              value={filters.from ?? ''}
              onChange={(e) => setFilter('from', e.target.value)}
              className="text-sm border border-gray-300 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs text-gray-500 whitespace-nowrap">Hasta</span>
            <input
              type="date"
              value={filters.to ?? ''}
              onChange={(e) => setFilter('to', e.target.value)}
              className="text-sm border border-gray-300 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      </div>

      {/* Tabla */}
      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
        {isLoading ? (
          <p className="text-gray-400 text-sm p-6">Cargando...</p>
        ) : items.length === 0 ? (
          <p className="text-gray-400 text-sm p-6">
            {hasActiveFilters ? 'No hay transacciones con esos filtros.' : 'Sin transacciones. Importa tu estado de cuenta o agrega una manual.'}
          </p>
        ) : (
          <>
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-4 py-3 text-gray-600 font-medium">Descripción</th>
                  <th className="text-left px-4 py-3 text-gray-600 font-medium">Categoría</th>
                  <th className="text-left px-4 py-3 text-gray-600 font-medium">Fecha</th>
                  <th className="text-right px-4 py-3 text-gray-600 font-medium">Monto</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {items.map((t) => (
                  <tr key={t.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-900 max-w-xs">
                      <p className="truncate">{t.description}</p>
                      {t.isImported && (
                        <span className="text-xs text-gray-400">Importado</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      <span className="flex items-center gap-1.5">
                        <span
                          className="w-2 h-2 rounded-full flex-shrink-0"
                          style={{ backgroundColor: t.categoryColor }}
                        />
                        {t.categoryIcon} {t.categoryName}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-500 whitespace-nowrap">
                      {new Date(t.date).toLocaleDateString('es-MX', { day: '2-digit', month: 'short', year: 'numeric' })}
                    </td>
                    <td className={`px-4 py-3 text-right font-semibold whitespace-nowrap ${t.type === 'Income' ? 'text-green-600' : 'text-red-600'}`}>
                      {t.type === 'Income' ? '+' : '-'}{fmt(t.amount)}
                    </td>
                    <td className="px-4 py-3">
                      <button
                        onClick={() => deleteMutation.mutate(t.id)}
                        className="text-gray-400 hover:text-red-500 transition-colors"
                      >
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Paginación */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200">
                <p className="text-sm text-gray-500">
                  Mostrando {((page - 1) * PAGE_SIZE) + 1}–{Math.min(page * PAGE_SIZE, total)} de {total.toLocaleString()}
                </p>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => setFilters(f => ({ ...f, page: Math.max(1, (f.page ?? 1) - 1) }))}
                    disabled={page <= 1}
                    className="p-1.5 rounded-lg hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    <ChevronLeft size={18} />
                  </button>

                  {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
                    let p: number
                    if (totalPages <= 7) {
                      p = i + 1
                    } else if (page <= 4) {
                      p = i + 1
                    } else if (page >= totalPages - 3) {
                      p = totalPages - 6 + i
                    } else {
                      p = page - 3 + i
                    }
                    return (
                      <button
                        key={p}
                        onClick={() => setFilters(f => ({ ...f, page: p }))}
                        className={`w-8 h-8 text-sm rounded-lg ${
                          p === page
                            ? 'bg-blue-600 text-white font-semibold'
                            : 'hover:bg-gray-100 text-gray-600'
                        }`}
                      >
                        {p}
                      </button>
                    )
                  })}

                  <button
                    onClick={() => setFilters(f => ({ ...f, page: Math.min(totalPages, (f.page ?? 1) + 1) }))}
                    disabled={page >= totalPages}
                    className="p-1.5 rounded-lg hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    <ChevronRight size={18} />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
