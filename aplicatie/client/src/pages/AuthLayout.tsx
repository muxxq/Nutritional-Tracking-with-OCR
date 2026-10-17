import type { ReactNode } from 'react'
import { Flame } from 'lucide-react'

/** Shared frame of the login and register pages. */
export function AuthLayout({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="flex items-center gap-3 mb-8 justify-center">
          <div className="w-11 h-11 rounded-xl bg-emerald-500 flex items-center justify-center shadow-md shadow-emerald-500/20">
            <Flame className="w-6 h-6 fill-white text-white" />
          </div>
          <span className="font-extrabold text-2xl text-slate-900 dark:text-white tracking-tight">NutriScan</span>
        </div>
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm p-6">
          <h1 className="text-xl font-bold text-slate-900 dark:text-white">{title}</h1>
          <p className="text-sm text-slate-500 dark:text-slate-400 mt-1 mb-6">{subtitle}</p>
          {children}
        </div>
      </div>
    </div>
  )
}

export function Field({
  label, name, type = 'text', value, onChange, error, autoComplete,
}: {
  label: string
  name: string
  type?: string
  value: string
  onChange: (value: string) => void
  error?: string
  autoComplete?: string
}) {
  const id = `field-${name}`
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-xs font-semibold text-slate-700 dark:text-slate-300">{label}</label>
      <input
        id={id}
        name={name}
        type={type}
        value={value}
        autoComplete={autoComplete}
        onChange={(e) => onChange(e.target.value)}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        className="w-full px-3 py-2.5 rounded-lg border bg-white dark:bg-slate-950 text-sm text-slate-900 dark:text-white border-slate-300 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 aria-invalid:border-red-500"
      />
      {error && <p id={`${id}-error`} className="text-xs text-red-600 dark:text-red-400">{error}</p>}
    </div>
  )
}

export function FormError({ message }: { message: string | null }) {
  return message ? (
    <p role="alert" className="text-sm text-red-700 dark:text-red-300 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900 rounded-lg px-3 py-2">
      {message}
    </p>
  ) : null
}

export function SubmitButton({ busy, children }: { busy: boolean; children: ReactNode }) {
  return (
    <button
      type="submit"
      disabled={busy}
      className="w-full py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 disabled:opacity-60 text-white text-sm font-bold shadow-md shadow-emerald-600/20 transition"
    >
      {children}
    </button>
  )
}
