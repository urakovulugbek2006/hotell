const PRIORITY = { 1: 'Low', 2: 'Normal', 3: 'High', 4: 'Critical' };
const MNT_STATUS = { 1: 'Reported', 2: 'Assigned', 3: 'In progress', 4: 'Completed', 5: 'Cancelled' };

export default function MaintenancePanel({ maintenance }) {
  return (
    <div className="panel">
      <div className="panel-head">
        <h3>Open Maintenance Issues</h3>
        <span className="muted">{maintenance.length}</span>
      </div>
      {maintenance.length === 0 ? <p className="muted">No open issues.</p> : (
        <table className="data-table">
          <thead>
            <tr><th>#</th><th>Room</th><th>Issue</th><th>Priority</th><th>Status</th></tr>
          </thead>
          <tbody>
            {maintenance.map((m) => (
              <tr key={m.id}>
                <td>{m.queuePosition || '-'}</td>
                <td>{m.roomNumber}</td>
                <td className="issue-cell">{m.description}</td>
                <td><span className={`badge badge-${m.priorityColor || 'secondary'}`}>{PRIORITY[m.priority] || m.priority}</span></td>
                <td>{MNT_STATUS[m.status] || m.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}