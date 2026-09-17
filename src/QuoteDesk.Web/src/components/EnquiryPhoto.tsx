import { useEffect, useState } from 'react'
import { getEnquiryImage } from '../api/endpoints'
import { Spinner } from './ui'

type PhotoState =
  | { status: 'loading' }
  | { status: 'none' }
  | { status: 'error' }
  | { status: 'ready'; url: string }

/**
 * The customer's photo, beside what the agent read from it. Handwriting misreads happen even on the
 * largest models, and a misread that names a real catalogue item (20mm read as 25mm) passes every
 * check in code — the human comparing the photo to the lines is the check that catches it.
 *
 * Fetched with the bearer token (an `<img src>` cannot send one) into an object URL, revoked on
 * unmount. A text enquiry has no photo, so this renders nothing.
 */
export function EnquiryPhoto({ enquiryId }: { enquiryId: number }) {
  const [state, setState] = useState<PhotoState>({ status: 'loading' })
  const [expanded, setExpanded] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    let url: string | null = null

    getEnquiryImage(enquiryId, controller.signal)
      .then((blob) => {
        // Cleanup may already have run (unmount, or a different enquiry) while the download
        // finished — creating an object URL now would leak it, since nothing would revoke it.
        if (controller.signal.aborted) return
        if (blob === null) {
          setState({ status: 'none' })
          return
        }
        url = URL.createObjectURL(blob)
        setState({ status: 'ready', url })
      })
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          console.error('Could not load the enquiry photo', err)
          setState({ status: 'error' })
        }
      })

    return () => {
      controller.abort()
      if (url !== null) URL.revokeObjectURL(url)
    }
  }, [enquiryId])

  if (state.status === 'none') return null

  return (
    <div className="border-b border-slate-100 bg-slate-50 px-[18px] py-3.5">
      {state.status === 'loading' && (
        <div className="flex items-center gap-2 text-[12px] text-slate-400">
          <Spinner className="size-3.5" /> Loading the customer's photo…
        </div>
      )}
      {state.status === 'error' && (
        <p className="text-[12px] text-red-600">
          The customer's photo could not be loaded — open the enquiry to check the lines before approving.
        </p>
      )}
      {state.status === 'ready' && (
        <div className="flex items-start gap-3.5">
          <button
            type="button"
            onClick={() => setExpanded((v) => !v)}
            className="shrink-0 overflow-hidden rounded-md border border-slate-200 bg-white"
            aria-label={expanded ? 'Shrink the photo' : 'Enlarge the photo'}
          >
            <img
              src={state.url}
              alt="The customer's photographed enquiry"
              className={expanded ? 'max-h-[560px] w-auto' : 'h-28 w-28 object-cover'}
            />
          </button>
          <div className="text-[12px] leading-relaxed text-slate-600">
            <div className="font-semibold text-amber-700">Read from a photo</div>
            Handwriting can be misread. Check every size, suffix and quantity below against the photo
            before approving.
            <div className="mt-1 text-[11px] text-slate-400">
              {expanded ? 'Click the photo to shrink it.' : 'Click the photo to enlarge it.'}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
