import { useState } from 'react'
import { CalendarDays, CheckCircle2, ChevronLeft, ChevronRight, Loader2, Plus, ShoppingCart, Trash2, Utensils } from 'lucide-react'
import { useNavigate, useSearchParams } from 'react-router'
import { problemMessage } from '@/api/problem'
import { FormError } from '@/components/ui/form'
import { MEALS } from '@/features/diary/labels'
import { useGenerateShoppingList } from '@/features/shopping/shoppingApi'
import { addDays, formatShortDay, isIsoDate, mondayOf, today } from '@/lib/dates'
import { STATUS } from './labels'
import { PlanMealDialog } from './PlanMealDialog'
import { useDeletePlannedMeal, useEatPlannedMeal, usePlan, type PlannedMealDto } from './plannerApi'

function amountText(m: PlannedMealDto) {
  return m.kind === 'recipe' || m.quantityGrams == null ? `${m.servings} ${m.servings === 1 ? 'porție' : 'porții'}` : `${Math.round(m.quantityGrams)} g`
}

/** FR-10, FR-11: a week of planned meals, each day against the target; "I ate this" logs a meal in the diary. */
export function PlannerPage() {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const week = mondayOf(isIsoDate(params.get('week')) ? params.get('week')! : today())
  const sunday = addDays(week, 6)
  const [selected, setSelected] = useState(() => (today() >= week && today() <= sunday ? today() : week))
  const [planning, setPlanning] = useState(false)

  const plan = usePlan(week, sunday)
  const eat = useEatPlannedMeal()
  const remove = useDeletePlannedMeal()
  const generate = useGenerateShoppingList()

  const goToWeek = (monday: string) => {
    setParams(monday === mondayOf(today()) ? {} : { week: monday })
    setSelected(monday === mondayOf(today()) ? today() : monday)
  }

  const day = plan.data?.days.find((d) => d.date === selected)
  // The server refuses "I ate this" after tomorrow (ADR-007): a meal planned for next week cannot have been eaten.
  const canEat = selected <= addDays(today(), 1)
  const error = eat.error ?? remove.error ?? generate.error

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
          <CalendarDays className="w-6 h-6 text-emerald-600" /> Planificator
        </h2>
        <div className="flex items-center gap-2">
          <button onClick={() => setPlanning(true)} className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 shadow-xs">
            <Plus className="w-4 h-4 text-emerald-600" /> Planifică o masă
          </button>
          <button
            onClick={() => generate.mutate({ from: week, to: sunday }, { onSuccess: () => navigate('/shopping') })}
            disabled={generate.isPending}
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-60 text-white text-xs font-bold shadow-xs"
          >
            <ShoppingCart className="w-4 h-4" /> {generate.isPending ? 'Se generează…' : 'Listă de cumpărături'}
          </button>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <button onClick={() => goToWeek(addDays(week, -7))} aria-label="Săptămâna anterioară" className="p-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800"><ChevronLeft className="w-4 h-4" /></button>
        <div className="flex-1 grid grid-cols-7 gap-1.5" role="tablist" aria-label="Zilele săptămânii">
          {(plan.data?.days ?? []).map((d) => {
            const { weekday, day: dayText } = formatShortDay(d.date)
            const isSelected = d.date === selected
            const planned = d.meals.reduce((n, m) => n + m.items.length, 0)
            return (
              <button
                key={d.date}
                role="tab"
                aria-selected={isSelected}
                onClick={() => setSelected(d.date)}
                className={`flex flex-col items-center py-2 rounded-xl border text-xs font-bold transition ${isSelected ? 'bg-emerald-600 text-white border-emerald-600' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300'}`}
              >
                <span className="capitalize">{weekday}</span>
                <span className={`text-[10px] font-medium ${isSelected ? 'text-emerald-100' : 'text-slate-500'}`}>{dayText}</span>
                {planned > 0 && <span className={`mt-1 w-2 h-2 rounded-full ${d.status === 'within' ? 'bg-emerald-400' : d.status === 'over' ? 'bg-amber-400' : 'bg-blue-400'}`} aria-label={STATUS[d.status].label} />}
              </button>
            )
          })}
        </div>
        <button onClick={() => goToWeek(addDays(week, 7))} aria-label="Săptămâna următoare" className="p-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800"><ChevronRight className="w-4 h-4" /></button>
      </div>

      {plan.isPending && <div className="flex justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-emerald-600" /></div>}
      <FormError message={plan.isError ? problemMessage(plan.error, 'Planul nu a putut fi încărcat.') : error ? problemMessage(error, 'Acțiunea nu a reușit.') : null} />

      {day && (
        <>
          <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-xs flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <span className="font-extrabold text-base text-slate-900 dark:text-white">{Math.round(day.totals.calories)} / {Math.round(day.targets.calories)} kcal</span>
              <span className={`text-[11px] font-bold px-2 py-0.5 rounded-md ${STATUS[day.status].className}`}>{STATUS[day.status].label}</span>
            </div>
            <p className="text-xs text-slate-500">P {day.totals.proteins} g · C {day.totals.carbs} g · G {day.totals.fats} g</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {day.meals.map((meal) => {
              const info = MEALS.find((m) => m.type === meal.mealType)!
              return (
                <section key={meal.mealType} aria-label={info.label} className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
                  <div className="p-3.5 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                    <h3 className="font-bold text-sm text-slate-900 dark:text-white"><span aria-hidden="true">{info.icon}</span> {info.label}</h3>
                    <span className="text-xs font-bold font-mono text-slate-600 dark:text-slate-300">{Math.round(meal.totals.calories)} kcal</span>
                  </div>
                  <ul className="divide-y divide-slate-100 dark:divide-slate-800/60">
                    {meal.items.map((m) => {
                      const name = m.recipe?.name ?? m.food?.name
                      return (
                        <li key={m.id} className="p-3.5 flex items-center justify-between gap-2 text-xs">
                          <div>
                            <p className="font-bold text-sm text-slate-900 dark:text-slate-100">{name}</p>
                            <p className="text-slate-500">{amountText(m)} · {Math.round(m.nutrients.calories)} kcal</p>
                          </div>
                          <div className="flex items-center gap-1">
                            {m.diaryEntryId ? (
                              <span className="flex items-center gap-1 text-emerald-700 dark:text-emerald-400 font-bold"><CheckCircle2 className="w-4 h-4" /> În jurnal</span>
                            ) : canEat ? (
                              <button onClick={() => eat.mutate(m.id)} disabled={eat.isPending} className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-100 hover:bg-emerald-200 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 font-bold">
                                <Utensils className="w-3.5 h-3.5" /> Am mâncat asta
                              </button>
                            ) : null}
                            <button onClick={() => remove.mutate(m.id)} aria-label={`Șterge ${name} din plan`} className="p-1.5 text-slate-400 hover:text-rose-600"><Trash2 className="w-3.5 h-3.5" /></button>
                          </div>
                        </li>
                      )
                    })}
                    {meal.items.length === 0 && <li className="p-4 text-center text-xs text-slate-500 italic">Nimic planificat.</li>}
                  </ul>
                </section>
              )
            })}
          </div>
        </>
      )}

      {planning && <PlanMealDialog date={selected} onClose={() => setPlanning(false)} />}
    </div>
  )
}
