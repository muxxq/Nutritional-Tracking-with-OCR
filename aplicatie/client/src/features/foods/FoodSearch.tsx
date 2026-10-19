import { useState, type ReactNode } from 'react'
import { Check, Loader2, Search } from 'lucide-react'
import type { Schemas } from '@/api/types'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useFoodSearch, type FoodSummaryDto } from './foodsApi'

/**
 * Catalog search with results as buttons. `whenEmpty` is shown instead of the results while nothing is typed
 * (e.g. the recent foods); `action` renders an extra control per row.
 */
export function FoodSearch({
  onPick, whenEmpty, scope = 'all', action, autoFocus = true,
}: {
  onPick: (food: FoodSummaryDto) => void
  whenEmpty?: ReactNode
  scope?: Schemas['FoodScope']
  action?: (food: FoodSummaryDto) => ReactNode
  autoFocus?: boolean
}) {
  const [query, setQuery] = useState('')
  const debounced = useDebouncedValue(query.trim(), 250)
  const search = useFoodSearch(debounced, scope)

  return (
    <div className="space-y-3">
      <div className="relative">
        <Search className="w-4 h-4 absolute left-3 top-3 text-slate-400" />
        <input
          type="search"
          value={query}
          autoFocus={autoFocus}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Caută în catalog (ex: pui, ovăz, iaurt)…"
          aria-label="Caută aliment"
          className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl pl-9 pr-3 py-2.5 text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500"
        />
      </div>

      {debounced === '' && whenEmpty}

      <ul className="max-h-64 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-xl divide-y divide-slate-100 dark:divide-slate-800" aria-label="Rezultate">
        {search.data?.items.map((food) => (
          <li key={food.id} className="flex items-center">
            <button type="button" onClick={() => onPick(food)} className="flex-1 text-left p-2.5 text-xs hover:bg-slate-50 dark:hover:bg-slate-800/40 text-slate-700 dark:text-slate-200">
              <p className="font-bold flex items-center gap-1.5">
                {food.name}
                {food.brand && <span className="font-normal text-slate-500">· {food.brand}</span>}
                {food.isVerified && <Check className="w-3.5 h-3.5 text-emerald-600" aria-label="verificat" />}
              </p>
              <p className="text-[10px] text-slate-500 dark:text-slate-400">
                {food.calories} kcal / 100 g · P {food.proteins} g · C {food.carbohydrates} g · G {food.fats} g
              </p>
            </button>
            {action?.(food)}
          </li>
        ))}
        {search.data && search.data.items.length === 0 && (
          <li className="p-4 text-center text-xs text-slate-500">Niciun aliment găsit.</li>
        )}
        {search.isPending && (
          <li className="p-4 flex justify-center"><Loader2 className="w-4 h-4 animate-spin text-emerald-600" /></li>
        )}
      </ul>
    </div>
  )
}
