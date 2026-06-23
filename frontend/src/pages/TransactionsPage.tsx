import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { transactionsApi } from '../api/transactions'
import { categoriesApi } from '../api/categories'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Trash2, Plus, Upload } from 'lucide-react'
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

export function TransactionsPage() {
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [importResult, setImportResult] = useState<{ imported: number; skipped: number } | null>(null)
  const [showForm, setShowForm] = useState(false)

  const { data: transactions = [], isLoading } = useQuery({
    queryKey: ['transactions'],
    queryFn: () => transactionsApi.getAll(),
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

  const importMutation = useMutation({
    mutationFn: transactionsApi.importBbva,
    onSuccess: (res) => {
      setImportResult(res)
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) importMutation.mutate(file)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Transacciones</h2>
        <div className="flex gap-2">
          <button
            onClick={() => fileRef.current?.click()}
            disabled={importMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
          >
            <Upload size={16} />
            {importMutation.isPending ? 'Importando...' : 'Importar BBVA'}
          </button>
          <input ref={fileRef} type="file" accept=".pdf" className="hidden" onChange={handleFileChange} />
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

      {showForm && (
        <form
          onSubmit={handleSubmit((d) => createMutation.mutate(d as any))}
          className="bg-white rounded-2xl border border-gray-200 p-6 grid grid-cols-2 gap-4"
        >
          <div className="col-span-2">
            <label className="text-sm font-medium text-gray-700">Descripción</label>
            <input
              {...register('description')}
              className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            {errors.description && <p className="text-red-500 text-xs mt-1">{errors.description.message}</p>}
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700">Monto</label>
            <input
              {...register('amount')}
              type="number"
              step="0.01"
              className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
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
            <input
              {...register('date')}
              type="date"
              className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
          <div className="col-span-2 flex justify-end gap-2">
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 text-sm border border-gray-300 rounded-lg">
              Cancelar
            </button>
            <button type="submit" disabled={isSubmitting} className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg disabled:opacity-50">
              Guardar
            </button>
          </div>
        </form>
      )}

      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
        {isLoading ? (
          <p className="text-gray-400 text-sm p-6">Cargando...</p>
        ) : transactions.length === 0 ? (
          <p className="text-gray-400 text-sm p-6">Sin transacciones. Importa tu estado de cuenta o agrega una manual.</p>
        ) : (
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
              {transactions.map((t) => (
                <tr key={t.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-900 max-w-xs truncate">{t.description}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {t.categoryIcon} {t.categoryName}
                  </td>
                  <td className="px-4 py-3 text-gray-500">
                    {new Date(t.date).toLocaleDateString('es-MX')}
                  </td>
                  <td className={`px-4 py-3 text-right font-semibold ${t.type === 'Income' ? 'text-green-600' : 'text-red-600'}`}>
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
        )}
      </div>
    </div>
  )
}
