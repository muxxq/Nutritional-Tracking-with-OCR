import { useState } from 'react'
import {
  ShoppingCart,
  Plus,
  Check,
  Trash2,
  RefreshCw
} from 'lucide-react'
import type { ShoppingItem } from '../types/nutrition'

interface ShoppingListViewProps {
  items: ShoppingItem[]
  onToggleItem: (id: string) => void
  onAddItem: (name: string, quantity: string, category: string) => void
  onClearPurchased: () => void
  onSyncFromPlanner: () => void
}

export function ShoppingListView({
  items,
  onToggleItem,
  onAddItem,
  onClearPurchased,
  onSyncFromPlanner,
}: ShoppingListViewProps) {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [newItemName, setNewItemName] = useState('')
  const [newItemQuantity, setNewItemQuantity] = useState('500 g')
  const [newItemCategory, setNewItemCategory] = useState('Legume & Fructe')

  const totalCount = items.length
  const purchasedCount = items.filter((i) => i.isPurchased).length
  const progressPercent = totalCount > 0 ? Math.round((purchasedCount / totalCount) * 100) : 0

  // Group by category
  const categories = Array.from(new Set(items.map((i) => i.category)))

  const handleAddSubmit = () => {
    if (!newItemName.trim()) return
    onAddItem(newItemName, newItemQuantity, newItemCategory)
    setNewItemName('')
    setIsAddModalOpen(false)
  }

  return (
    <div className="space-y-6">
      
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <div>
          <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
            <ShoppingCart className="w-6 h-6 text-emerald-600" />
            <span>Listă Consolidată de Cumpărături</span>
          </h2>
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-400 font-medium">
            Agregă automat ingredientele din planul săptămânal pentru cumpărături eficiente (FR-12)
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={onSyncFromPlanner}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 shadow-xs hover:bg-slate-50 dark:hover:bg-slate-700 transition"
          >
            <RefreshCw className="w-3.5 h-3.5 text-emerald-600" />
            <span>Sincronizează din Plan</span>
          </button>
          <button
            onClick={() => setIsAddModalOpen(true)}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition"
          >
            <Plus className="w-4 h-4" />
            <span>Adaugă Articol</span>
          </button>
        </div>
      </div>

      {/* Progress & Summary Card */}
      <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-xs flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="space-y-1 w-full sm:w-auto">
          <div className="flex items-center gap-2">
            <span className="font-extrabold text-base text-slate-900 dark:text-white">Progres Cumpărături</span>
            <span className="text-xs font-bold font-mono px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300">
              {purchasedCount} din {totalCount} bifate ({progressPercent}%)
            </span>
          </div>
          <p className="text-xs text-slate-500 font-medium">
            Bifează articolele pe măsură ce le adaugi în coș în magazin.
          </p>
        </div>

        {purchasedCount > 0 && (
          <button
            onClick={onClearPurchased}
            className="w-full sm:w-auto px-3 py-1.5 rounded-xl border border-rose-200 dark:border-rose-900/60 bg-rose-50 dark:bg-rose-950/30 hover:bg-rose-100 text-rose-700 dark:text-rose-300 text-xs font-bold transition flex items-center justify-center gap-1.5 shrink-0"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>Șterge cele {purchasedCount} bifate</span>
          </button>
        )}
      </div>

      {/* Grouped Shopping List */}
      <div className="space-y-5">
        {categories.map((category) => {
          const catItems = items.filter((i) => i.category === category)
          return (
            <div
              key={category}
              className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-xs"
            >
              <div className="px-4 py-2.5 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                <span className="text-xs font-extrabold text-slate-900 dark:text-white uppercase tracking-wider">
                  {category}
                </span>
                <span className="text-[10px] font-bold text-slate-500 font-mono">
                  {catItems.filter((i) => i.isPurchased).length}/{catItems.length}
                </span>
              </div>

              <div className="divide-y divide-slate-100 dark:divide-slate-800/60">
                {catItems.map((item) => (
                  <div
                    key={item.id}
                    onClick={() => onToggleItem(item.id)}
                    className="p-3.5 flex items-center justify-between text-xs cursor-pointer hover:bg-slate-50/70 dark:hover:bg-slate-800/30 transition select-none"
                  >
                    <div className="flex items-center gap-3">
                      {/* Checkbox */}
                      <div
                        className={`w-5 h-5 rounded-lg flex items-center justify-center transition border ${
                          item.isPurchased
                            ? 'bg-emerald-500 border-emerald-500 text-white'
                            : 'border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800'
                        }`}
                      >
                        {item.isPurchased && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                      </div>

                      <span
                        className={`text-sm font-semibold transition ${
                          item.isPurchased
                            ? 'line-through text-slate-400 dark:text-slate-500'
                            : 'text-slate-900 dark:text-slate-100'
                        }`}
                      >
                        {item.name}
                      </span>
                    </div>

                    <span
                      className={`font-mono text-xs font-bold ${
                        item.isPurchased
                          ? 'text-slate-400'
                          : 'text-emerald-700 dark:text-emerald-400'
                      }`}
                    >
                      {item.quantity}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )
        })}

        {totalCount === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 p-8 text-center bg-white dark:bg-slate-900">
            <ShoppingCart className="w-10 h-10 text-slate-400 mx-auto mb-2 opacity-60" />
            <p className="font-bold text-sm text-slate-800 dark:text-slate-200">Lista de cumpărături este goală</p>
            <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
              Apasă pe „Sincronizează din Plan” pentru a prelua automat toate ingredientele săptămânii sau adaugă manual articole.
            </p>
          </div>
        )}
      </div>

      {/* MODAL: Adaugă articol */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-150">
          <div className="w-full max-w-md rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden">
            <div className="p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
              <h3 className="font-bold text-sm text-slate-900 dark:text-white">Adaugă Articol pe Listă</h3>
              <button onClick={() => setIsAddModalOpen(false)} className="text-slate-400 font-bold text-sm">✕</button>
            </div>

            <div className="p-5 space-y-3.5">
              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Denumire Produs:</label>
                <input
                  type="text"
                  value={newItemName}
                  onChange={(e) => setNewItemName(e.target.value)}
                  placeholder="ex: Brânză Mozzarella light"
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Cantitate:</label>
                <input
                  type="text"
                  value={newItemQuantity}
                  onChange={(e) => setNewItemQuantity(e.target.value)}
                  placeholder="ex: 2 bucăți, 500 g, 1 L"
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Categorie Magazin:</label>
                <select
                  value={newItemCategory}
                  onChange={(e) => setNewItemCategory(e.target.value)}
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                >
                  <option value="Fructe & Legume">Fructe & Legume</option>
                  <option value="Carne & Pește">Carne & Pește</option>
                  <option value="Lactate">Lactate</option>
                  <option value="Cereale">Cereale</option>
                  <option value="Băcănie">Băcănie</option>
                </select>
              </div>
            </div>

            <div className="p-3.5 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-200 dark:border-slate-800 flex justify-end gap-2">
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="px-3 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-semibold"
              >
                Anulează
              </button>
              <button
                onClick={handleAddSubmit}
                disabled={!newItemName.trim()}
                className="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white text-xs font-bold"
              >
                Adaugă
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
