import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'

type StatusDto = {
  openShift: null | {
    shiftId: string
    openedAtUtc: string
    openedAtZurich: string
  }
}

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

function newIdempotencyKey() {
  return crypto.randomUUID()
}

export function DashboardPage() {
  const qc = useQueryClient()

  const statusQuery = useQuery({
    queryKey: ['me', 'status'],
    queryFn: async () => (await api.get<StatusDto>('/me/status')).data,
  })

  const clockIn = useMutation({
    mutationFn: async () =>
      (await api.post<ShiftDto>('/punches/clock-in', null, { headers: { 'Idempotency-Key': newIdempotencyKey() } }))
        .data,
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['me', 'status'] })
      await qc.invalidateQueries({ queryKey: ['me', 'shifts'] })
    },
  })

  const clockOut = useMutation({
    mutationFn: async () =>
      (await api.post<ShiftDto>('/punches/clock-out', null, { headers: { 'Idempotency-Key': newIdempotencyKey() } }))
        .data,
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['me', 'status'] })
      await qc.invalidateQueries({ queryKey: ['me', 'shifts'] })
    },
  })

  const open = statusQuery.data?.openShift ?? null

  return (
    <div style={{ maxWidth: 720, margin: '20px auto' }}>
      <h1>Dashboard</h1>

      {statusQuery.isLoading ? <p>Loading status...</p> : null}
      {statusQuery.isError ? <p style={{ color: 'crimson' }}>Failed to load status.</p> : null}

      <div style={{ padding: 12, border: '1px solid #ddd', borderRadius: 8 }}>
        <p>
          Status: <b>{open ? 'Clocked in' : 'Clocked out'}</b>
        </p>
        {open ? (
          <p>
            Open since: <b>{new Date(open.openedAtZurich).toLocaleString()}</b>
          </p>
        ) : null}

        <div style={{ display: 'flex', gap: 8 }}>
          <button disabled={!statusQuery.data || !!open || clockIn.isPending} onClick={() => clockIn.mutate()}>
            {clockIn.isPending ? 'Clocking in...' : 'Clock In'}
          </button>
          <button disabled={!open || clockOut.isPending} onClick={() => clockOut.mutate()}>
            {clockOut.isPending ? 'Clocking out...' : 'Clock Out'}
          </button>
        </div>

        {clockIn.isError ? <p style={{ color: 'crimson' }}>Clock in failed.</p> : null}
        {clockOut.isError ? <p style={{ color: 'crimson' }}>Clock out failed.</p> : null}
      </div>
    </div>
  )
}

