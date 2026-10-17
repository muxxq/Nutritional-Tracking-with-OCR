import { useState } from 'react'
import {
  Camera,
  CheckCircle2,
  AlertTriangle,
  RefreshCw
} from 'lucide-react'
import type { MealType, FoodItem } from '../types/nutrition'

interface OcrScanModalProps {
  isOpen: boolean
  onClose: () => void
  onSaveToDiary: (mealType: MealType, food: FoodItem, grams: number) => void
}

export function OcrScanModal({ isOpen, onClose, onSaveToDiary }: OcrScanModalProps) {
  const [scanState, setScanState] = useState<'idle' | 'scanning' | 'success'>('success')
  const [targetMeal, setTargetMeal] = useState<MealType>('lunch')

  // Editable HitL form values
  const [productName, setProductName] = useState('Etichetă Scanată — Iaurt Proteic')
  const [calories, setCalories] = useState('78')
  const [proteins, setProteins] = useState('11.5')
  const [carbs, setCarbs] = useState('4.2')
  const [fats, setFats] = useState('1.5')
  const [servingGrams, setServingGrams] = useState('150')

  // After every hook: React needs the same hooks in the same order on every render.
  if (!isOpen) return null

  // Sample presets for demo testing
  const applyPreset = (name: string, cal: string, p: string, c: string, f: string) => {
    setScanState('scanning')
    setTimeout(() => {
      setProductName(name)
      setCalories(cal)
      setProteins(p)
      setCarbs(c)
      setFats(f)
      setScanState('success')
    }, 600)
  }

  const handleConfirmSave = () => {
    const food: FoodItem = {
      id: `ocr-${Date.now()}`,
      name: productName,
      brand: 'OCR Scanat',
      calories: parseFloat(calories) || 0,
      proteins: parseFloat(proteins) || 0,
      carbohydrates: parseFloat(carbs) || 0,
      fats: parseFloat(fats) || 0,
      category: 'Lactate',
    }

    onSaveToDiary(targetMeal, food, parseFloat(servingGrams) || 100)
    onClose()
  }

  // Energy coherence check (FR-06): 4*P + 4*C + 9*G vs Cal
  const calcKcal = (parseFloat(proteins) || 0) * 4 + (parseFloat(carbs) || 0) * 4 + (parseFloat(fats) || 0) * 9
  const actualKcal = parseFloat(calories) || 0
  const isCoherent = actualKcal === 0 || Math.abs(calcKcal - actualKcal) / Math.max(1, actualKcal) <= 0.15

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4 bg-slate-900/70 backdrop-blur-xs animate-in fade-in duration-150">
      <div className="w-full max-w-lg rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden max-h-[92vh] flex flex-col">
        
        {/* Modal Header */}
        <div className="p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <Camera className="w-5 h-5 text-emerald-600" />
            <h3 className="font-bold text-sm text-slate-900 dark:text-white">
              Scanare Etichetă & Extragere OCR (UC01)
            </h3>
          </div>
          <button onClick={onClose} className="text-slate-400 font-bold text-sm">✕</button>
        </div>

        {/* Modal Content */}
        <div className="p-4 sm:p-5 space-y-4 overflow-y-auto">
          
          {/* Viewfinder Preview */}
          <div className="relative aspect-video rounded-xl bg-slate-950 flex flex-col items-center justify-center text-slate-400 overflow-hidden border-2 border-dashed border-emerald-500/60">
            {scanState === 'scanning' ? (
              <div className="flex flex-col items-center gap-2">
                <RefreshCw className="w-8 h-8 text-emerald-400 animate-spin" />
                <span className="text-xs font-mono text-emerald-400">Procesare OpenCV & Inferență OCR...</span>
              </div>
            ) : (
              <>
                <div className="absolute inset-3 border border-emerald-400/50 rounded-lg pointer-events-none flex flex-col justify-between p-2">
                  <span className="text-[10px] text-emerald-400 uppercase font-mono tracking-wider">
                    Vizor Aliniat: 100g Tabel Nutrițional
                  </span>
                  <span className="text-[10px] text-emerald-400 font-mono text-right">
                    OpenCV Binarizare Otsu Activă
                  </span>
                </div>
                <Camera className="w-9 h-9 text-emerald-500 mb-1" />
                <span className="text-xs text-slate-300 font-semibold">Tabel nutrițional detectat</span>
              </>
            )}
          </div>

          {/* Quick Demo Presets */}
          <div className="space-y-1.5">
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
              Testează rapid o etichetă de mostră:
            </span>
            <div className="flex items-center gap-1.5 flex-wrap">
              <button
                onClick={() => applyPreset('Iaurt Proteic 2%', '78', '11.5', '4.2', '1.5')}
                className="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 text-xs font-semibold text-slate-700 dark:text-slate-200"
              >
                🥛 Iaurt Proteic
              </button>
              <button
                onClick={() => applyPreset('Baton cu Arahide & Proteine', '412', '30.0', '35.0', '16.0')}
                className="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 text-xs font-semibold text-slate-700 dark:text-slate-200"
              >
                🍫 Baton Proteic
              </button>
              <button
                onClick={() => applyPreset('Ton în Suc Propriu', '115', '26.0', '0.0', '1.0')}
                className="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 text-xs font-semibold text-slate-700 dark:text-slate-200"
              >
                🐟 Ton Conservă
              </button>
            </div>
          </div>

          {/* HitL Form */}
          <div className="bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl p-4 space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-slate-800 dark:text-slate-200">
                Validare Asistată (Human-in-the-Loop):
              </span>
              <span className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-700 dark:text-emerald-400">
                <CheckCircle2 className="w-3.5 h-3.5" /> Scor Încredere: 92%
              </span>
            </div>

            <div>
              <label className="text-[10px] font-bold text-slate-500 block mb-0.5">Denumire Produs Extrasă:</label>
              <input
                type="text"
                value={productName}
                onChange={(e) => setProductName(e.target.value)}
                className="w-full bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg px-2.5 py-1 text-xs font-bold text-slate-900 dark:text-white"
              />
            </div>

            {/* Nutrients Grid with Confidence Highlighting */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
              <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-emerald-300 dark:border-emerald-800">
                <span className="text-[10px] font-bold text-emerald-700 dark:text-emerald-400 uppercase block">Calorii</span>
                <input
                  type="number"
                  value={calories}
                  onChange={(e) => setCalories(e.target.value)}
                  className="w-full font-black text-sm text-slate-900 dark:text-white bg-transparent focus:outline-none"
                />
                <span className="text-[9px] text-slate-400">kcal / 100g</span>
              </div>

              <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700">
                <span className="text-[10px] font-bold text-blue-600 uppercase block">Proteine</span>
                <input
                  type="number"
                  value={proteins}
                  onChange={(e) => setProteins(e.target.value)}
                  className="w-full font-bold text-xs text-slate-900 dark:text-white bg-transparent focus:outline-none"
                />
                <span className="text-[9px] text-slate-400">g / 100g</span>
              </div>

              <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700">
                <span className="text-[10px] font-bold text-amber-600 uppercase block">Carbohidrați</span>
                <input
                  type="number"
                  value={carbs}
                  onChange={(e) => setCarbs(e.target.value)}
                  className="w-full font-bold text-xs text-slate-900 dark:text-white bg-transparent focus:outline-none"
                />
                <span className="text-[9px] text-slate-400">g / 100g</span>
              </div>

              <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700">
                <span className="text-[10px] font-bold text-rose-600 uppercase block">Grăsimi</span>
                <input
                  type="number"
                  value={fats}
                  onChange={(e) => setFats(e.target.value)}
                  className="w-full font-bold text-xs text-slate-900 dark:text-white bg-transparent focus:outline-none"
                />
                <span className="text-[9px] text-slate-400">g / 100g</span>
              </div>
            </div>

            {/* Target Meal & Portion */}
            <div className="grid grid-cols-2 gap-3 pt-2 border-t border-slate-200 dark:border-slate-700">
              <div>
                <label className="text-[10px] font-bold text-slate-500 uppercase block mb-1">Salvează la masa:</label>
                <select
                  value={targetMeal}
                  onChange={(e) => setTargetMeal(e.target.value as MealType)}
                  className="w-full bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs font-semibold"
                >
                  <option value="breakfast">Mic Dejun</option>
                  <option value="lunch">Prânz</option>
                  <option value="dinner">Cină</option>
                  <option value="snack">Gustare</option>
                </select>
              </div>

              <div>
                <label className="text-[10px] font-bold text-slate-500 uppercase block mb-1">Porție consumată:</label>
                <div className="flex items-center gap-1">
                  <input
                    type="number"
                    value={servingGrams}
                    onChange={(e) => setServingGrams(e.target.value)}
                    className="w-full bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs text-center font-bold"
                  />
                  <span className="text-xs text-slate-500 font-semibold">g</span>
                </div>
              </div>
            </div>

            {/* FR-06 Coherence warning */}
            {!isCoherent && (
              <div className="flex items-center gap-1.5 text-[11px] text-amber-700 dark:text-amber-400 font-medium">
                <AlertTriangle className="w-3.5 h-3.5 shrink-0" />
                <span>Atenție (FR-06): Caloriile declarate diferă cu &gt;15% de calculul 4P + 4C + 9G. Verifică valorile!</span>
              </div>
            )}
          </div>

        </div>

        {/* Modal Footer */}
        <div className="p-4 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-200 dark:border-slate-800 flex justify-end gap-2 shrink-0">
          <button
            onClick={onClose}
            className="px-3.5 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-semibold"
          >
            Anulează
          </button>
          <button
            onClick={handleConfirmSave}
            className="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition"
          >
            Salvează în Jurnal
          </button>
        </div>

      </div>
    </div>
  )
}
