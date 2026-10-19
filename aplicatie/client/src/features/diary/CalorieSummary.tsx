import type { DiaryDayDto } from './diaryApi'

const RING = 264 // circumference of r = 42

function MacroBar({ label, eaten, target, color, text }: { label: string; eaten: number; target: number; color: string; text: string }) {
  const percent = target > 0 ? Math.min(100, Math.round((eaten / target) * 100)) : 0
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-xs font-semibold">
        <span className="text-slate-700 dark:text-slate-200">{label}</span>
        <span className="text-slate-500 dark:text-slate-400 font-mono">
          <strong className={`${text} font-bold`}>{Math.round(eaten)}g</strong> / {Math.round(target)}g
        </span>
      </div>
      <div className="w-full h-2.5 rounded-full bg-slate-100 dark:bg-slate-800 overflow-hidden" role="progressbar" aria-label={label} aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}>
        <div className={`h-full ${color} rounded-full transition-all duration-500`} style={{ width: `${percent}%` }} />
      </div>
    </div>
  )
}

/** The day against its targets: the calorie ring shows what is left, the bars the macronutrients (FR-08, daily part). */
export function CalorieSummary({ day }: { day: DiaryDayDto }) {
  const { totals, targets, remaining } = day
  const percent = targets.calories > 0 ? Math.round((totals.calories / targets.calories) * 100) : 0
  const over = remaining.calories < 0

  return (
    <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 sm:p-6 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <span className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">Bilanț caloric</span>
        <span className="text-xs font-semibold px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-400">
          Țintă: {Math.round(targets.calories)} kcal
        </span>
      </div>

      <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
        <div className="relative w-36 h-36 sm:w-40 sm:h-40 flex items-center justify-center shrink-0">
          <svg className="w-full h-full -rotate-90" viewBox="0 0 100 100" aria-hidden="true">
            <circle cx="50" cy="50" r="42" className="stroke-slate-100 dark:stroke-slate-800" strokeWidth="11" fill="transparent" />
            <circle
              cx="50" cy="50" r="42"
              className={`${over ? 'stroke-rose-500' : 'stroke-emerald-500'} transition-all duration-700 ease-out`}
              strokeWidth="11"
              strokeDasharray={RING}
              strokeDashoffset={RING - (RING * Math.min(100, percent)) / 100}
              strokeLinecap="round"
              fill="transparent"
            />
          </svg>
          <div className="absolute flex flex-col items-center text-center">
            <span className="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
              {Math.abs(Math.round(remaining.calories))}
            </span>
            <span className="text-[10px] sm:text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {over ? 'kcal peste' : 'kcal rămase'}
            </span>
          </div>
        </div>

        <dl className="space-y-2.5 w-full sm:w-auto text-xs">
          <div className="flex items-center justify-between sm:justify-start gap-4">
            <dt className="text-slate-500 dark:text-slate-400 font-medium">Consumat:</dt>
            <dd className="font-bold text-slate-900 dark:text-white text-sm">{Math.round(totals.calories)} kcal</dd>
          </div>
          <div className="flex items-center justify-between sm:justify-start gap-4">
            <dt className="text-slate-500 dark:text-slate-400 font-medium">Țintă zilnică:</dt>
            <dd className="font-semibold text-slate-700 dark:text-slate-300">{Math.round(targets.calories)} kcal</dd>
          </div>
          <div className="flex items-center justify-between sm:justify-start gap-4">
            <dt className="text-slate-500 dark:text-slate-400 font-medium">Progres:</dt>
            <dd className={`font-bold ${over ? 'text-rose-600' : 'text-emerald-600 dark:text-emerald-400'}`}>{percent}%</dd>
          </div>
        </dl>
      </div>

      <div className="mt-6 pt-5 border-t border-slate-100 dark:border-slate-800 space-y-3.5">
        <span className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">Macronutrienți</span>
        <MacroBar label="Proteine" eaten={totals.proteins} target={targets.proteins} color="bg-blue-500" text="text-blue-600 dark:text-blue-400" />
        <MacroBar label="Carbohidrați" eaten={totals.carbs} target={targets.carbs} color="bg-amber-500" text="text-amber-600 dark:text-amber-400" />
        <MacroBar label="Grăsimi" eaten={totals.fats} target={targets.fats} color="bg-rose-500" text="text-rose-600 dark:text-rose-400" />
      </div>
    </div>
  )
}
