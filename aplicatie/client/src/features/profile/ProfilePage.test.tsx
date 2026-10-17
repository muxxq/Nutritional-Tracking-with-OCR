import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useSession } from '@/auth/session'
import { fakeApi, json, renderPage } from '@/test/render'
import { ProfilePage } from './ProfilePage'
import type { ProfileDto } from './profileApi'

const target = (value: number, source: 'default' | 'calculated' | 'manual' = 'default') => ({ value, source })

const emptyProfile: ProfileDto = {
  email: 'ana@exemplu.ro', firstName: 'Ana', lastName: 'Pop', birthDate: null, age: null,
  heightCm: 0, weightKg: 0, gender: 'female', activityLevel: 'sedentary', goal: 'maintainWeight',
  isProfileComplete: false, bmr: null, tdee: null,
  targets: { calories: target(2000), proteinsGrams: target(100), carbsGrams: target(250), fatsGrams: target(67) },
}

// The R1 profile from ADR-003: 1700 kcal, 124 / 195 / 47 g.
const r1: ProfileDto = {
  ...emptyProfile, birthDate: '1998-05-10', age: 28, heightCm: 168, weightKg: 62,
  activityLevel: 'moderatelyActive', goal: 'loseWeight', isProfileComplete: true, bmr: 1364, tdee: 2114,
  targets: {
    calories: target(1700, 'calculated'), proteinsGrams: target(124, 'calculated'),
    carbsGrams: target(195, 'calculated'), fatsGrams: target(47, 'calculated'),
  },
}

describe('ProfilePage', () => {
  beforeEach(() => {
    useSession.getState().signIn({
      accessToken: 't', accessTokenExpiresAtUtc: '2026-10-17T12:00:00Z',
      user: { id: '1', email: 'ana@exemplu.ro', firstName: 'Ana', lastName: 'Pop', isProfileComplete: false },
    })
  })

  afterEach(() => vi.unstubAllGlobals())

  it('onboarding: sends the profile, marks the session complete and opens the diary', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'GET /api/profile': () => json(emptyProfile),
      'PUT /api/profile': () => json(r1),
    })
    renderPage('/onboarding', <ProfilePage onboarding />, { '/diary': 'jurnalul' })

    // Act
    await userEvent.type(await screen.findByLabelText('Data nașterii'), '1998-05-10')
    await userEvent.type(screen.getByLabelText('Înălțime (cm)'), '168')
    await userEvent.type(screen.getByLabelText('Greutate (kg)'), '62')
    await userEvent.selectOptions(screen.getByLabelText('Nivel de activitate'), 'moderatelyActive')
    await userEvent.selectOptions(screen.getByLabelText('Obiectiv'), 'loseWeight')
    await userEvent.click(screen.getByRole('button', { name: 'Calculează țintele' }))

    // Assert
    expect(await screen.findByText('jurnalul')).toBeInTheDocument()
    const put = fetchMock.mock.calls.map((c) => c[0] as Request).find((r) => r.method === 'PUT')!
    expect(await put.json()).toEqual({
      firstName: 'Ana', lastName: 'Pop', birthDate: '1998-05-10', heightCm: 168, weightKg: 62,
      gender: 'female', activityLevel: 'moderatelyActive', goal: 'loseWeight',
    })
    expect(useSession.getState().user?.isProfileComplete).toBe(true)
  })

  it("shows the server's message next to the invalid field", async () => {
    // Arrange
    fakeApi({
      'GET /api/profile': () => json(emptyProfile),
      'PUT /api/profile': () =>
        json({ title: 'Date invalide', status: 400, errors: { HeightCm: ['Înălțimea trebuie să fie între 100 și 250 cm.'] } }, 400),
    })
    renderPage('/onboarding', <ProfilePage onboarding />)

    // Act
    await userEvent.type(await screen.findByLabelText('Înălțime (cm)'), '20')
    await userEvent.click(screen.getByRole('button', { name: 'Calculează țintele' }))

    // Assert
    expect(await screen.findByText('Înălțimea trebuie să fie între 100 și 250 cm.')).toBeInTheDocument()
    expect(screen.getByLabelText('Înălțime (cm)')).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows calculated targets and sends empty overrides as null (back to calculated)', async () => {
    // Arrange
    const fetchMock = fakeApi({
      'GET /api/profile': () => json(r1),
      'PUT /api/profile/targets': () => json({ ...r1, targets: { ...r1.targets, proteinsGrams: target(140, 'manual') } }),
    })
    renderPage('/profile', <ProfilePage />)

    const caloriesCard = (await screen.findByText('Calorii')).closest('div')!
    expect(within(caloriesCard).getByText('1700')).toBeInTheDocument()
    expect(within(caloriesCard).getByText('calculat')).toBeInTheDocument()
    expect(screen.getByText(/1364 kcal/)).toBeInTheDocument()

    // Act
    await userEvent.type(screen.getByLabelText('Proteine (g)'), '140')
    await userEvent.click(screen.getByRole('button', { name: 'Salvează țintele' }))

    // Assert
    const put = fetchMock.mock.calls.map((c) => c[0] as Request).find((r) => r.method === 'PUT')!
    expect(await put.json()).toEqual({ calories: null, proteinsGrams: 140, carbsGrams: null, fatsGrams: null })
    expect(await screen.findByText('manual')).toBeInTheDocument()
  })
})
