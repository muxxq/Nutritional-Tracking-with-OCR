import { BarChart3, ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'
import { Link, useSearchParams } from 'react-router'
import { problemMessage } from '@/api/problem'
import { FormError } from '@/components/ui/form'
import { STATUS } from '@/features/planner/labels'
import { addDays, addMonths, firstOfMonth, formatShortDay, isIsoDate, lastOfMonth, mondayOf, parseIsoDate, today } from '@/lib/dates'
import { CaloriesChart } from './CaloriesChart'
import { useStatistics, type StatisticsDto } from './statsApi'

type Period = 'week' | 'month'

function rangeOf(period: Period, anchor: string) {
  return period === 'week'
    ? { from: mondayOf(anchor), to: addDays(mondayOf(anchor), 6) }
    : { from: firstOfMonth(anchor), to: lastOfMonth(anchor) }
}

function title(period: Period, from: string, to: string) {
  if (period === 'month') {
    const text = parseIsoDate(from).toLocaleDateString('ro-RO', { month: 'long', year: 'numeric' })
    return text.charAt(0).toUpperCase() + text.slice(1)
  }
  return `${formatShortDay(from).day} – ${formatShortDay(to).day}`
}

const signed = (value: number, unit: string) => `${value > 0 ? '+' : value < 0 ? '−' : ''}${Math.abs(Math.round(value))} ${unit}`

function Tile({ label, value, detail }: { label: string; value: string; detail?: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 shadow-xs">
      <p className="text-xs font-semibold text-slate-500 dark:text-slate-400">{label}</p>
      <p className="mt-1 text-2xl font-extrabold text-slate-900 dark:text-white">{value}</p>
      {detail && <p className="text-xs text-slate-600 dark:text-slate-400 mt-0.5">{detail}</p>}
    </div>
  )
}

function Summary({ stats }: { stats: StatisticsDto }) {
  if (!stats.average || !stats.deviation) {
    return (
      <p className="rounded-2xl border border-dashed border-slate-300 dark:border-slate-700 p-6 text-center text-sm text-slate-500">
        Nicio zi notată în această perioadă. <Link to="/diary" className="font-semibold text-emerald-700 dark:text-emerald-400 hover:underline">Deschide jurnalul</Link>
      </p>
    )
  }
  const { under, within, over } = stats.statusCounts
  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <Tile label="Media zilnică" value={`${Math.round(stats.average.calories)} kcal`} detail={`P ${stats.average.proteins} g · C ${stats.average.carbs} g · G ${stats.average.fats} g`} />
      <Tile
        label="Abatere față de țintă"
        value={signed(stats.deviation.calories, 'kcal')}
        detail={stats.caloriesDeviationPercent != null ? `${stats.caloriesDeviationPercent > 0 ? '+' : ''}${stats.caloriesDeviationPercent}% din ${Math.round(stats.targets.calories)} kcal` : undefined}
      />
      <Tile label="Zile notate" value={`${stats.loggedDays} din ${stats.totalDays}`} detail="Media se face doar pe zilele notate." />
      <Tile label="Zile în țintă (±5%)" value={String(within)} detail={`${under} sub · ${over} peste`} />
    </div>
  )
}

/** FR-08: averages and deviation from the target over a week or a month, with the calories of each day. */
export function StatisticsPage() {
  const [params, setParams] = useSearchParams()
  const period: Period = params.get('period') === 'month' ? 'month' : 'week'
  const anchor = isIsoDate(params.get('from')) ? params.get('from')! : today()
  const { from, to } = rangeOf(period, anchor)
  const stats = useStatistics(from, to)

  const go = (nextPeriod: Period, nextAnchor: string) => setParams({ period: nextPeriod, from: rangeOf(nextPeriod, nextAnchor).from })
  const step = (direction: 1 | -1) => go(period, period === 'week' ? addDays(from, 7 * direction) : addMonths(from, direction))

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
          <BarChart3 className="w-6 h-6 text-emerald-600" /> Statistici
        </h2>
        <div className="flex items-center gap-2">
          <div className="flex gap-1 bg-slate-100 dark:bg-slate-800 p-1 rounded-lg" role="group" aria-label="Perioada">
            {(['week', 'month'] as const).map((p) => (
              // Switching keeps today in view when the current period contains it (a week can start in last month).
              <button key={p} aria-pressed={period === p} onClick={() => go(p, today() >= from && today() <= to ? today() : from)} className={`px-3 py-1 rounded-md text-xs font-semibold ${period === p ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs' : 'text-slate-600 dark:text-slate-400'}`}>
                {p === 'week' ? 'Săptămână' : 'Lună'}
              </button>
            ))}
          </div>
          <button onClick={() => step(-1)} aria-label="Perioada anterioară" className="p-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800"><ChevronLeft className="w-4 h-4" /></button>
          <span className="min-w-32 text-center text-sm font-bold text-slate-800 dark:text-slate-100">{title(period, from, to)}</span>
          <button onClick={() => step(1)} aria-label="Perioada următoare" className="p-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800"><ChevronRight className="w-4 h-4" /></button>
        </div>
      </div>

      <FormError message={stats.isError ? problemMessage(stats.error, 'Statisticile nu au putut fi încărcate.') : null} />
      {stats.isPending && <div className="flex justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-emerald-600" /></div>}

      {stats.data && (
        <div className={`space-y-6 ${stats.isPlaceholderData ? 'opacity-60' : ''}`}>
          <Summary stats={stats.data} />

          <section className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-xs space-y-4">
            <h3 className="text-sm font-bold text-slate-900 dark:text-white">Calorii pe zi</h3>
            <CaloriesChart stats={stats.data} />
            <details>
              <summary className="cursor-pointer text-xs font-semibold text-emerald-700 dark:text-emerald-400">Vezi ca tabel</summary>
              <table className="mt-3 w-full text-xs">
                <thead>
                  <tr className="text-left text-slate-500">
                    <th className="font-semibold pb-1">Ziua</th><th className="font-semibold pb-1 text-right">kcal</th>
                    <th className="font-semibold pb-1 text-right">P</th><th className="font-semibold pb-1 text-right">C</th>
                    <th className="font-semibold pb-1 text-right">G</th><th className="font-semibold pb-1 text-right">Față de țintă</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800 tabular-nums">
                  {stats.data.days.map((d) => (
                    <tr key={d.date} className={d.isLogged ? '' : 'text-slate-400'}>
                      <td className="py-1.5">{d.date}</td>
                      <td className="py-1.5 text-right">{d.isLogged ? Math.round(d.totals.calories) : '—'}</td>
                      <td className="py-1.5 text-right">{d.isLogged ? d.totals.proteins : '—'}</td>
                      <td className="py-1.5 text-right">{d.isLogged ? d.totals.carbs : '—'}</td>
                      <td className="py-1.5 text-right">{d.isLogged ? d.totals.fats : '—'}</td>
                      <td className="py-1.5 text-right">{d.status ? STATUS[d.status].label : 'nenotat'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </details>
          </section>
        </div>
      )}
    </div>
  )
}
