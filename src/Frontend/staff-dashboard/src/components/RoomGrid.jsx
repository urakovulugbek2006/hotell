const STATUS_LABEL = {
  1: 'Available', 2: 'Occupied', 3: 'Dirty', 4: 'Cleaning', 5: 'Clean', 6: 'Maintenance', 7: 'Out of Order'
};
const STATUS_CLASS = {
  1: 'st-available', 2: 'st-occupied', 3: 'st-dirty', 4: 'st-cleaning',
  5: 'st-clean', 6: 'st-maint', 7: 'st-ooo'
};

export default function RoomGrid({ rooms }) {
  return (
    <div className="panel">
      <div className="panel-head">
        <h3>Room Status</h3>
        <span className="muted">{rooms.length} rooms</span>
      </div>
      <div className="room-tiles">
        {rooms.length === 0 && <p className="muted">No room data yet.</p>}
        {rooms.map((room) => (
          <div className={`room-tile ${STATUS_CLASS[room.status] || ''}`} key={room.id || room.roomNumber}>
            <div className="rt-number">{room.roomNumber}</div>
            <div className="rt-status">{STATUS_LABEL[room.status] || room.status}</div>
            {room.currentGuestName && <div className="rt-guest">{room.currentGuestName}</div>}
          </div>
        ))}
      </div>
      <div className="legend">
        {Object.entries(STATUS_LABEL).map(([k, label]) => (
          <span className="legend-item" key={k}>
            <span className={`legend-dot ${STATUS_CLASS[k]}`}></span>{label}
          </span>
        ))}
      </div>
    </div>
  );
}