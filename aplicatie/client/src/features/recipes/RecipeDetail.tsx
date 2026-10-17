import { Archive, BookOpen, Loader2, Pencil } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import { FormError } from '@/components/ui/form'
import { useArchiveRecipe, useRecipe, type RecipeDto } from './recipesApi'

/** A recipe's ingredients with their values, the totals and the instructions, computed now from the catalog (ADR-006). */
export function RecipeDetail({ id, onEdit, onLog }: { id: string; onEdit: (recipe: RecipeDto) => void; onLog: (recipe: RecipeDto) => void }) {
  const recipe = useRecipe(id)
  const archive = useArchiveRecipe()

  if (recipe.isPending) {
    return <div className="p-4 flex justify-center"><Loader2 className="w-5 h-5 animate-spin text-emerald-600" /></div>
  }
  if (recipe.isError) {
    return <div className="p-4"><FormError message={problemMessage(recipe.error, 'Rețeta nu a putut fi încărcată.')} /></div>
  }

  const r = recipe.data
  return (
    <div className="p-4 space-y-4 border-t border-slate-200 dark:border-slate-800">
      {r.description && <p className="text-xs text-slate-600 dark:text-slate-400">{r.description}</p>}
      <table className="w-full text-xs">
        <thead>
          <tr className="text-left text-slate-500"><th className="font-semibold pb-1">Ingredient</th><th className="font-semibold pb-1 text-right">Cantitate</th><th className="font-semibold pb-1 text-right">kcal</th></tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {r.ingredients.map((i) => (
            <tr key={i.food.id}>
              <td className="py-1.5 text-slate-800 dark:text-slate-200">{i.food.name}{i.food.isArchived && <span className="ml-1 text-slate-400">(arhivat)</span>}</td>
              <td className="py-1.5 text-right font-mono">{i.quantityGrams} g</td>
              <td className="py-1.5 text-right font-mono">{Math.round(i.nutrients.calories)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="font-bold text-slate-900 dark:text-white"><td className="pt-2">Total</td><td /><td className="pt-2 text-right font-mono">{Math.round(r.total.calories)}</td></tr>
        </tfoot>
      </table>
      {r.instructions && <p className="text-xs whitespace-pre-line text-slate-700 dark:text-slate-300">{r.instructions}</p>}
      <FormError message={archive.error ? problemMessage(archive.error, 'Rețeta nu a putut fi arhivată.') : null} />
      <div className="flex flex-wrap gap-2">
        <button onClick={() => onLog(r)} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold">
          <BookOpen className="w-3.5 h-3.5" /> Adaugă în jurnal
        </button>
        <button onClick={() => onEdit(r)} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200">
          <Pencil className="w-3.5 h-3.5" /> Editează
        </button>
        <button onClick={() => archive.mutate(r.id)} disabled={archive.isPending} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-bold text-rose-700 dark:text-rose-400">
          <Archive className="w-3.5 h-3.5" /> Arhivează
        </button>
      </div>
    </div>
  )
}
