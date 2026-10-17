import { fireEvent, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { addDays, mondayOf, today } from '@/lib/dates'
import { fakeApi, json, renderPage } from '@/test/render'
import { StatisticsPage } from './StatisticsPage'
import type { StatisticsDto } from './statsApi'

const n = (calories: number) => ({ calories, proteins: 100, carbs: 200, fats: 50 })
const monday = mondayOf(today())

// The ADR-009 example: 1500, 1700 and 1900 kcal on three of seven days → average 1700, exactly on target.
function week(): StatisticsDto {
  const logged: Record<number, number> = { 0: 1500, 2: 1700, 4: 1900 }
  return {
    from: monday, to: addDays(monday, 6), totalDays: 7, loggedDays: 3,
    targets: n(1700), average: n(1700), deviation: n(0), caloriesDeviationPercent: 0,
    statusCounts: { under: 1, within: 1, over: 1 }, isProfileComplete: true,
    days: Array.from({ length: 7 }, (_, i) => ({
      date: addDays(monday, i),
      isLogged: i in logged,
      totals: n(logged[i] ?? 0),
      status: i in logged ? (logged[i] < 1615 ? 'under' : logged[i] > 1785 ? 'over' : 'within') : null,
    })),
  }
}

describe('StatisticsPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('FR-08: shows the average over logged days, the deviation and the day counts', async () => {
    // Arrange
    const fetchMock = fakeApi({ 'GET /api/stats': () => json(week()) })

    // Act
    renderPage('/stats', <StatisticsPage />)

    // Assert
    expect(await screen.findByText('1700 kcal')).toBeInTheDocument()
    expect(screen.getByText('0 kcal')).toBeInTheDocument()
    expect(screen.getByText('3 din 7')).toBeInTheDocument()
    expect(screen.getByText('1 sub · 1 peste')).toBeInTheDocument()
    const url = new URL((fetchMock.mock.calls[0][0] as Request).url)
    expect(Object.fromEntries(url.searchParams)).toEqual({ from: monday, to: addDays(monday, 6) })
  })

  it('an unlogged day has no column and says so, instead of showing zero', async () => {
    // Arrange
    fakeApi({ 'GET /api/stats': () => json(week()) })
    renderPage('/stats', <StatisticsPage />)

    // Act
    const unlogged = await screen.findByLabelText(`${addDays(monday, 1)}: nenotat`)
    fireEvent.focus(unlogged)

    // Assert
    expect(screen.getByRole('tooltip')).toHaveTextContent('Nimic notat')
    expect(screen.getByLabelText(`${monday}: 1500 kcal`)).toBeInTheDocument()
  })

  it('switches to the calendar month', async () => {
    // Arrange
    const fetchMock = fakeApi({ 'GET /api/stats': () => json(week()) })
    renderPage('/stats', <StatisticsPage />)
    await screen.findByText('3 din 7')

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Lună' }))

    // Assert
    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    const url = new URL((fetchMock.mock.calls[1][0] as Request).url)
    expect(url.searchParams.get('from')).toMatch(/-01$/)
  })

  it('without logged days it points to the diary instead of showing empty numbers', async () => {
    // Arrange
    fakeApi({
      'GET /api/stats': () =>
        json({ ...week(), loggedDays: 0, average: null, deviation: null, caloriesDeviationPercent: null, statusCounts: { under: 0, within: 0, over: 0 }, days: week().days.map((d) => ({ ...d, isLogged: false, status: null, totals: n(0) })) }),
    })

    // Act
    renderPage('/stats', <StatisticsPage />)

    // Assert
    expect(await screen.findByText(/Nicio zi notată/)).toBeInTheDocument()
  })
})
