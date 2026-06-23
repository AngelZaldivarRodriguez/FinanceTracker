import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../api/dashboard'
import { budgetsApi } from '../api/budgets'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  LineChart, Line, CartesianGrid, Legend,
} from 'recharts'
import { TrendingUp, TrendingDown, Wallet, AlertTriangle } from 'lucide-react'

const fmt = (n: number) =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 }).format(n)

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString('es-MX', { day: '2-digit', month: 'short' })

export function DashboardPage() {
  const now = new Date()
  const month = now.getMonth() + 1
  const year = now.getFullYear()

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', month, year],
    queryFn: () => dashboardApi.get(month, year),
  })

  const { data: monthly = [] } = useQuery({
    queryKey: ['dashboard-monthly'],
    queryFn: () => dashboardApi.getMonthly(6),
  })

  const { data: budgets = [] } = useQuery({
    queryKey: ['budgets', month, year],
    queryFn: () => budgetsApi.getAll(month, year),
  })

  if (isLoading) return <div className="text-gray-400 text-sm">Cargando...</div>
  if (!data) return null

  const budgetsAtRisk = budgets.filter(b => b.percentage >= 80).sort((a, b) => b.percentage - a.percentage)

  // Flujo acumulado del mes
  let cumulative = 0
  const cumulativeFlow = data.dailyFlow.map(d => {
    cumulative += d.income - d.expenses
    return { date: d.date.slice(8, 10), neto: cumulative }
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Dashboard</h2>
        <span className="text-sm text-gray-500">
          {now.toLocaleDateString('es-MX', { month: 'long', year: 'numeric' })}
        </span>
      </div>

      {/* Cards */}
      <div className="grid grid-cols-3 gap-4">
        <StatCard
          label="Balance del mes"
          value={fmt(data.balance)}
          icon={<Wallet size={20} />}
          color={data.balance >= 0 ? 'text-blue-600' : 'text-red-600'}
          bg={data.balance >= 0 ? 'bg-blue-50' : 'bg-red-50'}
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

      {/* Ingresos vs Gastos por mes + Flujo acumulado */}
      <div className="grid grid-cols-2 gap-6">
        <div className="bg-white rounded-2xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Ingresos vs Gastos — últimos 6 meses</h3>
          {monthly.length === 0 ? (
            <p className="text-gray-400 text-sm">Sin datos</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={monthly} barGap={4}>
                <XAxis dataKey="label" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} tickFormatter={(v) => `$${(v/1000).toFixed(0)}k`} />
                <Tooltip formatter={(v: number) => fmt(v)} />
                <Legend />
                <Bar dataKey="income" name="Ingresos" fill="#22c55e" radius={[4, 4, 0, 0]} />
                <Bar dataKey="expenses" name="Gastos" fill="#ef4444" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Flujo acumulado del mes</h3>
          {cumulativeFlow.length === 0 ? (
            <p className="text-gray-400 text-sm">Sin movimientos este mes</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <LineChart data={cumulativeFlow}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} tickFormatter={(v) => `$${(v/1000).toFixed(0)}k`} />
                <Tooltip formatter={(v: number) => fmt(v)} labelFormatter={(l) => `Día ${l}`} />
                <Line
                  type="monotone"
                  dataKey="neto"
                  name="Neto acumulado"
                  stroke="#3b82f6"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Gasto por categoría + Presupuestos en riesgo */}
      <div className="grid grid-cols-2 gap-6">
        <div className="bg-white rounded-2xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Gasto por categoría este mes</h3>
          {data.spendingByCategory.length === 0 ? (
            <p className="text-gray-400 text-sm">Sin gastos este mes</p>
          ) : (
            <ul className="space-y-3">
              {data.spendingByCategory.map((c) => (
                <li key={c.categoryName}>
                  <div className="flex items-center justify-between mb-1">
                    <span className="flex items-center gap-2 text-sm text-gray-700">
                      <span>{c.categoryIcon}</span>
                      {c.categoryName}
                    </span>
                    <div className="text-right">
                      <span className="text-sm font-semibold text-gray-900">{fmt(c.amount)}</span>
                      <span className="text-xs text-gray-400 ml-2">{c.percentage}%</span>
                    </div>
                  </div>
                  <div className="w-full bg-gray-100 rounded-full h-2">
                    <div
                      className="h-2 rounded-full transition-all"
                      style={{ width: `${c.percentage}%`, backgroundColor: c.categoryColor }}
                    />
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="space-y-6">
          {/* Presupuestos en riesgo */}
          <div className="bg-white rounded-2xl border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <AlertTriangle size={16} className="text-yellow-500" />
              Presupuestos en riesgo
            </h3>
            {budgetsAtRisk.length === 0 ? (
              <p className="text-gray-400 text-sm">Todos los presupuestos bajo control ✓</p>
            ) : (
              <ul className="space-y-3">
                {budgetsAtRisk.map(b => (
                  <li key={b.id}>
                    <div className="flex items-center justify-between mb-1">
                      <span className="flex items-center gap-2 text-sm text-gray-700">
                        <span>{b.categoryIcon}</span>
                        {b.categoryName}
                      </span>
                      <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${
                        b.percentage >= 100 ? 'bg-red-100 text-red-700' : 'bg-yellow-50 text-yellow-700'
                      }`}>
                        {Math.round(b.percentage)}%
                      </span>
                    </div>
                    <div className="w-full bg-gray-100 rounded-full h-2">
                      <div
                        className={`h-2 rounded-full ${b.percentage >= 100 ? 'bg-red-500' : 'bg-yellow-400'}`}
                        style={{ width: `${Math.min(b.percentage, 100)}%` }}
                      />
                    </div>
                    <p className="text-xs text-gray-400 mt-0.5">{fmt(b.spentAmount)} de {fmt(b.limitAmount)}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Últimas transacciones */}
          <div className="bg-white rounded-2xl border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Últimos movimientos</h3>
            <ul className="space-y-2">
              {data.recentTransactions.slice(0, 5).map((t) => (
                <li key={t.id} className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-base">{t.categoryIcon}</span>
                    <div>
                      <p className="text-sm font-medium text-gray-900 truncate max-w-[160px]">{t.description}</p>
                      <p className="text-xs text-gray-400">{formatDate(t.date)}</p>
                    </div>
                  </div>
                  <span className={`text-sm font-semibold ${t.type === 'Income' ? 'text-green-600' : 'text-red-600'}`}>
                    {t.type === 'Income' ? '+' : '-'}{fmt(t.amount)}
                  </span>
                </li>
              ))}
            </ul>
          </div>
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
