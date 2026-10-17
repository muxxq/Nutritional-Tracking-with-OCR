import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { fieldErrors, problemMessage } from '@/api/problem'
import { register } from '@/auth/authApi'
import { AuthLayout, Field, FormError, SubmitButton } from './AuthLayout'

export function RegisterPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '' })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const set = (field: keyof typeof form) => (value: string) => setForm({ ...form, [field]: value })

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setErrors({})
    try {
      await register(form)
      navigate('/', { replace: true })
    } catch (err) {
      const perField = fieldErrors(err)
      setErrors(perField)
      // Field errors are shown next to their inputs; anything else (e.g. 409 email taken) goes above the button.
      setError(Object.keys(perField).length ? null : problemMessage(err, 'Înregistrarea a eșuat.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthLayout title="Cont nou" subtitle="Datele tale nutriționale rămân private.">
      <form onSubmit={submit} className="space-y-4" noValidate>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Prenume" name="firstName" autoComplete="given-name" value={form.firstName} onChange={set('firstName')} error={errors.firstName} />
          <Field label="Nume" name="lastName" autoComplete="family-name" value={form.lastName} onChange={set('lastName')} error={errors.lastName} />
        </div>
        <Field label="Email" name="email" type="email" autoComplete="email" value={form.email} onChange={set('email')} error={errors.email} />
        <Field label="Parolă (minimum 8 caractere)" name="password" type="password" autoComplete="new-password" value={form.password} onChange={set('password')} error={errors.password} />
        <FormError message={error} />
        <SubmitButton busy={busy}>{busy ? 'Se creează contul…' : 'Creează contul'}</SubmitButton>
      </form>
      <p className="text-sm text-slate-600 dark:text-slate-400 mt-6 text-center">
        Ai deja cont?{' '}
        <Link to="/login" className="font-semibold text-emerald-700 dark:text-emerald-400 hover:underline">Autentifică-te</Link>
      </p>
    </AuthLayout>
  )
}
