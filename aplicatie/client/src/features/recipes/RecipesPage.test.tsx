import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fakeApi, json, renderPage } from '@/test/render'
import { RecipesPage } from './RecipesPage'

const oatsSummary = { id: 'oats', name: 'Fulgi de ovăz', brand: null, calories: 389, proteins: 16.9, carbohydrates: 66.3, fats: 6.9, isVerified: true, isOwn: false }
const n = (calories: number, proteins: number, carbs: number, fats: number) => ({ calories, proteins, carbs, fats })

// The SRS acceptance example (FR-09): 200 g oats, 4 servings → 194.5 kcal per serving.
const srsRecipe = {
  id: 'r1', name: 'Ovăz cu lapte', description: null, instructions: 'Se fierbe 5 minute.', servingsCount: 4, prepTimeMinutes: 10, isArchived: false,
  ingredients: [{ food: { id: 'oats', name: 'Fulgi de ovăz', brand: null, isArchived: false }, quantityGrams: 200, nutrients: n(778, 33.8, 132.6, 13.8) }],
  total: n(778, 33.8, 132.6, 13.8), perServing: n(194.5, 8.5, 33.2, 3.5),
}
const srsSummary = { id: 'r1', name: 'Ovăz cu lapte', servingsCount: 4, prepTimeMinutes: 10, ingredientCount: 1, perServing: srsRecipe.perServing }

const bodyOf = async (fetchMock: ReturnType<typeof fakeApi>, method: string, path: string) =>
  (fetchMock.mock.calls.map((c) => c[0] as Request).find((r) => r.method === method && r.url.endsWith(path))!).json()

describe('RecipesPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('FR-09: builds the SRS recipe with a live per-serving preview and saves it', async () => {
    // Arrange
    let saved: (typeof srsSummary)[] = []
    const fetchMock = fakeApi({
      'GET /api/recipes': () => json({ items: saved, page: 1, pageSize: 50, totalCount: saved.length }),
      'GET /api/foods': () => json({ items: [oatsSummary], page: 1, pageSize: 20, totalCount: 1 }),
      'POST /api/recipes': () => {
        saved = [srsSummary]
        return json(srsRecipe, 201)
      },
      'GET /api/recipes/r1': () => json(srsRecipe),
    })
    renderPage('/recipes', <RecipesPage />)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'Rețetă nouă' }))
    const dialog = screen.getByRole('dialog')
    await userEvent.type(within(dialog).getByLabelText('Denumire'), 'Ovăz cu lapte')
    await userEvent.clear(within(dialog).getByLabelText('Porții'))
    await userEvent.type(within(dialog).getByLabelText('Porții'), '4')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Adaugă ingredient' }))
    await userEvent.click(await within(dialog).findByRole('button', { name: /Fulgi de ovăz/ }))
    await userEvent.clear(within(dialog).getByLabelText('Grame Fulgi de ovăz'))
    await userEvent.type(within(dialog).getByLabelText('Grame Fulgi de ovăz'), '200')

    // Assert: 778 kcal / 4 = 194.5, shown rounded
    expect(within(dialog).getByText('195 kcal')).toBeInTheDocument()

    // Act
    await userEvent.click(within(dialog).getByRole('button', { name: 'Salvează rețeta' }))

    // Assert
    expect(await bodyOf(fetchMock, 'POST', '/api/recipes')).toEqual({
      name: 'Ovăz cu lapte', description: null, instructions: null, servingsCount: 4, prepTimeMinutes: null,
      ingredients: [{ foodId: 'oats', quantityGrams: 200 }],
    })
    expect(await screen.findByText('Se fierbe 5 minute.')).toBeInTheDocument()
  })

  it('logs a recipe in the diary by the serving', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'GET /api/recipes': () => json({ items: [srsSummary], page: 1, pageSize: 50, totalCount: 1 }),
      'GET /api/recipes/r1': () => json(srsRecipe),
      'POST /api/diary/entries': () => json({}, 201),
    })
    renderPage('/recipes', <RecipesPage />)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: /Ovăz cu lapte/ }))
    await userEvent.click(await screen.findByRole('button', { name: 'Adaugă în jurnal' }))
    const dialog = screen.getByRole('dialog')
    await userEvent.clear(within(dialog).getByLabelText('Porții'))
    await userEvent.type(within(dialog).getByLabelText('Porții'), '2')

    // Assert
    expect(within(dialog).getByText('389 kcal')).toBeInTheDocument()

    // Act
    await userEvent.click(within(dialog).getByRole('button', { name: 'Adaugă în jurnal' }))

    // Assert
    await vi.waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(await bodyOf(fetchMock, 'POST', '/api/diary/entries')).toMatchObject({ recipeId: 'r1', servings: 2, quantityGrams: null })
  })
})
