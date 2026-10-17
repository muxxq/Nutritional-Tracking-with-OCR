import { useEffect, type ReactNode } from 'react'
import { X } from 'lucide-react'

/** Modal dialog in the app's style; Escape and the close button call onClose. */
export function Dialog({
  title, icon, onClose, children, footer, wide = false,
}: {
  title: string
  icon?: ReactNode
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
  wide?: boolean
}) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center sm:p-4 bg-slate-900/60 backdrop-blur-xs">
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`w-full ${wide ? 'sm:max-w-2xl' : 'sm:max-w-lg'} max-h-[92vh] flex flex-col rounded-t-2xl sm:rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden`}
      >
        <div className="p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            {icon}
            <h3 className="font-bold text-sm text-slate-900 dark:text-white">{title}</h3>
          </div>
          <button onClick={onClose} aria-label="Închide" className="p-1 rounded-lg text-slate-400 hover:text-slate-600 dark:hover:text-slate-200">
            <X className="w-4 h-4" />
          </button>
        </div>
        <div className="p-5 space-y-4 overflow-y-auto">{children}</div>
        {footer && (
          <div className="p-3.5 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-200 dark:border-slate-800 flex justify-end gap-2">
            {footer}
          </div>
        )}
      </div>
    </div>
  )
}

export function SecondaryButton({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="px-3.5 py-2 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
    >
      {children}
    </button>
  )
}

export function PrimaryButton({
  onClick, disabled, children, type = 'button', form,
}: {
  onClick?: () => void
  disabled?: boolean
  children: ReactNode
  type?: 'button' | 'submit'
  form?: string
}) {
  return (
    <button
      type={type}
      form={form}
      onClick={onClick}
      disabled={disabled}
      className="px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white text-xs font-bold shadow-xs transition"
    >
      {children}
    </button>
  )
}
