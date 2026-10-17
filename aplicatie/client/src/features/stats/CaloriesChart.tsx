import { useState } from 'react'
import { STATUS } from '@/features/planner/labels'
import { formatShortDay, parseIsoDate } from '@/lib/dates'
import type { StatisticsDto } from './statsApi'

/** A clean axis maximum: the larger of the target and the highest day, plus headroom, up to the next 500 kcal. */
function axisMax(stats: StatisticsDto) {
  const highest = Math.max(stats.targets.calories, ...stats.days.map((d) => d.totals.calories))
  return Math.max(500, Math.ceil((highest * 1.1) / 500) * 500)
}

/**
 * Calories per day as columns against the target (a hairline reference). One series, so no legend: the title names it.
 * Unlogged days get no column at all - a zero would read as "ate nothing" (ADR-009 D2). The day's status lives in the
 * tooltip and the table, never in the column colour (status colours are reserved for state).
 */
export function CaloriesChart({ stats }: { stats: StatisticsDto }) {
  const [active, setActive] = useState<number | null>(null)
  const max = axisMax(stats)
  const ticks = [0, max / 2, max]
  const percent = (kcal: number) => `${(kcal / max) * 100}%`
  const dense = stats.days.length > 14

  return (
    <figure className="space-y-2" aria-label="Calorii pe zi față de țintă">
      <div className="relative h-56 pl-10">
        {/* Y axis ticks and hairline grid */}
        {ticks.map((t) => (
          <div key={t} className="absolute left-10 right-0 border-t border-slate-100 dark:border-slate-800" style={{ bottom: percent(t) }}>
            <span className="absolute -left-10 -translate-y-1/2 w-8 text-right text-[10px] tabular-nums text-slate-500 dark:text-slate-400">
              {t >= 1000 ? `${(t / 1000).toLocaleString('ro-RO')}K` : t}
            </span>
          </div>
        ))}

        {/* Target reference line, labelled once at its right end */}
        <div className="absolute left-10 right-0 border-t border-slate-500 dark:border-slate-400 z-10 pointer-events-none" style={{ bottom: percent(stats.targets.calories) }}>
          <span className="absolute right-0 -top-4 text-[10px] font-semibold text-slate-600 dark:text-slate-300 bg-white/80 dark:bg-slate-900/80 px-1">
            Țintă {Math.round(stats.targets.calories)}
          </span>
        </div>

        <div className="absolute inset-0 left-10 flex items-end" style={{ gap: 2 }}>
          {stats.days.map((d, i) => (
            <div
              key={d.date}
              tabIndex={0}
              aria-label={d.isLogged ? `${d.date}: ${Math.round(d.totals.calories)} kcal` : `${d.date}: nenotat`}
              onMouseEnter={() => setActive(i)}
              onMouseLeave={() => setActive(null)}
              onFocus={() => setActive(i)}
              onBlur={() => setActive(null)}
              // The whole slot is the hit target, wider and taller than the column itself.
              className="relative flex-1 h-full flex items-end justify-center outline-none focus-visible:bg-slate-100 dark:focus-visible:bg-slate-800 rounded"
            >
              {d.isLogged ? (
                <div className="w-full max-w-6 rounded-t bg-[#059669] transition-all" style={{ height: percent(d.totals.calories) }} />
              ) : (
                <div className="w-1.5 h-1.5 mb-0.5 rounded-full bg-slate-300 dark:bg-slate-600" aria-hidden="true" />
              )}
              {active === i && (
                <div role="tooltip" className={`absolute bottom-full mb-2 z-20 w-40 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 shadow-lg p-2.5 text-[11px] text-slate-700 dark:text-slate-200 ${i > stats.days.length / 2 ? 'right-0' : 'left-0'}`}>
                  <p className="font-bold capitalize">{parseIsoDate(d.date).toLocaleDateString('ro-RO', { weekday: 'long', day: 'numeric', month: 'long' })}</p>
                  {d.isLogged ? (
                    <>
                      <p className="mt-1 text-sm font-extrabold text-slate-900 dark:text-white">{Math.round(d.totals.calories)} kcal</p>
                      <p>P {d.totals.proteins} g · C {d.totals.carbs} g · G {d.totals.fats} g</p>
                      {d.status && <p className="mt-1 font-semibold">{STATUS[d.status].label}</p>}
                    </>
                  ) : (
                    <p className="mt-1 text-slate-500">Nimic notat</p>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="pl-10 flex" style={{ gap: 2 }} aria-hidden="true">
        {stats.days.map((d, i) => {
          const { weekday } = formatShortDay(d.date)
          const dayOfMonth = parseIsoDate(d.date).getDate()
          return (
            <span key={d.date} className="flex-1 text-center text-[10px] text-slate-500 dark:text-slate-400 capitalize">
              {dense ? (i === 0 || dayOfMonth % 5 === 0 ? dayOfMonth : '') : weekday}
            </span>
          )
        })}
      </div>
      <figcaption className="sr-only">Calorii consumate pe fiecare zi; linia orizontală este ținta zilnică.</figcaption>
    </figure>
  )
}
