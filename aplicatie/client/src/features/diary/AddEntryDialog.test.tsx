import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fakeApi, json, renderPage } from '@/test/render'
import { AddEntryDialog } from './AddEntryDialog'

const oats = {
  id: 'f1', name: 'Fulgi de ovăz', brand: 'Generic', barcode: null, servingSizeGrams: 40, isVerified: true, isOwn: false,
  energyWarning: null, category: 'grains',
  per100g: { calories: 389, proteins: 16.9, carbohydrates: 66.3, sugars: null, fats: 6.9, saturatedFats: null, fiber: null, saltGrams: null },
}
const summary = { id: 'f1', name: 'Fulgi de ovăz', brand: 'Generic', calories: 389, proteins: 16.9, carbohydrates: 66.3, fats: 6.9, isVerified: true, isOwn: false }

const posted = (fetchMock: ReturnType<typeof fakeApi>, path: string) =>
  fetchMock.mock.calls.map((c) => c[0] as Request).find((r) => r.method === 'POST' && r.url.endsWith(path))!

describe('AddEntryDialog', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('searches the catalog, previews the macros and logs the food in servings', async () => {
    // Arrange
    const onClose = vi.fn()
    const fetchMock = fakeApi({
      'GET /api/diary/recent-foods': () => json([]),
      'GET /api/foods': (r) => json({ items: new URL(r.url).searchParams.get('query') === 'ovaz' ? [summary] : [], page: 1, pageSize: 20, totalCount: 1 }),
      'GET /api/foods/f1': () => json(oats),
      'POST /api/diary/entries': () => json({}, 201),
    })
    renderPage('/diary', <AddEntryDialog date="2026-10-17" meal="breakfast" onClose={onClose} />)

    // Act
    await userEvent.type(screen.getByLabelText('Caută aliment'), 'ovaz')
    await userEvent.click(await screen.findByRole('button', { name: /Fulgi de ovăz/ }))
    await userEvent.selectOptions(await screen.findByLabelText('Unitate'), 'servings')
    await userEvent.clear(screen.getByLabelText('Cantitate'))
    await userEvent.type(screen.getByLabelText('Cantitate'), '2')

    // Assert: 2 servings × 40 g = 80 g → 311 kcal
    expect(screen.getByText('311 kcal')).toBeInTheDocument()

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Adaugă în jurnal' }))

    // Assert
    await vi.waitFor(() => expect(onClose).toHaveBeenCalled())
    expect(await posted(fetchMock, '/api/diary/entries').json()).toEqual({
      foodId: 'f1', date: '2026-10-17', mealType: 'breakfast', quantityGrams: null, servings: 2,
    })
  })

  it('while OCR is unavailable, a scan opens the empty food form with the server message (NFR-12)', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'POST /api/scans': () => json({
        status: 'unavailable', message: 'Recunoașterea automată nu este disponibilă acum. Completează valorile manual.',
        servingBase: null, overallConfidence: null, fields: {},
      }),
    })
    renderPage('/diary', <AddEntryDialog date="2026-10-17" meal="lunch" startWithScan onClose={() => {}} />)

    // Act
    await userEvent.upload(screen.getByLabelText('Fotografie etichetă'), new File([new Uint8Array([0xff, 0xd8, 0xff])], 'eticheta.jpg', { type: 'image/jpeg' }))

    // Assert
    expect(await screen.findByText(/nu este disponibilă acum/)).toBeInTheDocument()
    expect(screen.getByLabelText('Denumire')).toHaveValue('')
    const upload = posted(fetchMock, '/api/scans')
    expect((await upload.formData()).get('file')).toMatchObject({ size: 3, type: 'image/jpeg' })
  })
})
