import { api } from '../../lib/api'
import type { AdminOpenShiftDto, ShiftDto } from './types'

export async function fetchOpenShifts() {
  const res = await api.get<AdminOpenShiftDto[]>('/admin/open-shifts')
  return res.data
}

export async function closeShift(shiftId: string, reason: string) {
  const res = await api.post<ShiftDto>(`/admin/shifts/${shiftId}/close`, { reason })
  return res.data
}

export async function downloadShiftsCsv(params?: { fromUtc?: string; toUtc?: string }) {
  // Must be downloaded via authenticated request (bearer token in header).
  const res = await api.get('/admin/reports/shifts.csv', {
    responseType: 'blob',
    params,
  })

  const blob = res.data as Blob
  const url = URL.createObjectURL(blob)
  try {
    const a = document.createElement('a')
    a.href = url
    a.download = 'shifts.csv'
    document.body.appendChild(a)
    a.click()
    a.remove()
  } finally {
    URL.revokeObjectURL(url)
  }
}

