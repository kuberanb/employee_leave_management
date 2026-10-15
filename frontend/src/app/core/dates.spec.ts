import { calendarDays, todayInIndia } from './dates';
describe('Leave dates', () => {
  it('counts both endpoints', () => expect(calendarDays('2026-11-02', '2026-11-04')).toBe(3));
  it('allows a single day', () => expect(calendarDays('2026-11-02', '2026-11-02')).toBe(1));
  it('handles month boundaries', () => expect(calendarDays('2026-10-31', '2026-11-02')).toBe(3));
  it('rejects reversed dates', () => expect(calendarDays('2026-11-04', '2026-11-02')).toBe(0));
  it('handles empty inputs', () => expect(calendarDays('', '')).toBe(0));
  it('formats the business date for date inputs', () => expect(todayInIndia()).toMatch(/^\d{4}-\d{2}-\d{2}$/));
});
