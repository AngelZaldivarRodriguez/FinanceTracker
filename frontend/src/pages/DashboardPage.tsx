import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../api/dashboard'
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts'
import { TrendingUp, TrendingDown, Wallet } from 'lucide-react'

const fmt = (n: number) =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(n)

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString('es-MX', { day: '2-digit', month: 'short' })

export function DashboardPage() {
  const now = new Date()
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', now.getMonth() + 1, now.getFullYear()],
    queryFn: () => dashboardApi.get(now.getMonth() + 1, now.getFullYear()),
  })

  if (isLoading) return <div className="text-gray-400 text-sm">Cargando...</div>
  if (!data) return null

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-gray-900">Dashboard</h2>

      {/* Cards */}
      <div className="grid grid-cols-3 gap-4">
        <StatCard
          label="Balance del mes"
          value={fmt(data.balance)}
          icon={<Wallet size={20} />}
          color={data.balance >= 0 ? 'text-blue-600' : 'text-red-600'}
          bg="bg-blue-50"
        />
        <StatCard
          label="Ingresos"
          value={fmt(data.totalIncome)}
          icon={<TrendingUp size={20} />}
          color="text-green-600"
          bg="bg-green-50"
        />
        <StatCard
          label="Gastos"
          value={fmt(data.totalExpenses)}
          icon={<TrendingDown size={20} />}
          color="text-red-600"
          bg="bg-red-50"
        />
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Pie chart */}
        <div className="bg-white rounded-2xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Gastos por categoría</h3>
          {data.spendingByCategory.length === 0 ? (
            <p className="text-gray-400 text-sm">Sin gastos este mes</p>
          ) : (
            <>
              <ResponsiveContainer width="100%" height={220}>
                <PieChart>
                  <Pie
                    data={data.spendingByCategory}
                    dataKey="amount"
                    nameKey="categoryName"
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                  >
                    {data.spendingByCategory.map((entry, i) => (
                      <Cell key={i} fill={entry.categoryColor} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(v) => fmt(Number(v))} />
                </PieChart>
              </ResponsiveContainer>
              <ul className="mt-2 space-y-1">
                {data.spendingByCategory.slice(0, 5).map((c) => (
                  <li key={c.categoryName} className="flex items-center justify-between text-sm">
                    <span className="flex items-center gap-2">
                      <span>{c.categoryIcon}</span>
                      <span className="text-gray-700">{c.categoryName}</span>
                    </span>
                    <span className="font-medium text-gray-900">{c.percentage}%</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </div>

        {/* Recent transactions */}
        <div className="bg-white rounded-2xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Últimas transacciones</h3>
          {data.recentTransactions.length === 0 ? (
            <p className="text-gray-400 text-sm">Sin transacciones</p>
          ) : (
            <ul className="space-y-3">
              {data.recentTransactions.map((t) => (
                <li key={t.id} className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <span className="text-lg">{t.categoryIcon}</span>
                    <div>
                      <p className="text-sm font-medium text-gray-900 truncate max-w-[180px]">{t.description}</p>
                      <p className="text-xs text-gray-400">{formatDate(t.date)}</p>
                    </div>
                  </div>
                  <span className={`text-sm font-semibold ${t.type === 'Income' ? 'text-green-600' : 'text-red-600'}`}>
                    {t.type === 'Income' ? '+' : '-'}{fmt(t.amount)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  )
}

function StatCard({ label, value, icon, color, bg }: {
  label: string
  value: string
  icon: React.ReactNode
  color: string
  bg: string
}) {
  return (
    <div className="bg-white rounded-2xl border border-gray-200 p-5">
      <div className={`inline-flex p-2 rounded-xl ${bg} ${color} mb-3`}>{icon}</div>
      <p className="text-sm text-gray-500">{label}</p>
      <p className={`text-2xl font-bold mt-1 ${color}`}>{value}</p>
    </div>
  )
}
