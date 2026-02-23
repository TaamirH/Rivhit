export type AdminOpenShiftDto = {
  shiftId: string
  userId: string
  email: string
  openedAtUtc: string
  openedAtZurich: string
}

export type PunchDto = {
  id: string
  type: number
  occurredAtUtc: string
  occurredAtZurich: string
  unixTimeSeconds: number
  timezone: string
  utcOffset: string
}

export type ShiftDto = {
  id: string
  openedAtUtc: string
  openedAtZurich: string
  closedAtUtc: string | null
  closedAtZurich: string | null
  punches: PunchDto[]
}

