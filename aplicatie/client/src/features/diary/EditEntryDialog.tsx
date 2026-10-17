import { useState } from 'react'
import { Pencil } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import { Dialog, PrimaryButton, SecondaryButton } from '@/components/ui/Dialog'
import { Field, FormError, SelectField } from '@/components/ui/form'
import { useUpdateEntry, type DiaryEntryDto } from './diaryApi'
import { MEAL_LABELS } from './labels'

/** Changes an entry's quantity, meal or day; what was eaten stays the same (ADR-005). */
export function EditEntryDialog({ entry, onClose }: { entry: DiaryEntryDto; onClose: () => void }) {
  const update = useUpdateEntry()
  // An entry keeps the unit it was logged in: servings for recipes and for foods logged by the serving.
  const inServings = entry.servings != null
  const [amount, setAmount] = useState(String(inServings ? entry.servings : entry.quantityGrams))
  const [meal, setMeal] = useState(entry.mealType)
  const [date, setDate] = useState(entry.date)

  const save = () => {
    const value = Number(amount.replace(',', '.')) || 0
    update.mutate(
      {
        id: entry.id,
        body: { date, mealType: meal, quantityGrams: inServings ? null : value, servings: inServings ? value : null },
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Dialog
      title={`Editează: ${entry.food?.name ?? entry.recipe?.name}`}
      icon={<Pencil className="w-4 h-4 text-emerald-600" />}
      onClose={onClose}
      footer={
        <>
          <SecondaryButton onClick={onClose}>Anulează</SecondaryButton>
          <PrimaryButton onClick={save} disabled={update.isPending}>{update.isPending ? 'Se salvează…' : 'Salvează'}</PrimaryButton>
        </>
      }
    >
      <Field label={inServings ? 'Porții' : 'Cantitate (g)'} name="amount" type="number" value={amount} onChange={setAmount} />
      <div className="grid grid-cols-2 gap-3">
        <SelectField label="Masa" name="meal" value={meal} onChange={setMeal} options={MEAL_LABELS} />
        <Field label="Ziua" name="date" type="date" value={date} onChange={setDate} />
      </div>
      <FormError message={update.error ? problemMessage(update.error, 'Intrarea nu a putut fi salvată.') : null} />
    </Dialog>
  )
}
