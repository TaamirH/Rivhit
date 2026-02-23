import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchOpenShifts, closeShift, downloadShiftsCsv } from './admin/adminApi'
import { AdminCloseShiftForm } from './admin/AdminCloseShiftForm'
import { useAuth } from '../auth/AuthContext'

export function AdminPage() {
  const { auth } = useAuth()
  const isAdmin = auth?.roles?.includes('Admin') ?? false
  const qc = useQueryClient()

  const openShiftsQuery = useQuery({
    queryKey: ['admin', 'open-shifts'],
    queryFn: fetchOpenShifts,
    enabled: isAdmin,
  })

  const closeShiftMutation = useMutation({
    mutationFn: async (vars: { shiftId: string; reason: string }) => closeShift(vars.shiftId, vars.reason),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['admin', 'open-shifts'] })
      await qc.invalidateQueries({ queryKey: ['me', 'status'] })
      await qc.invalidateQueries({ queryKey: ['me', 'shifts'] })
    },
  })

  const downloadCsvMutation = useMutation({
    mutationFn: async () => downloadShiftsCsv(),
  })

  return (
    <div className="container" style={{ maxWidth: 720 }}>
      <h1>Admin</h1>
      <div className="card">
        {!isAdmin ? (
          <p>You are not an admin.</p>
        ) : (
          <>
            <p className="muted" style={{ marginTop: 0 }}>
              Tools: view open shifts, close a shift with a reason, export CSV.
            </p>

            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
              <button onClick={() => downloadCsvMutation.mutate()} disabled={downloadCsvMutation.isPending}>
                {downloadCsvMutation.isPending ? 'Downloading...' : 'Download shifts CSV'}
              </button>
              <button onClick={() => openShiftsQuery.refetch()} disabled={openShiftsQuery.isFetching}>
                {openShiftsQuery.isFetching ? 'Refreshing...' : 'Refresh'}
              </button>
            </div>
            {downloadCsvMutation.isError ? (
              <p style={{ color: 'crimson' }}>CSV download failed. Make sure you are logged in as an admin.</p>
            ) : null}

            {openShiftsQuery.isLoading ? <p>Loading open shifts...</p> : null}
            {openShiftsQuery.isError ? (
              <p style={{ color: 'crimson' }}>Failed to load open shifts.</p>
            ) : null}

            {openShiftsQuery.data?.length ? (
              <table className="table">
                <thead>
                  <tr>
                    <th>Employee</th>
                    <th>Opened</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {openShiftsQuery.data.map((s) => (
                    <tr key={s.shiftId}>
                      <td>{s.email}</td>
                      <td>{new Date(s.openedAtZurich).toLocaleString()}</td>
                      <td>
                        <AdminCloseShiftForm
                          shiftId={s.shiftId}
                          busy={closeShiftMutation.isPending}
                          onClose={async (reason) => {
                            await closeShiftMutation.mutateAsync({ shiftId: s.shiftId, reason })
                          }}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : openShiftsQuery.isLoading ? null : (
              <p>No open shifts.</p>
            )}

            {closeShiftMutation.isError ? (
              <p style={{ color: 'crimson' }}>Failed to close shift. Check the reason and try again.</p>
            ) : null}
          </>
        )}
      </div>
    </div>
  )
}

