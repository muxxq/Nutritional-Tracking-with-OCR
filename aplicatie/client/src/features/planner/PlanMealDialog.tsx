import { useState } from 'react'
import { CalendarPlus, Loader2, Search } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import { Dialog, PrimaryButton, SecondaryButton } from '@/components/ui/Dialog'
import { Field, FormError, SelectField } from '@/components/ui/form'
import { MEAL_LABELS, type MealType } from '@/features/diary/labels'
import { FoodSearch } from '@/features/foods/FoodSearch'
import type { FoodSummaryDto } from '@/features/foods/foodsApi'
import { useRecipeSearch, type RecipeSummaryDto } from '@/features/recipes/recipesApi'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useCreatePlannedMeal } from './plannerApi'

type Picked = { kind: 'recipe'; recipe: RecipeSummaryDto } | { kind: 'food'; food: FoodSummaryDto }

/** Plans a recipe (in servings) or a food (in grams) for a day and meal (FR-10, ADR-007 D1). */
export function PlanMealDialog({ date: initialDate, onClose }: { date: string; onClose: () => void }) {
  const create = useCreatePlannedMeal()
  const [source, setSource] = useState<'recipe' | 'food'>('recipe')
  const [picked, setPicked] = useState<Picked | null>(null)
  const [amount, setAmount] = useState('1')
  const [meal, setMeal] = useState<MealType>('lunch')
  const [date, setDate] = useState(initialDate)
  const value = Number(amount.replace(',', '.')) || 0

  const pick = (p: Picked) => {
    setPicked(p)
    setAmount(p.kind === 'recipe' ? '1' : '100')
  }

  const save = () =>
    picked &&
    create.mutate(
      picked.kind === 'recipe'
        ? { recipeId: picked.recipe.id, date, mealType: meal, servings: value, quantityGrams: null }
        : { foodId: picked.food.id, date, mealType: meal, quantityGrams: value, servings: null },
      { onSuccess: onClose },
    )

  const kcal = picked ? Math.round(picked.kind === 'recipe' ? picked.recipe.perServing.calories * value : (picked.food.calories * value) / 100) : 0

  return (
    <Dialog
      title="Planifică o masă"
      icon={<CalendarPlus className="w-4 h-4 text-emerald-600" />}
      onClose={onClose}
      footer={
        <>
          <SecondaryButton onClick={onClose}>Anulează</SecondaryButton>
          <PrimaryButton onClick={save} disabled={!picked || value <= 0 || create.isPending}>{create.isPending ? 'Se salvează…' : 'Planifică'}</PrimaryButton>
        </>
      }
    >
      {picked ? (
        <>
          <div className="flex items-center justify-between">
            <p className="font-bold text-sm text-slate-900 dark:text-white">{picked.kind === 'recipe' ? picked.recipe.name : picked.food.name}</p>
            <button onClick={() => setPicked(null)} className="text-xs font-semibold text-emerald-700 dark:text-emerald-400 hover:underline">Schimbă</button>
          </div>
          <div className="grid grid-cols-3 gap-3">
            <Field label={picked.kind === 'recipe' ? 'Porții' : 'Grame'} name="amount" type="number" value={amount} onChange={setAmount} hint={`${kcal} kcal`} />
            <SelectField label="Masa" name="meal" value={meal} onChange={setMeal} options={MEAL_LABELS} />
            <Field label="Ziua" name="date" type="date" value={date} onChange={setDate} />
          </div>
        </>
      ) : (
        <>
          <div className="flex gap-1 bg-slate-100 dark:bg-slate-800 p-1 rounded-lg w-fit" role="tablist">
            {(['recipe', 'food'] as const).map((s) => (
              <button key={s} role="tab" aria-selected={source === s} onClick={() => setSource(s)} className={`px-3 py-1 rounded-md text-xs font-semibold ${source === s ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs' : 'text-slate-600 dark:text-slate-400'}`}>
                {s === 'recipe' ? 'Rețetă' : 'Aliment'}
              </button>
            ))}
          </div>
          {source === 'recipe' ? <RecipePicker onPick={(recipe) => pick({ kind: 'recipe', recipe })} /> : <FoodSearch onPick={(food) => pick({ kind: 'food', food })} />}
        </>
      )}
      <FormError message={create.error ? problemMessage(create.error, 'Masa nu a putut fi planificată.') : null} />
    </Dialog>
  )
}

function RecipePicker({ onPick }: { onPick: (recipe: RecipeSummaryDto) => void }) {
  const [query, setQuery] = useState('')
  const search = useRecipeSearch(useDebouncedValue(query.trim(), 250))
  return (
    <div className="space-y-3">
      <div className="relative">
        <Search className="w-4 h-4 absolute left-3 top-3 text-slate-400" />
        <input type="search" autoFocus value={query} onChange={(e) => setQuery(e.target.value)} aria-label="Caută rețete" placeholder="Caută în rețetele tale…" className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl pl-9 pr-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
      </div>
      <ul className="max-h-64 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-xl divide-y divide-slate-100 dark:divide-slate-800" aria-label="Rețete">
        {search.data?.items.map((r) => (
          <li key={r.id}>
            <button onClick={() => onPick(r)} className="w-full text-left p-2.5 text-xs hover:bg-slate-50 dark:hover:bg-slate-800/40">
              <p className="font-bold text-slate-800 dark:text-slate-100">{r.name}</p>
              <p className="text-[10px] text-slate-500">{Math.round(r.perServing.calories)} kcal / porție</p>
            </button>
          </li>
        ))}
        {search.data?.items.length === 0 && <li className="p-4 text-center text-xs text-slate-500">Nicio rețetă. Creează una în Rețetar.</li>}
        {search.isPending && <li className="p-4 flex justify-center"><Loader2 className="w-4 h-4 animate-spin text-emerald-600" /></li>}
      </ul>
    </div>
  )
}
