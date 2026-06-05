import { useState } from 'react';
import { roomApi, bookingApi } from '../api/client.js';

const ROOM_TYPES = [
  { value: 1, label: 'Single' },
  { value: 2, label: 'Double' },
  { value: 3, label: 'Suite' },
  { value: 4, label: 'Accessible' }
];

function today() {
  return new Date().toISOString().split('T')[0];
}
function tomorrow() {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().split('T')[0];
}

export default function RoomsPage() {
  const [query, setQuery] = useState({
    checkInDate: today(),
    checkOutDate: tomorrow(),
    roomType: '',
    floor: ''
  });
  const [rooms, setRooms] = useState([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState(null);

  const search = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    try {
      const payload = {
        checkInDate: query.checkInDate,
        checkOutDate: query.checkOutDate,
        roomType: query.roomType ? Number(query.roomType) : null,
        floor: query.floor ? Number(query.floor) : null
      };
      const res = await roomApi.availability(payload);
      setRooms(res.data?.data?.items || []);
      if ((res.data?.data?.items || []).length === 0) {
        setMessage({ type: 'error', text: 'No rooms available for the selected criteria.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: 'Could not load rooms. Is the API running on port 5006?' });
    } finally {
      setLoading(false);
    }
  };

  const book = async (room) => {
    const guestId = localStorage.getItem('guest_id');
    if (!guestId) {
      setMessage({ type: 'error', text: 'Please sign in before booking.' });
      return;
    }
    try {
      const res = await bookingApi.create({
        guestId: Number(guestId),
        requestedRoomType: room.type,
        checkInDate: query.checkInDate,
        checkOutDate: query.checkOutDate,
        floorPreference: room.floor
      });
      if (res.data?.isSuccess) {
        setMessage({ type: 'success', text: `Booking created for room ${room.roomNumber}! Reference #${res.data.data.id}.` });
      } else {
        setMessage({ type: 'error', text: res.data?.errorMessage || 'Booking failed.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: 'Booking request failed.' });
    }
  };

  return (
    <div>
      <h2 className="page-title">Find &amp; Book a Room</h2>

      <form className="panel" onSubmit={search}>
        <div className="form-row">
          <div className="form-group">
            <label>Check-in</label>
            <input type="date" value={query.checkInDate}
              onChange={(e) => setQuery({ ...query, checkInDate: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Check-out</label>
            <input type="date" value={query.checkOutDate}
              onChange={(e) => setQuery({ ...query, checkOutDate: e.target.value })} />
          </div>
          <div className="form-group">
            <label>Room type</label>
            <select value={query.roomType}
              onChange={(e) => setQuery({ ...query, roomType: e.target.value })}>
              <option value="">Any</option>
              {ROOM_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label>Floor</label>
            <select value={query.floor}
              onChange={(e) => setQuery({ ...query, floor: e.target.value })}>
              <option value="">Any</option>
              <option value="1">Floor 1</option>
              <option value="2">Floor 2</option>
            </select>
          </div>
        </div>
        <button className="btn btn-primary" disabled={loading}>
          {loading ? 'Searching…' : 'Search rooms'}
        </button>
      </form>

      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      <div className="room-grid">
        {rooms.map((room) => (
          <div className="room-card" key={room.id}>
            <h4>Room {room.roomNumber}</h4>
            <p className="muted">{room.roomTypeDescription}</p>
            <div className="room-rate">${room.nightlyRate}<span className="muted" style={{ fontSize: '0.8rem' }}>/night</span></div>
            <div style={{ margin: '0.5rem 0' }}>
              <span className="tag">Floor {room.floor}</span>
              {room.isAccessible && <span className="tag">Accessible</span>}
              {room.nearElevator && <span className="tag">Near elevator</span>}
              {room.nearStairs && <span className="tag">Near stairs</span>}
            </div>
            <button className="btn btn-primary btn-block" onClick={() => book(room)}>Book this room</button>
          </div>
        ))}
      </div>
    </div>
  );
}