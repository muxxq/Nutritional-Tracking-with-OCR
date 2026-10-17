import type { ReactNode } from 'react'

const inputClass =
  'w-full px-3 py-2.5 rounded-lg border bg-white dark:bg-slate-950 text-sm text-slate-900 dark:text-white border-slate-300 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 aria-invalid:border-red-500'

interface FieldShellProps {
  id: string
  label: string
  error?: string
  hint?: string
  children: ReactNode
}

function FieldShell({ id, label, error, hint, children }: FieldShellProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-xs font-semibold text-slate-700 dark:text-slate-300">{label}</label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-xs text-red-600 dark:text-red-400">{error}</p>
      ) : hint ? (
        <p className="text-xs text-slate-500 dark:text-slate-400">{hint}</p>
      ) : null}
    </div>
  )
}

export function Field({
  label, name, type = 'text', value, onChange, error, hint, autoComplete, placeholder,
}: {
  label: string
  name: string
  type?: string
  value: string
  onChange: (value: string) => void
  error?: string
  hint?: string
  autoComplete?: string
  placeholder?: string
}) {
  const id = `field-${name}`
  return (
    <FieldShell id={id} label={label} error={error} hint={hint}>
      <input
        id={id}
        name={name}
        // Numbers are typed as text with a decimal keypad: <input type="number"> rejects the decimal comma ("6,9")
        // that Romanian users type; the forms parse both "," and ".".
        type={type === 'number' ? 'text' : type}
        value={value}
        inputMode={type === 'number' ? 'decimal' : undefined}
        placeholder={placeholder}
        autoComplete={autoComplete}
        onChange={(e) => onChange(e.target.value)}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        className={inputClass}
      />
    </FieldShell>
  )
}

export function SelectField<T extends string>({
  label, name, value, onChange, options, error,
}: {
  label: string
  name: string
  value: T
  onChange: (value: T) => void
  options: Record<T, string>
  error?: string
}) {
  const id = `field-${name}`
  return (
    <FieldShell id={id} label={label} error={error}>
      <select
        id={id}
        name={name}
        value={value}
        onChange={(e) => onChange(e.target.value as T)}
        aria-invalid={error ? true : undefined}
        className={inputClass}
      >
        {(Object.entries(options) as [T, string][]).map(([key, text]) => (
          <option key={key} value={key}>{text}</option>
        ))}
      </select>
    </FieldShell>
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

/** A white card section, as used across the app. */
export function Card({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <section className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm p-5">
      <h2 className="text-base font-bold text-slate-900 dark:text-white">{title}</h2>
      {subtitle && <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{subtitle}</p>}
      <div className="mt-4">{children}</div>
    </section>
  )
}
