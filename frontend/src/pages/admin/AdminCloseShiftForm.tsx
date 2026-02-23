import { type FormEvent, useState } from 'react'

export function AdminCloseShiftForm(props: {
  shiftId: string
  busy: boolean
  onClose: (reason: string) => Promise<void> | void
}) {
  const [reason, setReason] = useState('')

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = reason.trim()
    if (!trimmed) return
    await props.onClose(trimmed)
    setReason('')
  }

  return (
    <form onSubmit={onSubmit} style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
      <input
        placeholder="Reason (required)"
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        disabled={props.busy}
        style={{ maxWidth: 340 }}
      />
      <button disabled={props.busy || !reason.trim()} type="submit">
        {props.busy ? 'Closing...' : 'Close shift'}
      </button>
    </form>
  )
}

