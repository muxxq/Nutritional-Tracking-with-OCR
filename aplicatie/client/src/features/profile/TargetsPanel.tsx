import { useState, type FormEvent } from 'react'
import { fieldErrors, problemMessage } from '@/api/problem'
import { Field, FormError, SubmitButton } from '@/components/ui/form'
import { TARGET_SOURCE_LABELS } from './labels'
import { useUpdateTargets, type ProfileDto } from './profileApi'

type TargetKey = 'calories' | 'proteinsGrams' | 'carbsGrams' | 'fatsGrams'

const TARGETS: { key: TargetKey; label: string; unit: string }[] = [
  { key: 'calories', label: 'Calorii', unit: 'kcal' },
  { key: 'proteinsGrams', label: 'Proteine', unit: 'g' },
  { key: 'carbsGrams', label: 'Carbohidrați', unit: 'g' },
  { key: 'fatsGrams', label: 'Grăsimi', unit: 'g' },
]

/** The daily targets with where each comes from, and manual overrides: an empty field means "calculate it". */
export function TargetsPanel({ profile }: { profile: ProfileDto }) {
  const update = useUpdateTargets()
  const manual = (key: TargetKey) => (profile.targets[key].source === 'manual' ? String(profile.targets[key].value) : '')
  const [form, setForm] = useState<Record<TargetKey, string>>({
    calories: manual('calories'),
    proteinsGrams: manual('proteinsGrams'),
    carbsGrams: manual('carbsGrams'),
    fatsGrams: manual('fatsGrams'),
  })

  const errors = update.error ? fieldErrors(update.error) : {}
  const generalError = update.error && !Object.keys(errors).length ? problemMessage(update.error, 'Țintele nu au putut fi salvate.') : null
  const toNumber = (value: string) => (value.trim() === '' ? null : Number(value.replace(',', '.')))

  const submit = (e: FormEvent) => {
    e.preventDefault()
    update.mutate({
      calories: toNumber(form.calories),
      proteinsGrams: toNumber(form.proteinsGrams),
      carbsGrams: toNumber(form.carbsGrams),
      fatsGrams: toNumber(form.fatsGrams),
    })
  }

  return (
    <div className="space-y-5">
      {profile.bmr != null && profile.tdee != null && (
        <p className="text-xs text-slate-600 dark:text-slate-400">
          Metabolism bazal (Mifflin–St Jeor): <strong>{profile.bmr} kcal</strong> · Necesar zilnic cu activitatea: <strong>{profile.tdee} kcal</strong>
        </p>
      )}

      <dl className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {TARGETS.map(({ key, label, unit }) => (
          <div key={key} className="rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 p-3">
            <dt className="text-xs font-semibold text-slate-500 dark:text-slate-400">{label}</dt>
            <dd className="mt-1 text-lg font-extrabold text-slate-900 dark:text-white">
              {Math.round(profile.targets[key].value)} <span className="text-xs font-semibold text-slate-500">{unit}</span>
            </dd>
            <dd className="text-[10px] uppercase tracking-wide font-bold text-emerald-700 dark:text-emerald-400">
              {TARGET_SOURCE_LABELS[profile.targets[key].source]}
            </dd>
          </div>
        ))}
      </dl>

      <form onSubmit={submit} className="space-y-4" noValidate>
        <p className="text-xs text-slate-600 dark:text-slate-400">
          Poți suprascrie oricare țintă. Lasă un câmp gol ca să revină la valoarea calculată; carbohidrații se recalculează din energia rămasă.
        </p>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {TARGETS.map(({ key, label, unit }) => (
            <Field
              key={key}
              label={`${label} (${unit})`}
              name={key}
              type="number"
              value={form[key]}
              placeholder={String(Math.round(profile.targets[key].value))}
              onChange={(value) => setForm({ ...form, [key]: value })}
              error={errors[key]}
            />
          ))}
        </div>
        <FormError message={generalError} />
        <SubmitButton busy={update.isPending}>{update.isPending ? 'Se salvează…' : 'Salvează țintele'}</SubmitButton>
      </form>
    </div>
  )
}
