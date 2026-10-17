import { useState } from 'react'
import { ChevronDown, ChevronUp, Clock, Loader2, Plus, Search, UtensilsCrossed } from 'lucide-react'
import { useSearchParams } from 'react-router'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { FoodCatalogTab } from './FoodCatalogTab'
import { LogRecipeDialog } from './LogRecipeDialog'
import { RecipeDetail } from './RecipeDetail'
import { RecipeEditor } from './RecipeEditor'
import { useRecipeSearch, type RecipeDto } from './recipesApi'

/** FR-09: the recipe book (values per serving, computed from the catalog) and the food catalog, as two tabs. */
export function RecipesPage() {
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'catalog' ? 'catalog' : 'recipes'

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
          <UtensilsCrossed className="w-6 h-6 text-emerald-600" /> Rețetar și alimente
        </h2>
        <div className="flex gap-1 bg-slate-100 dark:bg-slate-800 p-1 rounded-lg" role="tablist">
          {(['recipes', 'catalog'] as const).map((t) => (
            <button key={t} role="tab" aria-selected={tab === t} onClick={() => setParams(t === 'recipes' ? {} : { tab: t })} className={`px-3 py-1.5 rounded-md text-xs font-semibold ${tab === t ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs' : 'text-slate-600 dark:text-slate-400'}`}>
              {t === 'recipes' ? 'Rețete' : 'Catalog alimente'}
            </button>
          ))}
        </div>
      </div>
      {tab === 'recipes' ? <RecipesTab /> : <FoodCatalogTab />}
    </div>
  )
}

function RecipesTab() {
  const [query, setQuery] = useState('')
  const search = useRecipeSearch(useDebouncedValue(query.trim(), 250))
  const [expanded, setExpanded] = useState<string | null>(null)
  const [editing, setEditing] = useState<RecipeDto | 'new' | null>(null)
  const [logging, setLogging] = useState<RecipeDto | null>(null)

  return (
    <div className="space-y-4">
      <div className="flex gap-2">
        <div className="relative flex-1">
          <Search className="w-4 h-4 absolute left-3 top-3 text-slate-400" />
          <input type="search" value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Caută rețete…" aria-label="Caută rețete" className="w-full bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl pl-9 pr-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
        </div>
        <button onClick={() => setEditing('new')} className="flex items-center gap-1.5 px-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold">
          <Plus className="w-4 h-4" /> Rețetă nouă
        </button>
      </div>

      {search.isPending && <div className="flex justify-center py-10"><Loader2 className="w-6 h-6 animate-spin text-emerald-600" /></div>}
      {search.data?.items.length === 0 && (
        <p className="text-center text-sm text-slate-500 py-10">{query ? 'Nicio rețetă găsită.' : 'Nu ai încă rețete. Creează prima rețetă.'}</p>
      )}

      <ul className="grid gap-3 md:grid-cols-2">
        {search.data?.items.map((r) => (
          <li key={r.id} className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden self-start">
            <button onClick={() => setExpanded(expanded === r.id ? null : r.id)} aria-expanded={expanded === r.id} className="w-full p-4 flex items-start justify-between text-left">
              <div className="space-y-1">
                <p className="font-bold text-sm text-slate-900 dark:text-white">{r.name}</p>
                <p className="text-xs text-slate-500 flex flex-wrap gap-x-3">
                  <span>{r.servingsCount} {r.servingsCount === 1 ? 'porție' : 'porții'}</span>
                  <span>{r.ingredientCount} ingrediente</span>
                  {r.prepTimeMinutes != null && <span className="flex items-center gap-1"><Clock className="w-3 h-3" />{r.prepTimeMinutes} min</span>}
                </p>
                <p className="text-xs font-semibold text-emerald-700 dark:text-emerald-400">
                  {Math.round(r.perServing.calories)} kcal / porție · P {r.perServing.proteins} · C {r.perServing.carbs} · G {r.perServing.fats}
                </p>
              </div>
              {expanded === r.id ? <ChevronUp className="w-4 h-4 text-slate-400" /> : <ChevronDown className="w-4 h-4 text-slate-400" />}
            </button>
            {expanded === r.id && <RecipeDetail id={r.id} onEdit={setEditing} onLog={setLogging} />}
          </li>
        ))}
      </ul>

      {editing && (
        <RecipeEditor
          recipe={editing === 'new' ? undefined : editing}
          onClose={() => setEditing(null)}
          onSaved={(saved) => {
            setEditing(null)
            setExpanded(saved.id)
          }}
        />
      )}
      {logging && <LogRecipeDialog recipe={logging} onClose={() => setLogging(null)} />}
    </div>
  )
}
