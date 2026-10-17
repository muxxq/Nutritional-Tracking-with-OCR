import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fakeApi, json, renderPage } from '@/test/render'
import { today } from '@/lib/dates'
import { DiaryPage } from './DiaryPage'
import type { DiaryDayDto } from './diaryApi'

const n = (calories: number, proteins = 0, carbs = 0, fats = 0) => ({ calories, proteins, carbs, fats })

const oatsEntry = {
  id: 'e1', kind: 'food' as const, date: today(), mealType: 'breakfast' as const, quantityGrams: 80, servings: 2,
  food: { id: 'f1', name: 'Fulgi de ovăz', brand: null, isArchived: false }, recipe: null, nutrients: n(311.2, 13.5, 53, 5.5),
}

function day(entries: (typeof oatsEntry)[]): DiaryDayDto {
  const total = entries.reduce((s, e) => s + e.nutrients.calories, 0)
  return {
    date: today(),
    meals: (['breakfast', 'lunch', 'dinner', 'snack'] as const).map((mealType) => ({
      mealType, entries: entries.filter((e) => e.mealType === mealType), totals: n(mealType === 'breakfast' ? total : 0),
    })),
    totals: n(total, 13.5, 53, 5.5), targets: n(1700, 124, 195, 47), remaining: n(1700 - total), isProfileComplete: true,
  }
}

describe('DiaryPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it("shows today's meals, entries and what is left of the target", async () => {
    // Arrange
    fakeApi({ [`GET /api/diary/days/${today()}`]: () => json(day([oatsEntry])) })

    // Act
    renderPage('/diary', <DiaryPage />)

    // Assert
    const breakfast = await screen.findByRole('region', { name: 'Mic dejun' })
    expect(within(breakfast).getByText('Fulgi de ovăz')).toBeInTheDocument()
    expect(within(breakfast).getByText('2 porții (80 g)')).toBeInTheDocument()
    expect(screen.getByText('1389')).toBeInTheDocument() // 1700 − 311.2, rounded
    expect(screen.getByText('kcal rămase')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Prânz' })).getByText('Nimic notat încă.')).toBeInTheDocument()
  })

  it('deletes an entry and reloads the day', async () => {
    // Arrange
    let entries = [oatsEntry]
    const fetchMock = fakeApi({
      [`GET /api/diary/days/${today()}`]: () => json(day(entries)),
      'DELETE /api/diary/entries/e1': () => {
        entries = []
        return new Response(null, { status: 204 })
      },
    })
    renderPage('/diary', <DiaryPage />)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'Șterge Fulgi de ovăz' }))

    // Assert
    expect(await screen.findAllByText('Nimic notat încă.')).toHaveLength(4)
    expect(fetchMock.mock.calls.filter((c) => (c[0] as Request).method === 'GET')).toHaveLength(2)
  })

  it('reads the day from the URL and moves to the previous one', async () => {
    // Arrange
    fakeApi({
      'GET /api/diary/days/2026-10-15': () => json({ ...day([]), date: '2026-10-15' }),
      'GET /api/diary/days/2026-10-14': () => json({ ...day([]), date: '2026-10-14' }),
    })
    renderPage('/diary', <DiaryPage />, {}, '/diary?date=2026-10-15')
    await screen.findByDisplayValue('2026-10-15')

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Ziua anterioară' }))

    // Assert
    expect(await screen.findByDisplayValue('2026-10-14')).toBeInTheDocument()
  })
})
