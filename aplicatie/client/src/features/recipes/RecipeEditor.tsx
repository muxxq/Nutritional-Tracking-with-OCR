import { useState, type FormEvent } from 'react'
import { BookOpen, Plus, Trash2 } from 'lucide-react'
import { fieldErrors, problemMessage } from '@/api/problem'
import { Dialog } from '@/components/ui/Dialog'
import { Field, FormError, SubmitButton } from '@/components/ui/form'
import { FoodSearch } from '@/features/foods/FoodSearch'
import { useSaveRecipe, type RecipeDto } from './recipesApi'

/** An ingredient row; `per100g` is kept only for the live preview (the server computes the real values). */
interface Row {
  foodId: string
  name: string
  grams: string
  per100g: { calories: number; proteins: number; carbs: number; fats: number }
}

const parse = (value: string) => Number(value.replace(',', '.')) || 0
const round1 = (value: number) => Math.round(value * 10) / 10

function rowsOf(recipe?: RecipeDto): Row[] {
  return (recipe?.ingredients ?? []).map((i) => {
    // Per 100 g back from the ingredient's own values: the detail has no catalog values per ingredient.
    const factor = i.quantityGrams > 0 ? 100 / i.quantityGrams : 0
    return {
      foodId: i.food.id,
      name: i.food.name,
      grams: String(i.quantityGrams),
      per100g: { calories: i.nutrients.calories * factor, proteins: i.nutrients.proteins * factor, carbs: i.nutrients.carbs * factor, fats: i.nutrients.fats * factor },
    }
  })
}

/** Creates or edits a recipe (FR-09): ingredients in grams, servings, and a live per-serving preview. */
export function RecipeEditor({ recipe, onClose, onSaved }: { recipe?: RecipeDto; onClose: () => void; onSaved: (recipe: RecipeDto) => void }) {
  const save = useSaveRecipe()
  const [name, setName] = useState(recipe?.name ?? '')
  const [description, setDescription] = useState(recipe?.description ?? '')
  const [instructions, setInstructions] = useState(recipe?.instructions ?? '')
  const [servings, setServings] = useState(String(recipe?.servingsCount ?? 2))
  const [prepTime, setPrepTime] = useState(recipe?.prepTimeMinutes != null ? String(recipe.prepTimeMinutes) : '')
  const [rows, setRows] = useState<Row[]>(() => rowsOf(recipe))
  const [picking, setPicking] = useState(false)

  const total = rows.reduce(
    (sum, r) => {
      const g = parse(r.grams) / 100
      return { calories: sum.calories + r.per100g.calories * g, proteins: sum.proteins + r.per100g.proteins * g, carbs: sum.carbs + r.per100g.carbs * g, fats: sum.fats + r.per100g.fats * g }
    },
    { calories: 0, proteins: 0, carbs: 0, fats: 0 },
  )
  const servingsCount = Math.max(1, Math.round(parse(servings)))

  const errors = save.error ? fieldErrors(save.error) : {}
  // "Ingredients" or "Ingredients[2].QuantityGrams": shown once, under the list.
  const ingredientsError = Object.entries(errors).find(([key]) => key.startsWith('ingredients'))?.[1]
  const generalError = save.error ? problemMessage(save.error, 'Rețeta nu a putut fi salvată.') : null

  const submit = (e: FormEvent) => {
    e.preventDefault()
    save.mutate(
      {
        id: recipe?.id,
        body: {
          name,
          description: description.trim() || null,
          instructions: instructions.trim() || null,
          servingsCount: Math.round(parse(servings)),
          prepTimeMinutes: prepTime.trim() === '' ? null : Math.round(parse(prepTime)),
          ingredients: rows.map((r) => ({ foodId: r.foodId, quantityGrams: parse(r.grams) })),
        },
      },
      { onSuccess: onSaved },
    )
  }

  return (
    <Dialog title={recipe ? `Editează: ${recipe.name}` : 'Rețetă nouă'} icon={<BookOpen className="w-4 h-4 text-emerald-600" />} onClose={onClose} wide>
      <form onSubmit={submit} className="space-y-4" noValidate>
        <Field label="Denumire" name="name" value={name} onChange={setName} error={errors.name} placeholder="ex: Ovăz cu lapte" />
        <div className="grid grid-cols-2 gap-3">
          <Field label="Porții" name="servingsCount" type="number" value={servings} onChange={setServings} error={errors.servingsCount} />
          <Field label="Timp de preparare (min, opțional)" name="prepTimeMinutes" type="number" value={prepTime} onChange={setPrepTime} error={errors.prepTimeMinutes} />
        </div>
        <Field label="Descriere (opțional)" name="description" value={description} onChange={setDescription} error={errors.description} />

        <fieldset className="space-y-2">
          <legend className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-2">Ingrediente</legend>
          {rows.map((row, i) => (
            <div key={row.foodId} className="flex items-center gap-2">
              <span className="flex-1 text-sm font-semibold text-slate-800 dark:text-slate-100 truncate">{row.name}</span>
              <input
                aria-label={`Grame ${row.name}`}
                inputMode="decimal"
                value={row.grams}
                onChange={(e) => setRows(rows.map((r, j) => (j === i ? { ...r, grams: e.target.value } : r)))}
                className="w-24 px-2 py-1.5 rounded-lg border text-right text-sm bg-white dark:bg-slate-950 border-slate-300 dark:border-slate-700"
              />
              <span className="text-xs text-slate-500">g</span>
              <button type="button" onClick={() => setRows(rows.filter((_, j) => j !== i))} aria-label={`Scoate ${row.name}`} className="p-1.5 text-slate-400 hover:text-rose-600">
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}
          {ingredientsError && <p className="text-xs text-red-600">{ingredientsError}</p>}

          {picking ? (
            <FoodSearch
              onPick={(food) => {
                if (!rows.some((r) => r.foodId === food.id)) {
                  setRows([...rows, { foodId: food.id, name: food.name, grams: '100', per100g: { calories: food.calories, proteins: food.proteins, carbs: food.carbohydrates, fats: food.fats } }])
                }
                setPicking(false)
              }}
            />
          ) : (
            <button type="button" onClick={() => setPicking(true)} className="flex items-center gap-1.5 text-xs font-bold text-emerald-700 dark:text-emerald-400 hover:underline">
              <Plus className="w-4 h-4" /> Adaugă ingredient
            </button>
          )}
        </fieldset>

        <div className="grid grid-cols-4 gap-2 text-center" aria-label="Per porție">
          {[
            ['Calorii / porție', `${Math.round(total.calories / servingsCount)} kcal`],
            ['Proteine', `${round1(total.proteins / servingsCount)} g`],
            ['Carbohidrați', `${round1(total.carbs / servingsCount)} g`],
            ['Grăsimi', `${round1(total.fats / servingsCount)} g`],
          ].map(([label, text]) => (
            <div key={label} className="p-2 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700">
              <span className="text-[10px] text-slate-500 uppercase font-semibold">{label}</span>
              <p className="font-extrabold text-xs text-slate-900 dark:text-white">{text}</p>
            </div>
          ))}
        </div>

        <div className="space-y-1.5">
          <label htmlFor="field-instructions" className="block text-xs font-semibold text-slate-700 dark:text-slate-300">Mod de preparare (opțional)</label>
          <textarea
            id="field-instructions"
            rows={4}
            value={instructions}
            onChange={(e) => setInstructions(e.target.value)}
            className="w-full px-3 py-2.5 rounded-lg border bg-white dark:bg-slate-950 text-sm border-slate-300 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        <FormError message={Object.keys(errors).length ? null : generalError} />
        <SubmitButton busy={save.isPending}>{save.isPending ? 'Se salvează…' : 'Salvează rețeta'}</SubmitButton>
      </form>
    </Dialog>
  )
}
