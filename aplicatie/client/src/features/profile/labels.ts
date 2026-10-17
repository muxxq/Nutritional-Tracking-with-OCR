import type { Schemas } from '@/api/types'

export const GENDER_LABELS: Record<Schemas['Gender'], string> = {
  female: 'Femeie',
  male: 'Bărbat',
}

export const ACTIVITY_LABELS: Record<Schemas['ActivityLevel'], string> = {
  sedentary: 'Sedentar (birou, fără sport)',
  lightlyActive: 'Ușor activ (sport 1–3 zile/săptămână)',
  moderatelyActive: 'Moderat activ (sport 3–5 zile/săptămână)',
  veryActive: 'Foarte activ (sport 6–7 zile/săptămână)',
  extremelyActive: 'Extrem de activ (muncă fizică și sport)',
}

export const GOAL_LABELS: Record<Schemas['NutritionGoal'], string> = {
  loseWeight: 'Slăbire',
  maintainWeight: 'Menținerea greutății',
  gainMuscle: 'Masă musculară',
}

export const TARGET_SOURCE_LABELS: Record<Schemas['TargetSource'], string> = {
  default: 'implicit',
  calculated: 'calculat',
  manual: 'manual',
}
