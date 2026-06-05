import { useState } from 'react';
import { maintenanceApi } from '../api/client.js';

const PRIORITIES = [
  { value: 1, label: 'Low' },
  { value: 2, label: 'Normal' },
  { value: 3, label: 'High' },
  { value: 4, label: 'Critical' }
];

export default function MaintenancePage() {
  const [form, setForm] = useState({ roomId: '', description: '', priority: 2, reportedBy: '' });
  const [message, setMessage] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e) => {
    e.preventDefault();
    if (!form.roomId || form.description.trim().length < 10) {
      setMessage({ type: 'error', text: 'Please enter a room number and a description of at least 10 characters.' });
      return;
    }
    setSubmitting(true);
    setMessage(null);
    try {
      const res = await maintenanceApi.create({
        roomId: Number(form.roomId),
        description: form.description,
        priority: Number(form.priority),
        reportedBy: form.reportedBy || 'Guest'
      });
      if (res.data?.isSuccess) {
        setMessage({ type: 'success', text: 'Your issue has been reported. Our maintenance team will respond by priority.' });
        setForm({ roomId: '', description: '', priority: 2, reportedBy: '' });
      } else {
        setMessage({ type: 'error', text: res.data?.errorMessage || 'Could not submit the report.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: 'Report request failed. Is the API running?' });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div>
      <h2 className="page-title">Report a Maintenance Issue</h2>
      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      <form className="panel" onSubmit={submit}>
        <div className="form-row">
          <div className="form-group">
            <label>Room number (room ID)</label>
            <input value={form.roomId} onChange={(e) => setForm({ ...form, roomId: e.target.value })} placeholder="e.g. 8" />
          </div>
          <div className="form-group">
            <label>Priority</label>
            <select value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}>
              {PRIORITIES.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
            </select>
          </div>
          <div className="form-group">
            <label>Your name (optional)</label>
            <input value={form.reportedBy} onChange={(e) => setForm({ ...form, reportedBy: e.target.value })} placeholder="Full name" />
          </div>
        </div>
        <div className="form-group" style={{ marginBottom: '1rem' }}>
          <label>Describe the issue</label>
          <textarea rows={4} value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            placeholder="e.g. The shower in the bathroom is leaking and won't turn off." />
        </div>
        <button className="btn btn-primary" disabled={submitting}>
          {submitting ? 'Submitting…' : 'Submit report'}
        </button>
      </form>
    </div>
  );
}