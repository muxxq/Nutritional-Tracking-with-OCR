import { useState, type FormEvent } from 'react'
import { fieldErrors, problemMessage } from '@/api/problem'
import { Field, FormError, SelectField, SubmitButton } from '@/components/ui/form'
import { ACTIVITY_LABELS, GENDER_LABELS, GOAL_LABELS } from './labels'
import { useUpdateProfile, type ProfileDto, type UpdateProfileRequest } from './profileApi'

/** Physical data and goal (FR-02); the server computes BMR, TDEE and the targets from them. */
export function ProfileForm({ profile, submitLabel, onSaved }: { profile: ProfileDto; submitLabel: string; onSaved?: () => void }) {
  const update = useUpdateProfile()
  const [form, setForm] = useState({
    firstName: profile.firstName,
    lastName: profile.lastName,
    birthDate: profile.birthDate ?? '',
    heightCm: profile.heightCm ? String(profile.heightCm) : '',
    weightKg: profile.weightKg ? String(profile.weightKg) : '',
    gender: profile.gender,
    activityLevel: profile.activityLevel,
    goal: profile.goal,
  })
  const set = <K extends keyof typeof form>(field: K) => (value: (typeof form)[K]) => setForm({ ...form, [field]: value })

  const errors = update.error ? fieldErrors(update.error) : {}
  const generalError = update.error && !Object.keys(errors).length ? problemMessage(update.error, 'Profilul nu a putut fi salvat.') : null

  const submit = (e: FormEvent) => {
    e.preventDefault()
    const body: UpdateProfileRequest = {
      ...form,
      // An empty field is sent as 0, which the server rejects with its own message for that field.
      heightCm: Number(form.heightCm.replace(',', '.')) || 0,
      weightKg: Number(form.weightKg.replace(',', '.')) || 0,
    }
    update.mutate(body, { onSuccess: () => onSaved?.() })
  }

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <div className="grid grid-cols-2 gap-3">
        <Field label="Prenume" name="firstName" value={form.firstName} onChange={set('firstName')} error={errors.firstName} />
        <Field label="Nume" name="lastName" value={form.lastName} onChange={set('lastName')} error={errors.lastName} />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Field label="Data nașterii" name="birthDate" type="date" value={form.birthDate} onChange={set('birthDate')} error={errors.birthDate} />
        <SelectField label="Sex" name="gender" value={form.gender} onChange={set('gender')} options={GENDER_LABELS} error={errors.gender} />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Field label="Înălțime (cm)" name="heightCm" type="number" min={100} max={250} value={form.heightCm} onChange={set('heightCm')} error={errors.heightCm} />
        <Field label="Greutate (kg)" name="weightKg" type="number" min={30} max={300} step="0.1" value={form.weightKg} onChange={set('weightKg')} error={errors.weightKg} />
      </div>
      <SelectField label="Nivel de activitate" name="activityLevel" value={form.activityLevel} onChange={set('activityLevel')} options={ACTIVITY_LABELS} error={errors.activityLevel} />
      <SelectField label="Obiectiv" name="goal" value={form.goal} onChange={set('goal')} options={GOAL_LABELS} error={errors.goal} />
      <FormError message={generalError} />
      <SubmitButton busy={update.isPending}>{update.isPending ? 'Se calculează…' : submitLabel}</SubmitButton>
    </form>
  )
}
