import { ApiError, apiFetch, apiJson } from './client'
import type {
  EnquiryCreatedResponse,
  EnquiryDetailResponse,
  PasteEnquiryRequest,
  PendingApprovalSummary,
  QuoteDetailResponse,
  QuoteSummaryResponse,
} from './types'

const jsonBody = (value: unknown): RequestInit => ({
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(value),
})

export const createEnquiry = (body: PasteEnquiryRequest): Promise<EnquiryCreatedResponse> =>
  apiJson('/api/enquiries', jsonBody(body))

export const getEnquiry = (id: number, signal?: AbortSignal): Promise<EnquiryDetailResponse> =>
  apiJson(`/api/enquiries/${id}`, { signal })

export const listApprovals = (signal?: AbortSignal): Promise<PendingApprovalSummary[]> =>
  apiJson('/api/approvals', { signal })

export const listQuotes = (signal?: AbortSignal): Promise<QuoteSummaryResponse[]> =>
  apiJson('/api/quotes', { signal })

export const getQuote = (id: number, signal?: AbortSignal): Promise<QuoteDetailResponse> =>
  apiJson(`/api/quotes/${id}`, { signal })

/** The enquiry's photo, or `null` when it has none (404). Kept separate from `getEnquiry` — the
 * detail response is fetched on every Desk navigation and must stay light. */
export async function getEnquiryImage(id: number, signal?: AbortSignal): Promise<Blob | null> {
  try {
    const response = await apiFetch(`/api/enquiries/${id}/image`, { signal })
    return await response.blob()
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null
    throw err
  }
}
