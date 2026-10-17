import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { problemMessage } from '@/api/problem'
import { login } from '@/auth/authApi'
import { AuthLayout, Field, FormError, SubmitButton } from './AuthLayout'

const TOO_MANY = 'Prea multe încercări. Încearcă din nou peste un minut.'

export function LoginPage() {
  const navigate = useNavigate()
  const from = (useLocation().state as { from?: string } | null)?.from ?? '/'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login({ email, password })
      navigate(from, { replace: true })
    } catch (err) {
      setError((err as { status?: number }).status === 429 ? TOO_MANY : problemMessage(err, 'Autentificarea a eșuat.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthLayout title="Autentificare" subtitle="Intră în contul tău NutriScan.">
      <form onSubmit={submit} className="space-y-4" noValidate>
        <Field label="Email" name="email" type="email" autoComplete="email" value={email} onChange={setEmail} />
        <Field label="Parolă" name="password" type="password" autoComplete="current-password" value={password} onChange={setPassword} />
        <FormError message={error} />
        <SubmitButton busy={busy}>{busy ? 'Se verifică…' : 'Intră în cont'}</SubmitButton>
      </form>
      <p className="text-sm text-slate-600 dark:text-slate-400 mt-6 text-center">
        Nu ai cont?{' '}
        <Link to="/register" className="font-semibold text-emerald-700 dark:text-emerald-400 hover:underline">Creează unul</Link>
      </p>
    </AuthLayout>
  )
}
