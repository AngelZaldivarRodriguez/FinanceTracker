import { api } from './client'

export interface CreditCardTransactionDto {
  id: string
  operationDate: string
  chargeDate: string
  description: string
  amount: number
  isCredit: boolean
  statementPeriod: string
}

export interface MsiPromotionDto {
  id: string
  description: string
  purchaseDate: string
  originalAmount: number
  pendingBalance: number
  monthlyPayment: number
  totalMonths: number
  paidMonths: number
  isCompleted: boolean
  progressPercent: number
}

export interface CreditCardDto {
  id: string
  lastFourDigits: string
  name: string
  creditLimit: number
  availableCredit: number
  regularBalance: number
  msiBalance: number
  totalBalance: number
  paymentToAvoidInterest: number
  minimumPayment: number
  cutoffDay: number
  paymentDueDay: number
  lastStatementDate: string
  nextPaymentDueDate: string
  daysUntilPayment: number
  promotions: MsiPromotionDto[]
  recentTransactions: CreditCardTransactionDto[]
}

export interface ParsedPromotion {
  description: string
  purchaseDate: string
  originalAmount: number
  pendingBalance: number
  monthlyPayment: number
  totalMonths: number
  paidMonths: number
}

export interface ParsedRegularTransactionDto {
  operationDate: string
  chargeDate: string
  description: string
  amount: number
  isCredit: boolean
}

export interface ParsedStatementData {
  lastFourDigits: string
  creditLimit: number
  availableCredit: number
  regularBalance: number
  msiBalance: number
  totalBalance: number
  paymentToAvoidInterest: number
  minimumPayment: number
  cutoffDay: number
  paymentDueDay: number
  lastStatementDate: string
  nextPaymentDueDate: string
  promotions: ParsedPromotion[]
  regularTransactions: ParsedRegularTransactionDto[]
}

export interface CreateCreditCardCommand {
  lastFourDigits: string
  name: string
  creditLimit: number
  availableCredit: number
  regularBalance: number
  msiBalance: number
  totalBalance: number
  paymentToAvoidInterest: number
  minimumPayment: number
  cutoffDay: number
  paymentDueDay: number
  lastStatementDate: string
  nextPaymentDueDate: string
  promotions: ParsedPromotion[]
  regularTransactions: ParsedRegularTransactionDto[]
}

export const creditCardsApi = {
  parseStatement: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.post<ParsedStatementData>('/credit-cards/parse-statement', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((r) => r.data)
  },

  create: (data: CreateCreditCardCommand) =>
    api.post<CreditCardDto>('/credit-cards', data).then((r) => r.data),

  getAll: () =>
    api.get<CreditCardDto[]>('/credit-cards').then((r) => r.data),

  updateFromStatement: (id: string, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.put<CreditCardDto>(`/credit-cards/${id}/update-statement`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((r) => r.data)
  },
}
