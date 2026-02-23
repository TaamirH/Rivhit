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

export function HistoryPage() {
  const shiftsQuery = useQuery({
    queryKey: ['me', 'shifts'],
    queryFn: async () => (await api.get<ShiftDto[]>('/me/shifts')).data,
  })

  return (
    <div style={{ maxWidth: 900, margin: '20px auto' }}>
      <h1>History</h1>
      {shiftsQuery.isLoading ? <p>Loading shifts...</p> : null}
      {shiftsQuery.isError ? <p style={{ color: 'crimson' }}>Failed to load shifts.</p> : null}

      {shiftsQuery.data?.length ? (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Opened (Zurich)</th>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Closed (Zurich)</th>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Duration</th>
              <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #ddd' }}>Punches</th>
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
                <td style={{ padding: 8, borderBottom: '1px solid #eee' }}>{s.punches.length}</td>
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

