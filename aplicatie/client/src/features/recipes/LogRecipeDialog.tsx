import { useState } from 'react'
import { BookOpen } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import { Dialog, PrimaryButton, SecondaryButton } from '@/components/ui/Dialog'
import { Field, FormError, SelectField } from '@/components/ui/form'
import { useAddEntry } from '@/features/diary/diaryApi'
import { MEAL_LABELS, suggestedMeal } from '@/features/diary/labels'
import { today } from '@/lib/dates'
import type { RecipeDto } from './recipesApi'

/** Logs a recipe in the diary by the serving (ADR-006 D3): its values are frozen as a snapshot at this moment. */
export function LogRecipeDialog({ recipe, onClose }: { recipe: RecipeDto; onClose: () => void }) {
  const add = useAddEntry()
  const [servings, setServings] = useState('1')
  const [meal, setMeal] = useState(suggestedMeal())
  const [date, setDate] = useState(today())
  const count = Number(servings.replace(',', '.')) || 0

  const save = () =>
    add.mutate({ recipeId: recipe.id, date, mealType: meal, servings: count, quantityGrams: null }, { onSuccess: onClose })

  return (
    <Dialog
      title={`În jurnal: ${recipe.name}`}
      icon={<BookOpen className="w-4 h-4 text-emerald-600" />}
      onClose={onClose}
      footer={
        <>
          <SecondaryButton onClick={onClose}>Anulează</SecondaryButton>
          <PrimaryButton onClick={save} disabled={add.isPending || count <= 0}>{add.isPending ? 'Se adaugă…' : 'Adaugă în jurnal'}</PrimaryButton>
        </>
      }
    >
      <Field label="Porții" name="servings" type="number" value={servings} onChange={setServings} hint={`${Math.round(recipe.perServing.calories * count)} kcal`} />
      <div className="grid grid-cols-2 gap-3">
        <SelectField label="Masa" name="meal" value={meal} onChange={setMeal} options={MEAL_LABELS} />
        <Field label="Ziua" name="date" type="date" value={date} onChange={setDate} />
      </div>
      <FormError message={add.error ? problemMessage(add.error, 'Rețeta nu a putut fi adăugată.') : null} />
    </Dialog>
  )
}
