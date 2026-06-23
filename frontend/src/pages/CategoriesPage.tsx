import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { categoriesApi } from '../api/categories'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Trash2, Plus } from 'lucide-react'
import { useState } from 'react'

const schema = z.object({
  name: z.string().min(1, 'Requerido'),
  icon: z.string().min(1, 'Requerido'),
  color: z.string().min(1, 'Requerido'),
})

type FormData = z.infer<typeof schema>

export function CategoriesPage() {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)

  const { data: categories = [], isLoading } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  })

  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { icon: '📦', color: '#6b7280' },
  })

  const createMutation = useMutation({
    mutationFn: categoriesApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      reset()
      setShowForm(false)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: categoriesApi.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories'] }),
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Categorías</h2>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          <Plus size={16} />
          Nueva
        </button>
      </div>

      {showForm && (
        <form
          onSubmit={handleSubmit((d) => createMutation.mutate(d))}
          className="bg-white rounded-2xl border border-gray-200 p-6 flex gap-4 items-end"
        >
          <div className="flex-1">
            <label className="text-sm font-medium text-gray-700">Nombre</label>
            <input {...register('name')} className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name.message}</p>}
          </div>
          <div className="w-24">
            <label className="text-sm font-medium text-gray-700">Ícono</label>
            <input {...register('icon')} className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-center" />
          </div>
          <div className="w-24">
            <label className="text-sm font-medium text-gray-700">Color</label>
            <input {...register('color')} type="color" className="mt-1 w-full h-9 border border-gray-300 rounded-lg cursor-pointer" />
          </div>
          <div className="flex gap-2">
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 text-sm border border-gray-300 rounded-lg">
              Cancelar
            </button>
            <button type="submit" className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg">
              Guardar
            </button>
          </div>
        </form>
      )}

      {isLoading ? (
        <p className="text-gray-400 text-sm">Cargando...</p>
      ) : (
        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-gray-600 font-medium">Categoría</th>
                <th className="text-left px-4 py-3 text-gray-600 font-medium">Tipo</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {categories.map((c) => (
                <tr key={c.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div
                        className="w-8 h-8 rounded-lg flex items-center justify-center text-base"
                        style={{ backgroundColor: c.color + '22' }}
                      >
                        {c.icon}
                      </div>
                      <span className="font-medium text-gray-900">{c.name}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${c.isDefault ? 'bg-gray-100 text-gray-600' : 'bg-blue-50 text-blue-700'}`}>
                      {c.isDefault ? 'Predeterminada' : 'Personalizada'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    {!c.isDefault && (
                      <button
                        onClick={() => deleteMutation.mutate(c.id)}
                        className="text-gray-400 hover:text-red-500 transition-colors"
                      >
                        <Trash2 size={16} />
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
