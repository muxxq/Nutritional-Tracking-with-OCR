import { useState, type FormEvent } from 'react'
import { AlertTriangle } from 'lucide-react'
import { fieldErrors, problemMessage } from '@/api/problem'
import type { Schemas } from '@/api/types'
import { Field, FormError, SelectField, SubmitButton } from '@/components/ui/form'
import type { ScanFieldsDto } from '@/features/scan/scanApi'
import { CATEGORY_LABELS } from './labels'
import { useCreateFood, useUpdateFood, type FoodDto, type SaveFoodRequest } from './foodsApi'

type NutrientKey = keyof ScanFieldsDto

const NUTRIENTS: { key: NutrientKey; label: string; unit: string; required: boolean }[] = [
  { key: 'calories', label: 'Energie', unit: 'kcal', required: true },
  { key: 'proteins', label: 'Proteine', unit: 'g', required: true },
  { key: 'carbohydrates', label: 'Carbohidrați', unit: 'g', required: true },
  { key: 'sugars', label: 'din care zaharuri', unit: 'g', required: false },
  { key: 'fats', label: 'Grăsimi', unit: 'g', required: true },
  { key: 'saturatedFats', label: 'din care saturate', unit: 'g', required: false },
  { key: 'fiber', label: 'Fibre', unit: 'g', required: false },
  { key: 'saltGrams', label: 'Sare', unit: 'g', required: false },
]

const parse = (value: string) => (value.trim() === '' ? null : Number(value.replace(',', '.')))

/**
 * "Add food" (NUT-47), also used to edit one's own food (`food`): the label's values per 100 g, typed by hand or prefilled from a scan. Fields the OCR is unsure
 * of (`needsReview`) are highlighted and must be confirmed, or edited, before the food can be saved (FR-05).
 */
export function FoodForm({
  prefill, food, onCreated, submitLabel = 'Salvează alimentul',
}: {
  prefill?: ScanFieldsDto
  food?: FoodDto
  onCreated: (food: FoodDto) => void
  submitLabel?: string
}) {
  const create = useCreateFood()
  const update = useUpdateFood()
  const save = food ? update : create
  const [name, setName] = useState(food?.name ?? '')
  const [brand, setBrand] = useState(food?.brand ?? '')
  const [servingSizeGrams, setServingSizeGrams] = useState(food?.servingSizeGrams != null ? String(food.servingSizeGrams) : '')
  const [category, setCategory] = useState<Schemas['FoodCategory']>(food?.category ?? 'other')
  const [values, setValues] = useState<Record<NutrientKey, string>>(() => {
    const initial = (key: NutrientKey) => {
      const stored = food?.per100g[key]
      return stored != null ? String(stored) : prefill?.[key] ? String(prefill[key]!.value) : ''
    }
    return Object.fromEntries(NUTRIENTS.map(({ key }) => [key, initial(key)])) as Record<NutrientKey, string>
  })
  const [toConfirm, setToConfirm] = useState<Set<NutrientKey>>(
    () => new Set(NUTRIENTS.filter(({ key }) => prefill?.[key]?.needsReview).map(({ key }) => key)),
  )

  const confirm = (key: NutrientKey) => {
    const next = new Set(toConfirm)
    next.delete(key)
    setToConfirm(next)
  }

  const errors = save.error ? fieldErrors(save.error) : {}
  const generalError = save.error && !Object.keys(errors).length ? problemMessage(save.error, 'Alimentul nu a putut fi salvat.') : null

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (toConfirm.size > 0) {
      return
    }
    const body: SaveFoodRequest = {
      name,
      brand: brand.trim() || null,
      servingSizeGrams: parse(servingSizeGrams),
      category,
      // Required values left empty go as 0: the server validates and its message points at the field.
      calories: parse(values.calories) ?? 0,
      proteins: parse(values.proteins) ?? 0,
      carbohydrates: parse(values.carbohydrates) ?? 0,
      fats: parse(values.fats) ?? 0,
      sugars: parse(values.sugars),
      saturatedFats: parse(values.saturatedFats),
      fiber: parse(values.fiber),
      saltGrams: parse(values.saltGrams),
    }
    if (food) {
      update.mutate({ id: food.id, body }, { onSuccess: onCreated })
    } else {
      create.mutate(body, { onSuccess: onCreated })
    }
  }

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <Field label="Denumire" name="name" value={name} onChange={setName} error={errors.name} placeholder="ex: Iaurt grecesc 2%" />
      <div className="grid grid-cols-2 gap-3">
        <Field label="Marcă (opțional)" name="brand" value={brand} onChange={setBrand} error={errors.brand} />
        <SelectField label="Categorie" name="category" value={category} onChange={setCategory} options={CATEGORY_LABELS} />
      </div>
      <Field
        label="Porție (g, opțional)"
        name="servingSizeGrams"
        type="number"
        value={servingSizeGrams}
        onChange={setServingSizeGrams}
        error={errors.servingSizeGrams}
        hint="Dacă o completezi, alimentul se poate nota și în porții."
      />

      <fieldset className="space-y-3">
        <legend className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-2">Valori nutriționale per 100 g</legend>
        {toConfirm.size > 0 && (
          <p role="status" className="flex items-start gap-2 text-xs text-amber-800 dark:text-amber-300 bg-amber-50 dark:bg-amber-950/40 border border-amber-300 dark:border-amber-800 rounded-lg px-3 py-2">
            <AlertTriangle className="w-4 h-4 shrink-0" />
            Verifică pe etichetă câmpurile marcate: recunoașterea automată nu este sigură de ele.
          </p>
        )}
        <div className="grid grid-cols-2 gap-3">
          {NUTRIENTS.map(({ key, label, unit, required }) => {
            const unsure = toConfirm.has(key)
            return (
              <div key={key} className={unsure ? 'rounded-xl ring-2 ring-amber-400 p-2 -m-2' : undefined}>
                <Field
                  label={`${label} (${unit})${required ? '' : ', opțional'}`}
                  name={key}
                  type="number"
                  value={values[key]}
                  onChange={(value) => {
                    setValues({ ...values, [key]: value })
                    if (unsure) {
                      confirm(key)
                    }
                  }}
                  error={errors[key]}
                />
                {unsure && (
                  <label className="mt-1.5 flex items-center gap-1.5 text-[11px] font-semibold text-amber-800 dark:text-amber-300">
                    <input type="checkbox" onChange={() => confirm(key)} />
                    Am verificat valoarea
                  </label>
                )}
              </div>
            )
          })}
        </div>
      </fieldset>
      <FormError message={generalError} />
      <SubmitButton busy={save.isPending || toConfirm.size > 0}>
        {toConfirm.size > 0 ? `Confirmă ${toConfirm.size === 1 ? 'câmpul marcat' : `cele ${toConfirm.size} câmpuri marcate`}` : save.isPending ? 'Se salvează…' : submitLabel}
      </SubmitButton>
    </form>
  )
}

