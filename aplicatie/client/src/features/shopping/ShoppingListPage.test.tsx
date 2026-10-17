import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fakeApi, json, renderPage } from '@/test/render'
import { ShoppingListPage } from './ShoppingListPage'
import type { ShoppingListDto } from './shoppingApi'

const chicken = { id: 'i1', name: 'Piept de pui', quantityGrams: 400, quantityText: null, category: 'meatFish' as const, isPurchased: false, isManual: false, foodId: 'f1' }
const paper = { id: 'i2', name: 'Hârtie de copt', quantityGrams: null, quantityText: '1 rolă', category: 'other' as const, isPurchased: false, isManual: true, foodId: null }

const listOf = (items: (typeof chicken | typeof paper)[]): ShoppingListDto => ({
  categories: (['meatFish', 'other'] as const)
    .map((category) => ({ category, items: items.filter((i) => i.category === category) }))
    .filter((c) => c.items.length > 0),
  totalCount: items.length,
  purchasedCount: items.filter((i) => i.isPurchased).length,
})

describe('ShoppingListPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('FR-12: shows the aggregated lines grouped by aisle', async () => {
    // Arrange
    fakeApi({ 'GET /api/shopping-list': () => json(listOf([chicken, paper])) })

    // Act
    renderPage('/shopping', <ShoppingListPage />)

    // Assert
    const meat = await screen.findByRole('region', { name: 'Carne și pește' })
    expect(within(meat).getByText('Piept de pui')).toBeInTheDocument()
    expect(within(meat).getByText('400 g')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Altele' })).getByText('1 rolă')).toBeInTheDocument()
    expect(screen.getByText('0 din 2 în coș')).toBeInTheDocument()
  })

  it('ticks a line at once, before the server answers', async () => {
    // Arrange
    let answer: (r: Response) => void = () => {}
    fakeApi({
      'GET /api/shopping-list': () => json(listOf([chicken])),
      'PATCH /api/shopping-list/items/i1': () => new Promise<Response>((resolve) => { answer = resolve }),
    })
    renderPage('/shopping', <ShoppingListPage />)
    const box = await screen.findByRole('checkbox', { name: /Piept de pui/ })

    // Act
    await userEvent.click(box)

    // Assert: checked while the PATCH is still pending
    expect(box).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByText('1 din 1 în coș')).toBeInTheDocument()
    answer(json({ ...chicken, isPurchased: true }))
  })

  it('rolls the tick back when the server refuses it', async () => {
    // Arrange
    fakeApi({
      'GET /api/shopping-list': () => json(listOf([chicken])),
      'PATCH /api/shopping-list/items/i1': () => json({ title: 'Articolul nu a fost găsit.', status: 404 }, 404),
    })
    renderPage('/shopping', <ShoppingListPage />)

    // Act
    await userEvent.click(await screen.findByRole('checkbox', { name: /Piept de pui/ }))

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Articolul nu a fost găsit.')
    expect(screen.getByRole('checkbox', { name: /Piept de pui/ })).toHaveAttribute('aria-checked', 'false')
  })

  it("explains when the week's plan is empty (UC05 A1)", async () => {
    // Arrange
    fakeApi({
      'GET /api/shopping-list': () => json(listOf([])),
      'POST /api/shopping-list/generate': () =>
        // The API's real answer (ShoppingListErrors.NothingPlanned)
        json({ title: 'Date invalide', status: 400, errors: { Range: ['Nu există mese planificate nemâncate în intervalul ales. Planifică mai întâi mesele.'] } }, 400),
    })
    renderPage('/shopping', <ShoppingListPage />)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: /Generează din planul săptămânii/ }))

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Nu există mese planificate nemâncate în intervalul ales.')
  })
})
