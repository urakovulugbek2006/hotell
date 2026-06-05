const ORDER_STATUS = {
  1: 'Received', 2: 'Preparing', 3: 'Out for delivery', 4: 'Delivered', 5: 'Cancelled'
};

export default function OrdersPanel({ orders }) {
  return (
    <div className="panel">
      <div className="panel-head">
        <h3>Active Room Service Orders</h3>
        <span className="muted">{orders.length}</span>
      </div>
      {orders.length === 0 ? <p className="muted">No active orders.</p> : (
        <table className="data-table">
          <thead>
            <tr><th>Room</th><th>Guest</th><th>Status</th><th>Total</th></tr>
          </thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id}>
                <td>{o.roomNumber}</td>
                <td>{o.guestName}</td>
                <td><span className={`badge badge-${o.statusColor || 'secondary'}`}>{ORDER_STATUS[o.status] || o.status}</span></td>
                <td>${(o.totalAmount ?? 0).toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}