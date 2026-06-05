const KIND_ICON = {
  info: 'ℹ️', success: '✅', error: '⚠️', room: '🛏️',
  order: '🍽️', maint: '🔧', checkin: '🔑'
};

export default function LiveFeed({ feed }) {
  return (
    <div className="panel feed-panel">
      <div className="panel-head">
        <h3>Live Activity</h3>
        <span className="pulse-dot" title="Real-time via WebSocket"></span>
      </div>
      <div className="feed-list">
        {feed.length === 0 && <p className="muted">Waiting for events…</p>}
        {feed.map((f) => (
          <div className={`feed-item feed-${f.kind}`} key={f.id}>
            <span className="feed-icon">{KIND_ICON[f.kind] || 'ℹ️'}</span>
            <div>
              <div className="feed-text">{f.text}</div>
              <div className="feed-time">{f.time.toLocaleTimeString()}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}