import { useQuery } from '@tanstack/react-query'
import { api } from '../lib/api'

type ShiftDto = {
  id: string
  openedAtUtc: string
  openedAtZurich: string
  closedAtUtc: string | null
  closedAtZurich: string | null
  punches: Array<{
    id: string
    type: number
    occurredAtUtc: string
    occurredAtZurich: string
    timezone: string
    utcOffset: string
  }>
}

type MonthStats = {
  daysWorked: number
  hoursWorked: number
  hoursPerDayAvg: number
}

function formatDuration(openedAtUtc: string, closedAtUtc: string | null) {
  if (!closedAtUtc) return '—'
  const start = new Date(openedAtUtc).getTime()
  const end = new Date(closedAtUtc).getTime()
  const sec = Math.max(0, Math.floor((end - start) / 1000))
  const h = Math.floor(sec / 3600)
  const m = Math.floor((sec % 3600) / 60)
  const s = sec % 60
  return `${h}h ${m}m ${s}s`
}

function formatHours(hours: number) {
  if (!Number.isFinite(hours)) return '—'
  return hours.toFixed(1)
}

function getZurichParts(d: Date) {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Zurich',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(d)

  const get = (type: string) => parts.find((p) => p.type === type)?.value ?? ''
  return { year: get('year'), month: get('month'), day: get('day') }
}

function computeThisMonthStats(shifts: ShiftDto[]): MonthStats {
  const nowParts = getZurichParts(new Date())
  const currentMonthKey = `${nowParts.year}-${nowParts.month}`

  const dayKeys = new Set<string>()
  let totalSeconds = 0

  for (const s of shifts) {
    const openedInstant = new Date(s.openedAtUtc)
    const openedParts = getZurichParts(openedInstant)
    const openedMonthKey = `${openedParts.year}-${openedParts.month}`
    if (openedMonthKey !== currentMonthKey) continue

    dayKeys.add(`${openedParts.year}-${openedParts.month}-${openedParts.day}`)

    if (s.closedAtUtc) {
      const startMs = openedInstant.getTime()
      const endMs = new Date(s.closedAtUtc).getTime()
      const sec = Math.max(0, (endMs - startMs) / 1000)
      totalSeconds += sec
    }
  }

  const hoursWorked = totalSeconds / 3600
  const daysWorked = dayKeys.size
  const hoursPerDayAvg = daysWorked ? hoursWorked / daysWorked : 0

  return { daysWorked, hoursWorked, hoursPerDayAvg }
}

export function HistoryPage() {
  const shiftsQuery = useQuery({
    queryKey: ['me', 'shifts'],
    queryFn: async () => (await api.get<ShiftDto[]>('/me/shifts')).data,
  })

  const monthStats = shiftsQuery.data ? computeThisMonthStats(shiftsQuery.data) : null

  return (
    <div style={{ maxWidth: 900, margin: '20px auto' }}>
      <h1>History</h1>
      {shiftsQuery.isLoading ? <p>Loading shifts...</p> : null}
      {shiftsQuery.isError ? <p style={{ color: 'crimson' }}>Failed to load shifts.</p> : null}

      {monthStats ? (
        <div style={{ padding: 12, border: '1px solid #ddd', borderRadius: 8, margin: '12px 0' }}>
          <h2 style={{ margin: '0 0 8px 0', fontSize: '1.2rem' }}>This month</h2>
          <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
            <div>
              <div style={{ opacity: 0.8 }}>Days worked</div>
              <div style={{ fontSize: '1.4rem' }}>{monthStats.daysWorked}</div>
            </div>
            <div>
              <div style={{ opacity: 0.8 }}>Hours worked</div>
              <div style={{ fontSize: '1.4rem' }}>{formatHours(monthStats.hoursWorked)}</div>
            </div>
            <div>
              <div style={{ opacity: 0.8 }}>Avg hours/day</div>
              <div style={{ fontSize: '1.4rem' }}>{formatHours(monthStats.hoursPerDayAvg)}</div>
            </div>
          </div>
        </div>
      ) : null}

      {shiftsQuery.data?.length ? (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Opened</th>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Closed</th>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Duration</th>
            </tr>
          </thead>
          <tbody>
            {shiftsQuery.data.map((s) => (
              <tr key={s.id}>
                <td style={{ padding: 8, borderBottom: '1px solid #eee' }}>
                  {new Date(s.openedAtZurich).toLocaleString()}
                </td>
                <td style={{ padding: 8, borderBottom: '1px solid #eee' }}>
                  {s.closedAtZurich ? new Date(s.closedAtZurich).toLocaleString() : 'OPEN'}
                </td>
                <td style={{ padding: 8, borderBottom: '1px solid #eee' }}>
                  {formatDuration(s.openedAtUtc, s.closedAtUtc)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : shiftsQuery.isLoading ? null : (
        <p>No shifts yet.</p>
      )}
    </div>
  )
}

