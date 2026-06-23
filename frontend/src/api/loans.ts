import { api } from './client'

export interface LoanSummary {
  id: string
  name: string
  originalAmount: number
  currentBalance: number
  annualRatePercent: number
  totalPayments: number
  paidPayments: number
  monthlyPayment: number
  startDate: string
  nextDueDate: string
  totalInterestPaid: number
  totalCapitalPaid: number
  daysUntilNextPayment: number
}

export interface AmortizationRow {
  number: number
  dueDate: string
  capital: number
  interest: number
  iva: number
  total: number
  balance: number
  isPaid: boolean
  paidDate: string | null
}

export interface LoanDetail {
  summary: LoanSummary
  schedule: AmortizationRow[]
}

export interface CreateLoanDto {
  name: string
  originalAmount: number
  annualRatePercent: number
  totalPayments: number
  monthlyPayment: number
  startDate: string
}

export const loansApi = {
  getAll: () => api.get<LoanSummary[]>('/loans').then((r) => r.data),
  getDetail: (id: string) => api.get<LoanDetail>(`/loans/${id}`).then((r) => r.data),
  create: (data: CreateLoanDto) => api.post<LoanSummary>('/loans', data).then((r) => r.data),
  markPaid: (loanId: string, paymentNumber: number, paidDate: string) =>
    api.post(`/loans/${loanId}/payments/${paymentNumber}/pay`, { paidDate }),
}
