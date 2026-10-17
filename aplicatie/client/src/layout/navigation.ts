import { BookOpen, CalendarDays, ShoppingCart, UtensilsCrossed, type LucideIcon } from 'lucide-react'

export interface NavItem {
  to: string
  label: string
  shortLabel: string
  icon: LucideIcon
}

/** One list for the desktop tabs and the mobile bottom bar. */
export const NAV_ITEMS: NavItem[] = [
  { to: '/diary', label: 'Jurnal', shortLabel: 'Jurnal', icon: BookOpen },
  { to: '/planner', label: 'Planificator Mese', shortLabel: 'Planificator', icon: CalendarDays },
  { to: '/recipes', label: 'Rețetar & Alimente', shortLabel: 'Rețete', icon: UtensilsCrossed },
  { to: '/shopping', label: 'Cumpărături', shortLabel: 'Cumpărături', icon: ShoppingCart },
]
