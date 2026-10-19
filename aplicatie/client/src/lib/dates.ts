/** Calendar dates as the API exchanges them ("yyyy-MM-dd"), in the user's own time zone. */

const pad = (n: number) => String(n).padStart(2, '0')

export function toIsoDate(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** Today on the user's clock: toISOString() would give the UTC date, which is "yesterday" after midnight in Romania. */
export const today = () => toIsoDate(new Date())

export function parseIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

export function addDays(iso: string, days: number): string {
  const d = parseIsoDate(iso)
  d.setDate(d.getDate() + days)
  return toIsoDate(d)
}

export const isIsoDate = (value: string | null): value is string =>
  !!value && /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(parseIsoDate(value).getTime())

export function formatDay(iso: string): string {
  const relative = { [today()]: 'Astăzi', [addDays(today(), -1)]: 'Ieri', [addDays(today(), 1)]: 'Mâine' }[iso]
  const long = parseIsoDate(iso).toLocaleDateString('ro-RO', { weekday: 'long', day: 'numeric', month: 'long' })
  return relative ? `${relative}, ${long}` : long.charAt(0).toUpperCase() + long.slice(1)
}

/** The Monday of the week containing `iso` (weeks start on Monday in Romania). */
export function mondayOf(iso: string): string {
  const d = parseIsoDate(iso)
  return addDays(iso, -((d.getDay() + 6) % 7))
}

export function formatShortDay(iso: string): { weekday: string; day: string } {
  const d = parseIsoDate(iso)
  return {
    weekday: d.toLocaleDateString('ro-RO', { weekday: 'short' }).replace('.', ''),
    day: d.toLocaleDateString('ro-RO', { day: 'numeric', month: 'short' }).replace('.', ''),
  }
}

export const firstOfMonth = (iso: string) => `${iso.slice(0, 8)}01`

export function lastOfMonth(iso: string): string {
  const d = parseIsoDate(firstOfMonth(iso))
  return toIsoDate(new Date(d.getFullYear(), d.getMonth() + 1, 0))
}

export function addMonths(iso: string, months: number): string {
  const d = parseIsoDate(firstOfMonth(iso))
  return toIsoDate(new Date(d.getFullYear(), d.getMonth() + months, 1))
}
