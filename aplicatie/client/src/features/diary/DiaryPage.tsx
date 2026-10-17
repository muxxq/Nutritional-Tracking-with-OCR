import { useState } from 'react'
import { Camera, ChevronLeft, ChevronRight, Loader2, Plus, Trash2 } from 'lucide-react'
import { Link, useSearchParams } from 'react-router'
import { problemMessage } from '@/api/problem'
import { FormError } from '@/components/ui/form'
import { addDays, formatDay, isIsoDate, today } from '@/lib/dates'
import { AddEntryDialog } from './AddEntryDialog'
import { CalorieSummary } from './CalorieSummary'
import { EditEntryDialog } from './EditEntryDialog'
import { useDeleteEntry, useDiaryDay, type DiaryEntryDto } from './diaryApi'
import { MEALS, suggestedMeal, type MealType } from './labels'

function quantityText(entry: DiaryEntryDto) {
  if (entry.kind === 'recipe' || entry.quantityGrams == null) {
    return `${entry.servings} ${entry.servings === 1 ? 'porție' : 'porții'}`
  }
  return entry.servings != null ? `${entry.servings} porții (${Math.round(entry.quantityGrams)} g)` : `${Math.round(entry.quantityGrams)} g`
}

/** FR-07 and the daily part of FR-08: one day's four meals against the targets. The day lives in the URL (?date=). */
export function DiaryPage() {
  const [params, setParams] = useSearchParams()
  const date = isIsoDate(params.get('date')) ? params.get('date')! : today()
  const setDate = (next: string) => setParams(next === today() ? {} : { date: next })

  const day = useDiaryDay(date)
  const remove = useDeleteEntry()
  const [adding, setAdding] = useState<{ meal: MealType; scan: boolean } | null>(null)
  const [editing, setEditing] = useState<DiaryEntryDto | null>(null)

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <div className="flex items-center gap-2">
          <button onClick={() => setDate(addDays(date, -1))} aria-label="Ziua anterioară" className="p-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800">
            <ChevronLeft className="w-4 h-4" />
          </button>
          <div className="text-center min-w-48">
            <h2 className="text-lg sm:text-xl font-extrabold text-slate-900 dark:text-white tracking-tight">{formatDay(date)}</h2>
            <input
              type="date"
              aria-label="Alege ziua"
              value={date}
              onChange={(e) => isIsoDate(e.target.value) && setDate(e.target.value)}
              className="text-xs text-slate-500 bg-transparent"
            />
          </div>
          <button onClick={() => setDate(addDays(date, 1))} aria-label="Ziua următoare" className="p-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800">
            <ChevronRight className="w-4 h-4" />
          </button>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setAdding({ meal: suggestedMeal(), scan: false })}
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 shadow-xs hover:bg-slate-50 dark:hover:bg-slate-700 transition"
          >
            <Plus className="w-4 h-4 text-emerald-600" /> Adaugă aliment
          </button>
          <button
            onClick={() => setAdding({ meal: suggestedMeal(), scan: true })}
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition"
          >
            <Camera className="w-4 h-4" /> Scanează eticheta
          </button>
        </div>
      </div>

      {day.isError && <FormError message={problemMessage(day.error, 'Ziua nu a putut fi încărcată.')} />}
      {day.isPending && (
        <div className="flex justify-center py-20" role="status" aria-label="Se încarcă jurnalul">
          <Loader2 className="w-8 h-8 animate-spin text-emerald-600" />
        </div>
      )}

      {day.data && (
        <div className={`grid grid-cols-1 lg:grid-cols-12 gap-6 ${day.isPlaceholderData ? 'opacity-60' : ''}`}>
          <div className="lg:col-span-5 space-y-5">
            {!day.data.isProfileComplete && (
              <p className="text-xs text-amber-900 dark:text-amber-200 bg-amber-50 dark:bg-amber-950/40 border border-amber-300 dark:border-amber-800 rounded-xl px-4 py-3">
                Țintele sunt valori implicite.{' '}
                <Link to="/onboarding" className="font-bold underline">Completează profilul</Link> ca să le calculăm pentru tine.
              </p>
            )}
            <CalorieSummary day={day.data} />
          </div>

          <div className="lg:col-span-7 space-y-4">
            {day.data.meals.map((meal) => {
              const info = MEALS.find((m) => m.type === meal.mealType)!
              return (
                <section key={meal.mealType} aria-label={info.label} className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-xs">
                  <div className="p-3.5 sm:p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                    <div className="flex items-center gap-2.5">
                      <span className="text-lg" aria-hidden="true">{info.icon}</span>
                      <h3 className="font-bold text-sm text-slate-900 dark:text-white">{info.label}</h3>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-bold font-mono text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-900 px-2 py-1 rounded-md border border-slate-200 dark:border-slate-700">
                        {Math.round(meal.totals.calories)} kcal
                      </span>
                      <button
                        onClick={() => setAdding({ meal: meal.mealType, scan: false })}
                        aria-label={`Adaugă la ${info.label.toLowerCase()}`}
                        className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-100 hover:bg-emerald-200 dark:bg-emerald-950 dark:hover:bg-emerald-900 text-emerald-800 dark:text-emerald-300 text-xs font-bold transition"
                      >
                        <Plus className="w-3.5 h-3.5" /> Adaugă
                      </button>
                    </div>
                  </div>

                  <ul className="divide-y divide-slate-100 dark:divide-slate-800/60">
                    {meal.entries.map((entry) => (
                      <li key={entry.id} className="p-3.5 flex items-center justify-between text-xs hover:bg-slate-50/70 dark:hover:bg-slate-800/30 transition">
                        <button onClick={() => setEditing(entry)} className="text-left space-y-0.5 flex-1">
                          <p className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                            {entry.food?.name ?? entry.recipe?.name}
                            {entry.kind === 'recipe' && <span className="ml-1.5 text-[10px] font-bold uppercase text-emerald-700 dark:text-emerald-400">rețetă</span>}
                          </p>
                          <p className="flex flex-wrap items-center gap-2 text-slate-500 dark:text-slate-400">
                            <span className="font-semibold text-emerald-700 dark:text-emerald-400">{quantityText(entry)}</span>
                            <span>P {entry.nutrients.proteins} g</span>
                            <span>C {entry.nutrients.carbs} g</span>
                            <span>G {entry.nutrients.fats} g</span>
                          </p>
                        </button>
                        <div className="flex items-center gap-3">
                          <span className="font-extrabold text-xs text-slate-900 dark:text-slate-100 font-mono">{Math.round(entry.nutrients.calories)} kcal</span>
                          <button
                            onClick={() => remove.mutate(entry.id)}
                            disabled={remove.isPending && remove.variables === entry.id}
                            aria-label={`Șterge ${entry.food?.name ?? entry.recipe?.name}`}
                            className="p-1.5 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </li>
                    ))}
                    {meal.entries.length === 0 && (
                      <li className="p-4 text-center text-xs text-slate-500 dark:text-slate-400 italic">Nimic notat încă.</li>
                    )}
                  </ul>
                </section>
              )
            })}
          </div>
        </div>
      )}

      {adding && <AddEntryDialog date={date} meal={adding.meal} startWithScan={adding.scan} onClose={() => setAdding(null)} />}
      {editing && <EditEntryDialog entry={editing} onClose={() => setEditing(null)} />}
    </div>
  )
}
