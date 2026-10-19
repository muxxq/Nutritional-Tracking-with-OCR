import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fakeApi, json, renderPage } from '@/test/render'
import { FoodForm } from './FoodForm'

const field = (value: number, confidence: number) => ({ value, confidence, needsReview: confidence < 0.8 })

describe('FoodForm', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('FR-05: a prefilled field with low confidence blocks saving until it is confirmed', async () => {
    // Arrange: the SRS example, proteins read with confidence 0.42
    const onCreated = vi.fn()
    const fetchMock = fakeApi({ 'POST /api/foods': (r) => r.clone().json().then((body) => json({ id: 'new', ...body, energyWarning: null }, 201)) })
    renderPage('/', <FoodForm prefill={{ calories: field(389, 0.95), proteins: field(16.9, 0.42), carbohydrates: field(66.3, 0.9), fats: field(6.9, 0.9) }} onCreated={onCreated} />)
    await userEvent.type(screen.getByLabelText('Denumire'), 'Ovăz scanat')

    // Assert: the unsure field is the only one asking for confirmation, and saving is blocked
    expect(screen.getByLabelText('Proteine (g)')).toHaveValue('16.9')
    expect(screen.getAllByText('Am verificat valoarea')).toHaveLength(1)
    const save = screen.getByRole('button', { name: 'Confirmă câmpul marcat' })
    expect(save).toBeDisabled()

    // Act
    await userEvent.click(screen.getByLabelText('Am verificat valoarea'))
    await userEvent.click(screen.getByRole('button', { name: 'Salvează alimentul' }))

    // Assert
    await vi.waitFor(() => expect(onCreated).toHaveBeenCalled())
    const body = await (fetchMock.mock.calls[0][0] as Request).json()
    expect(body).toMatchObject({ name: 'Ovăz scanat', calories: 389, proteins: 16.9, carbohydrates: 66.3, fats: 6.9, sugars: null, category: 'other' })
  })

  it('editing an unsure value counts as confirming it', async () => {
    // Arrange
    renderPage('/', <FoodForm prefill={{ proteins: field(1.69, 0.3) }} onCreated={() => {}} />)

    // Act
    await userEvent.clear(screen.getByLabelText('Proteine (g)'))
    await userEvent.type(screen.getByLabelText('Proteine (g)'), '16,9')

    // Assert
    expect(screen.queryByText('Am verificat valoarea')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Salvează alimentul' })).toBeEnabled()
  })

  it('accepts a decimal comma and shows server errors next to the field', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'POST /api/foods': () => json({ title: 'Date invalide', status: 400, errors: { Name: ["'Name' must not be empty."] } }, 400),
    })
    renderPage('/', <FoodForm onCreated={() => {}} />)

    // Act
    await userEvent.type(screen.getByLabelText('Grăsimi (g)'), '6,9')
    await userEvent.click(screen.getByRole('button', { name: 'Salvează alimentul' }))

    // Assert
    expect(await screen.findByText("'Name' must not be empty.")).toBeInTheDocument()
    expect(await (fetchMock.mock.calls[0][0] as Request).json()).toMatchObject({ fats: 6.9 })
  })
})
