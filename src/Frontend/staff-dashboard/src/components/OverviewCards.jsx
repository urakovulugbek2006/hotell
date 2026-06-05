export default function OverviewCards({ overview }) {
  if (!overview) {
    return <div className="overview-cards"><div className="ov-card muted">Loading overview…</div></div>;
  }

  const cards = [
    { label: 'Occupancy', value: `${Math.round(overview.occupancyRate)}%`, icon: '📊', accent: '#1f4e79' },
    { label: 'Occupied Rooms', value: `${overview.occupiedRooms}/${overview.totalRooms}`, icon: '🛏️', accent: '#2e7d52' },
    { label: "Today's Revenue", value: `$${(overview.todaysRevenue ?? 0).toFixed(0)}`, icon: '💰', accent: '#c8a45c' },
    { label: 'Active Guests', value: overview.activeGuests, icon: '👤', accent: '#5a4fcf' },
    { label: 'Pending Orders', value: overview.pendingOrders, icon: '🍽️', accent: '#d98324' },
    { label: 'Open Maintenance', value: overview.openMaintenanceRequests, icon: '🔧', accent: '#b3403a' }
  ];

  return (
    <div className="overview-cards">
      {cards.map((c) => (
        <div className="ov-card" key={c.label} style={{ borderLeftColor: c.accent }}>
          <div className="ov-icon">{c.icon}</div>
          <div>
            <div className="ov-value">{c.value}</div>
            <div className="ov-label">{c.label}</div>
          </div>
        </div>
      ))}
    </div>
  );
}