import { useEffect, useState } from 'react';
import { orderApi } from '../api/client.js';

export default function RoomServicePage() {
  const [menu, setMenu] = useState([]);
  const [cart, setCart] = useState({});
  const [roomNumber, setRoomNumber] = useState('');
  const [guestName, setGuestName] = useState('');
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    orderApi.menu()
      .then((res) => setMenu(res.data?.data?.items || []))
      .catch(() => setMessage({ type: 'error', text: 'Could not load the menu. Is the API running?' }))
      .finally(() => setLoading(false));
  }, []);

  const addToCart = (item) => {
    setCart((prev) => ({ ...prev, [item.id]: { item, qty: (prev[item.id]?.qty || 0) + 1 } }));
  };
  const removeFromCart = (id) => {
    setCart((prev) => {
      const next = { ...prev };
      if (next[id] && next[id].qty > 1) next[id].qty -= 1;
      else delete next[id];
      return { ...next };
    });
  };

  const cartLines = Object.values(cart);
  const total = cartLines.reduce((sum, l) => sum + l.item.price * l.qty, 0);

  const placeOrder = async () => {
    if (!roomNumber || !guestName || cartLines.length === 0) {
      setMessage({ type: 'error', text: 'Enter your room number, name and add at least one item.' });
      return;
    }
    try {
      const res = await orderApi.create({
        roomId: Number(roomNumber),
        guestName,
        items: cartLines.map((l) => ({
          itemName: l.item.name,
          quantity: l.qty,
          unitPrice: l.item.price
        }))
      });
      if (res.data?.isSuccess) {
        setMessage({ type: 'success', text: 'Order placed! You can track its progress on the dashboard.' });
        setCart({});
      } else {
        setMessage({ type: 'error', text: res.data?.errorMessage || 'Order failed.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: 'Order request failed.' });
    }
  };

  return (
    <div>
      <h2 className="page-title">Room Service</h2>
      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      <div className="form-row">
        <div className="form-group">
          <label>Your room number (room ID)</label>
          <input value={roomNumber} onChange={(e) => setRoomNumber(e.target.value)} placeholder="e.g. 1" />
        </div>
        <div className="form-group">
          <label>Your name</label>
          <input value={guestName} onChange={(e) => setGuestName(e.target.value)} placeholder="Full name" />
        </div>
      </div>

      <div className="panel">
        <h3>Menu</h3>
        {loading ? <p className="muted">Loading menu…</p> : menu.map((item) => (
          <div className="menu-item" key={item.id}>
            <div>
              <strong>{item.name}</strong>
              <div className="muted" style={{ fontSize: '0.85rem' }}>{item.description}</div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div className="price">${item.price}</div>
              <button className="btn btn-primary" style={{ padding: '0.3rem 0.8rem', marginTop: '0.3rem' }}
                onClick={() => addToCart(item)}>Add</button>
            </div>
          </div>
        ))}
      </div>

      <div className="panel">
        <h3>Your Order</h3>
        {cartLines.length === 0 ? <p className="muted">No items yet.</p> : (
          <>
            {cartLines.map((l) => (
              <div className="cart-line" key={l.item.id}>
                <span>{l.item.name} × {l.qty}</span>
                <span>
                  ${(l.item.price * l.qty).toFixed(2)}
                  <button className="btn" style={{ padding: '0 0.5rem', marginLeft: '0.5rem' }}
                    onClick={() => removeFromCart(l.item.id)}>−</button>
                </span>
              </div>
            ))}
            <hr style={{ margin: '0.75rem 0', border: 'none', borderTop: '1px solid #eef2f7' }} />
            <div className="cart-line"><strong>Total</strong><strong>${total.toFixed(2)}</strong></div>
            <button className="btn btn-primary btn-block" style={{ marginTop: '1rem' }} onClick={placeOrder}>
              Place order
            </button>
          </>
        )}
      </div>
    </div>
  );
}