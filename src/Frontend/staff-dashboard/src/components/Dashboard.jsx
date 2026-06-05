import { useEffect, useRef, useState, useCallback } from 'react';
import { dashboardApi } from '../api/dashboardApi.js';
import { createDashboardConnection } from '../api/signalr.js';
import OverviewCards from './OverviewCards.jsx';
import RoomGrid from './RoomGrid.jsx';
import OrdersPanel from './OrdersPanel.jsx';
import MaintenancePanel from './MaintenancePanel.jsx';
import LiveFeed from './LiveFeed.jsx';

export default function Dashboard({ onLogout }) {
  const [overview, setOverview] = useState(null);
  const [rooms, setRooms] = useState([]);
  const [orders, setOrders] = useState([]);
  const [maintenance, setMaintenance] = useState([]);
  const [feed, setFeed] = useState([]);
  const [connection, setConnection] = useState('connecting');
  const connRef = useRef(null);

  const pushFeed = useCallback((text, kind = 'info') => {
    setFeed((prev) => [{ id: Date.now() + Math.random(), text, kind, time: new Date() }, ...prev].slice(0, 30));
  }, []);

  // Initial + periodic REST load (fallback / first paint)
  const loadAll = useCallback(async () => {
    try {
      const [ov, rs, ord, mnt] = await Promise.all([
        dashboardApi.overview(),
        dashboardApi.roomStatus(),
        dashboardApi.activeOrders(),
        dashboardApi.activeMaintenance()
      ]);
      setOverview(ov.data);
      setRooms(rs.data?.rooms || []);
      setOrders(ord.data || []);
      setMaintenance(mnt.data || []);
    } catch {
      pushFeed('Could not reach the Dashboard API (port 5005).', 'error');
    }
  }, [pushFeed]);

  useEffect(() => {
    loadAll();

    // Real-time SignalR connection
    const conn = createDashboardConnection({
      onConnectionChange: (status) => {
        setConnection(status);
        if (status === 'connected') pushFeed('Connected to live dashboard feed.', 'success');
        if (status === 'disconnected') pushFeed('Live feed disconnected.', 'error');
      },
      onInitialData: (data) => {
        if (data?.overview) setOverview(data.overview);
        if (data?.roomStatus?.rooms) setRooms(data.roomStatus.rooms);
      },
      onRoomStatus: (u) => {
        pushFeed(u.message || 'Room status changed', 'room');
        loadAll();
      },
      onOrder: (u) => {
        pushFeed(u.message || 'Order updated', 'order');
        loadAll();
      },
      onMaintenance: (u) => {
        pushFeed(u.message || 'Maintenance updated', 'maint');
        loadAll();
      },
      onCheckInOut: (u) => {
        pushFeed(u.message || 'Check-in/out', 'checkin');
        loadAll();
      },
      onAlert: (u) => pushFeed(`ALERT: ${u.message}`, 'error')
    });
    connRef.current = conn;

    // Safety-net polling every 20s in case the socket drops
    const poll = setInterval(loadAll, 20000);

    return () => {
      clearInterval(poll);
      conn?.stop();
    };
  }, [loadAll, pushFeed]);

  const statusColor = {
    connected: '#2e7d52',
    reconnecting: '#c8a45c',
    connecting: '#c8a45c',
    disconnected: '#b3403a'
  }[connection];

  return (
    <div className="dash">
      <header className="dash-header">
        <div className="dash-brand">🏨 HotelOS <span>Operations Dashboard</span></div>
        <div className="dash-header-right">
          <span className="conn-pill" style={{ background: statusColor }}>
            ● {connection}
          </span>
          <button className="btn-logout" onClick={onLogout}>Sign out</button>
        </div>
      </header>

      <main className="dash-main">
        <OverviewCards overview={overview} />

        <div className="dash-grid">
          <section className="dash-col-wide">
            <RoomGrid rooms={rooms} />
            <div className="two-col">
              <OrdersPanel orders={orders} />
              <MaintenancePanel maintenance={maintenance} />
            </div>
          </section>
          <aside className="dash-col-narrow">
            <LiveFeed feed={feed} />
          </aside>
        </div>
      </main>
    </div>
  );
}