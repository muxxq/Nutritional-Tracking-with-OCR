import { useState } from 'react'
import { Check, Loader2, Plus, RefreshCw, ShoppingCart, Trash2 } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import type { Schemas } from '@/api/types'
import { Dialog, PrimaryButton, SecondaryButton } from '@/components/ui/Dialog'
import { Field, FormError, SelectField } from '@/components/ui/form'
import { CATEGORY_LABELS } from '@/features/foods/labels'
import { addDays, mondayOf, today } from '@/lib/dates'
import {
  useAddShoppingItem, useClearPurchased, useDeleteShoppingItem, useGenerateShoppingList, useSetPurchased, useShoppingList,
  type ShoppingListItemDto,
} from './shoppingApi'

function quantity(item: ShoppingListItemDto) {
  if (item.quantityGrams == null) {
    return item.quantityText ?? ''
  }
  return item.quantityGrams >= 1000 ? `${(item.quantityGrams / 1000).toLocaleString('ro-RO')} kg` : `${item.quantityGrams} g`
}

/** FR-12: the list generated from the plan, grouped by aisle, with ticks and lines added by hand. */
export function ShoppingListPage() {
  const list = useShoppingList()
  const generate = useGenerateShoppingList()
  const toggle = useSetPurchased()
  const remove = useDeleteShoppingItem()
  const clear = useClearPurchased()
  const [adding, setAdding] = useState(false)

  const monday = mondayOf(today())
  const error = generate.error ?? toggle.error ?? remove.error ?? clear.error
  const data = list.data
  const percent = data && data.totalCount > 0 ? Math.round((data.purchasedCount / data.totalCount) * 100) : 0

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
          <ShoppingCart className="w-6 h-6 text-emerald-600" /> Listă de cumpărături
        </h2>
        <div className="flex items-center gap-2">
          <button
            onClick={() => generate.mutate({ from: monday, to: addDays(monday, 6) })}
            disabled={generate.isPending}
            title="Înlocuiește articolele generate cu ce cere planul săptămânii; articolele adăugate de mână rămân."
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 shadow-xs"
          >
            <RefreshCw className={`w-3.5 h-3.5 text-emerald-600 ${generate.isPending ? 'animate-spin' : ''}`} /> Generează din planul săptămânii
          </button>
          <button onClick={() => setAdding(true)} className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs">
            <Plus className="w-4 h-4" /> Adaugă articol
          </button>
        </div>
      </div>

      <FormError message={list.isError ? problemMessage(list.error, 'Lista nu a putut fi încărcată.') : error ? problemMessage(error, 'Acțiunea nu a reușit.') : null} />
      {list.isPending && <div className="flex justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-emerald-600" /></div>}

      {data && data.totalCount === 0 && (
        <p className="text-center text-sm text-slate-500 py-12">
          Lista e goală. Planifică mesele săptămânii și apasă „Generează din planul săptămânii”, sau adaugă articole de mână.
        </p>
      )}

      {data && data.totalCount > 0 && (
        <>
          <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-xs flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="w-full sm:w-auto space-y-2">
              <p className="font-extrabold text-base text-slate-900 dark:text-white">{data.purchasedCount} din {data.totalCount} în coș</p>
              <div className="w-full sm:w-64 h-2 rounded-full bg-slate-100 dark:bg-slate-800 overflow-hidden" role="progressbar" aria-label="Progres cumpărături" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}>
                <div className="h-full bg-emerald-500 transition-all" style={{ width: `${percent}%` }} />
              </div>
            </div>
            {data.purchasedCount > 0 && (
              <button onClick={() => clear.mutate(undefined)} disabled={clear.isPending} className="w-full sm:w-auto px-3 py-2 rounded-xl border border-rose-200 dark:border-rose-900/60 bg-rose-50 dark:bg-rose-950/30 text-rose-700 dark:text-rose-300 text-xs font-bold flex items-center justify-center gap-1.5">
                <Trash2 className="w-3.5 h-3.5" /> Șterge cele {data.purchasedCount} bifate
              </button>
            )}
          </div>

          <div className="space-y-5">
            {data.categories.map((category) => (
              <section key={category.category} aria-label={CATEGORY_LABELS[category.category]} className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-xs">
                <div className="px-4 py-2.5 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                  <h3 className="text-xs font-extrabold text-slate-900 dark:text-white uppercase tracking-wider">{CATEGORY_LABELS[category.category]}</h3>
                  <span className="text-[10px] font-bold text-slate-500 font-mono">{category.items.filter((i) => i.isPurchased).length}/{category.items.length}</span>
                </div>
                <ul className="divide-y divide-slate-100 dark:divide-slate-800/60">
                  {category.items.map((item) => (
                    <li key={item.id} className="flex items-center justify-between text-xs">
                      <button
                        role="checkbox"
                        aria-checked={item.isPurchased}
                        onClick={() => toggle.mutate({ id: item.id, isPurchased: !item.isPurchased })}
                        className="flex-1 p-3.5 flex items-center gap-3 text-left select-none"
                      >
                        <span className={`w-5 h-5 rounded-lg flex items-center justify-center border ${item.isPurchased ? 'bg-emerald-500 border-emerald-500 text-white' : 'border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800'}`}>
                          {item.isPurchased && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                        </span>
                        <span className={`font-semibold text-sm ${item.isPurchased ? 'line-through text-slate-400' : 'text-slate-900 dark:text-slate-100'}`}>{item.name}</span>
                        <span className="ml-auto font-mono text-slate-500">{quantity(item)}</span>
                      </button>
                      <button onClick={() => remove.mutate(item.id)} aria-label={`Șterge ${item.name}`} className="p-3 text-slate-400 hover:text-rose-600"><Trash2 className="w-3.5 h-3.5" /></button>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        </>
      )}

      {adding && <AddItemDialog onClose={() => setAdding(false)} />}
    </div>
  )
}

function AddItemDialog({ onClose }: { onClose: () => void }) {
  const add = useAddShoppingItem()
  const [name, setName] = useState('')
  const [quantityText, setQuantityText] = useState('')
  const [category, setCategory] = useState<Schemas['FoodCategory']>('other')

  return (
    <Dialog
      title="Adaugă articol"
      icon={<Plus className="w-4 h-4 text-emerald-600" />}
      onClose={onClose}
      footer={
        <>
          <SecondaryButton onClick={onClose}>Anulează</SecondaryButton>
          <PrimaryButton
            onClick={() => add.mutate({ name, quantity: quantityText.trim() || null, category }, { onSuccess: onClose })}
            disabled={add.isPending}
          >
            {add.isPending ? 'Se adaugă…' : 'Adaugă'}
          </PrimaryButton>
        </>
      }
    >
      <Field label="Articol" name="name" value={name} onChange={setName} placeholder="ex: Hârtie de copt" />
      <div className="grid grid-cols-2 gap-3">
        <Field label="Cantitate (opțional)" name="quantity" value={quantityText} onChange={setQuantityText} placeholder="ex: 1 rolă" />
        <SelectField label="Raion" name="category" value={category} onChange={setCategory} options={CATEGORY_LABELS} />
      </div>
      <FormError message={add.error ? problemMessage(add.error, 'Articolul nu a putut fi adăugat.') : null} />
    </Dialog>
  )
}
