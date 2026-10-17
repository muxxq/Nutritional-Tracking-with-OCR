import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { addDays, mondayOf, today } from '@/lib/dates'
import { fakeApi, json, renderPage } from '@/test/render'
import { PlannerPage } from './PlannerPage'
import type { PlanDto, PlannedMealDto } from './plannerApi'

const n = (calories: number) => ({ calories, proteins: 10, carbs: 20, fats: 5 })
const monday = mondayOf(today())

const recipeMeal = (diaryEntryId: string | null = null): PlannedMealDto => ({
  id: 'p1', kind: 'recipe', date: today(), mealType: 'lunch', quantityGrams: null, servings: 2,
  food: null, recipe: { id: 'r1', name: 'Ovăz cu lapte', isArchived: false }, nutrients: n(389), diaryEntryId,
})

function week(items: PlannedMealDto[]): PlanDto {
  return {
    from: monday, to: addDays(monday, 6), isProfileComplete: true,
    days: Array.from({ length: 7 }, (_, i) => {
      const date = addDays(monday, i)
      const dayItems = items.filter((m) => m.date === date)
      const total = dayItems.reduce((s, m) => s + m.nutrients.calories, 0)
      return {
        date, targets: n(1700), totals: n(total), status: 'under' as const,
        meals: (['breakfast', 'lunch', 'dinner', 'snack'] as const).map((mealType) => ({
          mealType, items: dayItems.filter((m) => m.mealType === mealType), totals: n(dayItems.filter((m) => m.mealType === mealType).reduce((s, m) => s + m.nutrients.calories, 0)),
        })),
      }
    }),
  }
}

describe('PlannerPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it("FR-11: 'I ate this' logs the planned meal and the plan then shows it as in the diary", async () => {
    // Arrange
    let eaten = false
    const fetchMock = fakeApi({
      'GET /api/plan': () => json(week([recipeMeal(eaten ? 'e1' : null)])),
      'POST /api/plan/items/p1/eat': () => {
        eaten = true
        return json({ id: 'e1' }, 201)
      },
    })
    renderPage('/planner', <PlannerPage />)

    // Act
    const lunch = await screen.findByRole('region', { name: 'Prânz' })
    expect(within(lunch).getByText('2 porții · 389 kcal')).toBeInTheDocument()
    await userEvent.click(within(lunch).getByRole('button', { name: 'Am mâncat asta' }))

    // Assert
    expect(await within(lunch).findByText('În jurnal')).toBeInTheDocument()
    const planRequest = fetchMock.mock.calls.map((c) => new URL((c[0] as Request).url)).find((u) => u.pathname === '/api/plan')!
    expect(Object.fromEntries(planRequest.searchParams)).toEqual({ from: monday, to: addDays(monday, 6) })
  })

  it('plans a recipe in servings for the selected day', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'GET /api/plan': () => json(week([])),
      'GET /api/recipes': () => json({ items: [{ id: 'r1', name: 'Ovăz cu lapte', servingsCount: 4, prepTimeMinutes: null, ingredientCount: 1, perServing: n(194.5) }], page: 1, pageSize: 50, totalCount: 1 }),
      'POST /api/plan/items': () => json({}, 201),
    })
    renderPage('/planner', <PlannerPage />)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'Planifică o masă' }))
    const dialog = screen.getByRole('dialog')
    await userEvent.click(await within(dialog).findByRole('button', { name: /Ovăz cu lapte/ }))
    await userEvent.clear(within(dialog).getByLabelText('Porții'))
    await userEvent.type(within(dialog).getByLabelText('Porții'), '2')
    await userEvent.selectOptions(within(dialog).getByLabelText('Masa'), 'dinner')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Planifică' }))

    // Assert
    await vi.waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    const post = fetchMock.mock.calls.map((c) => c[0] as Request).find((r) => r.method === 'POST')!
    expect(await post.json()).toEqual({ recipeId: 'r1', date: today(), mealType: 'dinner', servings: 2, quantityGrams: null })
  })

  it('generates the shopping list for the week shown and opens it', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'GET /api/plan': () => json(week([recipeMeal()])),
      'POST /api/shopping-list/generate': () => json({ categories: [], totalCount: 0, purchasedCount: 0 }),
    })
    renderPage('/planner', <PlannerPage />, { '/shopping': 'lista de cumpărături' })

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'Listă de cumpărături' }))

    // Assert
    expect(await screen.findByText('lista de cumpărături')).toBeInTheDocument()
    const post = fetchMock.mock.calls.map((c) => c[0] as Request).find((r) => r.method === 'POST')!
    expect(await post.json()).toEqual({ from: monday, to: addDays(monday, 6) })
  })
})
