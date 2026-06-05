import { useState } from 'react';

export default function LoginGate({ onLogin }) {
  const [token, setToken] = useState('');
  const [error, setError] = useState(false);

  const submit = (e) => {
    e.preventDefault();
    const ok = onLogin(token);
    if (!ok) setError(true);
  };

  return (
    <div className="login-wrap">
      <form className="login-card" onSubmit={submit}>
        <div className="login-logo">🏨 HotelOS</div>
        <h2>Operations Dashboard</h2>
        <p className="muted">Staff access only. Enter your access token to continue.</p>
        {error && <div className="alert alert-error">Invalid access token.</div>}
        <input
          type="password"
          placeholder="Access token"
          value={token}
          onChange={(e) => { setToken(e.target.value); setError(false); }}
        />
        <button className="btn btn-primary btn-block" type="submit">Sign in</button>
        <p className="muted hint">Demo token: <code>hotelos-admin-2024</code></p>
      </form>
    </div>
  );
}