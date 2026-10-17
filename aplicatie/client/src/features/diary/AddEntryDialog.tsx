import { useState } from 'react'
import { AlertTriangle, Camera, Clock, Loader2, Plus, Utensils } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import { Dialog, PrimaryButton, SecondaryButton } from '@/components/ui/Dialog'
import { FormError, SelectField } from '@/components/ui/form'
import { FoodForm } from '@/features/foods/FoodForm'
import { FoodSearch } from '@/features/foods/FoodSearch'
import { useFood, type FoodDto } from '@/features/foods/foodsApi'
import { ScanStep } from '@/features/scan/ScanStep'
import type { ScanFieldsDto } from '@/features/scan/scanApi'
import { useAddEntry, useRecentFoods } from './diaryApi'
import { MEAL_LABELS, type MealType } from './labels'

type Step =
  | { name: 'pick' }
  | { name: 'scan' }
  | { name: 'create'; prefill?: ScanFieldsDto; notice?: string | null }
  | { name: 'quantity'; foodId: string; warning?: string | null }

/**
 * Logs a food on a day (FR-07): picked from the catalog or the recent foods, created by hand (NUT-47)
 * or read from a label photo first (FR-03 → FR-05).
 */
export function AddEntryDialog({
  date, meal: initialMeal, startWithScan = false, onClose,
}: {
  date: string
  meal: MealType
  startWithScan?: boolean
  onClose: () => void
}) {
  const [step, setStep] = useState<Step>(startWithScan ? { name: 'scan' } : { name: 'pick' })
  const [meal, setMeal] = useState(initialMeal)

  const onFoodCreated = (food: FoodDto) => setStep({ name: 'quantity', foodId: food.id, warning: food.energyWarning })

  const title = {
    pick: `Adaugă la ${MEAL_LABELS[meal].toLowerCase()}`,
    scan: 'Scanează eticheta',
    create: 'Aliment nou',
    quantity: `Cantitate la ${MEAL_LABELS[meal].toLowerCase()}`,
  }[step.name]

  return (
    <Dialog title={title} icon={<Utensils className="w-4 h-4 text-emerald-600" />} onClose={onClose}>
      {step.name === 'pick' && (
        <PickFood
          onPick={(foodId) => setStep({ name: 'quantity', foodId })}
          onCreate={() => setStep({ name: 'create' })}
          onScan={() => setStep({ name: 'scan' })}
        />
      )}
      {step.name === 'scan' && (
        <ScanStep onResult={(prefill, notice) => setStep({ name: 'create', prefill, notice })} onManual={() => setStep({ name: 'create' })} />
      )}
      {step.name === 'create' && (
        <>
          {step.notice && (
            <p role="status" className="text-xs text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 rounded-lg px-3 py-2">{step.notice}</p>
          )}
          <FoodForm prefill={step.prefill} onCreated={onFoodCreated} submitLabel="Salvează și continuă" />
        </>
      )}
      {step.name === 'quantity' && (
        <QuantityStep
          foodId={step.foodId}
          warning={step.warning}
          date={date}
          meal={meal}
          onMealChange={setMeal}
          onBack={() => setStep({ name: 'pick' })}
          onDone={onClose}
        />
      )}
    </Dialog>
  )
}

function PickFood({ onPick, onCreate, onScan }: { onPick: (foodId: string) => void; onCreate: () => void; onScan: () => void }) {
  const recent = useRecentFoods()

  return (
    <>
      <FoodSearch
        onPick={(food) => onPick(food.id)}
        whenEmpty={
          (recent.data?.length ?? 0) > 0 && (
            <div>
              <p className="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-2">
                <Clock className="w-3.5 h-3.5" /> Recente
              </p>
              <div className="flex flex-wrap gap-2">
                {recent.data!.map(({ food }) => (
                  <button
                    key={food.id}
                    onClick={() => onPick(food.id)}
                    className="px-3 py-1.5 rounded-full border border-slate-300 dark:border-slate-700 text-xs font-semibold text-slate-700 dark:text-slate-200 hover:bg-emerald-50 dark:hover:bg-emerald-950/40"
                  >
                    {food.name}
                  </button>
                ))}
              </div>
            </div>
          )
        }
      />

      <div className="grid grid-cols-2 gap-2">
        <button onClick={onCreate} className="flex items-center justify-center gap-1.5 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800">
          <Plus className="w-4 h-4 text-emerald-600" /> Aliment nou
        </button>
        <button onClick={onScan} className="flex items-center justify-center gap-1.5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold">
          <Camera className="w-4 h-4" /> Scanează eticheta
        </button>
      </div>
    </>
  )
}

function QuantityStep({
  foodId, warning, date, meal, onMealChange, onBack, onDone,
}: {
  foodId: string
  warning?: string | null
  date: string
  meal: MealType
  onMealChange: (meal: MealType) => void
  onBack: () => void
  onDone: () => void
}) {
  const food = useFood(foodId)
  const add = useAddEntry()
  const [unit, setUnit] = useState<'grams' | 'servings'>('grams')
  const [amount, setAmount] = useState('100')

  if (food.isPending) {
    return <div className="flex justify-center py-8"><Loader2 className="w-6 h-6 animate-spin text-emerald-600" /></div>
  }
  if (food.isError) {
    return <FormError message={problemMessage(food.error, 'Alimentul nu a putut fi încărcat.')} />
  }

  const f = food.data
  const value = Number(amount.replace(',', '.')) || 0
  const grams = unit === 'servings' && f.servingSizeGrams ? value * f.servingSizeGrams : value
  // Display only: the server computes and stores the entry's values (snapshot, ADR-005).
  const preview = (per100: number) => Math.round(((per100 * grams) / 100) * 10) / 10

  const save = () =>
    add.mutate(
      { foodId: f.id, date, mealType: meal, quantityGrams: unit === 'grams' ? value : null, servings: unit === 'servings' ? value : null },
      { onSuccess: onDone },
    )

  return (
    <>
      <div>
        <p className="font-bold text-sm text-slate-900 dark:text-white">{f.name}</p>
        <p className="text-xs text-slate-500">{f.brand ?? 'Fără marcă'} · {f.per100g.calories} kcal / 100 g</p>
      </div>

      {warning && (
        <p role="status" className="flex items-start gap-2 text-xs text-amber-800 dark:text-amber-300 bg-amber-50 dark:bg-amber-950/40 border border-amber-300 dark:border-amber-800 rounded-lg px-3 py-2">
          <AlertTriangle className="w-4 h-4 shrink-0" /> {warning}
        </p>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <label htmlFor="amount" className="block text-xs font-semibold text-slate-700 dark:text-slate-300">Cantitate</label>
          <div className="flex gap-2">
            <input
              id="amount"
              type="text"
              inputMode="decimal"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className="w-full px-3 py-2.5 rounded-lg border bg-white dark:bg-slate-950 text-sm font-bold text-slate-900 dark:text-white border-slate-300 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
            {f.servingSizeGrams ? (
              <select
                aria-label="Unitate"
                value={unit}
                onChange={(e) => {
                  const next = e.target.value as 'grams' | 'servings'
                  setUnit(next)
                  setAmount(next === 'servings' ? '1' : '100')
                }}
                className="px-2 rounded-lg border bg-white dark:bg-slate-950 text-xs border-slate-300 dark:border-slate-700"
              >
                <option value="grams">g</option>
                <option value="servings">porții ({f.servingSizeGrams} g)</option>
              </select>
            ) : (
              <span className="self-center text-xs font-semibold text-slate-500">g</span>
            )}
          </div>
        </div>
        <SelectField label="Masa" name="meal" value={meal} onChange={onMealChange} options={MEAL_LABELS} />
      </div>

      <div className="grid grid-cols-4 gap-2 text-center" aria-label="Valori pentru cantitatea aleasă">
        {[
          ['Calorii', `${Math.round(preview(f.per100g.calories))} kcal`, 'text-emerald-600'],
          ['Proteine', `${preview(f.per100g.proteins)} g`, 'text-blue-600'],
          ['Carbohidrați', `${preview(f.per100g.carbohydrates)} g`, 'text-amber-600'],
          ['Grăsimi', `${preview(f.per100g.fats)} g`, 'text-rose-600'],
        ].map(([label, text, color]) => (
          <div key={label} className="p-2 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700">
            <span className="text-[10px] text-slate-500 uppercase font-semibold">{label}</span>
            <p className={`font-extrabold text-xs ${color}`}>{text}</p>
          </div>
        ))}
      </div>

      <FormError message={add.error ? problemMessage(add.error, 'Intrarea nu a putut fi salvată.') : null} />
      <div className="flex justify-end gap-2">
        <SecondaryButton onClick={onBack}>Alt aliment</SecondaryButton>
        <PrimaryButton onClick={save} disabled={add.isPending || value <= 0}>
          {add.isPending ? 'Se adaugă…' : 'Adaugă în jurnal'}
        </PrimaryButton>
      </div>
    </>
  )
}
