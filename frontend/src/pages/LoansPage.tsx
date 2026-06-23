import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { loansApi } from '../api/loans'

interface CreateLoanDto {
  name: string
  originalAmount: number
  annualRatePercent: number
  totalPayments: number
  monthlyPayment: number
  startDate: string
  carPrice: number
  downPayment: number
  initialPaidPayments: number
}
import { Car, CheckCircle2, Clock, Plus, ChevronDown, ChevronUp, AlertTriangle, X } from 'lucide-react'

const fmt = (n: number) =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 }).format(n)

const fmtDate = (iso: string) =>
  new Date(iso).toLocaleDateString('es-MX', { day: '2-digit', month: 'short', year: 'numeric' })

export function LoansPage() {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [selectedLoan, setSelectedLoan] = useState<string | null>(null)
  const [showAll, setShowAll] = useState(false)

  const { data: loans = [], isLoading } = useQuery({
    queryKey: ['loans'],
    queryFn: loansApi.getAll,
  })

  const { data: detail } = useQuery({
    queryKey: ['loan-detail', selectedLoan],
    queryFn: () => loansApi.getDetail(selectedLoan!),
    enabled: !!selectedLoan,
  })

  const createMutation = useMutation({
    mutationFn: loansApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['loans'] })
      setShowForm(false)
    },
  })

  const markPaidMutation = useMutation({
    mutationFn: ({ loanId, number }: { loanId: string; number: number }) =>
      loansApi.markPaid(loanId, number, new Date().toISOString()),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['loans'] })
      qc.invalidateQueries({ queryKey: ['loan-detail', selectedLoan] })
    },
  })

  const activeLoan = loans[0]
  const schedule = detail?.schedule ?? []
  const visibleRows = showAll ? schedule : schedule.slice(0, 12)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-blue-50 rounded-xl">
            <Car size={22} className="text-blue-600" />
          </div>
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Créditos</h2>
            <p className="text-sm text-gray-400">Seguimiento de préstamos y créditos</p>
          </div>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          <Plus size={16} />
          Nuevo crédito
        </button>
      </div>

      {/* Modal nuevo crédito */}
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md shadow-xl">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold">Nuevo crédito</h3>
              <button onClick={() => setShowForm(false)}><X size={18} className="text-gray-400" /></button>
            </div>
            <LoanForm
              onSubmit={(data) => createMutation.mutate(data)}
              loading={createMutation.isPending}
            />
          </div>
        </div>
      )}

      {isLoading && <p className="text-gray-400 text-sm">Cargando...</p>}

      {loans.length === 0 && !isLoading && (
        <div className="bg-white rounded-2xl border border-gray-200 p-12 text-center">
          <Car size={40} className="text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500">No tienes créditos registrados.</p>
          <button
            onClick={() => setShowForm(true)}
            className="mt-4 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Agregar crédito
          </button>
        </div>
      )}

      {loans.map((loan) => {
        const totalPaid = loan.downPayment + loan.totalCapitalPaid
        const progress = loan.carPrice > 0
          ? (totalPaid / loan.carPrice) * 100
          : ((loan.originalAmount - loan.currentBalance) / loan.originalAmount) * 100
        const isSelected = selectedLoan === loan.id
        const urgent = loan.daysUntilNextPayment <= 5 && loan.daysUntilNextPayment >= 0
        const overdue = loan.daysUntilNextPayment < 0

        return (
          <div key={loan.id} className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
            {/* Header del crédito */}
            <div className="p-6">
              <div className="flex items-start justify-between mb-5">
                <div>
                  <h3 className="text-lg font-bold text-gray-900">{loan.name}</h3>
                  <p className="text-sm text-gray-400">
                    {loan.paidPayments} de {loan.totalPayments} pagos • Tasa {loan.annualRatePercent}% anual
                  </p>
                </div>
                {overdue && (
                  <span className="flex items-center gap-1 text-xs font-semibold bg-red-100 text-red-700 px-3 py-1 rounded-full">
                    <AlertTriangle size={12} />
                    Pago vencido
                  </span>
                )}
                {urgent && !overdue && (
                  <span className="flex items-center gap-1 text-xs font-semibold bg-yellow-50 text-yellow-700 px-3 py-1 rounded-full">
                    <Clock size={12} />
                    Vence en {loan.daysUntilNextPayment} días
                  </span>
                )}
              </div>

              {/* Cards de resumen */}
              <div className="grid grid-cols-4 gap-4 mb-5">
                <MiniCard label="Saldo actual" value={fmt(loan.currentBalance)} color="text-blue-600" />
                <MiniCard label="Próximo pago" value={fmt(loan.monthlyPayment)} color="text-gray-900" />
                <MiniCard label="Capital pagado" value={fmt(loan.totalCapitalPaid)} color="text-green-600" />
                <MiniCard label="Intereses pagados" value={fmt(loan.totalInterestPaid)} color="text-red-500" />
              </div>

              {/* Próxima fecha */}
              <div className={`flex items-center justify-between rounded-xl px-4 py-3 mb-4 ${
                overdue ? 'bg-red-50 border border-red-200' :
                urgent ? 'bg-yellow-50 border border-yellow-200' :
                'bg-gray-50'
              }`}>
                <span className="text-sm text-gray-600">Fecha próximo pago</span>
                <span className={`text-sm font-semibold ${overdue ? 'text-red-600' : urgent ? 'text-yellow-700' : 'text-gray-900'}`}>
                  {fmtDate(loan.nextDueDate)}
                  {loan.daysUntilNextPayment >= 0 && ` (${loan.daysUntilNextPayment} días)`}
                </span>
              </div>

              {/* Barra de progreso */}
              <div>
                <div className="flex justify-between text-xs text-gray-500 mb-1">
                  <span>Progreso del crédito</span>
                  <span>{progress.toFixed(1)}% pagado</span>
                </div>
                <div className="w-full bg-gray-100 rounded-full h-3">
                  <div
                    className="h-3 rounded-full bg-gradient-to-r from-blue-500 to-green-500 transition-all"
                    style={{ width: `${progress}%` }}
                  />
                </div>
                <div className="flex justify-between text-xs text-gray-400 mt-1">
                  <span>{fmt(totalPaid)} pagado del auto</span>
                  <span>{fmt((loan.carPrice > 0 ? loan.carPrice : loan.originalAmount) - totalPaid)} restante</span>
                </div>
              </div>
            </div>

            {/* Toggle tabla amortización */}
            <button
              onClick={() => {
                if (!isSelected) setSelectedLoan(loan.id)
                else setSelectedLoan(null)
              }}
              className="w-full flex items-center justify-center gap-2 py-3 border-t border-gray-100 text-sm text-gray-500 hover:bg-gray-50 transition-colors"
            >
              {isSelected ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
              {isSelected ? 'Ocultar tabla de amortización' : 'Ver tabla de amortización'}
            </button>

            {/* Tabla de amortización */}
            {isSelected && (
              <div className="border-t border-gray-100">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="text-left px-4 py-2 text-gray-500 font-medium">#</th>
                      <th className="text-left px-4 py-2 text-gray-500 font-medium">Fecha</th>
                      <th className="text-right px-4 py-2 text-gray-500 font-medium">Cap. Vehículo</th>
                      <th className="text-right px-4 py-2 text-gray-500 font-medium">Cap. Seguro</th>
                      <th className="text-right px-4 py-2 text-gray-500 font-medium">Interés+IVA</th>
                      <th className="text-right px-4 py-2 text-gray-500 font-medium">Seg. Vida</th>
                      <th className="text-right px-4 py-2 text-gray-500 font-medium">Total</th>
                      <th className="text-right px-4 py-2 text-gray-500 font-medium">Saldo</th>
                      <th className="px-4 py-2" />
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {visibleRows.map((row) => (
                      <tr key={row.number} className={`${row.isPaid ? 'bg-green-50/50' : 'hover:bg-gray-50'}`}>
                        <td className="px-4 py-2 text-gray-400">{row.number}</td>
                        <td className="px-4 py-2 text-gray-600 whitespace-nowrap">{fmtDate(row.dueDate)}</td>
                        <td className="px-4 py-2 text-right text-gray-700">{fmt(row.capital)}</td>
                        <td className="px-4 py-2 text-right text-gray-500">{row.capitalSeguro > 0 ? fmt(row.capitalSeguro) : '—'}</td>
                        <td className="px-4 py-2 text-right text-gray-700">{fmt(row.interestWithIva)}</td>
                        <td className="px-4 py-2 text-right text-gray-500">{fmt(row.seguroVida)}</td>
                        <td className="px-4 py-2 text-right font-semibold text-gray-900">{fmt(row.total)}</td>
                        <td className="px-4 py-2 text-right text-blue-600">{fmt(row.balance)}</td>
                        <td className="px-4 py-2 text-right">
                          {row.isPaid ? (
                            <CheckCircle2 size={16} className="text-green-500 ml-auto" />
                          ) : (
                            <button
                              onClick={() => markPaidMutation.mutate({ loanId: loan.id, number: row.number })}
                              disabled={markPaidMutation.isPending}
                              className="text-xs px-2 py-1 bg-blue-50 text-blue-600 rounded hover:bg-blue-100 disabled:opacity-50 whitespace-nowrap"
                            >
                              Marcar pagado
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {schedule.length > 12 && (
                  <button
                    onClick={() => setShowAll(!showAll)}
                    className="w-full py-3 text-sm text-blue-600 hover:bg-blue-50 transition-colors"
                  >
                    {showAll ? 'Ver menos' : `Ver los ${schedule.length - 12} pagos restantes`}
                  </button>
                )}
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}

function MiniCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div className="bg-gray-50 rounded-xl p-3">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className={`text-base font-bold ${color}`}>{value}</p>
    </div>
  )
}

function LoanForm({ onSubmit, loading }: { onSubmit: (d: CreateLoanDto) => void; loading: boolean }) {
  const [form, setForm] = useState<CreateLoanDto>({
    name: 'KIA K4 Sedan GT-Line 2026',
    originalAmount: 341226.26,
    annualRatePercent: 14.99,
    totalPayments: 60,
    monthlyPayment: 8722.11,
    startDate: '2025-08-05',
    carPrice: 517900.00,
    downPayment: 195000.00,
    initialPaidPayments: 9,
  })

  const set = (k: keyof CreateLoanDto, v: string | number) =>
    setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-3">
      {[
        { label: 'Nombre', key: 'name', type: 'text' },
        { label: 'Monto original', key: 'originalAmount', type: 'number' },
        { label: 'Tasa anual (%)', key: 'annualRatePercent', type: 'number' },
        { label: 'Total de pagos', key: 'totalPayments', type: 'number' },
        { label: 'Pago mensual', key: 'monthlyPayment', type: 'number' },
        { label: 'Fecha de inicio del crédito', key: 'startDate', type: 'date' },
        { label: 'Precio total del auto', key: 'carPrice', type: 'number' },
        { label: 'Enganche pagado', key: 'downPayment', type: 'number' },
        { label: 'Pagos ya realizados', key: 'initialPaidPayments', type: 'number' },
      ].map(({ label, key, type }) => (
        <div key={key}>
          <label className="text-sm font-medium text-gray-700">{label}</label>
          <input
            type={type}
            step={type === 'number' ? '0.01' : undefined}
            value={form[key as keyof CreateLoanDto]}
            onChange={(e) => set(key as keyof CreateLoanDto, type === 'number' ? parseFloat(e.target.value) : e.target.value)}
            className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>
      ))}
      <button
        onClick={() => onSubmit(form)}
        disabled={loading}
        className="w-full py-2 bg-blue-600 text-white rounded-lg text-sm font-medium disabled:opacity-50 mt-2"
      >
        {loading ? 'Guardando...' : 'Guardar crédito'}
      </button>
    </div>
  )
}
