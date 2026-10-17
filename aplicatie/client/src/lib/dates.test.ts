import { afterEach, describe, expect, it, vi } from 'vitest'
import { addDays, addMonths, isIsoDate, lastOfMonth, mondayOf, today } from './dates'

describe('dates', () => {
  afterEach(() => vi.useRealTimers())

  it('today is the local date, not the UTC one', () => {
    // Arrange: 00:30 on 18 Oct in Bucharest is still 17 Oct in UTC
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 9, 18, 0, 30))

    // Act & Assert
    expect(today()).toBe('2026-10-18')
  })

  it('adds days across month ends', () => {
    expect(addDays('2026-10-31', 1)).toBe('2026-11-01')
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28')
  })

  it('accepts only yyyy-MM-dd', () => {
    expect(isIsoDate('2026-10-17')).toBe(true)
    expect(isIsoDate('17.10.2026')).toBe(false)
    expect(isIsoDate(null)).toBe(false)
  })

  it('finds the Monday of a week, also from a Sunday', () => {
    expect(mondayOf('2026-10-17')).toBe('2026-10-12') // Saturday
    expect(mondayOf('2026-10-18')).toBe('2026-10-12') // Sunday
    expect(mondayOf('2026-10-19')).toBe('2026-10-19') // Monday
  })

  it('knows month lengths, February included', () => {
    expect(lastOfMonth('2026-02-10')).toBe('2026-02-28')
    expect(lastOfMonth('2028-02-10')).toBe('2028-02-29')
    expect(addMonths('2026-12-15', 1)).toBe('2027-01-01')
  })
})
