import { useState } from 'react'
import { Archive, Pencil, Plus, UtensilsCrossed } from 'lucide-react'
import { Dialog } from '@/components/ui/Dialog'
import { FoodForm } from '@/features/foods/FoodForm'
import { FoodSearch } from '@/features/foods/FoodSearch'
import { useArchiveFood, useFood } from '@/features/foods/foodsApi'

/** The food catalog: global foods are read-only; one's own foods can be edited and archived (ADR-004). */
export function FoodCatalogTab() {
  const [scope, setScope] = useState<'all' | 'mine'>('all')
  const [editing, setEditing] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const archive = useArchiveFood()

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <div className="flex gap-1 bg-slate-100 dark:bg-slate-800 p-1 rounded-lg" role="group" aria-label="Ce alimente">
          {(['all', 'mine'] as const).map((s) => (
            <button key={s} onClick={() => setScope(s)} aria-pressed={scope === s} className={`px-3 py-1 rounded-md text-xs font-semibold ${scope === s ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs' : 'text-slate-600 dark:text-slate-400'}`}>
              {s === 'all' ? 'Toate' : 'Ale mele'}
            </button>
          ))}
        </div>
        <button onClick={() => setCreating(true)} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold">
          <Plus className="w-4 h-4" /> Aliment nou
        </button>
      </div>

      <FoodSearch
        key={scope}
        scope={scope}
        autoFocus={false}
        onPick={(food) => food.isOwn && setEditing(food.id)}
        action={(food) =>
          food.isOwn && (
            <div className="flex pr-2">
              <button onClick={() => setEditing(food.id)} aria-label={`Editează ${food.name}`} className="p-1.5 text-slate-400 hover:text-emerald-600"><Pencil className="w-4 h-4" /></button>
              <button onClick={() => archive.mutate(food.id)} aria-label={`Arhivează ${food.name}`} className="p-1.5 text-slate-400 hover:text-rose-600"><Archive className="w-4 h-4" /></button>
            </div>
          )
        }
      />

      {creating && (
        <Dialog title="Aliment nou" icon={<UtensilsCrossed className="w-4 h-4 text-emerald-600" />} onClose={() => setCreating(false)}>
          <FoodForm onCreated={() => setCreating(false)} />
        </Dialog>
      )}
      {editing && <EditFoodDialog id={editing} onClose={() => setEditing(null)} />}
    </div>
  )
}

function EditFoodDialog({ id, onClose }: { id: string; onClose: () => void }) {
  const food = useFood(id)
  return (
    <Dialog title={food.data ? `Editează: ${food.data.name}` : 'Editează alimentul'} icon={<Pencil className="w-4 h-4 text-emerald-600" />} onClose={onClose}>
      {food.data && <FoodForm food={food.data} onCreated={onClose} submitLabel="Salvează modificările" />}
    </Dialog>
  )
}
