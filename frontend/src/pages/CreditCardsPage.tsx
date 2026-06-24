import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  CreditCard,
  Upload,
  AlertTriangle,
  Clock,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  X,
  RefreshCw,
} from 'lucide-react'
import { creditCardsApi } from '../api/creditCards'
import type { CreditCardDto, CreditCardTransactionDto, ParsedStatementData } from '../api/creditCards'

const fmt = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })
const fmtMoney = (n: number) => fmt.format(n)
const fmtDate = (iso: string) =>
  new Date(iso).toLocaleDateString('es-MX', { day: '2-digit', month: 'short', year: 'numeric' })

export function CreditCardsPage() {
  const qc = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const updateFileInputRef = useRef<HTMLInputElement>(null)

  const [uploadStep, setUploadStep] = useState<'idle' | 'parsing' | 'preview' | 'saving'>('idle')
  const [parsedData, setParsedData] = useState<ParsedStatementData | null>(null)
  const [cardName, setCardName] = useState('')
  const [parseError, setParseError] = useState<string | null>(null)
  const [expandedCards, setExpandedCards] = useState<Set<string>>(new Set())
  const [updatingCardId, setUpdatingCardId] = useState<string | null>(null)

  const { data: cards = [], isLoading } = useQuery({
    queryKey: ['credit-cards'],
    queryFn: creditCardsApi.getAll,
  })

  const parseMutation = useMutation({
    mutationFn: creditCardsApi.parseStatement,
    onSuccess: (data) => {
      setParsedData(data)
      setCardName(`BBVA *${data.lastFourDigits}`)
      setUploadStep('preview')
      setParseError(null)
    },
    onError: (err: any) => {
      setParseError(err?.response?.data?.detail ?? 'Error al procesar el PDF')
      setUploadStep('idle')
    },
  })

  const createMutation = useMutation({
    mutationFn: creditCardsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['credit-cards'] })
      setUploadStep('idle')
      setParsedData(null)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) =>
      creditCardsApi.updateFromStatement(id, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['credit-cards'] })
      setUpdatingCardId(null)
    },
    onError: (err: any) => {
      setParseError(err?.response?.data?.detail ?? 'Error al actualizar')
      setUpdatingCardId(null)
    },
  })

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setUploadStep('parsing')
    setParseError(null)
    parseMutation.mutate(file)
    e.target.value = ''
  }

  const handleUpdateFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file || !updatingCardId) return
    updateMutation.mutate({ id: updatingCardId, file })
    e.target.value = ''
  }

  const handleConfirm = () => {
    if (!parsedData) return
    setUploadStep('saving')
    createMutation.mutate({
      ...parsedData,
      name: cardName,
    })
  }

  const toggleExpand = (id: string) => {
    setExpandedCards((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-blue-50 dark:bg-blue-900/20 rounded-xl">
            <CreditCard size={22} className="text-blue-600 dark:text-blue-400" />
          </div>
          <div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Tarjetas de Crédito</h2>
            <p className="text-sm text-gray-400">BBVA MSI y saldos</p>
          </div>
        </div>
        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={uploadStep !== 'idle'}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
        >
          <Upload size={16} />
          Importar estado de cuenta
        </button>
        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf"
          className="hidden"
          onChange={handleFileSelect}
        />
        <input
          ref={updateFileInputRef}
          type="file"
          accept=".pdf"
          className="hidden"
          onChange={handleUpdateFileSelect}
        />
      </div>

      {/* Upload states */}
      {uploadStep === 'parsing' && (
        <div className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 p-8 text-center">
          <div className="animate-spin w-8 h-8 border-2 border-blue-600 border-t-transparent rounded-full mx-auto mb-3" />
          <p className="text-gray-600 dark:text-gray-300 font-medium">Analizando PDF...</p>
          <p className="text-sm text-gray-400 mt-1">Extrayendo datos del estado de cuenta BBVA</p>
        </div>
      )}

      {parseError && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 flex items-start gap-3">
          <AlertTriangle size={18} className="text-red-500 mt-0.5 flex-shrink-0" />
          <div>
            <p className="text-sm font-medium text-red-700 dark:text-red-400">Error al procesar el PDF</p>
            <p className="text-xs text-red-600 dark:text-red-500 mt-1">{parseError}</p>
          </div>
          <button onClick={() => setParseError(null)} className="ml-auto">
            <X size={16} className="text-red-400" />
          </button>
        </div>
      )}

      {/* Preview modal */}
      {uploadStep === 'preview' && parsedData && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 w-full max-w-2xl shadow-xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Confirmar datos del estado de cuenta
              </h3>
              <button onClick={() => { setUploadStep('idle'); setParsedData(null) }}>
                <X size={18} className="text-gray-400 hover:text-gray-600" />
              </button>
            </div>

            {/* Card name input */}
            <div className="mb-4">
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Nombre de la tarjeta</label>
              <input
                type="text"
                value={cardName}
                onChange={(e) => setCardName(e.target.value)}
                className="mt-1 w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              />
            </div>

            {/* Parsed summary */}
            <div className="grid grid-cols-2 gap-3 mb-5">
              <StatRow label="Últimos 4 dígitos" value={`*${parsedData.lastFourDigits}`} />
              <StatRow label="Límite de crédito" value={fmtMoney(parsedData.creditLimit)} />
              <StatRow label="Crédito disponible" value={fmtMoney(parsedData.availableCredit)} />
              <StatRow label="Saldo total" value={fmtMoney(parsedData.totalBalance)} />
              <StatRow label="Saldo cargos regulares" value={fmtMoney(parsedData.regularBalance)} />
              <StatRow label="Saldo cargo a meses" value={fmtMoney(parsedData.msiBalance)} />
              <StatRow label="Pago sin intereses" value={fmtMoney(parsedData.paymentToAvoidInterest)} />
              <StatRow label="Pago mínimo" value={fmtMoney(parsedData.minimumPayment)} />
              <StatRow label="Fecha de corte" value={fmtDate(parsedData.lastStatementDate)} />
              <StatRow label="Fecha límite de pago" value={fmtDate(parsedData.nextPaymentDueDate)} />
            </div>

            {parsedData.promotions.length > 0 && (
              <div className="mb-5">
                <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                  MSI Detectadas ({parsedData.promotions.length})
                </h4>
                <div className="space-y-2">
                  {parsedData.promotions.map((p, i) => (
                    <div key={i} className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-3">
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">{p.description}</p>
                          <p className="text-xs text-gray-500 dark:text-gray-400">
                            {fmtDate(p.purchaseDate)} · {fmtMoney(p.originalAmount)}
                          </p>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-semibold text-blue-600 dark:text-blue-400">
                            {fmtMoney(p.monthlyPayment)}/mes
                          </p>
                          <p className="text-xs text-gray-500">{p.paidMonths} de {p.totalMonths} pagos</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex gap-3">
              <button
                onClick={() => { setUploadStep('idle'); setParsedData(null) }}
                className="flex-1 py-2 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirm}
                disabled={createMutation.isPending}
                className="flex-1 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
              >
                {createMutation.isPending ? 'Guardando...' : 'Confirmar y guardar'}
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading && (
        <p className="text-gray-400 text-sm">Cargando tarjetas...</p>
      )}

      {cards.length === 0 && !isLoading && uploadStep === 'idle' && (
        <div className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 p-12 text-center">
          <CreditCard size={40} className="text-gray-300 dark:text-gray-600 mx-auto mb-3" />
          <p className="text-gray-500 dark:text-gray-400">No tienes tarjetas registradas.</p>
          <button
            onClick={() => fileInputRef.current?.click()}
            className="mt-4 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Importar estado de cuenta
          </button>
        </div>
      )}

      {cards.map((card) => (
        <CardView
          key={card.id}
          card={card}
          expanded={expandedCards.has(card.id)}
          onToggle={() => toggleExpand(card.id)}
          onUpdate={() => {
            setUpdatingCardId(card.id)
            updateFileInputRef.current?.click()
          }}
          updating={updateMutation.isPending && updatingCardId === card.id}
        />
      ))}
    </div>
  )
}

function CardView({
  card,
  expanded,
  onToggle,
  onUpdate,
  updating,
}: {
  card: CreditCardDto
  expanded: boolean
  onToggle: () => void
  onUpdate: () => void
  updating: boolean
}) {
  const [txExpanded, setTxExpanded] = useState(false)
  const utilizationPct = card.creditLimit > 0 ? (card.totalBalance / card.creditLimit) * 100 : 0
  const overdue = card.daysUntilPayment < 0
  const urgent = card.daysUntilPayment >= 0 && card.daysUntilPayment <= 5
  const activePromos = card.promotions.filter((p) => !p.isCompleted)

  return (
    <div className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div className="p-6">
        {/* Card header */}
        <div className="flex items-start justify-between mb-5">
          <div>
            <h3 className="text-lg font-bold text-gray-900 dark:text-white">{card.name}</h3>
            <p className="text-sm text-gray-400">•••• •••• •••• {card.lastFourDigits}</p>
          </div>
          <button
            onClick={onUpdate}
            disabled={updating}
            title="Actualizar con nuevo estado de cuenta"
            className="flex items-center gap-1.5 text-xs px-3 py-1.5 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 disabled:opacity-50"
          >
            <RefreshCw size={13} className={updating ? 'animate-spin' : ''} />
            {updating ? 'Actualizando...' : 'Actualizar'}
          </button>
        </div>

        {/* Payment warning banner */}
        {overdue && (
          <div className="mb-4 flex items-center gap-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl px-4 py-3">
            <AlertTriangle size={16} className="text-red-500 flex-shrink-0" />
            <span className="text-sm font-medium text-red-700 dark:text-red-400">
              Pago vencido — {fmtMoney(card.paymentToAvoidInterest)} pendiente
            </span>
          </div>
        )}
        {urgent && !overdue && (
          <div className="mb-4 flex items-center gap-2 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-xl px-4 py-3">
            <Clock size={16} className="text-yellow-500 flex-shrink-0" />
            <span className="text-sm font-medium text-yellow-700 dark:text-yellow-400">
              Tu pago vence en {card.daysUntilPayment} días — {fmtMoney(card.paymentToAvoidInterest)}
            </span>
          </div>
        )}

        {/* Key stats grid */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-5">
          <StatCard
            label="Saldo total"
            value={fmtMoney(card.totalBalance)}
            color="text-gray-900 dark:text-white"
          />
          <StatCard
            label="Disponible"
            value={fmtMoney(card.availableCredit)}
            color="text-green-600 dark:text-green-400"
          />
          <StatCard
            label="Pago sin intereses"
            value={fmtMoney(card.paymentToAvoidInterest)}
            color="text-blue-600 dark:text-blue-400"
          />
          <StatCard
            label={overdue ? 'Días vencido' : `Vence en`}
            value={overdue ? `${Math.abs(card.daysUntilPayment)} días` : `${card.daysUntilPayment} días`}
            color={overdue ? 'text-red-600 dark:text-red-400' : urgent ? 'text-yellow-600 dark:text-yellow-400' : 'text-gray-900 dark:text-white'}
          />
        </div>

        {/* Utilization bar */}
        <div className="mb-5">
          <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mb-1">
            <span>Utilización del crédito</span>
            <span>{utilizationPct.toFixed(1)}% usado · Corte día {card.cutoffDay} · Pago día {card.paymentDueDay}</span>
          </div>
          <div className="w-full bg-gray-100 dark:bg-gray-700 rounded-full h-3">
            <div
              className={`h-3 rounded-full transition-all ${
                utilizationPct > 80
                  ? 'bg-gradient-to-r from-orange-500 to-red-500'
                  : 'bg-gradient-to-r from-blue-500 to-green-500'
              }`}
              style={{ width: `${Math.min(utilizationPct, 100)}%` }}
            />
          </div>
          <div className="flex justify-between text-xs text-gray-400 mt-1">
            <span>{fmtMoney(card.totalBalance)} usado</span>
            <span>Límite {fmtMoney(card.creditLimit)}</span>
          </div>
        </div>

        {/* MSI Activas */}
        {activePromos.length > 0 && (
          <div>
            <button
              onClick={onToggle}
              className="w-full flex items-center justify-between text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3"
            >
              <span>MSI Activas ({activePromos.length})</span>
              {expanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            </button>

            {expanded && (
              <div className="space-y-3">
                {activePromos.map((p) => (
                  <MsiRow key={p.id} promo={p} />
                ))}
              </div>
            )}

            {!expanded && (
              <div className="space-y-2">
                {activePromos.slice(0, 2).map((p) => (
                  <MsiRow key={p.id} promo={p} compact />
                ))}
                {activePromos.length > 2 && (
                  <button
                    onClick={onToggle}
                    className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    Ver {activePromos.length - 2} más...
                  </button>
                )}
              </div>
            )}
          </div>
        )}

        {card.promotions.length > 0 && activePromos.length === 0 && (
          <div className="flex items-center gap-2 text-sm text-green-600 dark:text-green-400">
            <CheckCircle2 size={16} />
            Todas las promociones MSI completadas
          </div>
        )}

        {/* Movimientos del periodo */}
        {card.recentTransactions && card.recentTransactions.length > 0 && (
          <div className="mt-4 pt-4 border-t border-gray-100 dark:border-gray-700">
            <button
              onClick={() => setTxExpanded((v) => !v)}
              className="w-full flex items-center justify-between text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2"
            >
              <span>
                Movimientos del periodo ({card.recentTransactions.length})
                {card.recentTransactions[0] && (
                  <span className="ml-2 font-normal text-gray-400 text-xs">
                    {new Date(card.recentTransactions[0].statementPeriod + '-01').toLocaleDateString('es-MX', { month: 'long', year: 'numeric' })}
                  </span>
                )}
              </span>
              {txExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            </button>
            {!txExpanded && (
              <button
                onClick={() => setTxExpanded(true)}
                className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
              >
                Ver {card.recentTransactions.length} movimientos
              </button>
            )}
            {txExpanded && (
              <div className="space-y-1 max-h-80 overflow-y-auto pr-1">
                {card.recentTransactions.map((tx) => (
                  <TransactionRow key={tx.id} tx={tx} />
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

function MsiRow({ promo, compact }: { promo: any; compact?: boolean }) {
  return (
    <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-3">
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="min-w-0">
          <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{promo.description}</p>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {new Date(promo.purchaseDate).toLocaleDateString('es-MX', { day: '2-digit', month: 'short', year: 'numeric' })}
            {' · '}Original: {fmtMoney(promo.originalAmount)}
          </p>
        </div>
        <div className="text-right flex-shrink-0">
          <p className="text-sm font-semibold text-blue-600 dark:text-blue-400">{fmtMoney(promo.monthlyPayment)}/mes</p>
          <p className="text-xs text-gray-500 dark:text-gray-400">{fmtMoney(promo.pendingBalance)} pendiente</p>
        </div>
      </div>
      {!compact && (
        <>
          <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2 mb-1">
            <div
              className="h-2 rounded-full bg-gradient-to-r from-blue-500 to-green-500"
              style={{ width: `${promo.progressPercent}%` }}
            />
          </div>
          <p className="text-xs text-gray-400">
            {promo.paidMonths} de {promo.totalMonths} pagos · {promo.progressPercent}%
          </p>
        </>
      )}
      {compact && (
        <p className="text-xs text-gray-400">{promo.paidMonths} de {promo.totalMonths} pagos</p>
      )}
    </div>
  )
}

function TransactionRow({ tx }: { tx: CreditCardTransactionDto }) {
  return (
    <div className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
      <div className="min-w-0 flex-1">
        <p className="text-xs font-medium text-gray-800 dark:text-gray-200 truncate">{tx.description}</p>
        <p className="text-xs text-gray-400 dark:text-gray-500">
          {new Date(tx.operationDate).toLocaleDateString('es-MX', { day: '2-digit', month: 'short', year: 'numeric' })}
        </p>
      </div>
      <span
        className={`text-xs font-semibold flex-shrink-0 ml-3 ${
          tx.isCredit ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'
        }`}
      >
        {tx.isCredit ? '+' : '-'}{fmtMoney(tx.amount)}
      </span>
    </div>
  )
}

function StatCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-3">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className={`text-sm font-bold ${color}`}>{value}</p>
    </div>
  )
}

function StatRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg px-3 py-2">
      <p className="text-xs text-gray-400">{label}</p>
      <p className="text-sm font-semibold text-gray-900 dark:text-white">{value}</p>
    </div>
  )
}
